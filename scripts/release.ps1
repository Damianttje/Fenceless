param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string]$Version,

    [string]$Owner = "Wavestorm",
    [string]$Repo = "Fenceless",
    [string]$Remote = "origin",
    [string]$TargetCommitish = "main",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$TestConfiguration = "Debug",
    [string]$Token,
    [string]$ReleaseNotes,
    [string]$ReleaseNotesFile,
    [switch]$Prerelease,
    [switch]$Draft,
    [switch]$SkipVersionBump,
    [switch]$SkipGitTag,
    [switch]$PushTag,
    [switch]$SkipUpload,
    [switch]$KeepArtifacts
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Add-Type -AssemblyName System.Net.Http

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Fail {
    param([string]$Message)
    throw $Message
}

function ConvertFrom-SecureStringPlainText {
    param([securestring]$SecureValue)

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Get-ReleaseToken {
    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        return $Token
    }

    if (-not [string]::IsNullOrWhiteSpace($env:CODEBERG_TOKEN)) {
        return $env:CODEBERG_TOKEN
    }

    $secureToken = Read-Host "Codeberg token" -AsSecureString
    return ConvertFrom-SecureStringPlainText $secureToken
}

function Invoke-CodebergJson {
    param(
        [ValidateSet("GET", "POST", "PATCH", "DELETE")]
        [string]$Method,
        [string]$Uri,
        [object]$Body,
        [string]$AuthToken,
        [switch]$AllowNotFound
    )

    $headers = @{
        Authorization = "token $AuthToken"
        Accept = "application/json"
    }

    try {
        if ($null -ne $Body) {
            $json = $Body | ConvertTo-Json -Depth 8
            return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $headers -ContentType "application/json" -Body $json
        }

        return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $headers
    }
    catch {
        $response = $_.Exception.Response
        if ($AllowNotFound -and $response -and [int]$response.StatusCode -eq 404) {
            return $null
        }

        throw
    }
}

function Invoke-CodebergAssetUpload {
    param(
        [string]$Uri,
        [string]$FilePath,
        [string]$AuthToken
    )

    $client = [System.Net.Http.HttpClient]::new()
    $fileStream = $null
    $multipart = $null

    try {
        $client.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("token", $AuthToken)
        $client.DefaultRequestHeaders.Accept.ParseAdd("application/json")

        $multipart = [System.Net.Http.MultipartFormDataContent]::new()
        $fileStream = [System.IO.File]::OpenRead($FilePath)
        $fileContent = [System.Net.Http.StreamContent]::new($fileStream)
        $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("application/octet-stream")
        $multipart.Add($fileContent, "attachment", [System.IO.Path]::GetFileName($FilePath))

        $response = $client.PostAsync($Uri, $multipart).GetAwaiter().GetResult()
        $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if (-not $response.IsSuccessStatusCode) {
            Fail "Asset upload failed with HTTP $([int]$response.StatusCode): $responseBody"
        }

        return $responseBody | ConvertFrom-Json
    }
    finally {
        if ($multipart) { $multipart.Dispose() }
        if ($fileStream) { $fileStream.Dispose() }
        $client.Dispose()
    }
}

function Update-ProjectVersion {
    param(
        [string]$ProjectPath,
        [string]$NewVersion
    )

    [xml]$project = Get-Content -Path $ProjectPath
    $propertyGroup = $project.Project.PropertyGroup | Select-Object -First 1

    foreach ($nodeName in @("AssemblyVersion", "FileVersion")) {
        $node = $propertyGroup.SelectSingleNode($nodeName)
        if ($null -eq $node) {
            $node = $project.CreateElement($nodeName)
            [void]$propertyGroup.AppendChild($node)
        }
        $node.InnerText = $NewVersion
    }

    $project.Save($ProjectPath)
}

