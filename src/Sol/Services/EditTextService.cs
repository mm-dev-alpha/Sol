using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sol.Models;
using Windows.ApplicationModel.DataTransfer;

namespace Sol.Services;

public sealed class EditTextService : IEditTextService, IDisposable
{
    public event EventHandler? ToggleRequested;
    public event EventHandler<EditTextOpenEventArgs>? OpenRequested;

    private const int HOTKEY_ID = 0x5345; // 'S', 'E' (Sol Edit)
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_E = 0x45;

    private IntPtr _registeredHwnd = IntPtr.Zero;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public bool RegisterGlobalHotkey(IntPtr hWnd)
    {
        UnregisterGlobalHotkey(_registeredHwnd);
        _registeredHwnd = hWnd;
        bool registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT | MOD_NOREPEAT, VK_E);
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT, VK_E);
        }
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_ALT | MOD_SHIFT | MOD_NOREPEAT, VK_E);
        }
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_ALT | MOD_SHIFT, VK_E);
        }
        return registered;
    }

    public void UnregisterGlobalHotkey(IntPtr hWnd)
    {
        if (_registeredHwnd != IntPtr.Zero)
        {
            UnregisterHotKey(_registeredHwnd, HOTKEY_ID);
            _registeredHwnd = IntPtr.Zero;
        }
    }

    public void RequestToggle()
    {
        ToggleRequested?.Invoke(this, EventArgs.Empty);
    }

    public void RequestOpen(string? text = null, EditTextTableDocument? table = null)
    {
        OpenRequested?.Invoke(this, new EditTextOpenEventArgs(text, table));
    }

    public CalculationResult EvaluateExpressions(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new CalculationResult([], 0, 0, 0, 0);
        }

        var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var lineResults = new List<string>();
        var variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        double sum = 0;
        int evaluatedCount = 0;
        int errorCount = 0;

        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//") || trimmed.StartsWith('#'))
            {
                lineResults.Add(string.Empty);
                continue;
            }

            try
            {
                // Check variable assignment: "name = expression"
                var assignMatch = Regex.Match(trimmed, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*(.+)$");
                if (assignMatch.Success)
                {
                    string varName = assignMatch.Groups[1].Value;
                    string expr = assignMatch.Groups[2].Value;
                    double val = EvaluateSingleExpression(expr, variables);
                    variables[varName] = val;
                    lineResults.Add(FormatNumber(val));
                    sum += val;
                    evaluatedCount++;
                    continue;
                }

                double resultVal = EvaluateSingleExpression(trimmed, variables);
                lineResults.Add(FormatNumber(resultVal));
                sum += resultVal;
                evaluatedCount++;
            }
            catch
            {
                errorCount++;
                lineResults.Add("Error");
            }
        }

        double average = evaluatedCount > 0 ? sum / evaluatedCount : 0;
        return new CalculationResult(lineResults, sum, average, errorCount, evaluatedCount);
    }

    private static double EvaluateSingleExpression(string expr, Dictionary<string, double> variables)
    {
        string replaced = expr.Trim();

        // Strip trailing equals if user wrote e.g. "12 * 4 ="
        if (replaced.EndsWith('='))
        {
            replaced = replaced[..^1].Trim();
        }

        // Replace variable tokens
        foreach (var kvp in variables.OrderByDescending(k => k.Key.Length))
        {
            replaced = Regex.Replace(
                replaced,
                @"\b" + Regex.Escape(kvp.Key) + @"\b",
                kvp.Value.ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);
        }

        // Replace custom functions or operators if necessary
        // e.g. sqrt(x) -> pow(x, 0.5) or standard Math operations
        replaced = Regex.Replace(replaced, @"\bsqrt\s*\(([^)]+)\)", "($1 ^ 0.5)", RegexOptions.IgnoreCase);

        using var dataTable = new DataTable();
        var computed = dataTable.Compute(replaced, "");
        return Convert.ToDouble(computed, CultureInfo.InvariantCulture);
    }

    private static string FormatNumber(double val)
    {
        if (double.IsNaN(val) || double.IsInfinity(val))
            return val.ToString();

        // Format neatly: integers without decimals, decimals with up to 4 places
        if (Math.Abs(val % 1) < 0.0000001)
            return ((long)val).ToString(CultureInfo.InvariantCulture);

        return val.ToString("G6", CultureInfo.InvariantCulture);
    }

    public async Task<bool> TryInsertTextAsync(string text)
    {
        try
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);

            await Task.Delay(150);

            SendCtrlV();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion u;
        public static int Size => Marshal.SizeOf(typeof(INPUT));
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private static void SendCtrlV()
    {
        var inputs = new INPUT[]
        {
            new() { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = 0 } } },
            new() { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_V, dwFlags = 0 } } },
            new() { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_V, dwFlags = KEYEVENTF_KEYUP } } },
            new() { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = KEYEVENTF_KEYUP } } }
        };

        SendInput((uint)inputs.Length, inputs, INPUT.Size);
    }

    public void Dispose()
    {
        UnregisterGlobalHotkey(_registeredHwnd);
        GC.SuppressFinalize(this);
    }
}
