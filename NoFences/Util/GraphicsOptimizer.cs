using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Fenceless.Util
{
    /// <summary>
    /// Utility class for optimizing GDI+ operations and preventing memory leaks
    /// </summary>
    public static class GraphicsOptimizer
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<(Color color, float width), Pen> _penCache = new Dictionary<(Color, float), Pen>();
        private static readonly Dictionary<Color, SolidBrush> _brushCache = new Dictionary<Color, SolidBrush>();

        /// <summary>
        /// Gets a cached pen or creates a new one if not available
        /// </summary>
        public static Pen GetCachedPen(Color color, float width = 1f)
        {
            var key = (color, width);
            lock (_lock)
            {
                if (_penCache.TryGetValue(key, out var cached))
                {
                    return cached;
                }
                var pen = new Pen(color, width);
                _penCache[key] = pen;
                return pen;
            }
        }

        public static SolidBrush GetCachedBrush(Color color)
        {
            lock (_lock)
            {
                if (_brushCache.TryGetValue(color, out var cached))
                {
                    return cached;
                }
                var brush = new SolidBrush(color);
                _brushCache[color] = brush;
                return brush;
            }
        }

        /// <summary>
        /// Disposes all cached GDI+ objects
        /// </summary>
        public static void DisposeCache()
        {
            lock (_lock)
            {
                foreach (var pen in _penCache.Values)
                    pen?.Dispose();
                _penCache.Clear();

                foreach (var brush in _brushCache.Values)
                    brush?.Dispose();
                _brushCache.Clear();
            }
        }

        /// <summary>
        /// Optimizes Graphics object settings for performance
        /// </summary>
        public static void OptimizeGraphics(Graphics g)
        {
            if (g == null) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        }

        /// <summary>
        /// Creates a high-quality thumbnail with proper disposal
        /// </summary>
        public static Bitmap CreateThumbnail(Image sourceImage, int width, int height)
        {
            if (sourceImage == null) return null;

            try
            {
                var thumbnail = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(thumbnail))
                {
                    OptimizeGraphics(graphics);
                    graphics.DrawImage(sourceImage, 0, 0, width, height);
                }
                return thumbnail;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if two rectangles intersect with a small tolerance
        /// </summary>
        public static bool RectanglesIntersect(Rectangle rect1, Rectangle rect2, int tolerance = 2)
        {
            var inflatedRect1 = Rectangle.Inflate(rect1, tolerance, tolerance);
            var inflatedRect2 = Rectangle.Inflate(rect2, tolerance, tolerance);
            return inflatedRect1.IntersectsWith(inflatedRect2);
        }

        /// <summary>
        /// Safely scales a rectangle while maintaining aspect ratio
        /// </summary>
        public static Rectangle ScaleRectangle(Rectangle source, float scaleX, float scaleY)
        {
            return new Rectangle(
                (int)(source.X * scaleX),
                (int)(source.Y * scaleY),
                (int)(source.Width * scaleX),
                (int)(source.Height * scaleY)
            );
        }

        /// <summary>
        /// Creates a rounded rectangle path for drawing
        /// </summary>
        public static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            var diameter = radius * 2;
            var arc = new Rectangle(rect.X, rect.Y, diameter, diameter);

            // Top-left corner
            path.AddArc(arc, 180, 90);
            // Top-right corner
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            // Bottom-right corner
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            // Bottom-left corner
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}