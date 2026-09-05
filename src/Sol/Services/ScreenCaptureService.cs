using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Sol.Models;
using Windows.Foundation;

namespace Sol.Services;

/// <summary>
/// High-performance screen capture service using Win32 GDI DIBSection into 32bpp BGRA BMP byte buffers.
/// </summary>
public class ScreenCaptureService : IScreenCaptureService
{
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    private const int SRCCOPY = 0x00CC0020;
    private const int CAPTUREBLT = 0x40000000;
    private const int DIB_RGB_COLORS = 0;
    private const int BI_RGB = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public int bmiColors;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateDIBSection(
        IntPtr hdc,
        ref BITMAPINFO pbmi,
        uint usage,
        out IntPtr ppvBits,
        IntPtr hSection,
        uint offset);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(
        IntPtr hdcDest,
        int nXDest,
        int nYDest,
        int nWidth,
        int nHeight,
        IntPtr hdcSrc,
        int nXSrc,
        int nYSrc,
        int dwRop);

    public ScreenBounds GetVirtualScreenBounds()
    {
        int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        if (width <= 0 || height <= 0)
        {
            width = 1920;
            height = 1080;
            x = 0;
            y = 0;
        }

        return new ScreenBounds(x, y, width, height);
    }

    public byte[] CaptureRegion(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return [];
        }

        IntPtr hdcScreen = GetDC(IntPtr.Zero);
        if (hdcScreen == IntPtr.Zero)
        {
            return [];
        }

        IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
        if (hdcMem == IntPtr.Zero)
        {
            ReleaseDC(IntPtr.Zero, hdcScreen);
            return [];
        }

