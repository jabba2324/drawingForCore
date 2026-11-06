// drawingForCore's own tests (MIT): what crashed the test host on macOS (xforcore/drawingForCore#13).
// - A Graphics whose image was disposed first. Microsoft's BufferedGraphics.Dispose (Unix) disposes its bitmap before
//   its Graphics, and on macOS Graphics.Dispose flushes first (GdiplusNative.Unix.cs: UseX11Drawable is false there),
//   so the engine's GdipFlush called into the image's freed SKBitmap. On Linux Graphics.Dispose doesn't flush, but
//   Flush, VisibleClipBounds or Clear after the image's Dispose crashed the same way.
// - The scans of a region as tall as GDI+'s infinite one (8,388,608 rows), as Microsoft's
//   RegionTests.Complement_InfiniteRegion_Success asks for: Skia rasterised every row, which took two minutes on
//   Linux and crashed in sk_region_set_path on macOS.
#nullable disable
using System.Drawing.Drawing2D;
using System.Linq;
using Xunit;

namespace System.Drawing.Tests.Engine
{
    public class MacOSCrashTests
    {
        [Fact]
        public void Graphics_ImageDisposedFirst_FlushAndDispose()
        {
            var image = new Bitmap(4, 3);
            var graphics = Graphics.FromImage(image);
            graphics.Clear(Color.Red);
            image.Dispose();

            graphics.Flush();
            graphics.Flush(FlushIntention.Sync);
            graphics.Dispose();
        }

        [Fact]
        public void Graphics_ImageDisposedFirst_KeepsItsBounds()
        {
            var image = new Bitmap(4, 3);
            using var graphics = Graphics.FromImage(image);
            image.Dispose();

            Assert.Equal(new RectangleF(0, 0, 4, 3), graphics.VisibleClipBounds);
            graphics.Clear(Color.Blue);
            graphics.FillRectangle(Brushes.Green, 0, 0, 2, 2);
        }

        // As Microsoft's BufferedGraphicsTests.Dispose_ActualMultipleTimes_Success, through the macOS path: the buffer's
        // bitmap is disposed before its Graphics, which flushes.
        [Fact]
        public void BufferedGraphics_Dispose_AfterDrawing()
        {
            using var context = new BufferedGraphicsContext();
            using var image = new Bitmap(3, 3);
            using var target = Graphics.FromImage(image);
            var buffered = context.Allocate(target, new Rectangle(0, 0, 3, 3));
            buffered.Graphics.Clear(Color.Red);
            buffered.Render();
            buffered.Graphics.Flush();
            buffered.Dispose();
            Assert.Null(buffered.Graphics);
        }

        // GDI+'s infinite region less a rectangle (Microsoft's Complement_InfiniteRegion_Success, with Exclude).
        [Fact]
        public void RegionScans_InfiniteLessRectangle()
        {
            using var region = new Region();
            using var matrix = new Matrix();
            region.Exclude(new Rectangle(1, 2, 3, 4));

            Assert.Equal(new[]
            {
                new RectangleF(-4194304, -4194304, 8388608, 4194306),
                new RectangleF(-4194304, 2, 4194305, 4),
                new RectangleF(4, 2, 4194300, 4),
                new RectangleF(-4194304, 6, 8388608, 4194298),
            }, region.GetRegionScans(matrix));
        }

        // A tall region whose rows differ only where its rectangles start and end, with a fractional edge.
        [Fact]
        public void RegionScans_TallRectangles()
        {
            using var region = new Region(new Rectangle(0, 0, 10, 100000));
            using var matrix = new Matrix();
            region.Union(new RectangleF(5, 50000.25f, 10, 10));
            region.Union(new Rectangle(-20, 99000, 5, 2000));

            Assert.Equal(new[]
            {
                new RectangleF(0, 0, 10, 50000),
                new RectangleF(0, 50000, 15, 10),
                new RectangleF(0, 50010, 10, 48990),
                new RectangleF(-20, 99000, 5, 1000),
                new RectangleF(0, 99000, 10, 1000),
                new RectangleF(-20, 100000, 5, 1000),
            }, region.GetRegionScans(matrix));
        }

        // A tall region with a sloping edge: every row differs, so none may be copied from another.
        [Fact]
        public void RegionScans_TallTriangle()
        {
            using var path = new GraphicsPath();
            path.AddPolygon(new[] { new Point(0, 0), new Point(0, 10000), new Point(100, 10000) });
            using var region = new Region(path);
            using var matrix = new Matrix();

            RectangleF[] scans = region.GetRegionScans(matrix);
            Assert.Equal(10000, scans.Max(s => s.Bottom));
            Assert.All(scans, s => Assert.Equal(0, s.Left));
            Assert.True(scans.Select(s => s.Width).Distinct().Count() >= 99);
            Assert.InRange(scans.Sum(s => s.Width * s.Height), 495000, 505000);
            Assert.True(scans.Zip(scans.Skip(1)).All(p => p.First.Width <= p.Second.Width)); // widening downwards
        }
    }
}