function Test-RequiredPackageFiles {
    param([string]$Directory)

    $requiredFiles = @(
        "Fenceless.exe",
        "Fenceless.dll",
        "Fenceless.runtimeconfig.json",
        "Fenceless.Updater.exe"
    )

    foreach ($fileName in $requiredFiles) {
        $path = Join-Path $Directory $fileName
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            Fail "Package validation failed. Missing required root file: $fileName"
        }
    }
}

function New-ZipPackage {
    param(
        [string]$SourceDirectory,
        [string]$ZipPath
    )

    if (Test-Path -LiteralPath $ZipPath) {
        Remove-Item -LiteralPath $ZipPath -Force
    }

    Compress-Archive -Path (Join-Path $SourceDirectory "*") -DestinationPath $ZipPath -CompressionLevel Optimal
}

function New-Sha256File {
    param(
        [string]$FilePath,
        [string]$ShaPath
    )

    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $FilePath).Hash.ToLowerInvariant()
    $fileName = [System.IO.Path]::GetFileName($FilePath)
    Set-Content -LiteralPath $ShaPath -Value "$hash  $fileName" -NoNewline -Encoding ASCII
}

function Ensure-GitTag {
    param(
        [string]$TagName,
        [switch]$Push
    )

    $existingTag = git tag --list $TagName
    if ([string]::IsNullOrWhiteSpace($existingTag)) {
        Write-Step "Creating git tag $TagName"
        git tag -a $TagName -m "Release $TagName"
    }
    else {
        Write-Step "Git tag $TagName already exists"
    }

    if ($Push) {
        Write-Step "Pushing git tag $TagName to $Remote"
        git push $Remote $TagName
    }
}

function Get-ReleaseNotesText {
    if (-not [string]::IsNullOrWhiteSpace($ReleaseNotesFile)) {
        if (-not (Test-Path -LiteralPath $ReleaseNotesFile -PathType Leaf)) {
            Fail "Release notes file was not found: $ReleaseNotesFile"
        }

        return Get-Content -LiteralPath $ReleaseNotesFile -Raw
    }

    if (-not [string]::IsNullOrWhiteSpace($ReleaseNotes)) {
        return $ReleaseNotes
    }

    return "Fenceless v$Version"
}

