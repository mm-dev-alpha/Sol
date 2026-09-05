using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Sol.Models;
using Sol.Services;
using Windows.Foundation;
using Xunit;

namespace Sol.Tests;

public class ScreenCaptureServiceTests
{
    private readonly IScreenCaptureService _service = new ScreenCaptureService();

    [Fact]
    public void CreateBmpFromPixels_GeneratesValidBmpHeader()
    {
        int width = 10;
        int height = 8;
        byte[] pixels = new byte[width * height * 4];
        // Fill some pixels
        pixels[0] = 0x12; // B
        pixels[1] = 0x34; // G
        pixels[2] = 0x56; // R
        pixels[3] = 0xFF; // A

        byte[] bmp = _service.CreateBmpFromPixels(pixels, width, height);

        Assert.NotNull(bmp);
        Assert.Equal(54 + (width * height * 4), bmp.Length);

        // Header checks
        Assert.Equal((byte)'B', bmp[0]);
        Assert.Equal((byte)'M', bmp[1]);

        int totalSize = BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(2, 4));
        Assert.Equal(bmp.Length, totalSize);

        int offBits = BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(10, 4));
        Assert.Equal(54, offBits);

        int biSize = BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(14, 4));
        Assert.Equal(40, biSize);

        int biWidth = BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(18, 4));
        Assert.Equal(width, biWidth);

        int biHeight = BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(22, 4));
        Assert.Equal(-height, biHeight); // Top-down

        short biPlanes = BinaryPrimitives.ReadInt16LittleEndian(bmp.AsSpan(26, 2));
        Assert.Equal(1, biPlanes);

        short biBitCount = BinaryPrimitives.ReadInt16LittleEndian(bmp.AsSpan(28, 2));
        Assert.Equal(32, biBitCount);

        // Verify pixel data retained
        Assert.Equal(0x12, bmp[54]);
        Assert.Equal(0x34, bmp[55]);
        Assert.Equal(0x56, bmp[56]);
        Assert.Equal(0xFF, bmp[57]);
    }

    [Fact]
    public void CropBmp_ExtractsCorrectSubRectangle()
    {
        // 4x4 image
        int width = 4;
        int height = 4;
        byte[] pixels = new byte[width * height * 4];

        // Place a distinct color at (1, 1) and (2, 1)
        int idx1 = ((1 * width) + 1) * 4;
        pixels[idx1] = 0xAA;
        pixels[idx1 + 1] = 0xBB;
        pixels[idx1 + 2] = 0xCC;
        pixels[idx1 + 3] = 0xFF;

        int idx2 = ((1 * width) + 2) * 4;
        pixels[idx2] = 0x11;
        pixels[idx2 + 1] = 0x22;
        pixels[idx2 + 2] = 0x33;
        pixels[idx2 + 3] = 0xFF;

        byte[] originalBmp = _service.CreateBmpFromPixels(pixels, width, height);

        // Crop 2x1 region starting at (1, 1)
        byte[] croppedBmp = _service.CropBmp(originalBmp, 1, 1, 2, 1);

        Assert.NotNull(croppedBmp);
        Assert.Equal(54 + (2 * 1 * 4), croppedBmp.Length);

        int cropW = BinaryPrimitives.ReadInt32LittleEndian(croppedBmp.AsSpan(18, 4));
        int cropH = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(croppedBmp.AsSpan(22, 4)));
        Assert.Equal(2, cropW);
        Assert.Equal(1, cropH);

        // Pixel 0 in cropped should be idx1
        Assert.Equal(0xAA, croppedBmp[54]);
        Assert.Equal(0xBB, croppedBmp[55]);
        Assert.Equal(0xCC, croppedBmp[56]);
        Assert.Equal(0xFF, croppedBmp[57]);

        // Pixel 1 in cropped should be idx2
        Assert.Equal(0x11, croppedBmp[58]);
        Assert.Equal(0x22, croppedBmp[59]);
        Assert.Equal(0x33, croppedBmp[60]);
        Assert.Equal(0xFF, croppedBmp[61]);
    }

    [Fact]
    public void PadBmp_PadsSmallImagesToMinimumDimensions()
    {
        int width = 20;
        int height = 15;
        byte[] pixels = new byte[width * height * 4];
        byte[] smallBmp = _service.CreateBmpFromPixels(pixels, width, height);

        byte[] paddedBmp = _service.PadBmp(smallBmp, 64, 64);

        int padW = BinaryPrimitives.ReadInt32LittleEndian(paddedBmp.AsSpan(18, 4));
        int padH = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(paddedBmp.AsSpan(22, 4)));

        Assert.True(padW >= 64, $"Expected width >= 64, got {padW}");
        Assert.True(padH >= 64, $"Expected height >= 64, got {padH}");
    }

    [Fact]
    public void FindWordAtPoint_ReturnsClickedWord()
    {
        var line1 = new OcrLine(
            "Hello World",
            new List<OcrWord>
            {
                new("Hello", new Rect(10, 20, 50, 20)),
                new("World", new Rect(70, 20, 60, 20))
            },
            new Rect(10, 20, 120, 20)
        );

        var ocrResult = new OcrResult(
            "Hello World",
            new List<OcrLine> { line1 },
            0f,
            OcrEngineKind.Windows
        );

        // Exact hit inside "World" (X=80, Y=25)
        string? word = _service.FindWordAtPoint(ocrResult, 80, 25);
        Assert.Equal("World", word);

        // Exact hit inside "Hello" (X=30, Y=25)
        string? hello = _service.FindWordAtPoint(ocrResult, 30, 25);
        Assert.Equal("Hello", hello);

        // Point far away (X=500, Y=500)
        string? none = _service.FindWordAtPoint(ocrResult, 500, 500);
        Assert.Null(none);
    }

    [Fact]
    public void FormatExtractedText_CollapsesSingleLineCorrectly()
    {
        string input = "  Line 1   \r\n\t  Line 2  \n\n  Line 3  ";

        string singleLine = _service.FormatExtractedText(input, singleLine: true);
        Assert.Equal("Line 1 Line 2 Line 3", singleLine);

        string standard = _service.FormatExtractedText(input, singleLine: false);
        Assert.Equal("Line 1   \r\n\t  Line 2  \n\n  Line 3", standard);
    }

    [Fact]
    public void GetVirtualScreenBounds_ReturnsPositiveDimensions()
    {
        var bounds = _service.GetVirtualScreenBounds();
        Assert.NotNull(bounds);
        Assert.True(bounds.Width > 0);
        Assert.True(bounds.Height > 0);
    }
}