        BITMAPINFO bmi = new()
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height, // top-down DIB
                biPlanes = 1,
                biBitCount = 32,
                biCompression = BI_RGB,
                biSizeImage = width * height * 4
            }
        };

        IntPtr hBitmap = CreateDIBSection(hdcMem, ref bmi, DIB_RGB_COLORS, out IntPtr ppvBits, IntPtr.Zero, 0);
        if (hBitmap == IntPtr.Zero || ppvBits == IntPtr.Zero)
        {
            if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);
            return [];
        }

        IntPtr hOldBitmap = SelectObject(hdcMem, hBitmap);
        try
        {
            BitBlt(hdcMem, 0, 0, width, height, hdcScreen, x, y, SRCCOPY | CAPTUREBLT);

            int pixelByteCount = width * height * 4;
            byte[] rawPixels = new byte[pixelByteCount];
            Marshal.Copy(ppvBits, rawPixels, 0, pixelByteCount);

            return CreateBmpFromPixels(rawPixels, width, height);
        }
        finally
        {
            SelectObject(hdcMem, hOldBitmap);
            DeleteObject(hBitmap);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);
        }
    }

    public byte[] CreateBmpFromPixels(byte[] bgraPixels, int width, int height)
    {
        int imageSize = width * height * 4;
        int totalSize = 54 + imageSize;
        byte[] bmp = new byte[totalSize];

        // BITMAPFILEHEADER (14 bytes)
        bmp[0] = 0x42; // 'B'
        bmp[1] = 0x4D; // 'M'
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2, 4), totalSize);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10, 4), 54); // offBits

        // BITMAPINFOHEADER (40 bytes)
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14, 4), 40); // biSize
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18, 4), width);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22, 4), -height); // negative for top-down
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(26, 2), 1); // biPlanes
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(28, 2), 32); // biBitCount
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(30, 4), 0); // BI_RGB
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(34, 4), imageSize);

        // Copy pixels
        Buffer.BlockCopy(bgraPixels, 0, bmp, 54, Math.Min(imageSize, bgraPixels.Length));
        return bmp;
    }

    public byte[] CropBmp(byte[] bmpBytes, int cropX, int cropY, int cropWidth, int cropHeight)
    {
        if (bmpBytes.Length < 54)
        {
            return [];
        }

        int srcWidth = BinaryPrimitives.ReadInt32LittleEndian(bmpBytes.AsSpan(18, 4));
        int srcHeight = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(bmpBytes.AsSpan(22, 4)));
        int pixelOffset = BinaryPrimitives.ReadInt32LittleEndian(bmpBytes.AsSpan(10, 4));

        if (srcWidth <= 0 || srcHeight <= 0 || pixelOffset >= bmpBytes.Length)
        {
            return [];
        }

        int clampedX = Math.Clamp(cropX, 0, srcWidth);
        int clampedY = Math.Clamp(cropY, 0, srcHeight);
        int clampedW = Math.Clamp(cropWidth, 1, srcWidth - clampedX);
        int clampedH = Math.Clamp(cropHeight, 1, srcHeight - clampedY);

        byte[] croppedPixels = new byte[clampedW * clampedH * 4];

        for (int row = 0; row < clampedH; row++)
        {
            int srcRowOffset = pixelOffset + ((clampedY + row) * srcWidth + clampedX) * 4;
            int dstRowOffset = row * clampedW * 4;

            int bytesToCopy = Math.Min(clampedW * 4, bmpBytes.Length - srcRowOffset);
            if (bytesToCopy > 0)
            {
                Buffer.BlockCopy(bmpBytes, srcRowOffset, croppedPixels, dstRowOffset, bytesToCopy);
            }
        }

        return CreateBmpFromPixels(croppedPixels, clampedW, clampedH);
    }

    public byte[] PadBmp(byte[] bmpBytes, int minWidth = 64, int minHeight = 64)
    {
        if (bmpBytes.Length < 54)
        {
            return bmpBytes;
        }

        int srcWidth = BinaryPrimitives.ReadInt32LittleEndian(bmpBytes.AsSpan(18, 4));
        int srcHeight = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(bmpBytes.AsSpan(22, 4)));
        int pixelOffset = BinaryPrimitives.ReadInt32LittleEndian(bmpBytes.AsSpan(10, 4));

        if (srcWidth >= minWidth && srcHeight >= minHeight)
        {
            return bmpBytes;
        }

        int targetWidth = Math.Max(srcWidth + 16, minWidth + 16);
        int targetHeight = Math.Max(srcHeight + 16, minHeight + 16);

        byte[] paddedPixels = new byte[targetWidth * targetHeight * 4];

        // Fill background with top-left pixel color if available, else white
        byte b = 0xFF, g = 0xFF, r = 0xFF, a = 0xFF;
        if (bmpBytes.Length >= pixelOffset + 4)
        {
            b = bmpBytes[pixelOffset];
            g = bmpBytes[pixelOffset + 1];
            r = bmpBytes[pixelOffset + 2];
            a = bmpBytes[pixelOffset + 3];
        }

        for (int i = 0; i < paddedPixels.Length; i += 4)
        {
            paddedPixels[i] = b;
            paddedPixels[i + 1] = g;
            paddedPixels[i + 2] = r;
            paddedPixels[i + 3] = a;
        }

        int offsetX = 8;
        int offsetY = 8;

        for (int row = 0; row < srcHeight; row++)
        {
            int srcRowOffset = pixelOffset + (row * srcWidth * 4);
            int dstRowOffset = ((offsetY + row) * targetWidth + offsetX) * 4;
            int bytesToCopy = Math.Min(srcWidth * 4, bmpBytes.Length - srcRowOffset);
            if (bytesToCopy > 0)
            {
                Buffer.BlockCopy(bmpBytes, srcRowOffset, paddedPixels, dstRowOffset, bytesToCopy);
            }
        }

        return CreateBmpFromPixels(paddedPixels, targetWidth, targetHeight);
    }

    public string? FindWordAtPoint(OcrResult ocrResult, double localX, double localY)
    {
        ArgumentNullException.ThrowIfNull(ocrResult);

        Point clickPoint = new(localX, localY);

        // 1. Exact bounding box hit
        foreach (OcrLine line in ocrResult.Lines)
        {
            foreach (OcrWord word in line.Words)
            {
                if (word.BoundingBox.Contains(clickPoint))
                {
                    return word.Text;
                }
            }
        }

        // 2. Proximity search (within 12 pixels tolerance from box edge)
        OcrWord? closestWord = null;
        double minDistanceSq = 144.0; // 12^2

        foreach (OcrLine line in ocrResult.Lines)
        {
            foreach (OcrWord word in line.Words)
            {
                double left = word.BoundingBox.X;
                double right = word.BoundingBox.X + word.BoundingBox.Width;
                double top = word.BoundingBox.Y;
                double bottom = word.BoundingBox.Y + word.BoundingBox.Height;

                double dx = 0.0;
                if (localX < left) dx = left - localX;
                else if (localX > right) dx = localX - right;

                double dy = 0.0;
                if (localY < top) dy = top - localY;
                else if (localY > bottom) dy = localY - bottom;

                double distSq = dx * dx + dy * dy;

                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    closestWord = word;
                }
            }
        }

        return closestWord?.Text;
    }

    public string FormatExtractedText(string rawText, bool singleLine = false)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        return singleLine ? CollapseToSingleLine(rawText) : rawText.Trim();
    }

    private static string CollapseToSingleLine(string text)
    {
        var words = text.Split(['\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words).Trim();
    }

    private const int HOTKEY_ID = 0x5346; // 'SF' / 'FS'
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_F = 0x46;
    private IntPtr _registeredHwnd = IntPtr.Zero;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public bool RegisterGlobalHotkey(IntPtr hWnd)
    {
        UnregisterGlobalHotkey(_registeredHwnd);
        _registeredHwnd = hWnd;
        bool registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT | MOD_NOREPEAT, VK_F);
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT, VK_F);
        }
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_ALT | MOD_SHIFT | MOD_NOREPEAT, VK_F);
        }
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_ALT | MOD_SHIFT, VK_F);
        }
        return registered;
    }

    public bool UnregisterGlobalHotkey(IntPtr hWnd)
    {
        if (_registeredHwnd != IntPtr.Zero)
        {
            var res = UnregisterHotKey(_registeredHwnd, HOTKEY_ID);
            _registeredHwnd = IntPtr.Zero;
            return res;
        }
        return true;
    }

    public void Dispose()
    {
        UnregisterGlobalHotkey(_registeredHwnd);
        GC.SuppressFinalize(this);
    }
}
