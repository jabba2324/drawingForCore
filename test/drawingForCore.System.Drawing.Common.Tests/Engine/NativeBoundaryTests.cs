// drawingForCore's own tests (MIT): what the engine's flat API functions do with the pointers and memory blocks
// Microsoft's code passes them (LockBits buffers with either stride sign, Scan0, point and rectangle arrays,
// EncoderParameters). They record the engine's behaviour where Microsoft's suite doesn't (negative strides,
// UserInputBuffer, JPEG quality, the pixels of the integer overloads), so that moving pointers to the edge of the
// flat API (xforcore/drawingForCore#3) can be shown to change nothing.
#nullable disable
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit;

namespace System.Drawing.Tests.Engine
{
    public class NativeBoundaryTests
    {
        const int W = 13, H = 7; // odd sizes: rows with padding, and 1bpp/4bpp rows ending mid-byte

        // A 32bppArgb bitmap with a pattern of opaque and translucent colours, set pixel by pixel (exact on every platform).
        static Bitmap Pattern(int w = W, int h = H)
        {
            var bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int a = (x + y) % 4 == 0 ? 128 : 255;
                    bitmap.SetPixel(x, y, Color.FromArgb(a, (x * 37 + y * 11) & 255, (x * 5 + y * 73) & 255, (x * y * 29 + 7) & 255));
                }
            return bitmap;
        }

