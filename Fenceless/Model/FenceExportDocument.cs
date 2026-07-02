using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fenceless.Model
{
    public sealed class FenceExportDocument
    {
        public int Version { get; set; } = 1;
        public string ExportDate { get; set; } = DateTime.UtcNow.ToString("o");
        public AppSettings? Settings { get; set; }
        public List<FenceInfo> Fences { get; set; } = new List<FenceInfo>();

        public static FenceExportDocument Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("Import file is empty.");

            var root = JObject.Parse(json);
            if (root["Fences"] == null)
                throw new InvalidDataException("Import file is missing the Fences collection.");

            var importData = root.ToObject<FenceExportDocument>();
            if (importData == null)
                throw new InvalidDataException("Import file could not be parsed.");

            if (importData.Fences == null)
                throw new InvalidDataException("Import file is missing the Fences collection.");

            importData.Fences = importData.Fences
                .Where(fence => fence != null)
                .Select(fence => FenceInfoValidator.Normalize(fence))
                .ToList();

            return importData;
        }
    }
}
