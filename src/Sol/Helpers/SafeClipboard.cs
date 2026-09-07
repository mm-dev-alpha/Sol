using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Sol.Helpers;

/// <summary>
/// Thread-safe and fault-tolerant clipboard operations with retry mechanism to protect
/// against transient external lock exceptions (CLIPBRD_E_CANT_OPEN / 0x800401D0).
/// </summary>
public static class SafeClipboard
{
    private const int CantOpenClipboardHResult = unchecked((int)0x800401D0);

    /// <summary>
    /// Attempts to set text on the system clipboard safely.
    /// Retries if locked by another application.
    /// When <paramref name="isSensitive"/> is true, prevents text from being saved to
    /// Windows Clipboard History or synced via Cloud Clipboard.
    /// </summary>
    public static bool TrySetText(string? text, int maxRetries = 3, int retryDelayMs = 50, bool isSensitive = false)
    {
        if (text is null)
            text = string.Empty;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var package = new DataPackage();
                package.SetText(text);

                if (isSensitive)
                {
                    var options = new ClipboardContentOptions
                    {
                        IsAllowedInHistory = false,
                        IsRoamable = false
                    };
                    Clipboard.SetContentWithOptions(package, options);
                }
                else
                {
                    Clipboard.SetContent(package);
                }
                return true;
            }
            catch (COMException ex) when (ex.HResult == CantOpenClipboardHResult || (uint)ex.HResult == 0x800401D0)
            {
                if (attempt == maxRetries - 1)
                {
                    AppLog.Write($"SafeClipboard.TrySetText failed after {maxRetries} attempts: {ex.Message}");
                    return false;
                }
                System.Threading.Thread.Sleep(retryDelayMs);
            }
            catch (Exception ex)
            {
                AppLog.Write($"SafeClipboard.TrySetText unexpected error: {ex.Message}");
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to set text on the system clipboard safely and asynchronously without blocking the UI thread.
    /// Retries if locked by another application.
    /// When <paramref name="isSensitive"/> is true, prevents text from being saved to
    /// Windows Clipboard History or synced via Cloud Clipboard.
    /// </summary>
    public static async Task<bool> TrySetTextAsync(string? text, int maxRetries = 3, int retryDelayMs = 50, bool isSensitive = false)
    {
        if (text is null)
            text = string.Empty;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var package = new DataPackage();
                package.SetText(text);

                if (isSensitive)
                {
                    var options = new ClipboardContentOptions
                    {
                        IsAllowedInHistory = false,
                        IsRoamable = false
                    };
                    Clipboard.SetContentWithOptions(package, options);
                }
                else
                {
                    Clipboard.SetContent(package);
                }
                return true;
            }
            catch (COMException ex) when (ex.HResult == CantOpenClipboardHResult || (uint)ex.HResult == 0x800401D0)
            {
                if (attempt == maxRetries - 1)
                {
                    AppLog.Write($"SafeClipboard.TrySetTextAsync failed after {maxRetries} attempts: {ex.Message}");
                    return false;
                }
                await Task.Delay(retryDelayMs).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AppLog.Write($"SafeClipboard.TrySetTextAsync unexpected error: {ex.Message}");
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to read text from the system clipboard safely.
    /// </summary>
    public static async Task<string?> TryGetTextAsync(int maxRetries = 3, int retryDelayMs = 50)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var dataPackageView = Clipboard.GetContent();
                if (dataPackageView != null && dataPackageView.Contains(StandardDataFormats.Text))
                {
                    return await dataPackageView.GetTextAsync();
                }
                return null;
            }
            catch (COMException ex) when (ex.HResult == CantOpenClipboardHResult || (uint)ex.HResult == 0x800401D0)
            {
                if (attempt == maxRetries - 1)
                {
                    AppLog.Write($"SafeClipboard.TryGetTextAsync failed after {maxRetries} attempts: {ex.Message}");
                    return null;
                }
                await Task.Delay(retryDelayMs);
            }
            catch (Exception ex)
            {
                AppLog.Write($"SafeClipboard.TryGetTextAsync unexpected error: {ex.Message}");
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to read bitmap image bytes from the system clipboard safely.
    /// </summary>
    public static async Task<byte[]?> TryGetBitmapBytesAsync(int maxRetries = 3, int retryDelayMs = 50)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var dataPackageView = Clipboard.GetContent();
                if (dataPackageView != null && dataPackageView.Contains(StandardDataFormats.Bitmap))
                {
                    var streamRef = await dataPackageView.GetBitmapAsync();
                    using var stream = await streamRef.OpenReadAsync();
                    using var memStream = new System.IO.MemoryStream();
                    await stream.AsStreamForRead().CopyToAsync(memStream);
                    return memStream.ToArray();
                }
                return null;
            }
            catch (COMException ex) when (ex.HResult == CantOpenClipboardHResult || (uint)ex.HResult == 0x800401D0)
            {
                if (attempt == maxRetries - 1)
                {
                    AppLog.Write($"SafeClipboard.TryGetBitmapBytesAsync failed after {maxRetries} attempts: {ex.Message}");
                    return null;
                }
                await Task.Delay(retryDelayMs);
            }
            catch (Exception ex)
            {
                AppLog.Write($"SafeClipboard.TryGetBitmapBytesAsync unexpected error: {ex.Message}");
                return null;
            }
        }

        return null;
    }
}
