// drawingForCore's package smoke test: draws, saves and reloads images with System.Drawing, as a .NET Framework
// application would, and checks that the assemblies are drawingForCore's with Microsoft's identities.
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

var failures = new List<string>();
void Check(bool ok, string what) { Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {what}"); if (!ok) failures.Add(what); }

var common = typeof(Bitmap).Assembly.GetName();
var drawing = System.Reflection.Assembly.Load("System.Drawing").GetName();
Console.WriteLine($"{common.FullName}\n{drawing.FullName}");
Check(common.Name == "System.Drawing.Common" && common.Version == new Version(11, 0, 0, 0)
    && Convert.ToHexString(common.GetPublicKeyToken()!) == "CC7B13FFCD2DDD51", "System.Drawing.Common 11.0.0.0, cc7b13ffcd2ddd51");
Check(drawing.Name == "System.Drawing" && drawing.Version == new Version(13, 5, 0, 0)
    && Convert.ToHexString(drawing.GetPublicKeyToken()!) == "B03F5F7F11D50A3A", "System.Drawing 13.5.0.0, b03f5f7f11d50a3a");

var dir = Directory.CreateTempSubdirectory("drawingForCore-smoke").FullName;
using (var bitmap = new Bitmap(64, 48))
using (var g = Graphics.FromImage(bitmap))
{
    g.Clear(Color.White);
    g.SmoothingMode = SmoothingMode.None;
    g.FillRectangle(Brushes.Red, 8, 8, 16, 16);
    using var pen = new Pen(Color.Blue, 3);
    g.DrawLine(pen, 0, 40, 63, 40);
    using var font = new Font(FontFamily.GenericSansSerif, 10);
    g.DrawString("Hi", font, Brushes.Black, 30, 4);
    bitmap.Save(Path.Combine(dir, "smoke.png"), ImageFormat.Png);
    bitmap.Save(Path.Combine(dir, "smoke.jpg"), ImageFormat.Jpeg);
    bitmap.Save(Path.Combine(dir, "smoke.gif"), ImageFormat.Gif);
}

using (var png = (Bitmap)Image.FromFile(Path.Combine(dir, "smoke.png")))
{
    Check(png.Width == 64 && png.Height == 48 && png.RawFormat.Equals(ImageFormat.Png), "PNG reloads at 64x48");
    Check(png.GetPixel(12, 12).ToArgb() == Color.Red.ToArgb(), "filled rectangle is red");
    Check(png.GetPixel(32, 40).ToArgb() == Color.Blue.ToArgb(), "line is blue");
    Check(png.GetPixel(60, 2).ToArgb() == Color.White.ToArgb(), "background is white");
    Check(Enumerable.Range(30, 20).Any(x => Enumerable.Range(4, 16).Any(y => png.GetPixel(x, y).R < 128)), "text is drawn");
}
using (var jpg = Image.FromFile(Path.Combine(dir, "smoke.jpg")))
    Check(jpg.RawFormat.Equals(ImageFormat.Jpeg) && jpg.PixelFormat == PixelFormat.Format24bppRgb, "JPEG reloads as 24bpp, as GDI+");
using (var gif = Image.FromFile(Path.Combine(dir, "smoke.gif")))
    Check(gif.RawFormat.Equals(ImageFormat.Gif) && gif.PixelFormat == PixelFormat.Format8bppIndexed, "GIF reloads as 8bpp indexed, as GDI+");

// .NET Framework code names GDI+'s types in System.Drawing, which forwards them to System.Drawing.Common.
Check(Type.GetType("System.Drawing.Bitmap, System.Drawing", throwOnError: false) == typeof(Bitmap), "System.Drawing forwards Bitmap to System.Drawing.Common");
Check(Type.GetType("System.Drawing.FontConverter, System.Drawing", throwOnError: false) == typeof(FontConverter), "System.Drawing forwards FontConverter to System.Drawing.Common");
Check(Type.GetType("System.Drawing.FontConverter+FontNameConverter, System.Drawing", throwOnError: false) == typeof(FontConverter.FontNameConverter), "... and its nested types");

// FontConverter, named in code (System.Drawing defined its own copy, and this didn't compile: CS0433) and through
// TypeDescriptor, as designers and configuration use it.
var converted = new FontConverter().ConvertFromInvariantString("Arial, 12pt, style=Bold") as Font;
Check(converted is { Size: 12, Bold: true }, "FontConverter parses \"Arial, 12pt, style=Bold\"");
Check(TypeDescriptor.GetConverter(typeof(Font)) is FontConverter, "Font's TypeConverter is FontConverter");

Directory.Delete(dir, true);
Console.WriteLine(failures.Count == 0 ? "Package smoke test passed." : $"{failures.Count} checks failed.");
return failures.Count == 0 ? 0 : 1;
