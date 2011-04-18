using System;
using System.Drawing;
using System.Runtime.InteropServices;

// http://jakubflorczyk.pl/index.php/2009/03/16/measurestring-w-net-compact-framework/comment-page-1/

internal static class GraphicExtentions
{
    private static Bitmap _workBitmap;
    private static Graphics _workGraphics;

    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public Rect(Rectangle r)
        {
            Left = r.Left;
            Top = r.Top;
            Bottom = r.Bottom;
            Right = r.Right;
        }
    }

    public static void DrawText(Graphics graphic, string text, Font font, Rectangle rectangle)
    {
        DrawText(graphic, text, font, Color.Black, rectangle);
    }

    public static void DrawText(Graphics graphic, string text, Font font, Color color, Rectangle rectangle)
    {
        DrawText(graphic, text, font, color, rectangle, DT_WORDBREAK);
    }

    public static void DrawText(Graphics graphic, string text, Font font, Color color, Rectangle rectangle, int format)
    {
#if PocketPC
        Rect bounds = new Rect(rectangle);
        IntPtr hFont = font.ToHfont();
        IntPtr hDc = graphic.GetHdc();
        uint drawColor = (uint)((color.B << 16) + (color.G << 8) + color.R);

        IntPtr originalObject = SelectObject(hDc, hFont);
        SetTextColor(hDc, drawColor);
        SetBkMode(hDc, TRANSPARENT);
        DrawText(hDc, text, text.Length, ref bounds, format);
        graphic.ReleaseHdc(hDc);
#else
        graphic.DrawString(text, font, new SolidBrush(color), rectangle);
#endif
    }

    public static SizeF MeasureString(string text, Font font, Rectangle rectangle)
    {
#if PocketPC
        IntPtr hWnd = GetDesktopWindow();
        IntPtr hDc = GetDC(hWnd);
        
        Rect bounds = new Rect(rectangle);
        IntPtr hFont = font.ToHfont();

        IntPtr originalObject = SelectObject(hDc, hFont);
        DrawText(hDc, text, text.Length, ref bounds, DT_CALCRECT | DT_WORDBREAK);
        SelectObject(hDc, originalObject);
        ReleaseDC(hWnd, hDc);

        return new SizeF(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
#else
        SizeF size = graphic.MeasureString(text, font, rectangle.Width, StringFormat.GenericDefault);
        return size;
#endif
    }

    /// <summary>
    /// 三角形を描画
    /// </summary>
    /// <param name="g">描画対象のグラフィック</param>
    /// <param name="x">三角形の中心座標 x</param>
    /// <param name="y">三角形の中心座標 y</param>
    /// <param name="radius">三角形の中心から頂点までの長さ</param>
    /// <param name="angle">三角形の傾き角度</param>
    public static void DrawTriangle(Graphics g, int x, int y, Pen pen, int radius, int angle)
    {
        Point[] points = new Point[3];

        int currentAngle = angle;
        for (int idx = 0; idx < 3; idx++)
        {
            int dx = (int)(x + Math.Sin(Math.PI * currentAngle / 180.0) * radius);
            int dy = (int)(y - Math.Cos(Math.PI * currentAngle / 180.0) * radius);
            points[idx] = new Point(dx, dy);
            currentAngle += 120;
        }

        g.DrawPolygon(pen, points);
    }

    /// <summary>
    /// 三角形を塗りつぶしで描画
    /// </summary>
    /// <param name="g">描画対象のグラフィック</param>
    /// <param name="x">三角形の中心座標 x</param>
    /// <param name="y">三角形の中心座標 y</param>
    /// <param name="radius">三角形の中心から頂点までの長さ</param>
    /// <param name="angle">三角形の傾き角度</param>
    public static void FillTriangle(Graphics g, int x, int y, Brush brush, int radius, int angle)
    {
        Point[] points = new Point[3];

        int currentAngle = angle;
        for (int idx = 0; idx < 3; idx++)
        {
            int dx = (int)(x + Math.Sin(Math.PI * currentAngle / 180.0) * radius);
            int dy = (int)(y - Math.Cos(Math.PI * currentAngle / 180.0) * radius);
            points[idx] = new Point(dx, dy);
            currentAngle += 120;
        }

        g.FillPolygon(brush, points);
    }

    public static int DT_END_ELLIPSIS = 0x000008000;
    public static int DT_EDITCONTROL = 0x000002000;
    public static int DT_CALCRECT = 0x000000400;
    public static int DT_WORDBREAK = 0x000000010;

    [DllImport("coredll.dll", EntryPoint = "GetDesktopWindow")]
    public static extern IntPtr GetDesktopWindow();

    [DllImport("coredll.dll", EntryPoint = "ReleaseDC")]
    public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("coredll.dll", EntryPoint = "GetDC")]
    public static extern IntPtr GetDC(IntPtr hWnd);
    
    [DllImport("coredll.dll", EntryPoint = "SelectObject")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    public static int TRANSPARENT = 1;
    [DllImport("coredll.dll", EntryPoint = "SetBkMode")]
    private static extern IntPtr SetBkMode(IntPtr hdc, int mode);

    [DllImport("coredll.dll", EntryPoint = "DrawText")]
    private static extern int DrawText(IntPtr hdc, string lpStr, int nCount, ref Rect lpRect, int wFormat);

    [DllImport("coredll.dll", EntryPoint = "SetTextColor")]
    private static extern IntPtr SetTextColor(IntPtr hdc, uint color);

    public static void FillRectangleAlpha(Graphics graphic, Brush brush, int left, int top, int width, int height, int transparency)
    {
        if (_workGraphics == null)
        {
            _workBitmap = new Bitmap(1024, 1024, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);
            _workGraphics = Graphics.FromImage(_workBitmap);
        }

        _workGraphics.FillRectangle(brush, 0, 0, width, height);

        PlatformAPIs.DrawAlpha(graphic, _workBitmap, (byte)transparency, left, top, width, height);
    }

    public struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }
    public enum BlendOperation : byte
    {
        AC_SRC_OVER = 0x00
    }

    public enum BlendFlags : byte
    {
        Zero = 0x00
    }

    public enum SourceConstantAlpha : byte
    {
        Transparent = 0x00,
        Opaque = 0xFF
    }

    public enum AlphaFormat : byte
    {
        AC_SRC_ALPHA = 0x01
    }

    public enum TernaryRasterOperations : uint
    {
        /// <summary>dest = source</summary>
        SRCCOPY = 0x00CC0020,
        /// <summary>dest = source OR dest</summary>
        SRCPAINT = 0x00EE0086,
        /// <summary>dest = source AND dest</summary>
        SRCAND = 0x008800C6,
        /// <summary>dest = source XOR dest</summary>
        SRCINVERT = 0x00660046,
        /// <summary>dest = source AND (NOT dest)</summary>
        SRCERASE = 0x00440328,
        /// <summary>dest = (NOT source)</summary>
        NOTSRCCOPY = 0x00330008,
        /// <summary>dest = (NOT src) AND (NOT dest)</summary>
        NOTSRCERASE = 0x001100A6,
        /// <summary>dest = (source AND pattern)</summary>
        MERGECOPY = 0x00C000CA,
        /// <summary>dest = (NOT source) OR dest</summary>
        MERGEPAINT = 0x00BB0226,
        /// <summary>dest = pattern</summary>
        PATCOPY = 0x00F00021,
        /// <summary>dest = DPSnoo</summary>
        PATPAINT = 0x00FB0A09,
        /// <summary>dest = pattern XOR dest</summary>
        PATINVERT = 0x005A0049,
        /// <summary>dest = (NOT dest)</summary>
        DSTINVERT = 0x00550009,
        /// <summary>dest = BLACK</summary>
        BLACKNESS = 0x00000042,
        /// <summary>dest = WHITE</summary>
        WHITENESS = 0x00FF0062
    }

    public class PlatformAPIs
    {
        [DllImport("coredll.dll")]
        extern public static Int32 AlphaBlend(IntPtr hdcDest, Int32 xDest, Int32 yDest, Int32 cxDest, Int32 cyDest, IntPtr hdcSrc, Int32 xSrc, Int32 ySrc, Int32 cxSrc, Int32 cySrc, BlendFunction blendFunction);

        [DllImport("coredll.dll")]
        public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth,
           int nHeight, IntPtr hObjSource, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

        public static void DrawAlpha(Graphics gx, Bitmap image, byte transp, int x, int y)
        {
            DrawAlpha(gx, image, transp, x, y, image.Width, image.Height);
        }

        public static void DrawAlpha(Graphics gx, Bitmap image, byte transp, int x, int y, int width, int height)
        {
            using (Graphics gxSrc = Graphics.FromImage(image))
            {
                IntPtr hdcDst = gx.GetHdc();
                IntPtr hdcSrc = gxSrc.GetHdc();
                BlendFunction blendFunction = new BlendFunction();
                blendFunction.BlendOp = (byte)BlendOperation.AC_SRC_OVER;   // Only supported blend operation
                blendFunction.BlendFlags = (byte)BlendFlags.Zero;           // Documentation says put 0 here
                blendFunction.SourceConstantAlpha = transp;// Constant alpha factor
                blendFunction.AlphaFormat = (byte)0;                        // Don't look for per pixel alpha
                PlatformAPIs.AlphaBlend(hdcDst, x, y, width, height, hdcSrc, 0, 0, width, height, blendFunction);
                gx.ReleaseHdc(hdcDst);    // Required cleanup to GetHdc()
                gxSrc.ReleaseHdc(hdcSrc);       // Required cleanup to GetHdc()
            }
        }
    }

}