        static byte[] Bytes(Bitmap bitmap, PixelFormat format)
        {
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, format);
            try
            {
                var bytes = new byte[data.Stride * data.Height];
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                // The padding at the end of each row of GDI+'s own buffer is not written: zero it.
                int rowBytes = (data.Width * Image.GetPixelFormatSize(format) + 7) / 8;
                for (int y = 0; y < data.Height; y++) Array.Clear(bytes, y * data.Stride + rowBytes, data.Stride - rowBytes);
                return bytes;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        static int[] Argb(Bitmap bitmap)
        {
            var result = new int[bitmap.Width * bitmap.Height];
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                    result[y * bitmap.Width + x] = bitmap.GetPixel(x, y).ToArgb();
            return result;
        }

        static string Sha(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).Substring(0, 16);

        // The integer overloads give the same pixels, points and matrices as the float ones.

        static Bitmap Draw(Action<Graphics> draw)
        {
            var bitmap = new Bitmap(64, 48, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            draw(g);
            return bitmap;
        }

        static void SamePixels(Action<Graphics> a, Action<Graphics> b)
        {
            using var first = Draw(a);
            using var second = Draw(b);
            byte[] x = Bytes(first, PixelFormat.Format32bppArgb), y = Bytes(second, PixelFormat.Format32bppArgb);
            Assert.Equal(x, y);
            Assert.Contains(x, v => v != 255); // something was drawn
        }

        static readonly Rectangle[] Rects = { new Rectangle(2, 3, 20, 10), new Rectangle(30, 5, 0, 8), new Rectangle(10, 20, 40, 20), new Rectangle(5, 5, -3, 4) };
        static RectangleF[] RectsF => Array.ConvertAll(Rects, r => (RectangleF)r);

        [Fact]
        public void DrawRectangles_IntegerAndFloat_SamePixels()
        {
            using var pen = new Pen(Color.Navy, 3);
            SamePixels(g => g.DrawRectangles(pen, Rects), g => g.DrawRectangles(pen, RectsF));
        }

        [Fact]
        public void FillRectangles_IntegerAndFloat_SamePixels()
        {
            using var brush = new SolidBrush(Color.FromArgb(200, Color.DarkRed));
            SamePixels(g => g.FillRectangles(brush, Rects), g => g.FillRectangles(brush, RectsF));
        }

        static readonly Point[] Parallelogram = { new Point(5, 4), new Point(50, 10), new Point(12, 40) };
        static PointF[] ParallelogramF => Array.ConvertAll(Parallelogram, p => (PointF)p);

        [Fact]
        public void DrawImagePoints_IntegerAndFloat_SamePixels()
        {
            using var image = Pattern();
            SamePixels(g => g.DrawImage(image, Parallelogram), g => g.DrawImage(image, ParallelogramF));
        }

        [Fact]
        public void DrawImagePointsRect_IntegerAndFloat_SamePixels()
        {
            using var image = Pattern();
            using var attributes = new ImageAttributes();
            SamePixels(g => g.DrawImage(image, Parallelogram, new Rectangle(1, 2, 9, 4), GraphicsUnit.Pixel),
                g => g.DrawImage(image, ParallelogramF, new RectangleF(1, 2, 9, 4), GraphicsUnit.Pixel));
            SamePixels(g => g.DrawImage(image, Parallelogram, new Rectangle(1, 2, 9, 4), GraphicsUnit.Pixel, attributes),
                g => g.DrawImage(image, ParallelogramF, new RectangleF(1, 2, 9, 4), GraphicsUnit.Pixel, attributes));
        }

        [Fact]
        public void PathGradientBrush_IntegerAndFloat_SamePixels()
        {
            Point[] points = { new Point(4, 4), new Point(60, 8), new Point(40, 44), new Point(2, 30) };
            using var a = new PathGradientBrush(points, WrapMode.Tile) { CenterColor = Color.Yellow, SurroundColors = new[] { Color.Blue } };
            using var b = new PathGradientBrush(Array.ConvertAll(points, p => (PointF)p), WrapMode.Tile) { CenterColor = Color.Yellow, SurroundColors = new[] { Color.Blue } };
            Assert.Equal(b.CenterPoint, a.CenterPoint);
            Assert.Equal(b.Rectangle, a.Rectangle);
            SamePixels(g => g.FillRectangle(a, 0, 0, 64, 48), g => g.FillRectangle(b, 0, 0, 64, 48));
        }

        [Fact]
        public void PathGradientBrush_FewerThanTwoPoints_ThrowsOutOfMemory()
        {
            Assert.Throws<OutOfMemoryException>(() => new PathGradientBrush(new Point[1]));
            Assert.Throws<OutOfMemoryException>(() => new PathGradientBrush(new Point[0]));
            Assert.Throws<OutOfMemoryException>(() => new PathGradientBrush(new PointF[1]));
        }

        [Fact]
        public void Matrix_RectangleAndPoints_IntegerAndFloat_SameElements()
        {
            using var a = new Matrix(new Rectangle(3, 4, 10, 20), Parallelogram);
            using var b = new Matrix(new RectangleF(3, 4, 10, 20), ParallelogramF);
            Assert.Equal(b.Elements, a.Elements);
            Assert.Equal(new float[] { 4.5f, 0.6f, 0.35f, 1.8f, -9.9f, -5f }, a.Elements.Select(e => MathF.Round(e, 4)).ToArray());
        }

        [Fact]
        public void TransformPoints_IntegerAndFloat_SamePoints()
        {
            using var bitmap = new Bitmap(10, 10);
            using var g = Graphics.FromImage(bitmap);
            g.RotateTransform(30);
            g.ScaleTransform(1.5f, 0.75f);
            g.PageUnit = GraphicsUnit.Millimeter;
            Point[] points = { new Point(0, 0), new Point(10, 3), new Point(-7, 12), new Point(1000, -1000) };
            PointF[] f = Array.ConvertAll(points, p => (PointF)p);
            g.TransformPoints(CoordinateSpace.Device, CoordinateSpace.World, points);
            g.TransformPoints(CoordinateSpace.Device, CoordinateSpace.World, f);
            Assert.Equal(Array.ConvertAll(f, Point.Round), points);
            Assert.NotEqual(new Point(10, 3), points[1]);
            Assert.Throws<ArgumentException>(() => g.TransformPoints(CoordinateSpace.Device, CoordinateSpace.World, new Point[0]));
            Assert.Throws<ArgumentException>(() => g.TransformPoints(CoordinateSpace.Device, CoordinateSpace.World, new PointF[0]));
        }

        // LockBits and UnlockBits in every format GDI+ locks, with GDI+'s own buffer and with the caller's, either stride sign.

        public static IEnumerable<object[]> Formats() => new[]
        {
            PixelFormat.Format1bppIndexed, PixelFormat.Format4bppIndexed, PixelFormat.Format8bppIndexed,
            PixelFormat.Format16bppGrayScale, PixelFormat.Format16bppRgb555, PixelFormat.Format16bppRgb565, PixelFormat.Format16bppArgb1555,
            PixelFormat.Format24bppRgb, PixelFormat.Format32bppRgb, PixelFormat.Format32bppArgb, PixelFormat.Format32bppPArgb,
            PixelFormat.Format48bppRgb, PixelFormat.Format64bppArgb, PixelFormat.Format64bppPArgb,
        }.Select(f => new object[] { f });

        static bool Indexed(PixelFormat format) => (format & PixelFormat.Indexed) != 0;

        // A bitmap of the format holding the pattern: indexed ones get bytes written through LockBits.
        static Bitmap PatternIn(PixelFormat format)
        {
            if (!Indexed(format))
            {
                var bitmap = new Bitmap(W, H, format);
                using var pattern = Pattern();
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.DrawImageUnscaled(pattern, 0, 0);
                }
                return bitmap;
            }
            var indexed = new Bitmap(W, H, format);
            var data = indexed.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, format);
            var bytes = new byte[data.Stride * H];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)(i * 151 + 17);
            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
            indexed.UnlockBits(data);
            return indexed;
        }

        // The bytes LockBits gives in each format, and the pixels writing them back gives (recorded from the engine
        // before #3's change).
        static readonly Dictionary<PixelFormat, (string Read, string RoundTrip)> Expected = new()
        {
            [PixelFormat.Format1bppIndexed] = ("E0290479215B4DC1", "0512CFAC84817C19"),
            [PixelFormat.Format4bppIndexed] = ("F661036991F5E7EA", "E318647ABC699AB3"),
            [PixelFormat.Format8bppIndexed] = ("E5D2DB5997D179BF", "5D82613985F20CDC"),
            [PixelFormat.Format16bppGrayScale] = ("B15CE8E8F9AEA675", "D2DE9DEFABE17CB5"),
            [PixelFormat.Format16bppRgb555] = ("B892AE797A25E465", "1E01634AAA0346CC"),
            [PixelFormat.Format16bppRgb565] = ("3FDD904F143AE5B9", "D2AC97A1CC7BBFD4"),
            [PixelFormat.Format16bppArgb1555] = ("00C7F706D7F0745A", "1E01634AAA0346CC"),
            [PixelFormat.Format24bppRgb] = ("1ECDAA1F3D3B548A", "4BB384C21BD86B69"),
            [PixelFormat.Format32bppRgb] = ("4BB384C21BD86B69", "4BB384C21BD86B69"),
            [PixelFormat.Format32bppArgb] = ("1AE7DE26AF1B86A7", "439C8A1BB834B229"),
            [PixelFormat.Format32bppPArgb] = ("1DAA0D4979DA8C5B", "1AE7DE26AF1B86A7"),
            [PixelFormat.Format48bppRgb] = ("2BA3EC9555217F74", "3CFC8D32BE251802"),
            [PixelFormat.Format64bppArgb] = ("DCD35D919047C118", "67378E7FBB730C3B"),
            [PixelFormat.Format64bppPArgb] = ("14688C490EA40937", "E6534B17015366D1"),
        };

        static byte[] ArgbBytes(int[] argb) => argb.SelectMany(BitConverter.GetBytes).ToArray();

        [Theory]
        [MemberData(nameof(Formats))]
        public void LockBits_ReadAndWriteBack_Recorded(PixelFormat format)
        {
            using var bitmap = PatternIn(format);
            byte[] read = Bytes(bitmap, format);

            using var target = new Bitmap(W, H, format);
            if (Indexed(format)) target.Palette = bitmap.Palette;
            var data = target.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, format);
            Marshal.Copy(read, 0, data.Scan0, read.Length);
            target.UnlockBits(data);

            Assert.Equal(Expected[format], (Sha(read), Sha(ArgbBytes(Argb(target)))));
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void LockBits_PartialRectangle_ReadsAndWritesOnlyIt(PixelFormat format)
        {
            using var bitmap = PatternIn(format);
            int[] before = Argb(bitmap);
            var rect = new Rectangle(3, 2, 7, 4);
            var data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, format);
            Assert.Equal(rect.Width, data.Width);
            Assert.Equal(rect.Height, data.Height);
            bitmap.UnlockBits(data);
            int[] after = Argb(bitmap);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (!rect.Contains(x, y)) Assert.Equal(before[y * W + x], after[y * W + x]);
            if (format == PixelFormat.Format32bppPArgb || format == PixelFormat.Format24bppRgb || format == PixelFormat.Format32bppRgb || Indexed(format))
                Assert.Equal(before, after); // lossless formats: writing back the bytes read changes nothing
        }

        // The caller's buffer (ImageLockMode.UserInputBuffer) with a negative stride: Scan0 is the top row, at the end of
        // the block, and each next row is |stride| bytes before it, as in a bottom-up DIB.
        [Theory]
        [MemberData(nameof(Formats))]
        public void LockBits_UserInputBuffer_NegativeStride(PixelFormat format)
        {
            using var bitmap = PatternIn(format);
            byte[] topDown = Bytes(bitmap, format);
            int stride = topDown.Length / H;
            IntPtr block = Marshal.AllocHGlobal(topDown.Length);
            try
            {
                var rect = new Rectangle(0, 0, W, H);
                var data = new BitmapData { Scan0 = block + (H - 1) * stride, Stride = -stride };
                bitmap.LockBits(rect, ImageLockMode.ReadOnly | ImageLockMode.UserInputBuffer, format, data);
                Assert.Equal(-stride, data.Stride);
                bitmap.UnlockBits(data);
                var bottomUp = new byte[topDown.Length];
                Marshal.Copy(block, bottomUp, 0, bottomUp.Length);
                int rowBytes = (W * Image.GetPixelFormatSize(format) + 7) / 8;
                for (int y = 0; y < H; y++)
                    Assert.Equal(topDown.AsSpan(y * stride, rowBytes).ToArray(), bottomUp.AsSpan((H - 1 - y) * stride, rowBytes).ToArray());

                // Writing: the same bottom-up block written into a new bitmap gives the same pixels as the top-down one.
                using var fromTopDown = new Bitmap(W, H, format);
                using var fromBottomUp = new Bitmap(W, H, format);
                if (Indexed(format)) { fromTopDown.Palette = bitmap.Palette; fromBottomUp.Palette = bitmap.Palette; }
                var d1 = fromTopDown.LockBits(rect, ImageLockMode.WriteOnly, format);
                Marshal.Copy(topDown, 0, d1.Scan0, topDown.Length);
                fromTopDown.UnlockBits(d1);
                var d2 = new BitmapData { Scan0 = block + (H - 1) * stride, Stride = -stride };
                fromBottomUp.LockBits(rect, ImageLockMode.WriteOnly | ImageLockMode.UserInputBuffer, format, d2);
                fromBottomUp.UnlockBits(d2);
                Assert.Equal(Argb(fromTopDown), Argb(fromBottomUp));
            }
            finally
            {
                Marshal.FreeHGlobal(block);
            }
        }

        // The caller's buffer with a wider stride than GDI+'s: the padding is left alone, except that an indexed format's
        // rows are cleared to the stride (the engine's behaviour).
        [Theory]
        [MemberData(nameof(Formats))]
        public void LockBits_UserInputBuffer_WiderStride(PixelFormat format)
        {
            using var bitmap = PatternIn(format);
            byte[] own = Bytes(bitmap, format);
            int ownStride = own.Length / H, stride = ownStride + 8;
            var block = Enumerable.Repeat((byte)0xAB, stride * H).ToArray();
            var handle = Marshal.AllocHGlobal(block.Length);
            try
            {
                Marshal.Copy(block, 0, handle, block.Length);
                var data = new BitmapData { Scan0 = handle, Stride = stride };
                bitmap.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly | ImageLockMode.UserInputBuffer, format, data);
                bitmap.UnlockBits(data);
                Marshal.Copy(handle, block, 0, block.Length);
                for (int y = 0; y < H; y++)
                {
                    int rowBytes = (W * Image.GetPixelFormatSize(format) + 7) / 8;
                    Assert.Equal(own.AsSpan(y * ownStride, rowBytes).ToArray(), block.AsSpan(y * stride, rowBytes).ToArray());
                    byte pad = Indexed(format) ? (byte)0 : (byte)0xAB;
                    Assert.All(block.AsSpan(y * stride + ownStride, 8).ToArray(), b => Assert.Equal(pad, b));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(handle);
            }
        }

        // Bitmap(width, height, stride, format, scan0) with a negative stride: scan0 is the top row.
        [Theory]
        [MemberData(nameof(Formats))]
        public void Scan0_NegativeStride_SameAsPositive(PixelFormat format)
        {
            if (Indexed(format)) return; // the indexed bitmap made from scan0 has the default palette, as the pattern's: still compared below
            using var bitmap = PatternIn(format);
            byte[] topDown = Bytes(bitmap, format);
            int stride = topDown.Length / H;
            var bottomUp = new byte[topDown.Length];
            for (int y = 0; y < H; y++) Array.Copy(topDown, y * stride, bottomUp, (H - 1 - y) * stride, stride);
            IntPtr a = Marshal.AllocHGlobal(topDown.Length), b = Marshal.AllocHGlobal(bottomUp.Length);
            try
            {
                Marshal.Copy(topDown, 0, a, topDown.Length);
                Marshal.Copy(bottomUp, 0, b, bottomUp.Length);
                using var positive = new Bitmap(W, H, stride, format, a);
                using var negative = new Bitmap(W, H, -stride, format, b + (H - 1) * stride);
                Assert.Equal(Argb(positive), Argb(negative));

            }
            finally
            {
                Marshal.FreeHGlobal(a);
                Marshal.FreeHGlobal(b);
            }
        }

        [Theory]
        [InlineData(PixelFormat.Format1bppIndexed)]
        [InlineData(PixelFormat.Format4bppIndexed)]
        [InlineData(PixelFormat.Format8bppIndexed)]
        public void Scan0_NegativeStride_Indexed(PixelFormat format)
        {
            using var bitmap = PatternIn(format);
            byte[] topDown = Bytes(bitmap, format);
            int stride = topDown.Length / H;
            var bottomUp = new byte[topDown.Length];
            for (int y = 0; y < H; y++) Array.Copy(topDown, y * stride, bottomUp, (H - 1 - y) * stride, stride);
            IntPtr a = Marshal.AllocHGlobal(topDown.Length), b = Marshal.AllocHGlobal(bottomUp.Length);
            try
            {
                Marshal.Copy(topDown, 0, a, topDown.Length);
                Marshal.Copy(bottomUp, 0, b, bottomUp.Length);
                using var positive = new Bitmap(W, H, stride, format, a);
                using var negative = new Bitmap(W, H, -stride, format, b + (H - 1) * stride);
                Assert.Equal(Bytes(positive, format), Bytes(negative, format));
                Assert.Equal(topDown, Bytes(positive, format));
            }
            finally
            {
                Marshal.FreeHGlobal(a);
                Marshal.FreeHGlobal(b);
            }
        }

        // Encoders: BMP and GIF are the engine's own (exact on every platform); recorded from the engine before #3's change.

        static byte[] Save(Image image, ImageFormat format, EncoderParameters parameters = null)
        {
            using var stream = new MemoryStream();
            if (parameters == null) image.Save(stream, format);
            else image.Save(stream, ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == format.Guid), parameters);
            return stream.ToArray();
        }

        [Theory]
        [InlineData(PixelFormat.Format1bppIndexed, "02254FF2283CFA50")]
        [InlineData(PixelFormat.Format4bppIndexed, "7BD3CC69236F3E9F")]
        [InlineData(PixelFormat.Format8bppIndexed, "BCB2436DF631086F")]
        [InlineData(PixelFormat.Format24bppRgb, "A8489E41336EBBDD")]
        [InlineData(PixelFormat.Format32bppArgb, "DDA0645B55E02F5C")]
        [InlineData(PixelFormat.Format32bppRgb, "DDA0645B55E02F5C")]
        [InlineData(PixelFormat.Format16bppRgb565, "A8489E41336EBBDD")]
        public void Bmp_Save_Recorded(PixelFormat format, string expected)
        {
            using var bitmap = PatternIn(format);
            byte[] bmp = Save(bitmap, ImageFormat.Bmp);
            Assert.Equal(expected, Sha(bmp));
            using var reloaded = new Bitmap(new MemoryStream(bmp));
            if (!Indexed(format) && format != PixelFormat.Format32bppArgb) Assert.Equal(Argb(bitmap), Argb(reloaded));
        }

        [Theory]
        [InlineData(PixelFormat.Format8bppIndexed, "98C80FF1A7BDCE5D")]
        [InlineData(PixelFormat.Format32bppArgb, "F7C74DDBF5FDD8E2")]
        [InlineData(PixelFormat.Format24bppRgb, "F7C74DDBF5FDD8E2")]
        public void Gif_Save_Recorded(PixelFormat format, string expected)
        {
            using var bitmap = PatternIn(format);
            Assert.Equal(expected, Sha(Save(bitmap, ImageFormat.Gif)));
        }

        // JPEG: Encoder.Quality is read from GDI+'s EncoderParameters block; 75 without one.
        [Fact]
        public void Jpeg_Quality_FromEncoderParameters()
        {
            using var bitmap = new Bitmap(64, 64, PixelFormat.Format24bppRgb);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    bitmap.SetPixel(x, y, Color.FromArgb((x * 37 + y * 11) & 255, (x * 5 + y * 73) & 255, (x * y * 29 + 7) & 255));

            static EncoderParameters Quality(long q, bool withOther = false)
            {
                var p = new EncoderParameters(withOther ? 2 : 1);
                int i = 0;
                if (withOther) p.Param[i++] = new EncoderParameter(Encoder.ColorDepth, 24L);
                p.Param[i] = new EncoderParameter(Encoder.Quality, q);
                return p;
            }

            byte[] none = Save(bitmap, ImageFormat.Jpeg);
            byte[] q75 = Save(bitmap, ImageFormat.Jpeg, Quality(75));
            byte[] q10 = Save(bitmap, ImageFormat.Jpeg, Quality(10));
            byte[] q90 = Save(bitmap, ImageFormat.Jpeg, Quality(90));
            byte[] q90Second = Save(bitmap, ImageFormat.Jpeg, Quality(90, withOther: true));
            byte[] q200 = Save(bitmap, ImageFormat.Jpeg, Quality(200));
            byte[] q100 = Save(bitmap, ImageFormat.Jpeg, Quality(100));
            byte[] other = Save(bitmap, ImageFormat.Jpeg, new EncoderParameters(1) { Param = { [0] = new EncoderParameter(Encoder.ColorDepth, 24L) } });
            Assert.Equal(none, q75);
            Assert.Equal(none, other);
            Assert.Equal(q90, q90Second);
            Assert.Equal(q100, q200); // clamped
            Assert.True(q10.Length < q75.Length && q75.Length < q90.Length, $"{q10.Length} {q75.Length} {q90.Length}");

            // The same through Save(string, ...): GdipSaveImageToFile reads the block too.
            string file = Path.GetTempFileName();
            try
            {
                bitmap.Save(file, ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid), Quality(10));
                Assert.Equal(q10, File.ReadAllBytes(file));
            }
            finally
            {
                File.Delete(file);
            }
        }

        // Path iterators copy into the caller's arrays.
        [Fact]
        public void PathIterator_EnumerateAndCopyData()
        {
            using var path = new GraphicsPath();
            path.AddLine(1, 2, 3, 4);
            path.AddRectangle(new Rectangle(5, 6, 7, 8));
            path.SetMarkers();
            path.AddEllipse(0, 0, 10, 10);
            using var iterator = new GraphicsPathIterator(path);
            var points = new PointF[path.PointCount + 3];
            var types = new byte[path.PointCount + 3];
            Assert.Equal(path.PointCount, iterator.Enumerate(ref points, ref types));
            Assert.Equal(path.PathPoints, points.Take(path.PointCount));
            Assert.Equal(path.PathTypes, types.Take(path.PointCount));

            var some = new PointF[4];
            var someTypes = new byte[4];
            Assert.Equal(4, iterator.CopyData(ref some, ref someTypes, 2, 5));
            Assert.Equal(path.PathPoints.Skip(2).Take(4), some);
            Assert.Equal(path.PathTypes.Skip(2).Take(4), someTypes);
            Assert.Equal(0, iterator.CopyData(ref some, ref someTypes, 5, 2));
            Assert.Equal(0, iterator.CopyData(ref some, ref someTypes, -1, 2));
            Assert.Equal(0, iterator.CopyData(ref some, ref someTypes, path.PointCount - 2, path.PointCount));

            var few = new PointF[2];
            var fewTypes = new byte[2];
            Assert.Equal(2, iterator.Enumerate(ref few, ref fewTypes));
            Assert.Equal(path.PathPoints.Take(2), few);
        }

        [Fact]
        public void Matrix_Elements()
        {
            using var matrix = new Matrix(1.5f, -2, 3.25f, 4, 5, -6);
            Assert.Equal(new[] { 1.5f, -2, 3.25f, 4, 5, -6 }, matrix.Elements);
            using var bitmap = new Bitmap(4, 4);
            using var g = Graphics.FromImage(bitmap);
            g.Transform = matrix;
            Assert.Equal(matrix.Elements, g.Transform.Elements);
        }
    }
}
