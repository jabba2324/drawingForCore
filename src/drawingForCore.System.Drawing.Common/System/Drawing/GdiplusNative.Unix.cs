// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal static partial class SafeNativeMethods
    {
        internal unsafe partial class Gdip
        {
            internal const string LibraryName = "libgdiplus";
            public static IntPtr Display = IntPtr.Zero;

            // Indicates whether X11 is available. It's available on Linux but not on recent macOS versions
            // When set to false, where Carbon Drawing is used instead.
            // macOS users can force X11 by setting the SYSTEM_DRAWING_COMMON_FORCE_X11 flag.
            public static bool UseX11Drawable { get; } =
                !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                Environment.GetEnvironmentVariable("SYSTEM_DRAWING_COMMON_FORCE_X11") != null;

            internal static IntPtr LoadNativeLibrary()
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                IntPtr lib = IntPtr.Zero;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (!NativeLibrary.TryLoad("libgdiplus.dylib", assembly, default, out lib))
                    {
                        // homebrew install location
                        if (!NativeLibrary.TryLoad("/usr/local/lib/libgdiplus.dylib", assembly, default, out lib))
                        {
                            // macports install location
                            NativeLibrary.TryLoad("/opt/local/lib/libgdiplus.dylib", assembly, default, out lib);
                        }
                    }
                }
                else
                {
                    // Various Unix package managers have chosen different names for the "libgdiplus" shared library.
                    // The mono project, where libgdiplus originated, allowed both of the names below to be used, via
                    // a global configuration setting. We prefer the "unversioned" shared object name, and fallback to
                    // the name suffixed with ".0".
                    if (!NativeLibrary.TryLoad("libgdiplus.so", assembly, default, out lib))
                    {
                        NativeLibrary.TryLoad("libgdiplus.so.0", assembly, default, out lib);
                    }
                }

                // This function may return a null handle. If it does, individual functions loaded from it will throw a DllNotFoundException,
                // but not until an attempt is made to actually use the function (rather than load it). This matches how PInvokes behave.
                return lib;
            }

            private static void PlatformInitialize()
            {
                // drawingForCore: GDI+ is SkiaGdiPlus (managed, on SkiaSharp); no native library to resolve.
            }

            // Imported functions

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdiplusStartup(out IntPtr token, ref StartupInput input, out StartupOutput output) => SkiaGdiPlus.GdiplusStartup(out token, ref input, out output);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static void GdiplusShutdown(ref ulong token) => SkiaGdiPlus.GdiplusShutdown(ref token);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static IntPtr GdipAlloc(int size) => SkiaGdiPlus.GdipAlloc(size);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static void GdipFree(IntPtr ptr) => SkiaGdiPlus.GdipFree(ptr);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipDeleteBrush(HandleRef brush) => SkiaGdiPlus.GdipDeleteBrush(brush);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetBrushType(IntPtr brush, out BrushType type) => SkiaGdiPlus.GdipGetBrushType(brush, out type);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipDeleteGraphics(HandleRef graphics) => SkiaGdiPlus.GdipDeleteGraphics(graphics);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipRestoreGraphics(IntPtr graphics, uint graphicsState) => SkiaGdiPlus.GdipRestoreGraphics(graphics, graphicsState);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipReleaseDC(HandleRef graphics, HandleRef hdc) => SkiaGdiPlus.GdipReleaseDC(graphics, hdc);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipFillPath(IntPtr graphics, IntPtr brush, IntPtr path) => SkiaGdiPlus.GdipFillPath(graphics, brush, path);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetNearestColor(IntPtr graphics, out int argb) => SkiaGdiPlus.GdipGetNearestColor(graphics, out argb);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathString(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref RectangleF layoutRect, IntPtr format) => SkiaGdiPlus.GdipAddPathString(path, s, lenght, family, style, emSize, ref layoutRect, format);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathStringI(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref Rectangle layoutRect, IntPtr format) => SkiaGdiPlus.GdipAddPathStringI(path, s, lenght, family, style, emSize, ref layoutRect, format);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics) => SkiaGdiPlus.GdipCreateFromHWND(hwnd, out graphics);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipCloneImage(IntPtr image, out IntPtr imageclone) => SkiaGdiPlus.GdipCloneImage(image, out imageclone);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetImagePaletteSize(IntPtr image, out int size) => SkiaGdiPlus.GdipGetImagePaletteSize(image, out size);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetImagePalette(IntPtr image, IntPtr palette, int size) => SkiaGdiPlus.GdipGetImagePalette(image, palette, size);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipSetImagePalette(IntPtr image, IntPtr palette) => SkiaGdiPlus.GdipSetImagePalette(image, palette);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetImageBounds(IntPtr image, out RectangleF source, ref GraphicsUnit unit) => SkiaGdiPlus.GdipGetImageBounds(image, out source, ref unit);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetImageThumbnail(IntPtr image, uint width, uint height, out IntPtr thumbImage, IntPtr callback, IntPtr callBackData) => SkiaGdiPlus.GdipGetImageThumbnail(image, width, height, out thumbImage, callback, callBackData);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipSaveImageToFile(IntPtr image, string filename, ref Guid encoderClsID, IntPtr encoderParameters) => SkiaGdiPlus.GdipSaveImageToFile(image, filename, ref encoderClsID, encoderParameters);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipSaveAdd(IntPtr image, IntPtr encoderParameters) => SkiaGdiPlus.GdipSaveAdd(image, encoderParameters);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipSaveAddImage(IntPtr image, IntPtr imagenew, IntPtr encoderParameters) => SkiaGdiPlus.GdipSaveAddImage(image, imagenew, encoderParameters);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetImageGraphicsContext(IntPtr image, out IntPtr graphics) => SkiaGdiPlus.GdipGetImageGraphicsContext(image, out graphics);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipCreatePath(FillMode brushMode, out IntPtr path) => SkiaGdiPlus.GdipCreatePath(brushMode, out path);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipCreatePath2(PointF[] points, byte[] types, int count, FillMode brushMode, out IntPtr path) => SkiaGdiPlus.GdipCreatePath2(points, types, count, brushMode, out path);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipCreatePath2I(Point[] points, byte[] types, int count, FillMode brushMode, out IntPtr path) => SkiaGdiPlus.GdipCreatePath2I(points, types, count, brushMode, out path);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipClonePath(IntPtr path, out IntPtr clonePath) => SkiaGdiPlus.GdipClonePath(path, out clonePath);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipDeletePath(HandleRef path) => SkiaGdiPlus.GdipDeletePath(path);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipResetPath(IntPtr path) => SkiaGdiPlus.GdipResetPath(path);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetPointCount(IntPtr path, out int count) => SkiaGdiPlus.GdipGetPointCount(path, out count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetPathTypes(IntPtr path, [Out] byte[] types, int count) => SkiaGdiPlus.GdipGetPathTypes(path, types, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetPathPoints(IntPtr path, [Out] PointF[] points, int count) => SkiaGdiPlus.GdipGetPathPoints(path, points, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetPathPointsI(IntPtr path, [Out] Point[] points, int count) => SkiaGdiPlus.GdipGetPathPointsI(path, points, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetPathFillMode(IntPtr path, out FillMode fillMode) => SkiaGdiPlus.GdipGetPathFillMode(path, out fillMode);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipSetPathFillMode(IntPtr path, FillMode fillMode) => SkiaGdiPlus.GdipSetPathFillMode(path, fillMode);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipStartPathFigure(IntPtr path) => SkiaGdiPlus.GdipStartPathFigure(path);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipClosePathFigure(IntPtr path) => SkiaGdiPlus.GdipClosePathFigure(path);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipClosePathFigures(IntPtr path) => SkiaGdiPlus.GdipClosePathFigures(path);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipSetPathMarker(IntPtr path) => SkiaGdiPlus.GdipSetPathMarker(path);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipClearPathMarkers(IntPtr path) => SkiaGdiPlus.GdipClearPathMarkers(path);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipReversePath(IntPtr path) => SkiaGdiPlus.GdipReversePath(path);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetPathLastPoint(IntPtr path, out PointF lastPoint) => SkiaGdiPlus.GdipGetPathLastPoint(path, out lastPoint);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathLine(IntPtr path, float x1, float y1, float x2, float y2) => SkiaGdiPlus.GdipAddPathLine(path, x1, y1, x2, y2);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathLine2(IntPtr path, PointF[] points, int count) => SkiaGdiPlus.GdipAddPathLine2(path, points, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathLine2I(IntPtr path, Point[] points, int count) => SkiaGdiPlus.GdipAddPathLine2I(path, points, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathArc(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle) => SkiaGdiPlus.GdipAddPathArc(path, x, y, width, height, startAngle, sweepAngle);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathBezier(IntPtr path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) => SkiaGdiPlus.GdipAddPathBezier(path, x1, y1, x2, y2, x3, y3, x4, y4);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathBeziers(IntPtr path, PointF[] points, int count) => SkiaGdiPlus.GdipAddPathBeziers(path, points, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathCurve(IntPtr path, PointF[] points, int count) => SkiaGdiPlus.GdipAddPathCurve(path, points, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathCurveI(IntPtr path, Point[] points, int count) => SkiaGdiPlus.GdipAddPathCurveI(path, points, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathCurve2(IntPtr path, PointF[] points, int count, float tension) => SkiaGdiPlus.GdipAddPathCurve2(path, points, count, tension);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathCurve2I(IntPtr path, Point[] points, int count, float tension) => SkiaGdiPlus.GdipAddPathCurve2I(path, points, count, tension);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathCurve3(IntPtr path, PointF[] points, int count, int offset, int numberOfSegments, float tension) => SkiaGdiPlus.GdipAddPathCurve3(path, points, count, offset, numberOfSegments, tension);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathCurve3I(IntPtr path, Point[] points, int count, int offset, int numberOfSegments, float tension) => SkiaGdiPlus.GdipAddPathCurve3I(path, points, count, offset, numberOfSegments, tension);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathClosedCurve(IntPtr path, PointF[] points, int count) => SkiaGdiPlus.GdipAddPathClosedCurve(path, points, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathClosedCurveI(IntPtr path, Point[] points, int count) => SkiaGdiPlus.GdipAddPathClosedCurveI(path, points, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathClosedCurve2(IntPtr path, PointF[] points, int count, float tension) => SkiaGdiPlus.GdipAddPathClosedCurve2(path, points, count, tension);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathClosedCurve2I(IntPtr path, Point[] points, int count, float tension) => SkiaGdiPlus.GdipAddPathClosedCurve2I(path, points, count, tension);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathRectangle(IntPtr path, float x, float y, float width, float height) => SkiaGdiPlus.GdipAddPathRectangle(path, x, y, width, height);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathRectangles(IntPtr path, RectangleF[] rects, int count) => SkiaGdiPlus.GdipAddPathRectangles(path, rects, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathEllipse(IntPtr path, float x, float y, float width, float height) => SkiaGdiPlus.GdipAddPathEllipse(path, x, y, width, height);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathEllipseI(IntPtr path, int x, int y, int width, int height) => SkiaGdiPlus.GdipAddPathEllipseI(path, x, y, width, height);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathPie(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle) => SkiaGdiPlus.GdipAddPathPie(path, x, y, width, height, startAngle, sweepAngle);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathPieI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle) => SkiaGdiPlus.GdipAddPathPieI(path, x, y, width, height, startAngle, sweepAngle);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathPolygon(IntPtr path, PointF[] points, int count) => SkiaGdiPlus.GdipAddPathPolygon(path, points, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathPath(IntPtr path, IntPtr addingPath, bool connect) => SkiaGdiPlus.GdipAddPathPath(path, addingPath, connect);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathLineI(IntPtr path, int x1, int y1, int x2, int y2) => SkiaGdiPlus.GdipAddPathLineI(path, x1, y1, x2, y2);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathArcI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle) => SkiaGdiPlus.GdipAddPathArcI(path, x, y, width, height, startAngle, sweepAngle);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathBezierI(IntPtr path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4) => SkiaGdiPlus.GdipAddPathBezierI(path, x1, y1, x2, y2, x3, y3, x4, y4);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathBeziersI(IntPtr path, Point[] points, int count) => SkiaGdiPlus.GdipAddPathBeziersI(path, points, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathPolygonI(IntPtr path, Point[] points, int count) => SkiaGdiPlus.GdipAddPathPolygonI(path, points, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathRectangleI(IntPtr path, int x, int y, int width, int height) => SkiaGdiPlus.GdipAddPathRectangleI(path, x, y, width, height);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipAddPathRectanglesI(IntPtr path, Rectangle[] rects, int count) => SkiaGdiPlus.GdipAddPathRectanglesI(path, rects, count);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipFlattenPath(IntPtr path, IntPtr matrix, float floatness) => SkiaGdiPlus.GdipFlattenPath(path, matrix, floatness);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipTransformPath(IntPtr path, IntPtr matrix) => SkiaGdiPlus.GdipTransformPath(path, matrix);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipWarpPath(IntPtr path, IntPtr matrix, PointF[] points, int count, float srcx, float srcy, float srcwidth, float srcheight, WarpMode mode, float flatness) => SkiaGdiPlus.GdipWarpPath(path, matrix, points, count, srcx, srcy, srcwidth, srcheight, mode, flatness);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipWidenPath(IntPtr path, IntPtr pen, IntPtr matrix, float flatness) => SkiaGdiPlus.GdipWidenPath(path, pen, matrix, flatness);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetPathWorldBounds(IntPtr path, out RectangleF bounds, IntPtr matrix, IntPtr pen) => SkiaGdiPlus.GdipGetPathWorldBounds(path, out bounds, matrix, pen);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetPathWorldBoundsI(IntPtr path, out Rectangle bounds, IntPtr matrix, IntPtr pen) => SkiaGdiPlus.GdipGetPathWorldBoundsI(path, out bounds, matrix, pen);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipIsVisiblePathPoint(IntPtr path, float x, float y, IntPtr graphics, out bool result) => SkiaGdiPlus.GdipIsVisiblePathPoint(path, x, y, graphics, out result);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipIsVisiblePathPointI(IntPtr path, int x, int y, IntPtr graphics, out bool result) => SkiaGdiPlus.GdipIsVisiblePathPointI(path, x, y, graphics, out result);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipIsOutlineVisiblePathPoint(IntPtr path, float x, float y, IntPtr pen, IntPtr graphics, out bool result) => SkiaGdiPlus.GdipIsOutlineVisiblePathPoint(path, x, y, pen, graphics, out result);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipIsOutlineVisiblePathPointI(IntPtr path, int x, int y, IntPtr pen, IntPtr graphics, out bool result) => SkiaGdiPlus.GdipIsOutlineVisiblePathPointI(path, x, y, pen, graphics, out result);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipCreateFontFromLogfont(IntPtr hdc, ref Interop.User32.LOGFONT lf, out IntPtr ptr) => SkiaGdiPlus.GdipCreateFontFromLogfont(hdc, ref lf, out ptr);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipCreateFontFromHfont(IntPtr hdc, out IntPtr font, ref Interop.User32.LOGFONT lf) => SkiaGdiPlus.GdipCreateFontFromHfont(hdc, out font, ref lf);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetMetafileHeaderFromFile(string filename, IntPtr header) => SkiaGdiPlus.GdipGetMetafileHeaderFromFile(filename, header);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetMetafileHeaderFromMetafile(IntPtr metafile, IntPtr header) => SkiaGdiPlus.GdipGetMetafileHeaderFromMetafile(metafile, header);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetMetafileHeaderFromEmf(IntPtr hEmf, IntPtr header) => SkiaGdiPlus.GdipGetMetafileHeaderFromEmf(hEmf, header);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetMetafileHeaderFromWmf(IntPtr hWmf, IntPtr wmfPlaceableFileHeader, IntPtr header) => SkiaGdiPlus.GdipGetMetafileHeaderFromWmf(hWmf, wmfPlaceableFileHeader, header);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetHemfFromMetafile(IntPtr metafile, out IntPtr hEmf) => SkiaGdiPlus.GdipGetHemfFromMetafile(metafile, out hEmf);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetMetafileDownLevelRasterizationLimit(IntPtr metafile, ref uint metafileRasterizationLimitDpi) => SkiaGdiPlus.GdipGetMetafileDownLevelRasterizationLimit(metafile, ref metafileRasterizationLimitDpi);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipSetMetafileDownLevelRasterizationLimit(IntPtr metafile, uint metafileRasterizationLimitDpi) => SkiaGdiPlus.GdipSetMetafileDownLevelRasterizationLimit(metafile, metafileRasterizationLimitDpi);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipCreateFromContext_macosx(IntPtr cgref, int width, int height, out IntPtr graphics) => SkiaGdiPlus.GdipCreateFromContext_macosx(cgref, width, height, out graphics);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipSetVisibleClip_linux(IntPtr graphics, ref Rectangle rect) => SkiaGdiPlus.GdipSetVisibleClip_linux(graphics, ref rect);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipCreateFromXDrawable_linux(IntPtr drawable, IntPtr display, out IntPtr graphics) => SkiaGdiPlus.GdipCreateFromXDrawable_linux(drawable, display, out graphics);

            // Stream functions for non-Win32 (libgdiplus specific)
            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipLoadImageFromDelegate_linux(StreamGetHeaderDelegate getHeader, StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr image) => SkiaGdiPlus.GdipLoadImageFromDelegate_linux(getHeader, getBytes, putBytes, doSeek, close, size, out image);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipSaveImageToDelegate_linux(IntPtr image, StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close, StreamSizeDelegate size, ref Guid encoderClsID, IntPtr encoderParameters) => SkiaGdiPlus.GdipSaveImageToDelegate_linux(image, getBytes, putBytes, doSeek, close, size, ref encoderClsID, encoderParameters);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipCreateMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader, StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr metafile) => SkiaGdiPlus.GdipCreateMetafileFromDelegate_linux(getHeader, getBytes, putBytes, doSeek, close, size, out metafile);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetMetafileHeaderFromDelegate_linux(StreamGetHeaderDelegate getHeader, StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close, StreamSizeDelegate size, IntPtr header) => SkiaGdiPlus.GdipGetMetafileHeaderFromDelegate_linux(getHeader, getBytes, putBytes, doSeek, close, size, header);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipRecordMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader, StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile) => SkiaGdiPlus.GdipRecordMetafileFromDelegate_linux(getHeader, getBytes, putBytes, doSeek, close, size, hdc, type, ref frameRect, frameUnit, description, out metafile);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipRecordMetafileFromDelegateI_linux(StreamGetHeaderDelegate getHeader, StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string? description, out IntPtr metafile) => SkiaGdiPlus.GdipRecordMetafileFromDelegateI_linux(getHeader, getBytes, putBytes, doSeek, close, size, hdc, type, ref frameRect, frameUnit, description, out metafile);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetPostScriptGraphicsContext([MarshalAs(UnmanagedType.LPStr)] string filename, int width, int height, double dpix, double dpiy, ref IntPtr graphics) => SkiaGdiPlus.GdipGetPostScriptGraphicsContext(filename, width, height, dpix, dpiy, ref graphics);

            // drawingForCore: was [DllImport(LibraryName)] (libgdiplus); GDI+ on SkiaSharp (SkiaGdiPlus).
            internal static int GdipGetPostScriptSavePage(IntPtr graphics) => SkiaGdiPlus.GdipGetPostScriptSavePage(graphics);
        }
    }

    // These are unix-only
    internal unsafe delegate int StreamGetHeaderDelegate(byte* buf, int bufsz);
    internal unsafe delegate int StreamGetBytesDelegate(byte* buf, int bufsz, bool peek);
    internal delegate long StreamSeekDelegate(int offset, int whence);
    internal unsafe delegate int StreamPutBytesDelegate(byte* buf, int bufsz);
    internal delegate void StreamCloseDelegate();
    internal delegate long StreamSizeDelegate();
}
