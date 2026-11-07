using System.Runtime.InteropServices;

namespace Doorstop;

// Draw Unicode into our own DIB with Windows UI fonts. No game fonts or Unity objects are touched.
internal sealed class MenuBitmap : IDisposable
{
    private readonly int _width, _height;
    private readonly bool _bgra;
    private readonly string _fontFamily;
    private readonly Dictionary<int, IntPtr> _fonts = new();
    private IntPtr _dc, _bitmap, _previousBitmap, _bits;

    internal MenuBitmap(int width, int height, bool bgra, string fontFamily = "Segoe UI")
    {
        _width = width; _height = height; _bgra = bgra; _fontFamily = fontFamily;
        try
        {
            _dc = CreateCompatibleDC(IntPtr.Zero);
            if (_dc == IntPtr.Zero) throw new InvalidOperationException("Create menu drawing context failed.");
            BitmapInfoHeader info = new()
            {
                Size = 40, Width = width, Height = -height, Planes = 1, BitCount = 32,
                SizeImage = checked((uint)(width * height * 4))
            };
            _bitmap = CreateDIBSection(_dc, ref info, 0, out _bits, IntPtr.Zero, 0);
            if (_bitmap == IntPtr.Zero || _bits == IntPtr.Zero) throw new InvalidOperationException("Create menu bitmap failed.");
            _previousBitmap = SelectObject(_dc, _bitmap);
            if (_previousBitmap == IntPtr.Zero || _previousBitmap == new IntPtr(-1))
                throw new InvalidOperationException("Select menu bitmap failed.");
            _ = SetBkMode(_dc, 1); // Transparent text background.
        }
        catch { Dispose(); throw; }
    }

    internal byte[] Pixels
    {
        get
        {
            if (!GdiFlush()) throw new InvalidOperationException("Menu text drawing did not complete.");
            byte[] pixels = new byte[checked(_width * _height * 4)];
            Marshal.Copy(_bits, pixels, 0, pixels.Length);
            for (int offset = 0; offset < pixels.Length; offset += 4)
            {
                if (!_bgra) (pixels[offset], pixels[offset + 2]) = (pixels[offset + 2], pixels[offset]);
                pixels[offset + 3] = 255; // GDI does not maintain the alpha channel.
            }
            return pixels;
        }
    }

    internal void Rect(int x, int y, int width, int height, uint rgb)
    {
        NativeRect rectangle = new() { Left = Math.Max(0, x), Top = Math.Max(0, y), Right = Math.Min(_width, x + width), Bottom = Math.Min(_height, y + height) };
        IntPtr brush = CreateSolidBrush(ColorRef(rgb));
        if (brush == IntPtr.Zero) throw new InvalidOperationException("Create menu brush failed.");
        try { if (FillRect(_dc, ref rectangle, brush) == 0) throw new InvalidOperationException("Fill menu bitmap failed."); }
        finally { _ = DeleteObject(brush); }
    }

    internal void Text(string text, int x, int y, int scale = 2, uint color = 0xe7edf7, int maxCharacters = 100)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (!_fonts.TryGetValue(scale, out IntPtr font))
        {
            font = CreateFontW(-Math.Max(10, scale * 9), 0, 0, 0, scale == 3 ? 600 : 400,
                0, 0, 0, 1, 0, 0, 4, 0, _fontFamily);
            if (font == IntPtr.Zero) throw new InvalidOperationException("Create menu font failed.");
            _fonts.Add(scale, font);
        }
        IntPtr previousFont = SelectObject(_dc, font);
        if (previousFont == IntPtr.Zero || previousFont == new IntPtr(-1)) throw new InvalidOperationException("Select menu font failed.");
        try
        {
            _ = SetTextColor(_dc, ColorRef(color));
            NativeRect rectangle = new()
            {
                Left = x, Top = y, Right = Math.Min(_width - 12, x + maxCharacters * 6 * scale),
                Bottom = Math.Min(_height, y + Math.Max(14, scale * 10 + 3))
            };
            if (DrawTextW(_dc, text, text.Length, ref rectangle, 0x20 | 0x800 | 0x8000) == 0)
                throw new InvalidOperationException("Draw menu text failed."); // Single line, no prefix, end ellipsis.
        }
        finally { _ = SelectObject(_dc, previousFont); }
    }

    private static uint ColorRef(uint rgb) => ((rgb & 255) << 16) | (rgb & 0xff00) | ((rgb >> 16) & 255);
    public void Dispose()
    {
        if (_dc != IntPtr.Zero && _previousBitmap != IntPtr.Zero && _previousBitmap != new IntPtr(-1)) _ = SelectObject(_dc, _previousBitmap);
        foreach (IntPtr font in _fonts.Values) _ = DeleteObject(font);
        _fonts.Clear();
        if (_bitmap != IntPtr.Zero) { _ = DeleteObject(_bitmap); _bitmap = IntPtr.Zero; }
        if (_dc != IntPtr.Zero) { _ = DeleteDC(_dc); _dc = IntPtr.Zero; }
        _bits = IntPtr.Zero; _previousBitmap = IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfoHeader
    {
        public uint Size; public int Width, Height; public ushort Planes, BitCount;
        public uint Compression, SizeImage; public int XPelsPerMeter, YPelsPerMeter; public uint ClrUsed, ClrImportant;
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateDIBSection(IntPtr dc, ref BitmapInfoHeader info, uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateSolidBrush(uint color);
    [DllImport("gdi32.dll")] private static extern int SetBkMode(IntPtr dc, int mode);
    [DllImport("gdi32.dll")] private static extern uint SetTextColor(IntPtr dc, uint color);
    [DllImport("gdi32.dll")] private static extern bool GdiFlush();
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr CreateFontW(int height, int width, int escapement, int orientation, int weight,
        uint italic, uint underline, uint strikeOut, uint charSet, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string face);
    [DllImport("user32.dll")] private static extern int FillRect(IntPtr dc, ref NativeRect rectangle, IntPtr brush);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int DrawTextW(IntPtr dc, string text, int count, ref NativeRect rectangle, uint format);
}