function Remove-ExistingAssets {
    param(
        [object]$Release,
        [string[]]$AssetNames,
        [string]$ApiBase,
        [string]$AuthToken
    )

    foreach ($asset in @($Release.assets)) {
        if ($AssetNames -contains $asset.name) {
            Write-Step "Removing existing asset $($asset.name)"
            $deleteUri = "$ApiBase/releases/$($Release.id)/assets/$($asset.id)"
            [void](Invoke-CodebergJson -Method DELETE -Uri $deleteUri -AuthToken $AuthToken)
        }
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$mainProject = Join-Path $repoRoot "Fenceless\Fenceless.csproj"
$updaterProject = Join-Path $repoRoot "Fenceless.Updater\Fenceless.Updater.csproj"
$solution = Join-Path $repoRoot "Fenceless.sln"
$tagName = "v$Version"
$packageName = "Fenceless-$tagName-$Runtime.zip"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishRoot = Join-Path $artifactsRoot "publish\$tagName"
$publishDir = Join-Path $publishRoot "app"
$updaterPublishDir = Join-Path $publishRoot "updater"
$releaseDir = Join-Path $artifactsRoot "release\$tagName"
$zipPath = Join-Path $releaseDir $packageName
$shaPath = "$zipPath.sha256"
$apiBase = "https://codeberg.org/api/v1/repos/$Owner/$Repo"

if (-not (Test-Path -LiteralPath $solution -PathType Leaf)) {
    Fail "Run this script from the Fenceless repo, or keep it in scripts\release.ps1."
}

if (-not $KeepArtifacts) {
    foreach ($path in @($publishRoot, $releaseDir)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $updaterPublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

if (-not $SkipVersionBump) {
    Write-Step "Updating project version to $Version"
    Update-ProjectVersion -ProjectPath $mainProject -NewVersion $Version
}

Write-Step "Restoring solution"
dotnet restore $solution
if ($LASTEXITCODE -ne 0) { Fail "dotnet restore failed." }

Write-Step "Running tests"
dotnet run --project (Join-Path $repoRoot "Fenceless.Tests\Fenceless.Tests.csproj") --configuration $TestConfiguration
if ($LASTEXITCODE -ne 0) { Fail "Tests failed." }

Write-Step "Publishing Fenceless for $Runtime"
dotnet publish $mainProject --configuration $Configuration --runtime $Runtime --self-contained true --output $publishDir -p:AssemblyVersion=$Version -p:FileVersion=$Version -p:Version=$Version
if ($LASTEXITCODE -ne 0) { Fail "Fenceless publish failed." }

Write-Step "Publishing updater helper for $Runtime"
dotnet publish $updaterProject --configuration $Configuration --runtime $Runtime --self-contained true --output $updaterPublishDir -p:AssemblyVersion=$Version -p:FileVersion=$Version -p:Version=$Version
if ($LASTEXITCODE -ne 0) { Fail "Updater publish failed." }

Write-Step "Copying updater helper into app package"
Copy-Item -LiteralPath (Join-Path $updaterPublishDir "*") -Destination $publishDir -Recurse -Force

Write-Step "Validating package root"
Test-RequiredPackageFiles -Directory $publishDir

Write-Step "Creating $packageName"
New-ZipPackage -SourceDirectory $publishDir -ZipPath $zipPath
New-Sha256File -FilePath $zipPath -ShaPath $shaPath

if (-not $SkipGitTag) {
    Ensure-GitTag -TagName $tagName -Push:$PushTag
}

if ($SkipUpload) {
    Write-Step "Skipping Codeberg upload"
    Write-Host "Package: $zipPath"
    Write-Host "SHA256:  $shaPath"
    exit 0
}

$releaseToken = Get-ReleaseToken
if ([string]::IsNullOrWhiteSpace($releaseToken)) {
    Fail "A Codeberg token is required. Set CODEBERG_TOKEN or pass -Token."
}

$releaseBody = Get-ReleaseNotesText
$releaseUri = "$apiBase/releases/tags/$tagName"
Write-Step "Looking up Codeberg release $tagName"
$release = Invoke-CodebergJson -Method GET -Uri $releaseUri -AuthToken $releaseToken -AllowNotFound

$releasePayload = @{
    tag_name = $tagName
    target_commitish = $TargetCommitish
    name = $tagName
    body = $releaseBody
    draft = [bool]$Draft
    prerelease = [bool]$Prerelease
}

if ($null -eq $release) {
    Write-Step "Creating Codeberg release $tagName"
    $release = Invoke-CodebergJson -Method POST -Uri "$apiBase/releases" -Body $releasePayload -AuthToken $releaseToken
}
else {
    Write-Step "Updating Codeberg release $tagName"
    $release = Invoke-CodebergJson -Method PATCH -Uri "$apiBase/releases/$($release.id)" -Body $releasePayload -AuthToken $releaseToken
}

Remove-ExistingAssets -Release $release -AssetNames @($packageName, "$packageName.sha256") -ApiBase $apiBase -AuthToken $releaseToken

foreach ($assetPath in @($zipPath, $shaPath)) {
    $assetName = [System.IO.Path]::GetFileName($assetPath)
    $encodedAssetName = [System.Uri]::EscapeDataString($assetName)
    $uploadUri = "$apiBase/releases/$($release.id)/assets?name=$encodedAssetName"
    Write-Step "Uploading $assetName"
    [void](Invoke-CodebergAssetUpload -Uri $uploadUri -FilePath $assetPath -AuthToken $releaseToken)
}

Write-Step "Release complete"
Write-Host "Release: https://codeberg.org/$Owner/$Repo/releases/tag/$tagName"
Write-Host "Package: $zipPath"
Write-Host "SHA256:  $shaPath"
