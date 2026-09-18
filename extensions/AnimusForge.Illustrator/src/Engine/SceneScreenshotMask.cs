using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AnimusForge.Illustrator.Engine
{
    // Pure image operation: never changes a widget, camera, scene or the caller's bitmap.
    internal static class SceneScreenshotMask
    {
        internal static readonly Color MaskColor = Color.FromArgb(255, 112, 112, 112);
        internal const int MaxMasks = 16;

        internal static bool TryCreatePixelBounds(float x, float y, float width, float height,
            float padding, out Rectangle bounds)
        {
            bounds = Rectangle.Empty;
            if (!IsFinite(x) || !IsFinite(y) || !IsFinite(width) || !IsFinite(height) ||
                !IsFinite(padding) || width <= 0 || height <= 0 || padding < 0) return false;
            double left = Math.Floor((double)x - padding), top = Math.Floor((double)y - padding);
            double right = Math.Ceiling((double)x + width + padding), bottom = Math.Ceiling((double)y + height + padding);
            if (left < int.MinValue || top < int.MinValue || right > int.MaxValue || bottom > int.MaxValue ||
                right - left > int.MaxValue || bottom - top > int.MaxValue) return false;
            bounds = Rectangle.FromLTRB((int)left, (int)top, (int)right, (int)bottom);
            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        internal static Bitmap Prepare(Bitmap source, IReadOnlyList<Rectangle> masks, bool hasUnknownOverlay)
        {
            if (source == null || hasUnknownOverlay || masks == null || masks.Count > MaxMasks) return null;
            var canvas = new Rectangle(0, 0, source.Width, source.Height);
            var clipped = new List<Rectangle>(masks.Count);
            foreach (Rectangle rectangle in masks)
            {
                if (rectangle.Width <= 0 || rectangle.Height <= 0) return null;
                Rectangle visible = Rectangle.Intersect(canvas, rectangle);
                if (visible.Width > 0 && visible.Height > 0) clipped.Add(visible);
            }
            // If more than half the view is UI, it is not a useful environment reference.
            if (CoveredArea(clipped) * 2L > (long)source.Width * source.Height) return null;
            Bitmap result = source.Clone(canvas, PixelFormat.Format32bppArgb);
            try
            {
                if (clipped.Count > 0)
                {
                    using (Graphics graphics = Graphics.FromImage(result))
                    using (var brush = new SolidBrush(MaskColor))
                    {
                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        foreach (Rectangle rectangle in clipped) graphics.FillRectangle(brush, rectangle);
                    }
                }
                return result;
            }
            catch { result.Dispose(); throw; }
        }

        private static long CoveredArea(List<Rectangle> rectangles)
        {
            // At most 16 panels, only on explicit generation; count overlapping masks once.
            var edges = new List<int>(rectangles.Count * 2);
            foreach (Rectangle rectangle in rectangles) { edges.Add(rectangle.Left); edges.Add(rectangle.Right); }
            edges.Sort();
            var intervals = new List<Rectangle>(rectangles.Count);
            long area = 0;
            for (int i = 1; i < edges.Count; i++)
            {
                int left = edges[i - 1], right = edges[i];
                if (left == right) continue;
                intervals.Clear();
                foreach (Rectangle rectangle in rectangles)
                    if (rectangle.Left < right && rectangle.Right > left) intervals.Add(rectangle);
                intervals.Sort((a, b) => a.Top.CompareTo(b.Top));
                int top = 0, bottom = 0;
                long coveredHeight = 0;
                bool started = false;
                foreach (Rectangle rectangle in intervals)
                {
                    if (!started) { top = rectangle.Top; bottom = rectangle.Bottom; started = true; }
                    else if (rectangle.Top > bottom) { coveredHeight += bottom - top; top = rectangle.Top; bottom = rectangle.Bottom; }
                    else bottom = Math.Max(bottom, rectangle.Bottom);
                }
                if (started) coveredHeight += bottom - top;
                area += (right - left) * coveredHeight;
            }
            return area;
        }
    }
}
