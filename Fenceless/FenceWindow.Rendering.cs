using Fenceless.Model;
using Fenceless.UI.Widgets;
using Fenceless.Util;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Fenceless
{
    public partial class FenceWindow
    {
        private void RenderEntry(Graphics g, FenceEntry entry, int x, int y, int itemWidth, int itemHeight, int iconSize, Color textColor)
        {
            try
            {
                var name = entry.Name;

                var iconBitmap = iconCache.GetIcon(entry.Path, iconSize);
                
                if (iconBitmap == null) return; // Safety check

                var textPosition = new PointF(x, y + iconBitmap.Height + 5);
                var textMaxSize = new SizeF(itemWidth, textHeight);

                using (var stringFormat = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                {
                    var textSize = g.MeasureString(name, iconFont, textMaxSize, stringFormat);
                    var outlineRect = new Rectangle(x - 2, y - 2, itemWidth + 2, iconBitmap.Height + (int)textSize.Height + 5 + 2);
                    var outlineRectInner = outlineRect.Shrink(1);

                    var mousePos = PointToClient(MousePosition);
                    var mouseOver = !isDraggingItem && mousePos.X >= x && mousePos.Y >= y && mousePos.X < x + outlineRect.Width && mousePos.Y < y + outlineRect.Height;

                    var isBeingDragged = isDraggingItem && draggingItem == entry.Path;

                    if (mouseOver && !isBeingDragged)
                    {
                        hoveringItem = entry.Path;
                        hasHoverUpdated = true;
                    }

                    if (mouseOver && shouldUpdateSelection && !isBeingDragged)
                    {
                        selectedItem = entry.Path;
                        shouldUpdateSelection = false;
                        hasSelectionUpdated = true;
                    }

                    if (mouseOver && shouldRunDoubleClick && !isDraggingItem)
                    {
                        shouldRunDoubleClick = false;
                        entry.Open();
                    }

                    float opacity = isBeingDragged ? 0.3f : 1.0f;
                
                    if ((selectedItem == entry.Path || selectedItems.Contains(entry.Path)) && !isBeingDragged)
                    {
                        if (mouseOver)
                        {
                            var pen = GraphicsOptimizer.GetCachedPen(Color.FromArgb(180, SystemColors.ActiveBorder), 2);
                            var brush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(120, SystemColors.GradientActiveCaption));
                            g.DrawRectangle(pen, outlineRectInner);
                            g.FillRectangle(brush, outlineRect);
                        }
                        else
                        {
                            var pen = GraphicsOptimizer.GetCachedPen(Color.FromArgb(150, SystemColors.ActiveBorder), 2);
                            var brush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(100, SystemColors.GradientInactiveCaption));
                            g.DrawRectangle(pen, outlineRectInner);
                            g.FillRectangle(brush, outlineRect);
                        }
                    }
                    else if (!isBeingDragged)
                    {
                        if (mouseOver)
                        {
                            var pen = GraphicsOptimizer.GetCachedPen(Color.FromArgb(120, SystemColors.ActiveBorder), 1);
                            var brush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(80, SystemColors.ActiveCaption));
                            g.DrawRectangle(pen, outlineRectInner);
                            g.FillRectangle(brush, outlineRect);
                        }
                    }

                    // Draw icon centered with optional transparency
                    var iconRect = new Rectangle(x + itemWidth / 2 - iconBitmap.Width / 2, y, iconBitmap.Width, iconBitmap.Height);
                
                    if (isBeingDragged)
                    {
                        // Use simple alpha blending for dragged items
                        using (var imageAttributes = new System.Drawing.Imaging.ImageAttributes())
                        {
                            var colorMatrix = new System.Drawing.Imaging.ColorMatrix();
                            colorMatrix.Matrix33 = opacity; // Alpha channel
                            imageAttributes.SetColorMatrix(colorMatrix);
                            g.DrawImage(iconBitmap, iconRect, 0, 0, iconBitmap.Width, iconBitmap.Height, GraphicsUnit.Pixel, imageAttributes);
                        }
                    }
                    else
                    {
                        g.DrawImage(iconBitmap, iconRect);
                    }
                
                    // Draw text with shadow if enabled
                    var textColorWithOpacity = isBeingDragged ?
                        Color.FromArgb((int)(textColor.A * opacity), textColor.R, textColor.G, textColor.B) : textColor;
                    
                    if (fenceInfo.ShowShadow && !isBeingDragged)
                    {
                        var shadowBrush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(180, 15, 15, 15));
                        g.DrawString(name, iconFont, shadowBrush,
                            new RectangleF(textPosition.Move(shadowDist, shadowDist), textMaxSize), stringFormat);
                    }
                
                    var textBrush = GraphicsOptimizer.GetCachedBrush(textColorWithOpacity);
                    g.DrawString(name, iconFont, textBrush, new RectangleF(textPosition, textMaxSize), stringFormat);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error rendering entry '{entry?.Path}': {ex.Message}", "FenceWindow", ex);
                
                // Draw error placeholder
                var errorBrush = GraphicsOptimizer.GetCachedBrush(Color.Red);
                g.FillRectangle(errorBrush, x, y, itemWidth, itemHeight);
            }
        }

        private void FenceWindow_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Use customizable colors with transparency
                var backgroundColor = ApplyTransparency(Color.FromArgb(fenceInfo.BackgroundColor), fenceInfo.BackgroundTransparency);
                var titleBackgroundColor = ApplyTransparency(Color.FromArgb(fenceInfo.TitleBackgroundColor), fenceInfo.TitleBackgroundTransparency);
                var textColor = ApplyTransparency(Color.FromArgb(fenceInfo.TextColor), fenceInfo.TextTransparency);
                var borderColor = ApplyTransparency(Color.FromArgb(fenceInfo.BorderColor), fenceInfo.BorderTransparency);

                // Background with customizable color and transparency
                var backgroundBrush = GraphicsOptimizer.GetCachedBrush(backgroundColor);
                if (fenceInfo.CornerRadius > 0)
                {
                    using (var path = CreateRoundedRectanglePath(ClientRectangle, fenceInfo.CornerRadius))
                    {
                        e.Graphics.FillPath(backgroundBrush, path);
                    }
                }
                else
                {
                    e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
                }

                var titleBrush = GraphicsOptimizer.GetCachedBrush(titleBackgroundColor);
                var titleRect = new RectangleF(0, 0, Width, titleHeight);
                if (fenceInfo.CornerRadius > 0)
                {
                    using (var titlePath = CreateRoundedRectanglePath(titleRect, fenceInfo.CornerRadius, true))
                    {
                        e.Graphics.FillPath(titleBrush, titlePath);
                    }
                }
                else
                {
                    e.Graphics.FillRectangle(titleBrush, titleRect);
                }

                var textBrush = GraphicsOptimizer.GetCachedBrush(textColor);
                using (var titleFormat = new StringFormat { Alignment = StringAlignment.Center })
                {
                    e.Graphics.DrawString(Text, titleFont, textBrush, new PointF(Width / 2, titleOffset), titleFormat);
                }

                if (isSearchActive && searchBox != null)
                {
                    var searchLabel = $"{searchMatchCount} match{(searchMatchCount != 1 ? "es" : "")}";
                    var searchLabelBrush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(180, textColor));
                    var searchLabelSize = e.Graphics.MeasureString(searchLabel, iconFont);
                    e.Graphics.DrawString(searchLabel, iconFont, searchLabelBrush,
                        new PointF(Width - searchBox.Width - 12 - searchLabelSize.Width, (titleHeight - searchLabelSize.Height) / 2));
                }

                if (fenceInfo.BorderWidth > 0)
                {
                    var borderPen = GraphicsOptimizer.GetCachedPen(borderColor, fenceInfo.BorderWidth);
                    if (fenceInfo.CornerRadius > 0)
                    {
                        var borderRect = new Rectangle(fenceInfo.BorderWidth / 2, fenceInfo.BorderWidth / 2, 
                            Width - fenceInfo.BorderWidth, Height - fenceInfo.BorderWidth);
                        using (var borderPath = CreateRoundedRectanglePath(borderRect, fenceInfo.CornerRadius))
                        {
                            e.Graphics.DrawPath(borderPen, borderPath);
                        }
                    }
                    else
                    {
                        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                    }
                }

                if (UsesWidgetRenderer())
                {
                    var accentColor = GetWidgetAccentColor();
                    var widgetContext = CreateWidgetRenderContext(e.Graphics, textColor, accentColor);
                    var result = widgetRenderer.Render(widgetContext);
                    scrollHeight = result.MaxScrollOffset;
                    scrollOffset = Math.Min(scrollOffset, scrollHeight);
                }
                else
                {
                    var layoutSnapshot = CreateLayoutSnapshot();
                    var paintState = e.Graphics.Save();
                    e.Graphics.SetClip(new Rectangle(0, titleHeight, Width, Height - titleHeight));

                    foreach (var layoutItem in layoutSnapshot.Items)
                    {
                        var file = layoutItem.Value;
                        try
                        {
                            var bounds = layoutItem.Bounds;

                            if (file.StartsWith("task:") || file.StartsWith("clip:"))
                            {
                                RenderVirtualEntry(e.Graphics, file, bounds.X, bounds.Y, bounds.Width, bounds.Height, fenceInfo.IconSize, textColor);
                            }
                            else
                            {
                                var entry = FenceEntry.FromPath(file);
                                if (entry == null)
                                    continue;

                                RenderEntry(e.Graphics, entry, bounds.X, bounds.Y, bounds.Width, bounds.Height, fenceInfo.IconSize, textColor);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Error rendering file '{file}': {ex.Message}", "FenceWindow", ex);
                        }
                    }

                    e.Graphics.Restore(paintState);

                    scrollHeight = layoutSnapshot.GetMaxScrollOffset(ClientRectangle.Height - titleHeight);
                    scrollOffset = Math.Min(scrollOffset, scrollHeight);
                }

                // Scroll bars
                if (scrollHeight > 0)
                {
                    var contentHeight = Height - titleHeight;
                    var totalContentHeight = scrollHeight + contentHeight;
                    var scrollbarHeight = Math.Max(20, contentHeight * contentHeight / Math.Max(contentHeight, totalContentHeight));
                    var maxThumbTravel = Math.Max(0, contentHeight - scrollbarHeight);
                    var thumbY = titleHeight + (scrollHeight == 0 ? 0 : scrollOffset * maxThumbTravel / scrollHeight);
                    var scrollBrush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(150, borderColor));
                    e.Graphics.FillRectangle(scrollBrush, new Rectangle(Width - 5, thumbY, 5, scrollbarHeight));
                }

                // Click handlers
                if (shouldUpdateSelection && !hasSelectionUpdated)
                    selectedItem = null;

                if (!hasHoverUpdated)
                    hoveringItem = null;

                if (AppSettings.Instance.ShowTooltips && hoveringItem != null)
                {
                    try
                    {
                        if (hoveringItem.StartsWith("task:"))
                        {
                            var title = RunningTasksFence.GetWindowTitle(hoveringItem);
                            itemToolTip.SetToolTip(this, $"[Window] {title}");
                        }
                        else if (hoveringItem.StartsWith("clip:"))
                        {
                            var parts = hoveringItem.Split(new[] { ':' }, 3);
                            if (parts.Length >= 3)
                                itemToolTip.SetToolTip(this, $"[Clipboard] {parts[2]}");
                            else
                                itemToolTip.SetToolTip(this, "[Clipboard item]");
                        }
                        else if (hoveringItem.StartsWith("clipimg:"))
                        {
                            itemToolTip.SetToolTip(this, "[Clipboard image]");
                        }
                        else if (File.Exists(hoveringItem))
                        {
                            var fileInfo = new System.IO.FileInfo(hoveringItem);
                            var size = FormatFileSize(fileInfo.Length);
                            var modified = fileInfo.LastWriteTime.ToString("g");
                            itemToolTip.SetToolTip(this, $"{fileInfo.Name}\nSize: {size}\nModified: {modified}\nPath: {hoveringItem}");
                        }
                        else if (System.IO.Directory.Exists(hoveringItem))
                        {
                            var dirInfo = new System.IO.DirectoryInfo(hoveringItem);
                            itemToolTip.SetToolTip(this, $"{dirInfo.Name}\nPath: {hoveringItem}");
                        }
                        else
                        {
                            itemToolTip.SetToolTip(this, hoveringItem);
                        }
                    }
                    catch
                    {
                        itemToolTip.SetToolTip(this, hoveringItem);
                    }
                }
                else
                {
                    itemToolTip.SetToolTip(this, null);
                }

                // Render drag target indicator
                if (isDraggingItem && dragTargetIndex >= 0)
                {
                    RenderDragTargetIndicator(e.Graphics, dragTargetIndex);
                }

                // Render dragged item at cursor position
                if (isDraggingItem && draggingItem != null)
                {
                    RenderDraggedItem(e.Graphics, draggingItem, dragCurrentPoint);
                }

                shouldRunDoubleClick = false;
                shouldUpdateSelection = false;
                hasSelectionUpdated = false;
                hasHoverUpdated = false;
            }
            catch (OutOfMemoryException ex)
            {
                logger.Critical("Out of memory in paint method, clearing caches", "FenceWindow", ex);
                
                // Emergency cleanup
                ClearIconCache();
                
                try
                {
                    var errorBrush = GraphicsOptimizer.GetCachedBrush(Color.DarkRed);
                    e.Graphics.FillRectangle(errorBrush, ClientRectangle);
                    var textBrush = GraphicsOptimizer.GetCachedBrush(Color.White);
                    e.Graphics.DrawString("Memory Error - Cache Cleared", SystemFonts.DefaultFont, textBrush, 10, 10);
                }
                catch
                {
                    // If even error drawing fails, just continue
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error in paint method: {ex.Message}", "FenceWindow", ex);
                
                // Draw simple error state
                try
                {
                    var errorBrush = GraphicsOptimizer.GetCachedBrush(Color.Red);
                    e.Graphics.FillRectangle(errorBrush, ClientRectangle);
                    var textBrush = GraphicsOptimizer.GetCachedBrush(Color.White);
                    e.Graphics.DrawString($"Render Error: {ex.Message}", SystemFonts.DefaultFont, textBrush, 10, 10);
                }
                catch
                {
                    // If even error drawing fails, just continue
                }
            }
        }

        private Color ApplyTransparency(Color baseColor, int transparencyPercent)
        {
            // Convert transparency percentage to alpha value (0-255)
            int alpha = (int)Math.Round(transparencyPercent * 255.0 / 100.0);
            alpha = Math.Max(0, Math.Min(255, alpha)); // Clamp to valid range
            
            return Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        }

        private FenceWidgetRenderContext CreateWidgetRenderContext(Graphics graphics, Color textColor, Color accentColor)
        {
            return new FenceWidgetRenderContext(
                graphics,
                ClientRectangle,
                fenceInfo,
                CreateWidgetSnapshot(),
                titleHeight,
                scrollOffset,
                selectedItem,
                hoveringItem,
                titleFont,
                iconFont,
                iconCache,
                textColor,
                accentColor);
        }

        private FenceWidgetSnapshot CreateWidgetSnapshot()
        {
            if (fenceProvider is IWidgetDataProvider widgetDataProvider)
                return widgetDataProvider.GetSnapshot();

            var items = GetFilteredFiles()
                .Select(value =>
                {
                    var entry = FenceEntryModel.FromLegacyValue(value);
                    return new FenceWidgetItem(
                        entry.Id,
                        entry.Kind,
                        entry.DisplayName,
                        entry.Kind.ToString(),
                        string.Empty,
                        value,
                        entry.Path,
                        entry.TaskHandle,
                        entry.ClipboardIndex,
                        iconPath: entry.Path);
                })
                .ToList();

            return new FenceWidgetSnapshot(fenceInfo.FenceType, items, fenceInfo.Name, status: $"{items.Count} item{(items.Count == 1 ? "" : "s")}");
        }

        private Color GetWidgetAccentColor()
        {
            switch (fenceInfo.FenceType)
            {
                case FenceType.LiveFolder:
                    return Color.FromArgb(73, 190, 138);
                case FenceType.RunningTasks:
                    return Color.FromArgb(91, 156, 255);
                case FenceType.ClipboardHistory:
                    return Color.FromArgb(218, 151, 74);
                default:
                    return Color.FromArgb(fenceInfo.BorderColor);
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(RectangleF rect, int radius, bool topOnly = false)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            var diameter = radius * 2;
            var arc = new RectangleF(0, 0, diameter, diameter);

            // Top left corner
            arc.Location = new PointF(rect.Left, rect.Top);
            path.AddArc(arc, 180, 90);

            // Top right corner
            arc.Location = new PointF(rect.Right - diameter, rect.Top);
            path.AddArc(arc, 270, 90);

            if (topOnly)
            {
                // Straight lines for bottom
                path.AddLine(rect.Right, rect.Top + radius, rect.Right, rect.Bottom);
                path.AddLine(rect.Right, rect.Bottom, rect.Left, rect.Bottom);
                path.AddLine(rect.Left, rect.Bottom, rect.Left, rect.Top + radius);
            }
            else
            {
                // Bottom right corner
                arc.Location = new PointF(rect.Right - diameter, rect.Bottom - diameter);
                path.AddArc(arc, 0, 90);

                // Bottom left corner
                arc.Location = new PointF(rect.Left, rect.Bottom - diameter);
                path.AddArc(arc, 90, 90);
            }

            path.CloseFigure();
            return path;
        }

        #region Virtual Entry Rendering

        private void RenderVirtualEntry(Graphics g, string entry, int x, int y, int entryWidth, int entryHeight, int iconSize, Color textColor)
        {
            try
            {
                var entryModel = FenceEntryModel.FromLegacyValue(entry);
                if (entryModel.Kind != FenceEntryKind.Task && entryModel.Kind != FenceEntryKind.ClipboardText)
                    return;

                var displayName = entryModel.DisplayName;
                var icon = entryModel.Kind == FenceEntryKind.Task
                    ? SystemIcons.Application
                    : SystemIcons.Information;

                if (string.IsNullOrEmpty(displayName))
                    displayName = "(unnamed)";

                var textPosition = new PointF(x, y + iconSize + 5);
                var textMaxSize = new SizeF(entryWidth, textHeight);
                using (var stringFormat = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                {
                    var textSize = g.MeasureString(displayName, iconFont, textMaxSize, stringFormat);
                    var outlineRect = new Rectangle(x - 2, y - 2, entryWidth + 2, iconSize + (int)textSize.Height + 5 + 2);
                    var outlineRectInner = outlineRect.Shrink(1);

                    var mousePos = PointToClient(MousePosition);
                    var mouseOver = !isDraggingItem && mousePos.X >= x && mousePos.Y >= y && mousePos.X < x + outlineRect.Width && mousePos.Y < y + outlineRect.Height;

                    if (mouseOver)
                    {
                        hoveringItem = entry;
                        hasHoverUpdated = true;
                    }

                    if (mouseOver && shouldUpdateSelection)
                    {
                        selectedItem = entry;
                        shouldUpdateSelection = false;
                        hasSelectionUpdated = true;
                    }

                    if ((selectedItem == entry || selectedItems.Contains(entry)))
                    {
                        if (mouseOver)
                        {
                            var pen = GraphicsOptimizer.GetCachedPen(Color.FromArgb(180, SystemColors.ActiveBorder), 2);
                            var brush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(120, SystemColors.GradientActiveCaption));
                            g.DrawRectangle(pen, outlineRectInner);
                            g.FillRectangle(brush, outlineRect);
                        }
                        else
                        {
                            var pen = GraphicsOptimizer.GetCachedPen(Color.FromArgb(150, SystemColors.ActiveBorder), 2);
                            var brush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(100, SystemColors.GradientInactiveCaption));
                            g.DrawRectangle(pen, outlineRectInner);
                            g.FillRectangle(brush, outlineRect);
                        }
                    }
                    else if (mouseOver)
                    {
                        var pen = GraphicsOptimizer.GetCachedPen(Color.FromArgb(120, SystemColors.ActiveBorder), 1);
                        var brush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(80, SystemColors.ActiveCaption));
                        g.DrawRectangle(pen, outlineRectInner);
                        g.FillRectangle(brush, outlineRect);
                    }

                    var iconRect = new Rectangle(x + entryWidth / 2 - iconSize / 2, y, iconSize, iconSize);
                    g.DrawIcon(icon, iconRect);

                    if (fenceInfo.ShowShadow)
                    {
                        var shadowBrush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(180, 15, 15, 15));
                        g.DrawString(displayName, iconFont, shadowBrush,
                            new RectangleF(textPosition.Move(shadowDist, shadowDist), textMaxSize), stringFormat);
                    }

                    var textBrush = GraphicsOptimizer.GetCachedBrush(textColor);
                    g.DrawString(displayName, iconFont, textBrush, new RectangleF(textPosition, textMaxSize), stringFormat);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error rendering virtual entry '{entry}': {ex.Message}", "FenceWindow", ex);
            }
        }

        #endregion

        #region Drag Feedback Rendering

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
            return $"{bytes / (1024 * 1024 * 1024):F1} GB";
        }

        private void RenderDragTargetIndicator(Graphics g, int targetIndex)
        {
            try
            {
                var layout = FenceGridLayout.Calculate(Width, fenceInfo.ItemSpacing, fenceInfo.IconSize, itemWidth, textHeight);
                var pos = layout.GetItemPosition(targetIndex, titleHeight, scrollOffset);
                
                // Simple pulsing effect without complex math
                var pulsePhase = (Environment.TickCount / 300) % 4;
                var alpha = pulsePhase < 2 ? 120 : 80;
                
                var brush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(alpha / 8, SystemColors.Highlight));
                var indicatorRect = new Rectangle(pos.X - 1, pos.Y - 1, layout.ActualItemWidth + 2, layout.ActualItemHeight + 2);
                
                g.FillRectangle(brush, indicatorRect);

                using (var pen = new Pen(Color.FromArgb(alpha, SystemColors.Highlight), 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawRectangle(pen, indicatorRect);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error rendering drag target indicator: {ex.Message}", "FenceWindow", ex);
            }
        }

        private void RenderDraggedItem(Graphics g, string itemPath, Point cursorPosition)
        {
            try
            {
                var entry = FenceEntry.FromPath(itemPath);
                if (entry == null) return;
                
                var iconSize = fenceInfo.IconSize;
                var cacheKey = $"{entry.Path}_{iconSize}";
                
                // Use cached icon if available
                var iconBitmap = iconCache.GetIcon(entry.Path, iconSize);
                
                // Fallback to creating temporary bitmap if cache failed (shouldn't happen often)
                if (iconBitmap == null)
                {
                    var icon = entry.ExtractIcon(thumbnailProvider);
                    if (icon.Width != iconSize || icon.Height != iconSize)
                    {
                        iconBitmap = new Bitmap(iconSize, iconSize);
                        using (var graphics = Graphics.FromImage(iconBitmap))
                        {
                            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            graphics.DrawIcon(icon, new Rectangle(0, 0, iconSize, iconSize));
                        }
                        // Don't cache here to avoid issues during drag
                    }
                    else
                    {
                        iconBitmap = icon.ToBitmap();
                    }
                }
                
                if (iconBitmap == null) return;
                
                // Position the dragged item slightly offset from cursor
                var drawX = cursorPosition.X - iconSize / 2;
                var drawY = cursorPosition.Y - iconSize / 2;
                
                // Simple shadow without complex effects
                var shadowBrush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(60, 0, 0, 0));
                g.FillEllipse(shadowBrush, drawX + 2, drawY + 2, iconSize, iconSize);
                
                // Draw the dragged icon with transparency
                using (var imageAttributes = new System.Drawing.Imaging.ImageAttributes())
                {
                    var colorMatrix = new System.Drawing.Imaging.ColorMatrix();
                    colorMatrix.Matrix33 = 0.8f; // Slightly transparent
                    imageAttributes.SetColorMatrix(colorMatrix);
                    g.DrawImage(iconBitmap, new Rectangle(drawX, drawY, iconSize, iconSize), 
                        0, 0, iconBitmap.Width, iconBitmap.Height, GraphicsUnit.Pixel, imageAttributes);
                }
                
                // Draw simplified item name
                var textColor = ApplyTransparency(Color.FromArgb(fenceInfo.TextColor), fenceInfo.TextTransparency);
                var textBrush = GraphicsOptimizer.GetCachedBrush(Color.FromArgb(180, textColor.R, textColor.G, textColor.B));
                var textRect = new RectangleF(drawX - 20, drawY + iconSize + 2, iconSize + 40, 20);
                using (var stringFormat = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                {
                    g.DrawString(entry.Name, iconFont, textBrush, textRect, stringFormat);
                }
                
                // The icon cache manages disposal, so no need to dispose here
                if (iconBitmap == null)
                {
                    logger.Warning($"Failed to get icon for dragged item '{itemPath}'", "FenceWindow");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error rendering dragged item '{itemPath}': {ex.Message}", "FenceWindow", ex);
            }
        }

        #endregion
    }
}
