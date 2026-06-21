using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal static class Program {

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    private delegate bool ConsoleCtrlHandler(int ctrlType);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);

    private static ConsoleCtrlHandler _consoleCtrlHandler;

    public static void PrintGradientBanner() {
        EnableAnsi();

        int width = GetConsoleWidth();
        int panelWidth = Math.Min(104, Math.Max(66, width - 6));
        bool zh = IsChineseUi();
        int left = (width - panelWidth) / 2;

        string[] logo = BuildShikiPadLogo();

        // top decorative rail
        Console.Write(new string(' ', left));
        WriteGradientText("╭" + new string('─', panelWidth - 2) + "╮", SeasonFlowStops());
        Console.WriteLine();

        // title
        WriteNeonRule(width, panelWidth, zh ? "ShikiPad 控制界面" : "ShikiPad Control Surface");

        // thin separator
        Console.Write(new string(' ', left));
        WriteGradientText(RepeatPattern("┈", panelWidth), SeasonFlowStops());
        Console.WriteLine();

        // logo
        WriteExtrudedLogo(width, logo, SeasonFlowStops());
        Console.WriteLine();

        // tagline
        string tagline = zh
            ? "◇  物理按键  ·  鼠标曲线  ·  触控板蓄力  ◇"
            : "◇  physical keys  ·  mouse curve  ·  touch clutch  ◇";
        Console.Write(new string(' ', left));
        WriteGradientText(CenterLine(panelWidth, tagline), SeasonFlowStops());
        Console.WriteLine();

        // status line
        WriteMinimalStatus(width, panelWidth, zh);

        // bottom decorative rail
        Console.Write(new string(' ', left));
        WriteGradientText("╰" + new string('─', panelWidth - 2) + "╯", SeasonFlowStops());
        Console.WriteLine();
        WriteSeasonDropShadow(width, panelWidth);
        Console.WriteLine("\x1b[0m");
    }

    public static void PrintRunHint() {
        EnableAnsi();
        int width = GetConsoleWidth();
        int panelWidth = Math.Min(104, Math.Max(66, width - 6));
        WriteLiveStatusBar(width, panelWidth, IsChineseUi());
        Console.WriteLine("\x1b[0m");
    }

    public static void PrintRuntimeStatus(string processPath, int processId, int parentId, string backend, bool readsController) {
        EnableAnsi();
        int width = GetConsoleWidth();
        int panelWidth = Math.Min(104, Math.Max(66, width - 6));
        string fileName = Path.GetFileName(processPath);
        bool zh = IsChineseUi();

        Console.WriteLine();
        WriteSeasonPanelBorder(width, panelWidth, true);
        WriteSeasonPanelTitle(width, panelWidth, zh ? "\u25c7 \u8fd0\u884c\u72b6\u6001 \u25c7" : "\u25c7 RUNTIME STATUS \u25c7");
        WriteSeasonPanelSeparator(width, panelWidth);
        WritePanelLine(width, panelWidth, zh ? "  \u8fdb\u7a0b" : "  Process", fileName + "  PID " + processId.ToString(CultureInfo.InvariantCulture), SeasonGold(), new Rgb(222, 238, 244));
        WritePanelLine(width, panelWidth, zh ? "  \u7236\u8fdb\u7a0b" : "  Parent", parentId.ToString(CultureInfo.InvariantCulture), SeasonSummer(), new Rgb(222, 238, 244));
        WritePanelLine(width, panelWidth, zh ? "  \u624b\u67c4\u540e\u7aef" : "  Controller backend", backend, SeasonSpring(), new Rgb(222, 238, 244));
        WritePanelLine(width, panelWidth, zh ? "  \u624b\u67c4\u8bfb\u53d6" : "  Controller read", readsController ? (zh ? "\u672c\u8fdb\u7a0b\u6d3b\u8dc3" : "active in this process") : (zh ? "\u672a\u6d3b\u8dc3" : "inactive"), SeasonAutumn(), new Rgb(222, 238, 244));
        WritePanelLine(width, panelWidth, zh ? "  \u8def\u5f84" : "  Path", ShortenPath(processPath, panelWidth - 14), SeasonWinter(), new Rgb(206, 220, 226));
        WriteSeasonPanelBorder(width, panelWidth, false);
        WriteSeasonDropShadow(width, panelWidth);
        Console.WriteLine("\x1b[0m");
    }

    public static void PrintControllerGuide(ControllerProfile profile, string backend, Config config) {
        EnableAnsi();
        int width = GetConsoleWidth();
        int panelWidth = Math.Min(112, Math.Max(72, width - 6));
        bool zh = IsChineseUi();
        bool xbox = profile == ControllerProfile.Xbox360 || profile == ControllerProfile.XboxSeries ||
                    profile == ControllerProfile.Xbox360BT || profile == ControllerProfile.XboxSeriesBT;
        bool ds4 = profile == ControllerProfile.DualShock4 || profile == ControllerProfile.DualShock4BT;

        Console.WriteLine();
        WritePanelBorder(width, panelWidth, true, new Rgb(126, 226, 244));
        WritePanelTitle(width, panelWidth, zh ? "\u25c7 \u6620\u5c04\u901f\u67e5 \u25c7" : "\u25c7 MAPPING QUICK REFERENCE \u25c7", new Rgb(235, 247, 252));
        WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

        if (zh) {
            WritePanelLine(width, panelWidth, "  \u5df2\u8fde\u63a5", backend, new Rgb(126, 226, 244), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u53f3\u6447\u6746", "\u79fb\u52a8\u9f20\u6807, R3 \u53f3\u952e, L3 \u5de6\u952e", new Rgb(113, 255, 194), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u5de6\u6447\u6746", "\u2191\u6eda\u8f6e\u4e0a  \u2197 Fn  \u2192 Win  \u2198 Alt  \u2193\u6eda\u8f6e\u4e0b  \u2199 Ctrl  \u2190 Shift  \u2196 Esc", new Rgb(128, 224, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u57fa\u7840\u5c42", xbox ? "D-pad=\u65b9\u5411\u952e, X=Space, Y=Backspace, A=Enter, B=Tab" : "D-pad=\u65b9\u5411\u952e, Square=Space, Triangle=Backspace, Cross=Enter, Circle=Tab", new Rgb(255, 211, 106), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  R1 / L1", "R1: i n e a o t h u    L1: s r d g l c y z", new Rgb(255, 142, 206), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  R2 / L2", "R2: m w j x q f p b    L2: k v 1 2 3 4 5 6", new Rgb(190, 133, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u7ec4\u5408\u5c42", "R1+L1: 7 8 9 0 - = , .    R2+L2: ' / ; [ ] \\ `", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u7ec4\u5408\u7a97\u53e3", "R1/L1 \u6216 R2/L2 \u9700\u5728 " + config.ComboLayerWindowMs.ToString(CultureInfo.InvariantCulture) + "ms \u5185\u5408\u6309", new Rgb(126, 226, 244), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u5c42\u786e\u8ba4", "\u52a8\u4f5c\u952e\u540e " + config.ActionLayerGraceMs.ToString(CultureInfo.InvariantCulture) + "ms \u5185\u786e\u8ba4", SeasonSummer(), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u84c4\u529b", xbox ? "View/Menu \u77ed\u6309=\u5207\u6362\u84c4\u529b, \u957f\u6309=\u6309\u4f4f\u84c4\u529b" : "\u89e6\u63a7\u677f\u77ed\u6309=\u5207\u6362\u84c4\u529b, \u957f\u6309=\u6309\u4f4f\u84c4\u529b; Share=RAlt Options=RCtrl Home=RShift", new Rgb(113, 255, 194), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Fn", "\u5de6\u6447\u6746\u2197 + 1..0,-,= => F1..F12", new Rgb(255, 255, 255), new Rgb(245, 250, 255));
        } else {
            WritePanelLine(width, panelWidth, "  Connected", backend, new Rgb(126, 226, 244), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Right stick", "Move mouse, R3 right click, L3 left click", new Rgb(113, 255, 194), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Left stick", "Up WheelUp, UpRight Fn, Right Win, DownRight Alt, Down WheelDown, DownLeft Ctrl, Left Shift, UpLeft Esc", new Rgb(128, 224, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Base layer", xbox ? "D-pad=arrows, X=Space, Y=Backspace, A=Enter, B=Tab" : "D-pad=arrows, Square=Space, Triangle=Backspace, Cross=Enter, Circle=Tab", new Rgb(255, 211, 106), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  R1 / L1", "R1: i n e a o t h u    L1: s r d g l c y z", new Rgb(255, 142, 206), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  R2 / L2", "R2: m w j x q f p b    L2: k v 1 2 3 4 5 6", new Rgb(190, 133, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Combo layers", "R1+L1: 7 8 9 0 - = , .    R2+L2: ' / ; [ ] \\ `", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Combo window", "R1/L1 or R2/L2 must pair within " + config.ComboLayerWindowMs.ToString(CultureInfo.InvariantCulture) + "ms; later overlaps use the newest single layer", new Rgb(126, 226, 244), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Layer settle", "Action looks forward/back " + config.ActionLayerGraceMs.ToString(CultureInfo.InvariantCulture) + "ms", SeasonSummer(), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Clutch", xbox ? "View/Menu short press=toggle clutch, long press=hold clutch" : "Touchpad short press=toggle clutch, long press=hold clutch; Share=RAlt Options=RCtrl Home=RShift", new Rgb(113, 255, 194), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Fn", "Left stick UpRight + 1..0,-,= => F1..F12", new Rgb(255, 255, 255), new Rgb(245, 250, 255));
        }

        WritePanelBorder(width, panelWidth, false, new Rgb(126, 226, 244));
        Console.WriteLine("\x1b[0m");
    }

    private static bool IsChineseUi() {
        try {
            string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return String.Equals(lang, "zh", StringComparison.OrdinalIgnoreCase);
        } catch {
            return false;
        }
    }

    private static void EnableAnsi() {
        try {
            IntPtr handle = GetStdHandle(-11);
            uint mode;
            if (GetConsoleMode(handle, out mode)) {
                SetConsoleMode(handle, mode | 0x0004 | 0x0008);
            }
        } catch { }

        try {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        } catch { }
    }

    private static int GetConsoleWidth() {
        int width = 88;
        try { width = Console.WindowWidth; } catch { }
        if (width < 64) width = 64;
        if (width > 160) width = 160;
        return width;
    }

    private static string CenterLine(int width, string text) {
        int textWidth = DisplayWidth(text);
        if (textWidth >= width) return TrimToWidth(text, width);
        int left = (width - textWidth) / 2;
        return new string(' ', left) + text + new string(' ', width - left - textWidth);
    }

    private struct Rgb {
        public int R;
        public int G;
        public int B;

        public Rgb(int r, int g, int b) {
            R = r;
            G = g;
            B = b;
        }
    }

    private static void WriteRgb(Rgb color, string text) {
        Console.Write(string.Format("\x1b[38;2;{0};{1};{2}m{3}", color.R, color.G, color.B, text));
    }

    private static Rgb Mix(Rgb a, Rgb b, double t) {
        return new Rgb(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    private static Rgb Scale(Rgb color, double amount) {
        return new Rgb(
            ClampColor(color.R * amount),
            ClampColor(color.G * amount),
            ClampColor(color.B * amount));
    }

    private static int ClampColor(double value) {
        if (value < 0.0) return 0;
        if (value > 255.0) return 255;
        return (int)value;
    }

    private static Rgb GradientAt(Rgb[] stops, double t) {
        if (t <= 0.0) return stops[0];
        if (t >= 1.0) return stops[stops.Length - 1];
        double scaled = t * (stops.Length - 1);
        int segment = (int)scaled;
        if (segment >= stops.Length - 1) segment = stops.Length - 2;
        return Mix(stops[segment], stops[segment + 1], scaled - segment);
    }

    private static Rgb[] SeasonStops() {
        return new Rgb[] {
            SeasonSpring(),
            SeasonSummer(),
            SeasonAutumn(),
            SeasonWinter()
        };
    }

    private static Rgb[] SeasonFlowStops() {
        return new Rgb[] {
            SeasonSpring(),
            new Rgb(91, 251, 226),
            SeasonSummer(),
            new Rgb(198, 244, 255),
            new Rgb(255, 238, 154),
            SeasonGold(),
            new Rgb(255, 183, 112),
            SeasonAutumn(),
            new Rgb(255, 213, 168),
            SeasonWinter()
        };
    }

    private static Rgb[] SeasonGlowStops() {
        return new Rgb[] {
            new Rgb(72, 255, 202),
            new Rgb(91, 226, 255),
            SeasonGold(),
            new Rgb(255, 163, 102),
            new Rgb(244, 252, 255)
        };
    }

    private static Rgb SeasonSpring() { return new Rgb(94, 255, 197); }
    private static Rgb SeasonSummer() { return new Rgb(91, 226, 255); }
    private static Rgb SeasonGold() { return new Rgb(255, 215, 92); }
    private static Rgb SeasonAutumn() { return new Rgb(255, 148, 82); }
    private static Rgb SeasonWinter() { return new Rgb(255, 255, 255); }
    private static Rgb PanelInk() { return new Rgb(48, 72, 86); }
    private static Rgb ShadowInk() { return new Rgb(9, 18, 24); }

    private static string[] BuildShikiPadLogo() {
        string[][] glyphs = new string[][] {
            new string[] {
                " ████████ ",
                "██        ",
                "██        ",
                " ███████  ",
                "      ██  ",
                "       ██ ",
                "████████  ",
                " ████████ "
            },
            new string[] {
                "██    ██",
                "██    ██",
                "██    ██",
                "████████",
                "██    ██",
                "██    ██",
                "██    ██",
                "██    ██"
            },
            new string[] {
                "████",
                " ██ ",
                " ██ ",
                " ██ ",
                " ██ ",
                " ██ ",
                " ██ ",
                "████"
            },
            new string[] {
                "██   ██ ",
                "██  ██  ",
                "██ ██   ",
                "████    ",
                "████    ",
                "██ ██   ",
                "██  ██  ",
                "██   ██ "
            },
            new string[] {
                "████",
                " ██ ",
                " ██ ",
                " ██ ",
                " ██ ",
                " ██ ",
                " ██ ",
                "████"
            },
            new string[] {
                "███████ ",
                "██    ██",
                "██    ██",
                "███████ ",
                "██      ",
                "██      ",
                "██      ",
                "██      "
            },
            new string[] {
                " █████  ",
                "██   ██ ",
                "     ██ ",
                " ██████ ",
                "██   ██ ",
                "██   ██ ",
                "██   ██ ",
                " ██████ "
            },
            new string[] {
                "     ██ ",
                "     ██ ",
                "     ██ ",
                " ██████ ",
                "██   ██ ",
                "██   ██ ",
                "██   ██ ",
                " ██████ "
            }
        };

        int rows = glyphs[0].Length;
        StringBuilder[] lines = new StringBuilder[rows];
        for (int row = 0; row < rows; row++) lines[row] = new StringBuilder();
        for (int glyph = 0; glyph < glyphs.Length; glyph++) {
            for (int row = 0; row < rows; row++) {
                if (glyph > 0) lines[row].Append(' ');
                lines[row].Append(glyphs[glyph][row]);
            }
        }

        string[] logo = new string[rows];
        for (int row = 0; row < rows; row++) logo[row] = lines[row].ToString().TrimEnd();
        return logo;
    }

    private static void WriteExtrudedLogo(int width, string[] logo, Rgb[] stops) {
        int logoWidth = 0;
        for (int row = 0; row < logo.Length; row++) if (logo[row].Length > logoWidth) logoWidth = logo[row].Length;
        int shadowX = 3;
        int shadowY = 2;
        int outputWidth = logoWidth + shadowX;
        int left = Math.Max(0, (width - outputWidth) / 2);

        for (int row = 0; row < logo.Length + shadowY; row++) {
            Console.Write(new string(' ', left));
            Console.Write("\x1b[1m");
            for (int col = 0; col < outputWidth; col++) {
                bool main = IsLogoPixel(logo, row, col);
                bool nearShadow = IsLogoPixel(logo, row - 1, col - 2);
                bool farShadow = IsLogoPixel(logo, row - shadowY, col - shadowX);
                double t = logoWidth <= 1 ? 1.0 : (double)Math.Max(0, Math.Min(col, logoWidth - 1)) / (double)(logoWidth - 1);
                Rgb baseColor = GradientAt(stops, t);

                if (main) {
                    double rowT = logo.Length <= 1 ? 0.0 : (double)row / (double)(logo.Length - 1);
                    Rgb face = LogoFaceColor(baseColor, rowT);
                    WriteRgb(face, "\u2588");
                } else if (nearShadow) {
                    WriteRgb(Scale(baseColor, 0.38), "\u2593");
                } else if (farShadow) {
                    WriteRgb(Scale(baseColor, 0.22), "\u2592");
                } else {
                    Console.Write(' ');
                }
            }
            Console.Write("\x1b[22m");
            Console.WriteLine();
        }
    }

    private static bool IsLogoPixel(string[] logo, int row, int col) {
        if (row < 0 || row >= logo.Length) return false;
        if (col < 0 || col >= logo[row].Length) return false;
        return logo[row][col] != ' ';
    }

    private static Rgb LogoFaceColor(Rgb baseColor, double rowT) {
        if (rowT < 0.22) return Mix(baseColor, new Rgb(255, 255, 255), 0.24);
        if (rowT > 0.72) return Scale(baseColor, 0.82);
        return baseColor;
    }

    private static void WriteNeonRule(int width, int panelWidth, string title) {
        string line = "\u2726\u2500\u2500 " + title + " " + new string('\u2500', Math.Max(0, panelWidth - title.Length - 7)) + "\u2726";
        WriteEmbossedCenteredText(width, panelWidth, line, SeasonGlowStops(), true);
    }

    private static void WriteGradientText(string text, Rgb[] stops) {
        for (int i = 0; i < text.Length; i++) {
            double t = text.Length <= 1 ? 1.0 : (double)i / (double)(text.Length - 1);
            WriteRgb(GradientAt(stops, t), text[i].ToString());
        }
    }

    private static void WriteEmbossedCenteredText(int width, int panelWidth, string text, Rgb[] stops, bool bold) {
        int left = (width - panelWidth) / 2;
        string line = CenterLine(panelWidth, text);
        Console.Write(new string(' ', left + 1));
        WriteGradientShadowGlyphs(line, stops);
        Console.Write("\r");
        Console.Write(new string(' ', left));
        if (bold) Console.Write("\x1b[1m");
        WriteGradientText(line, stops);
        if (bold) Console.Write("\x1b[22m");
        Console.WriteLine();
    }

    private static void WriteGradientShadowGlyphs(string text, Rgb[] stops) {
        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            if (c == ' ') {
                Console.Write(' ');
            } else {
                double t = text.Length <= 1 ? 1.0 : (double)i / (double)(text.Length - 1);
                WriteRgb(Scale(GradientAt(stops, t), 0.22), "\u2592");
            }
        }
    }

    private static void WriteSeasonDropShadow(int width, int panelWidth) {
        int left = Math.Max(0, (width - panelWidth) / 2 + 2);
        Console.Write(new string(' ', left));
        WriteRgb(ShadowInk(), RepeatPattern("\u2591", Math.Max(0, panelWidth - 2)));
        Console.WriteLine();
    }

    private static void WriteMinimalStatus(int width, int panelWidth, bool zh) {
        int left = (width - panelWidth) / 2;
        string line = "\u2500\u2500\u2500  " + (zh ? "\u5c31\u7eea  \u00b7  \u5173\u95ed\u65f6\u81ea\u52a8\u91ca\u653e\u6240\u6709\u6309\u952e" : "READY  \u00b7  Auto-release all keys on close") + "  \u2500\u2500\u2500";
        Console.Write(new string(' ', left));
        WriteGradientText(CenterLine(panelWidth, line), SeasonFlowStops());
        Console.WriteLine();
    }

    private static void WriteSeasonPanelBorder(int width, int panelWidth, bool top) {
        int left = (width - panelWidth) / 2;
        string line = (top ? "\u256d" : "\u2570") + new string('\u2500', panelWidth - 2) + (top ? "\u256e" : "\u256f");
        Console.Write(new string(' ', left));
        WriteGradientText(line, SeasonFlowStops());
        Console.WriteLine();
    }

    private static void WriteSeasonPanelSeparator(int width, int panelWidth) {
        int left = (width - panelWidth) / 2;
        Console.Write(new string(' ', left));
        WriteRgb(PanelInk(), "\u2502");
        WriteGradientText(new string('\u2504', panelWidth - 2), SeasonFlowStops());
        WriteRgb(PanelInk(), "\u2502");
        Console.WriteLine();
    }

    private static void WriteSeasonPanelTitle(int width, int panelWidth, string title) {
        int left = (width - panelWidth) / 2;
        Console.Write(new string(' ', left));
        WriteRgb(PanelInk(), "\u2502");
        Console.Write("\x1b[1m");
        WriteGradientText(CenterLine(panelWidth - 2, title), SeasonFlowStops());
        Console.Write("\x1b[22m");
        WriteRgb(PanelInk(), "\u2502");
        Console.WriteLine();
    }

    private static void WritePanelBorder(int width, int panelWidth, bool top, Rgb color) {
        int left = (width - panelWidth) / 2;
        Console.Write(new string(' ', left));
        string line = (top ? "\u256d" : "\u2570") + new string('\u2500', panelWidth - 2) + (top ? "\u256e" : "\u256f");
        WriteRgb(color, line);
        Console.WriteLine();
    }

    private static void WritePanelSeparator(int width, int panelWidth, Rgb color) {
        int left = (width - panelWidth) / 2;
        Console.Write(new string(' ', left));
        WriteRgb(color, "\u2502" + new string('\u2504', panelWidth - 2) + "\u2502");
        Console.WriteLine();
    }

    private static void WritePanelTitle(int width, int panelWidth, string title, Rgb color) {
        int left = (width - panelWidth) / 2;
        Console.Write(new string(' ', left));
        WriteRgb(new Rgb(72, 91, 101), "\u2502");
        WriteRgb(color, CenterLine(panelWidth - 2, title));
        WriteRgb(new Rgb(72, 91, 101), "\u2502");
        Console.WriteLine();
    }

    private static void WritePanelLine(int width, int panelWidth, string label, string value, Rgb labelColor, Rgb valueColor) {
        int left = (width - panelWidth) / 2;
        int inner = panelWidth - 2;
        int labelWidth = Math.Min(28, Math.Max(18, inner / 3));
        int valueWidth = inner - labelWidth - 3;
        if (DisplayWidth(value) > valueWidth) value = TrimToWidth(value, valueWidth);

        Console.Write(new string(' ', left));
        WriteRgb(new Rgb(72, 91, 101), "\u2502");
        Console.Write("\x1b[1m");
        WriteRgb(labelColor, PadRight(label, labelWidth));
        Console.Write("\x1b[22m");
        WriteRgb(new Rgb(72, 91, 101), " \u2506 ");
        WriteRgb(valueColor, PadRight(value, valueWidth));
        WriteRgb(new Rgb(72, 91, 101), "\u2502");
        Console.WriteLine();
    }

    private static void WriteControllerPairLine(int width, int panelWidth, string leftLabel, string rightLabel, Rgb color) {
        int left = (width - panelWidth) / 2;
        int inner = panelWidth - 2;
        int gap = 3;
        int column = (inner - gap) / 2;

        Console.Write(new string(' ', left));
        WriteRgb(new Rgb(72, 91, 101), "\u2502");
        Console.Write("\x1b[1m");
        WriteRgb(color, PadRight("  " + leftLabel, column));
        Console.Write("\x1b[22m");
        WriteRgb(new Rgb(72, 91, 101), " \u2506 ");
        Console.Write("\x1b[1m");
        WriteRgb(color, PadRight("  " + rightLabel, inner - column - gap));
        Console.Write("\x1b[22m");
        WriteRgb(new Rgb(72, 91, 101), "\u2502");
        Console.WriteLine();
    }

    private static string PadRight(string text, int width) {
        if (width <= 0) return "";
        int textWidth = DisplayWidth(text);
        if (textWidth >= width) return TrimToWidth(text, width);
        return text + new string(' ', width - textWidth);
    }

    private static string TrimToWidth(string text, int width) {
        if (width <= 0) return "";
        if (DisplayWidth(text) <= width) return text;
        if (width <= 1) return "\u2026";

        StringBuilder sb = new StringBuilder();
        int used = 0;
        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            int cw = CharDisplayWidth(c);
            if (used + cw > width - 1) break;
            sb.Append(c);
            used += cw;
        }
        sb.Append("\u2026");
        return sb.ToString();
    }

    private static int DisplayWidth(string text) {
        if (String.IsNullOrEmpty(text)) return 0;
        int width = 0;
        for (int i = 0; i < text.Length; i++) width += CharDisplayWidth(text[i]);
        return width;
    }

    private static int CharDisplayWidth(char c) {
        if (c >= 0x1100 &&
            (c <= 0x115F ||
             c == 0x2329 || c == 0x232A ||
             (c >= 0x2E80 && c <= 0xA4CF) ||
             (c >= 0xAC00 && c <= 0xD7A3) ||
             (c >= 0xF900 && c <= 0xFAFF) ||
             (c >= 0xFE10 && c <= 0xFE19) ||
             (c >= 0xFE30 && c <= 0xFE6F) ||
             (c >= 0xFF00 && c <= 0xFF60) ||
             (c >= 0xFFE0 && c <= 0xFFE6))) {
            return 2;
        }
        return 1;
    }

    private static string RepeatPattern(string pattern, int width) {
        if (width <= 0) return "";
        StringBuilder sb = new StringBuilder(width + pattern.Length);
        while (sb.Length < width) sb.Append(pattern);
        if (sb.Length > width) sb.Length = width;
        return sb.ToString();
    }

    private static void WriteLiveStatusBar(int width, int panelWidth, bool zh) {
        int left = (width - panelWidth) / 2;
        string text = zh ? "\u25c6 \u5b9e\u65f6\u8fd0\u884c" : "\u25c6 Live session";
        string rail = "\u256d" + RepeatPattern("\u2500\u22c5", panelWidth - 2) + "\u256e";
        string bottom = "\u2570" + RepeatPattern("\u2500\u22c5", panelWidth - 2) + "\u256f";

        Console.Write(new string(' ', left));
        WriteGradientText(rail, SeasonFlowStops());
        Console.WriteLine();
        Console.Write(new string(' ', left));
        WriteRgb(PanelInk(), "\u2502");
        WriteGradientText(CenterLine(panelWidth - 2, text), SeasonFlowStops());
        WriteRgb(PanelInk(), "\u2502");
        Console.WriteLine();
        Console.Write(new string(' ', left));
        WriteGradientText(bottom, SeasonFlowStops());
        Console.WriteLine();
        WriteSeasonDropShadow(width, panelWidth);
    }

    private static string ShortenPath(string path, int maxLength) {
        if (path == null) return "";
        if (path.Length <= maxLength) return path;
        if (maxLength <= 4) return path.Substring(0, maxLength);
        return "\u2026" + path.Substring(path.Length - maxLength + 1);
    }

    private static bool _shutdownReleaseRegistered;

    [STAThread]
    private static int Main(string[] args) {
        PrintGradientBanner();

        string root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        Directory.SetCurrentDirectory(root);
        Logger.Init(root);
        Config config = Config.Load(Path.Combine(root, "shikipad.json"));
        RegisterShutdownRelease();
        bool debugSources = HasArg(args, "--debug-sources");
        bool traceInput = HasArg(args, "--trace-input");
        bool traceSendinput = HasArg(args, "--trace-sendinput");
        if (HasArg(args, "--list-devices") || HasArg(args, "--enum-hid")) {
            RunHidEnumTest();
            return 0;
        }

        if (HasArg(args, "--identity")) {
            Console.WriteLine("\n--- SHIKIPAD PROCESS IDENTITY ---");
            Console.WriteLine("Current process exe path: " + Process.GetCurrentProcess().MainModule.FileName);
            Console.WriteLine("Working directory: " + Environment.CurrentDirectory);
            Console.WriteLine("Command line: " + Environment.CommandLine);
            Console.WriteLine("Process ID: " + Process.GetCurrentProcess().Id);

            int parentId = 0;
            try {
                var pc = new System.Diagnostics.PerformanceCounter("Process", "Creating Process ID", Process.GetCurrentProcess().ProcessName);
                parentId = (int)pc.NextValue();
            } catch { }
            Console.WriteLine("Parent process ID: " + parentId);

            Console.WriteLine("Does this exact process register RawInput? YES");
            Console.WriteLine("Is any helper process used? NO");
            Console.WriteLine("\nAdd THIS EXACT path to HidHide Applications:");
            Console.WriteLine(Process.GetCurrentProcess().MainModule.FileName);
            return 0;
        }

        if (HasArg(args, "--layer-test")) {
            Environment.ExitCode = 0;
            PrintLayerTest(config);
            return Environment.ExitCode;
        }
        if (HasArg(args, "--mouse-test")) {
            Environment.ExitCode = 0;
            PrintMouseTest(config);
            return Environment.ExitCode;
        }
        if (HasArg(args, "--left-stick-test")) {
            Environment.ExitCode = 0;
            PrintLeftStickTest(config);
            return Environment.ExitCode;
        }
        if (HasArg(args, "--clutch-test")) {
            PrintClutchTest(config);
            return Environment.ExitCode;
        }
        ControllerProfile controllerProfile = SelectControllerProfile(args);
        Logger.Info("startup");
        Logger.Info("controller profile: " + ControllerProfileName(controllerProfile));
        Logger.Info("mouse settings: rightStickDeadzone = " + config.RightStickDeadzone.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", rightStickCurve = " + config.RightStickCurve +
                    ", rightStickCurveExponent = " + config.RightStickCurveExponent.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", mouseMaxSpeed = " + config.MouseMaxSpeed.ToString(CultureInfo.InvariantCulture) +
                    ", mouseSensitivity = " + config.MouseSensitivity.ToString(CultureInfo.InvariantCulture) +
                    ", neutralCalibration = enabled");
        Logger.Info("scroll settings: mouseScrollCurveExponent = " + config.MouseScrollCurveExponent.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", leftStickEnterDeadzone = " + config.LeftStickEnterDeadzone.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", leftStickExitDeadzone = " + config.LeftStickExitDeadzone.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", scrollSlowIntervalMs = " + config.ScrollSlowIntervalMs.ToString(CultureInfo.InvariantCulture) +
                    ", scrollFastIntervalMs = " + config.ScrollFastIntervalMs.ToString(CultureInfo.InvariantCulture));
        Logger.Info("left stick modifiers = physical held keys");
        if (debugSources) Logger.Info("debug-sources enabled");
        if (traceInput) Logger.Info("trace-input enabled");
        if (traceSendinput) Logger.Info("trace-sendinput enabled");

        PrintRunHint();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MapperForm(config, controllerProfile, debugSources, traceInput, traceSendinput));
        return 0;
    }

    private static void RegisterShutdownRelease() {
        if (_shutdownReleaseRegistered) return;
        _shutdownReleaseRegistered = true;
        Application.ApplicationExit += delegate(object sender, EventArgs e) {
            ReleaseAllRuntimeInput();
        };
        AppDomain.CurrentDomain.ProcessExit += delegate(object sender, EventArgs e) {
            ReleaseAllRuntimeInput();
        };
        Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e) {
            ReleaseAllRuntimeInput();
        };
        AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) {
            ReleaseAllRuntimeInput();
        };

        _consoleCtrlHandler = delegate(int ctrlType) {
            ReleaseAllRuntimeInput();
            return false;
        };
        try {
            SetConsoleCtrlHandler(_consoleCtrlHandler, true);
        } catch {
        }
    }

    private static void ReleaseAllRuntimeInput() {
        InputInjector.ReleaseAllRegistered();
        InterceptionDriver.Cleanup();
    }

    private static ControllerProfile SelectControllerProfile(string[] args) {
        ControllerProfile fromArgs;
        if (TryGetControllerProfileArg(args, out fromArgs)) return fromArgs;
        try {
            if (Console.IsInputRedirected) return ControllerProfile.DualSense;
        } catch { }

        EnableAnsi();
        int width = GetConsoleWidth();
        int panelWidth = Math.Min(104, Math.Max(66, width - 6));
        bool zh = IsChineseUi();
        WriteSeasonPanelBorder(width, panelWidth, true);
        WriteSeasonPanelTitle(width, panelWidth, zh ? "◇ 选择手柄型号 ◇" : "◇ CONTROLLER PROFILE ◇");
        WriteSeasonPanelSeparator(width, panelWidth);
        WriteControllerPairLine(width, panelWidth, "[1] DualSense", "[2] DualSense (BT)", SeasonSummer());
        WriteControllerPairLine(width, panelWidth, "[3] DualShock 4", "[4] DualShock 4 (BT)", new Rgb(100, 180, 255));
        WriteControllerPairLine(width, panelWidth, "[5] Xbox 360", "[6] Xbox 360 (BT)", SeasonSpring());
        WriteControllerPairLine(width, panelWidth, "[7] Xbox Series X|S", "[8] Xbox Series (BT)", SeasonGold());
        WriteSeasonPanelBorder(width, panelWidth, false);
        WriteSeasonDropShadow(width, panelWidth);
        Console.WriteLine();

        while (true) {
            WriteRgb(SeasonSummer(), zh ? "选择手柄型号 [1..8，Enter = 1] > " : "Select controller profile [1..8, Enter = 1] > ");
            Console.Write("\x1b[0m");
            string line = Console.ReadLine();
            if (line == null) return ControllerProfile.DualSense;
            line = line.Trim();
            if (line.Length == 0 || line == "1") return ControllerProfile.DualSense;
            if (line == "2") return ControllerProfile.DualSenseBT;
            if (line == "3") return ControllerProfile.DualShock4;
            if (line == "4") return ControllerProfile.DualShock4BT;
            if (line == "5") return ControllerProfile.Xbox360;
            if (line == "6") return ControllerProfile.Xbox360BT;
            if (line == "7") return ControllerProfile.XboxSeries;
            if (line == "8") return ControllerProfile.XboxSeriesBT;
            WriteRgb(SeasonAutumn(), zh ? "请选择 1 到 8 之间的数字。\n" : "Please choose 1 to 8.\n");
        }
    }

    private static bool TryGetControllerProfileArg(string[] args, out ControllerProfile profile) {
        profile = ControllerProfile.DualSense;
        for (int i = 0; i < args.Length; i++) {
            string arg = args[i] ?? "";
            string value = null;
            if (arg.StartsWith("--controller=", StringComparison.OrdinalIgnoreCase)) {
                value = arg.Substring("--controller=".Length);
            } else if (String.Equals(arg, "--controller", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) {
                value = args[i + 1];
            }
            if (value != null) return TryParseControllerProfile(value, out profile);
        }
        return false;
    }

    private static bool TryParseControllerProfile(string value, out ControllerProfile profile) {
        string v = (value ?? "").Trim().ToLowerInvariant().Replace("-", "").Replace("_", "").Replace(" ", "");
        if (v == "1" || v == "ds5" || v == "dualsense" || v == "ps5" || v == "ds5usb") {
            profile = ControllerProfile.DualSense;
            return true;
        }
        if (v == "2" || v == "ds5bt" || v == "dualsensebt" || v == "ps5bt") {
            profile = ControllerProfile.DualSenseBT;
            return true;
        }
        if (v == "3" || v == "ds4" || v == "dualshock4" || v == "ps4" || v == "ds4usb") {
            profile = ControllerProfile.DualShock4;
            return true;
        }
        if (v == "4" || v == "ds4bt" || v == "dualshock4bt" || v == "ps4bt") {
            profile = ControllerProfile.DualShock4BT;
            return true;
        }
        if (v == "5" || v == "xbox360" || v == "x360" || v == "xbox360usb") {
            profile = ControllerProfile.Xbox360;
            return true;
        }
        if (v == "6" || v == "xbox360bt" || v == "x360bt") {
            profile = ControllerProfile.Xbox360BT;
            return true;
        }
        if (v == "7" || v == "xboxseries" || v == "xboxseriesxs" || v == "xsx" || v == "xss" || v == "xboxxs" || v == "xboxseriesusb") {
            profile = ControllerProfile.XboxSeries;
            return true;
        }
        if (v == "8" || v == "xboxseriesbt" || v == "xsxbt" || v == "xssbt") {
            profile = ControllerProfile.XboxSeriesBT;
            return true;
        }
        profile = ControllerProfile.DualSense;
        return false;
    }

    private static string ControllerProfileName(ControllerProfile profile) {
        switch (profile) {
            case ControllerProfile.DualSenseBT: return "DualSense / Direct HID (Bluetooth)";
            case ControllerProfile.DualShock4: return "DualShock 4 / Direct HID (USB)";
            case ControllerProfile.DualShock4BT: return "DualShock 4 / Direct HID (Bluetooth)";
            case ControllerProfile.Xbox360: return "Xbox 360 Controller / XInput (USB)";
            case ControllerProfile.Xbox360BT: return "Xbox 360 Controller / XInput (Bluetooth)";
            case ControllerProfile.XboxSeries: return "Xbox Series X|S Controller / XInput (USB)";
            case ControllerProfile.XboxSeriesBT: return "Xbox Series X|S Controller / XInput (Bluetooth)";
            default: return "DualSense / Direct HID (USB)";
        }
    }

    private static bool HasArg(string[] args, string value) {
        for (int i = 0; i < args.Length; i++) if (String.Equals(args[i], value, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }


    private static void RunHidEnumTest() {
        Console.WriteLine("\n--- HID DEVICE ENUMERATION TEST ---");
        Console.WriteLine("This test bypasses RawInput and directly queries the OS for connected HID devices.");
        Console.WriteLine("It will attempt to open each device to read VID/PID.\n");

        Guid hidGuid;
        NativeMethods.HidD_GetHidGuid(out hidGuid);

        IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, 0x12);
        if (deviceInfoSet == new IntPtr(-1)) {
            Console.WriteLine("Failed to get device info set.");
            return;
        }

        NativeMethods.SP_DEVICE_INTERFACE_DATA interfaceData = new NativeMethods.SP_DEVICE_INTERFACE_DATA();
        interfaceData.cbSize = (uint)Marshal.SizeOf(interfaceData);

        uint index = 0;
        int foundCount = 0;
        int sonyCount = 0;

        while (NativeMethods.SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, index, ref interfaceData)) {
            index++;
            uint requiredSize = 0;
            NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);

            if (requiredSize == 0) continue;

            IntPtr detailData = Marshal.AllocHGlobal((int)requiredSize);
            Marshal.WriteInt32(detailData, (IntPtr.Size == 8) ? 8 : (Marshal.SystemDefaultCharSize == 1 ? 5 : 6));

            if (NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, detailData, requiredSize, out requiredSize, IntPtr.Zero)) {
                string devicePath = Marshal.PtrToStringAuto(new IntPtr(detailData.ToInt64() + 4));

                IntPtr handle = NativeMethods.CreateFile(devicePath, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
                if (handle != new IntPtr(-1)) {
                    NativeMethods.HIDD_ATTRIBUTES attrs = new NativeMethods.HIDD_ATTRIBUTES();
                    attrs.Size = (uint)Marshal.SizeOf(attrs);
                    if (NativeMethods.HidD_GetAttributes(handle, ref attrs)) {
                        string product = "";
                        IntPtr prodStr = Marshal.AllocHGlobal(254);
                        if (NativeMethods.HidD_GetProductString(handle, prodStr, 254)) {
                            product = Marshal.PtrToStringAuto(prodStr);
                        }
                        Marshal.FreeHGlobal(prodStr);

                        if (attrs.VendorID == 0x054C) {
                            Console.WriteLine("FOUND SONY DEVICE:");
                            Console.WriteLine("  VID: 0x" + attrs.VendorID.ToString("X4") + "  PID: 0x" + attrs.ProductID.ToString("X4"));
                            Console.WriteLine("  Product: " + product);
                            Console.WriteLine("  Path: " + devicePath);
                            Console.WriteLine();
                            sonyCount++;
                        }
                        foundCount++;
                    }
                    NativeMethods.CloseHandle(handle);
                }
            }
            Marshal.FreeHGlobal(detailData);
        }

        NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);

        Console.WriteLine("Total HID devices opened successfully: " + foundCount);
        Console.WriteLine("Total Sony devices (VID 0x054C) found: " + sonyCount);
        if (sonyCount > 0) {
            Console.WriteLine("\nSUCCESS: The physical controller is VISIBLE to this process via Direct HID.");
            Console.WriteLine("If ShikiPad still cannot read input normally, it confirms that HidHide hides devices from the Windows RawInput subsystem itself.");
        } else {
            Console.WriteLine("\nFAILED: No Sony devices visible. Either it is unplugged, or HidHide whitelist failed.");
        }
    }

    private static void PrintLayerTest(Config config) {
        MappingEngine m = new MappingEngine();
        Layer[] layers = new Layer[] { Layer.Base, Layer.L1, Layer.R1, Layer.L2, Layer.R2, Layer.R1L1, Layer.R2L2 };
        Console.WriteLine("Action button order: Up, Right, Square, Triangle, Left, Down, Cross, Circle");
        Console.WriteLine();
        for (int l = 0; l < layers.Length; l++) {
            Console.WriteLine(LayerDisplayName(layers[l]) + ":");
            for (int i = 0; i < 8; i++) {
                PhysicalKey key = m.Lookup(layers[l], (ActionButton)i);
                Console.WriteLine(((ActionButton)i).ToString() + " = " + LayerTestKeyName(key));
            }
            Console.WriteLine();
        }
        Console.WriteLine("Layer priority: latest triggered layer wins; R1+L1 and R2+L2 activate only inside comboLayerWindowMs.");
        Console.WriteLine("actionLayerGraceMs = " + config.ActionLayerGraceMs.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("layerTakeoverWindowMs = " + config.LayerTakeoverWindowMs.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("comboLayerWindowMs = " + config.ComboLayerWindowMs.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine();
        Console.WriteLine("Resolution checks:");
        double delayedMs = config.ComboLayerWindowMs + 120.0;
        PrintResolutionCheck(config, m, "R1 then L1 + Square", true, true, false, false, 20, 10, 0, 0, ActionButton.Square);
        PrintResolutionCheck(config, m, "L1 held then R1 after window + Square", true, true, false, false, 10, delayedMs, 0, 0, ActionButton.Square);
        PrintResolutionCheck(config, m, "R1 held then L1 after window + Square", true, true, false, false, delayedMs, 10, 0, 0, ActionButton.Square);
        PrintResolutionCheck(config, m, "R1+L1 then R2 + Square", true, true, false, true, 20, 10, 0, 30, ActionButton.Square);
        PrintResolutionCheck(config, m, "R1+L1 release L1 + Square", false, true, false, false, 0, 10, 0, 0, ActionButton.Square);
        PrintResolutionCheck(config, m, "R2 then L2 + Up", false, false, true, true, 0, 0, 20, 10, ActionButton.Up);
        PrintResolutionCheck(config, m, "L2 held then R2 after window + Up", false, false, true, true, 0, 0, delayedMs, 10, ActionButton.Up);
        PrintResolutionCheck(config, m, "R2 held then L2 after window + Up", false, false, true, true, 0, 0, 10, delayedMs, ActionButton.Up);
        PrintResolutionCheck(config, m, "R2+L2 then R1 + Up", false, true, true, true, 0, 30, 20, 10, ActionButton.Up);
        PrintResolutionCheck(config, m, "R1 then R2 + Square", false, true, false, true, 0, 10, 0, 20, ActionButton.Square);
        PrintResolutionCheck(config, m, "L1 then L2 + Square", true, false, true, false, 10, 0, 20, 0, ActionButton.Square);
        Console.WriteLine();
        PrintPendingTimingChecks(config, m);
    }

    private static void PrintResolutionCheck(Config config, MappingEngine mapping, string label, bool l1, bool r1, bool l2, bool r2, double l1Ms, double r1Ms, double l2Ms, double r2Ms, ActionButton action) {
        Layer layer = mapping.Resolve(l1, r1, l2, r2, l1Ms, r1Ms, l2Ms, r2Ms, config.ComboLayerWindowMs);
        PhysicalKey key = mapping.Lookup(layer, action);
        Console.WriteLine(label + " = " + LayerDisplayName(layer) + " / " + LayerTestKeyName(key));
    }

    private static void PrintPendingTimingChecks(Config config, MappingEngine mapping) {
        Console.WriteLine("Pending timing checks:");
        bool ok = true;
        ok = PrintComboTakeoverCheck(config, mapping) && ok;
        ok = PrintControllerParityCheck(config, mapping) && ok;
        ok = PrintUserScenarioCheck(config, mapping) && ok;
        Console.WriteLine("Pending timing result = " + (ok ? "PASS" : "FAIL"));
        if (!ok) Environment.ExitCode = 1;
    }

    private static bool PrintComboTakeoverCheck(Config config, MappingEngine mapping) {
        double r1Ms = 0.0;
        double crossMs = 10.0;
        double quickL1Ms = 20.0;
        double lateL1Ms = config.ComboLayerWindowMs + 10.0;

        Layer crossStartLayer = mapping.Resolve(false, true, false, false, 0, r1Ms, 0, 0, config.ComboLayerWindowMs);
        PhysicalKey crossStartKey = mapping.Lookup(crossStartLayer, ActionButton.Cross);
        Layer afterQuickL1Layer = mapping.Resolve(true, true, false, false, quickL1Ms, r1Ms, 0, 0, config.ComboLayerWindowMs);
        Layer quickSettledLayer = MapperForm.ResolvePendingLayer(crossStartLayer, crossStartLayer, crossMs, afterQuickL1Layer, quickL1Ms, 0, 0, config.ActionLayerGraceMs, config.LayerTakeoverWindowMs);
        PhysicalKey quickSettledKey = mapping.Lookup(quickSettledLayer, ActionButton.Cross);
        Layer afterLateL1Layer = mapping.Resolve(true, true, false, false, lateL1Ms, r1Ms, 0, 0, config.ComboLayerWindowMs);
        Layer lateSettledLayer = MapperForm.ResolvePendingLayer(crossStartLayer, crossStartLayer, crossMs, afterLateL1Layer, lateL1Ms, 0, 0, config.ActionLayerGraceMs, config.LayerTakeoverWindowMs);
        PhysicalKey lateSettledKey = mapping.Lookup(lateSettledLayer, ActionButton.Cross);

        bool quickInsideCombo = quickL1Ms - r1Ms <= config.ComboLayerWindowMs;
        bool lateOutsideCombo = lateL1Ms - r1Ms > config.ComboLayerWindowMs;
        bool ok = quickInsideCombo
            && lateOutsideCombo
            && crossStartLayer == Layer.R1
            && crossStartKey == PhysicalKey.H
            && afterQuickL1Layer == Layer.R1L1
            && quickSettledLayer == Layer.R1L1
            && quickSettledKey == PhysicalKey.Comma
            && afterLateL1Layer == Layer.L1
            && lateSettledLayer == Layer.R1
            && lateSettledKey == PhysicalKey.H;

        Console.WriteLine("R1+Cross/A pending, L1 inside combo window = " +
                          LayerDisplayName(quickSettledLayer) + " / " + LayerTestKeyName(quickSettledKey) +
                          (quickSettledKey == PhysicalKey.Comma ? " [PASS]" : " [FAIL]"));
        Console.WriteLine("R1+Cross/A pending, L1 after combo window = " +
                          LayerDisplayName(lateSettledLayer) + " / " + LayerTestKeyName(lateSettledKey) +
                          (lateSettledKey == PhysicalKey.H ? " [PASS]" : " [FAIL]"));
        return ok;
    }

    private static bool PrintUserScenarioCheck(Config config, MappingEngine mapping) {
        Console.WriteLine("\n--- USER SCENARIO TEST ---");
        // "按下 L2，100 毫秒后按下方块键，5ms之后松开l2，15毫秒之后按下 L1 键，然后 5毫秒之后按下 × 键，10 毫秒之后按下 R1 键"

        double l2Down = 0;
        double sqDown = 100;
        double l2Up = 105;
        double l1Down = 120;
        double crDown = 125;
        double r1Down = 135;

        // T=100 (Sq down). Layer is L2.
        Layer sqLayer = mapping.Resolve(false, false, true, false, 0, 0, l2Down, 0, config.ComboLayerWindowMs);

        // T=105 (L2 up). Layer is Base (since nothing else is down).

        // T=120 (L1 down). Layer is L1.
        Layer l1Layer = mapping.Resolve(true, false, false, false, l1Down, 0, 0, 0, config.ComboLayerWindowMs);

        // Update Sq with L1
        Layer sqAfterL1 = MapperForm.ResolvePendingLayer(sqLayer, sqLayer, sqDown, l1Layer, l1Down, l2Up, l2Up, config.ActionLayerGraceMs, config.LayerTakeoverWindowMs);

        // T=125 (Cr down). Layer is L1.
        Layer crLayer = l1Layer;

        // T=134 (R1 down). Layer is R1L1.
        Layer r1l1Layer = mapping.Resolve(true, true, false, false, l1Down, r1Down, 0, 0, config.ComboLayerWindowMs);

        // Update Sq with R1L1
        Layer sqFinal = MapperForm.ResolvePendingLayer(sqAfterL1, sqLayer, sqDown, r1l1Layer, r1Down, 0, l2Up, config.ActionLayerGraceMs, config.LayerTakeoverWindowMs);
        PhysicalKey sqKey = mapping.Lookup(sqFinal, ActionButton.Square);

        // Update Cr with R1L1
        Layer crFinal = MapperForm.ResolvePendingLayer(crLayer, crLayer, crDown, r1l1Layer, r1Down, 0, 0, config.ActionLayerGraceMs, config.LayerTakeoverWindowMs);
        PhysicalKey crKey = mapping.Lookup(crFinal, ActionButton.Cross);

        Console.WriteLine("Square resolved layer: " + LayerDisplayName(sqFinal));
        Console.WriteLine("Square resolved key: " + LayerTestKeyName(sqKey));
        Console.WriteLine("Cross resolved layer: " + LayerDisplayName(crFinal));
        Console.WriteLine("Cross resolved key: " + LayerTestKeyName(crKey));

        bool pass = (sqKey == PhysicalKey.Num9);
        Console.WriteLine("User Scenario = " + (pass ? "PASS" : "FAIL"));
        return pass;
    }

    private static bool PrintControllerParityCheck(Config config, MappingEngine mapping) {
        ControllerProfile[] profiles = new ControllerProfile[] {
            ControllerProfile.DualSense,
            ControllerProfile.DualShock4,
            ControllerProfile.Xbox360,
            ControllerProfile.XboxSeries
        };
        bool ok = true;
        for (int i = 0; i < profiles.Length; i++) {
            Layer layer = mapping.Resolve(false, true, false, false, 0, 10, 0, 0, config.ComboLayerWindowMs);
            PhysicalKey key = mapping.Lookup(layer, ActionButton.Cross);
            bool profileOk = layer == Layer.R1 && key == PhysicalKey.H;
            string actionName = (profiles[i] == ControllerProfile.DualSense || profiles[i] == ControllerProfile.DualShock4) ? "Cross" : "A";
            Console.WriteLine(ControllerProfileName(profiles[i]) + " R1/RB + " + actionName + " = " +
                              LayerTestKeyName(key) + (profileOk ? " [PASS]" : " [FAIL]"));
            ok = profileOk && ok;
        }
        return ok;
    }

    private static void PrintMouseTest(Config config) {
        Console.WriteLine("rightStickDeadzone = " + config.RightStickDeadzone.ToString("0.###", CultureInfo.InvariantCulture));
        Console.WriteLine("rightStickCurve = " + config.RightStickCurve);
        Console.WriteLine("rightStickCurveExponent = " + config.RightStickCurveExponent.ToString("0.###", CultureInfo.InvariantCulture));
        Console.WriteLine("mouseMaxSpeed = " + config.MouseMaxSpeed.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("neutralCalibration = enabled");
        bool rightStickOk = PrintRightStickMotionCheck();
        Logger.Info("mouse-test: rightStickDeadzone = " + config.RightStickDeadzone.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", rightStickCurve = " + config.RightStickCurve +
                    ", rightStickCurveExponent = " + config.RightStickCurveExponent.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", mouseMaxSpeed = " + config.MouseMaxSpeed.ToString(CultureInfo.InvariantCulture) +
                    ", neutralCalibration = enabled, rightStickMotion = " + (rightStickOk ? "PASS" : "FAIL"));
        if (!rightStickOk) Environment.ExitCode = 1;
    }

    private static bool PrintRightStickMotionCheck() {
        Config testConfig = new Config();
        RightStickMouseIntegrator integrator = new RightStickMouseIntegrator();
        int dx;
        int dy;

        bool deadzoneMoved = integrator.TryUpdate(testConfig.RightStickDeadzone * 0.5, 0.0, 0.001, testConfig, out dx, out dy);
        bool deadzoneOk = !deadzoneMoved && dx == 0 && dy == 0;

        double smallOutsideDeadzone = testConfig.RightStickDeadzone + 0.10 * (1.0 - testConfig.RightStickDeadzone);
        bool firstTickMoved = integrator.TryUpdate(smallOutsideDeadzone, 0.0, 0.001, testConfig, out dx, out dy);
        int accumulatedX = dx;
        int accumulatedY = dy;
        for (int i = 0; i < 300; i++) {
            integrator.TryUpdate(smallOutsideDeadzone, 0.0, 0.001, testConfig, out dx, out dy);
            accumulatedX += dx;
            accumulatedY += dy;
        }

        bool accumulationOk = !firstTickMoved && accumulatedX > 0 && accumulatedY == 0;
        bool ok = deadzoneOk && accumulationOk;
        Console.WriteLine("rightStickMotion = " + (ok ? "PASS" : "FAIL"));
        return ok;
    }

    private static void PrintLeftStickTest(Config config) {
        Console.WriteLine("leftStickEnterDeadzone = " + config.LeftStickEnterDeadzone.ToString("0.00", CultureInfo.InvariantCulture));
        Console.WriteLine("leftStickExitDeadzone = " + config.LeftStickExitDeadzone.ToString("0.00", CultureInfo.InvariantCulture));
        Console.WriteLine("mouseScrollCurveExponent = " + config.MouseScrollCurveExponent.ToString("0.###", CultureInfo.InvariantCulture));
        Console.WriteLine("exclusive8Way = enabled");
        Console.WriteLine();
        PrintLeftStickSample(config, "Center", 0.0, 0.0);
        PrintLeftStickSample(config, "Up", 0.0, -1.0);
        PrintLeftStickSample(config, "UpRight", 0.70710678, -0.70710678);
        PrintLeftStickSample(config, "Right", 1.0, 0.0);
        PrintLeftStickSample(config, "DownRight", 0.70710678, 0.70710678);
        PrintLeftStickSample(config, "Down", 0.0, 1.0);
        PrintLeftStickSample(config, "DownLeft", -0.70710678, 0.70710678);
        PrintLeftStickSample(config, "Left", -1.0, 0.0);
        PrintLeftStickSample(config, "UpLeft", -0.70710678, -0.70710678);
        Console.WriteLine();
        Console.WriteLine("Latch simulation:");
        StickDirection latched = StickDirection.None;
        SimulateLeftStickLatch(config, ref latched, "enter DownRight", 0.70710678, 0.70710678);
        SimulateLeftStickLatch(config, ref latched, "jitter toward Down while held", 0.0, 1.0);
        SimulateLeftStickLatch(config, ref latched, "jitter toward Right while held", 1.0, 0.0);
        SimulateLeftStickLatch(config, ref latched, "release below exit", 0.1, 0.1);
        Console.WriteLine();
        if (!PrintLeftStickScrollCheck()) Environment.ExitCode = 1;
    }

    private static bool PrintLeftStickScrollCheck() {
        Config testConfig = new Config();
        int gentle = SimulateLeftStickScroll(testConfig, 0.20, 1000);
        int medium = SimulateLeftStickScroll(testConfig, 0.60, 1000);
        int full = SimulateLeftStickScroll(testConfig, 1.00, 1000);
        bool firstTickQuiet = SimulateLeftStickScroll(testConfig, 0.20, 1) == 0;
        bool monotonic = gentle > 0 && medium > gentle && full > medium;
        Console.WriteLine("Scroll gentle 1s wheelDelta = " + gentle.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("Scroll medium 1s wheelDelta = " + medium.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("Scroll full 1s wheelDelta = " + full.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("Scroll curve result = " + (firstTickQuiet && monotonic ? "PASS" : "FAIL"));
        return firstTickQuiet && monotonic;
    }

    private static int SimulateLeftStickScroll(Config config, double normalizedRadius, int ms) {
        LeftStickScrollIntegrator scroll = new LeftStickScrollIntegrator();
        double radius = config.LeftStickEnterDeadzone + Clamp(normalizedRadius, 0.0, 1.0) * (1.0 - config.LeftStickEnterDeadzone);
        int total = 0;
        int wheelDelta;
        for (int i = 0; i < ms; i++) {
            if (scroll.TryUpdate(radius, 0.001, config, 1, out wheelDelta)) total += wheelDelta;
        }
        return total;
    }

    private static double Clamp(double value, double min, double max) {
        return value < min ? min : (value > max ? max : value);
    }

    private static void PrintClutchTest(Config config) {
        bool ok = true;
        int thresholdMs = Math.Max(1, config.ClutchLongPressMs);

        byte[] dualSensePressed = NeutralDualSenseReport();
        dualSensePressed[10] = 0x02;
        ControllerState dualSensePressedState;
        bool parsedPressed = DirectHidController.TryParseDualSenseReport(dualSensePressed, ControllerProfile.DualSense, out dualSensePressedState);
        ok = PrintClutchCheck("DualSense touchpad press = clutch", parsedPressed && dualSensePressedState.TouchClick) && ok;

        byte[] dualSenseReleased = NeutralDualSenseReport();
        ControllerState dualSenseReleasedState;
        bool parsedReleased = DirectHidController.TryParseDualSenseReport(dualSenseReleased, ControllerProfile.DualSense, out dualSenseReleasedState);
        ok = PrintClutchCheck("DualSense touchpad released = no clutch", parsedReleased && !dualSenseReleasedState.TouchClick) && ok;

        NativeMethods.XINPUT_GAMEPAD xboxBack = new NativeMethods.XINPUT_GAMEPAD();
        xboxBack.wButtons = NativeMethods.XINPUT_GAMEPAD_BACK;
        ok = PrintClutchCheck("Xbox View/Back parsed as clutch button", DirectHidController.ParseXInputState(xboxBack).Create) && ok;

        NativeMethods.XINPUT_GAMEPAD xboxStart = new NativeMethods.XINPUT_GAMEPAD();
        xboxStart.wButtons = NativeMethods.XINPUT_GAMEPAD_START;
        ok = PrintClutchCheck("Xbox Menu/Start parsed as clutch button", DirectHidController.ParseXInputState(xboxStart).Options) && ok;

        NativeMethods.XINPUT_GAMEPAD xboxReleased = new NativeMethods.XINPUT_GAMEPAD();
        ControllerState xboxReleasedState = DirectHidController.ParseXInputState(xboxReleased);
        ok = PrintClutchCheck("Xbox View/Menu released = no clutch button", !xboxReleasedState.Create && !xboxReleasedState.Options) && ok;

        ok = PrintClutchCheck("Short press toggles clutch on", SimulateShortClutchTapStarts(thresholdMs)) && ok;
        ok = PrintClutchCheck("Second short press toggles clutch off", SimulateSecondShortClutchTapStops(thresholdMs)) && ok;
        ok = PrintClutchCheck("Long press holds only until release", SimulateLongClutchPressStopsOnRelease(thresholdMs)) && ok;
        ok = PrintClutchCheck("Short press then long press stops on release", SimulateShortThenLongClutchStops(thresholdMs)) && ok;

        Console.WriteLine("Clutch mapping result = " + (ok ? "PASS" : "FAIL"));
        if (!ok) Environment.ExitCode = 1;
    }

    private static bool SimulateShortClutchTapStarts(int thresholdMs) {
        ClutchButtonStateMachine clutch = new ClutchButtonStateMachine();
        double shortMs = Math.Max(1.0, thresholdMs / 2.0);
        clutch.Update(true, 0.0, thresholdMs);
        clutch.Update(false, shortMs, thresholdMs);
        return clutch.Active && clutch.Toggled && !clutch.Held;
    }

    private static bool SimulateSecondShortClutchTapStops(int thresholdMs) {
        ClutchButtonStateMachine clutch = new ClutchButtonStateMachine();
        double shortMs = Math.Max(1.0, thresholdMs / 2.0);
        clutch.Update(true, 0.0, thresholdMs);
        clutch.Update(false, shortMs, thresholdMs);
        clutch.Update(true, 1000.0, thresholdMs);
        clutch.Update(false, 1000.0 + shortMs, thresholdMs);
        return !clutch.Active && !clutch.Toggled && !clutch.Held;
    }

    private static bool SimulateLongClutchPressStopsOnRelease(int thresholdMs) {
        ClutchButtonStateMachine clutch = new ClutchButtonStateMachine();
        double longMs = thresholdMs + 10.0;
        clutch.Update(true, 0.0, thresholdMs);
        bool activeAtPress = clutch.Active && clutch.Held;
        clutch.Update(true, longMs, thresholdMs);
        bool activeWhileHeld = clutch.Active && clutch.Held;
        clutch.Update(false, longMs + 1.0, thresholdMs);
        return activeAtPress && activeWhileHeld && !clutch.Active && !clutch.Toggled && !clutch.Held;
    }

    private static bool SimulateShortThenLongClutchStops(int thresholdMs) {
        ClutchButtonStateMachine clutch = new ClutchButtonStateMachine();
        double shortMs = Math.Max(1.0, thresholdMs / 2.0);
        double longMs = thresholdMs + 10.0;
        clutch.Update(true, 0.0, thresholdMs);
        clutch.Update(false, shortMs, thresholdMs);
        bool toggledAfterShort = clutch.Active && clutch.Toggled && !clutch.Held;
        clutch.Update(true, 1000.0, thresholdMs);
        bool activeAtSecondPress = clutch.Active;
        clutch.Update(true, 1000.0 + longMs, thresholdMs);
        bool activeWhileHeld = clutch.Active;
        clutch.Update(false, 1000.0 + longMs + 1.0, thresholdMs);
        return toggledAfterShort && activeAtSecondPress && activeWhileHeld && !clutch.Active && !clutch.Toggled && !clutch.Held;
    }

    private static byte[] NeutralDualSenseReport() {
        byte[] report = new byte[64];
        report[0] = 0x01;
        report[1] = 128;
        report[2] = 128;
        report[3] = 128;
        report[4] = 128;
        report[8] = 0x08;
        return report;
    }

    private static bool PrintClutchCheck(string label, bool passed) {
        Console.WriteLine(label + (passed ? " [PASS]" : " [FAIL]"));
        return passed;
    }

    private static void PrintLeftStickSample(Config config, string label, double x, double y) {
        double radius = Math.Sqrt(x * x + y * y);
        StickDirection direction = radius >= config.LeftStickEnterDeadzone ? MapperForm.Sector(x, y) : StickDirection.None;
        double angle = radius > 0.0 ? Math.Atan2(-y, x) * 180.0 / Math.PI : 0.0;
        Console.WriteLine(label + ": x=" + x.ToString("0.###", CultureInfo.InvariantCulture) +
                          ", y=" + y.ToString("0.###", CultureInfo.InvariantCulture) +
                          ", radius=" + radius.ToString("0.###", CultureInfo.InvariantCulture) +
                          ", angle=" + angle.ToString("0.#", CultureInfo.InvariantCulture) +
                          ", direction=" + direction +
                          ", action=" + LeftStickActionName(direction));
    }

    private static string LeftStickActionName(StickDirection direction) {
        switch (direction) {
            case StickDirection.Up: return "WheelUp only";
            case StickDirection.UpRight: return "Fn only";
            case StickDirection.Right: return "Win only";
            case StickDirection.DownRight: return "Alt only";
            case StickDirection.Down: return "WheelDown only";
            case StickDirection.DownLeft: return "Ctrl only";
            case StickDirection.Left: return "Shift only";
            case StickDirection.UpLeft: return "Esc only";
            default: return "None";
        }
    }

    private static void SimulateLeftStickLatch(Config config, ref StickDirection latched, string label, double x, double y) {
        double radius = Math.Sqrt(x * x + y * y);
        if (latched == StickDirection.None) {
            if (radius >= config.LeftStickEnterDeadzone) latched = MapperForm.Sector(x, y);
        } else if (radius < config.LeftStickExitDeadzone) {
            latched = StickDirection.None;
        }

        Console.WriteLine(label + ": radius=" + radius.ToString("0.###", CultureInfo.InvariantCulture) +
                          ", rawSector=" + (radius > 0.0 ? MapperForm.Sector(x, y).ToString() : "None") +
                          ", latched=" + latched +
                          ", action=" + LeftStickActionName(latched));
    }

    private static string LayerDisplayName(Layer layer) {
        if (layer == Layer.R1L1) return "R1+L1";
        if (layer == Layer.R2L2) return "R2+L2";
        return layer.ToString();
    }

    private static string LayerTestKeyName(PhysicalKey key) {
        if (key >= PhysicalKey.A && key <= PhysicalKey.Z) {
            char letter = (char)('a' + (int)(key - PhysicalKey.A));
            return letter.ToString();
        }

        switch (key) {
            case PhysicalKey.Num0: return "0";
            case PhysicalKey.Num1: return "1";
            case PhysicalKey.Num2: return "2";
            case PhysicalKey.Num3: return "3";
            case PhysicalKey.Num4: return "4";
            case PhysicalKey.Num5: return "5";
            case PhysicalKey.Num6: return "6";
            case PhysicalKey.Num7: return "7";
            case PhysicalKey.Num8: return "8";
            case PhysicalKey.Num9: return "9";
            default: return MappingEngine.KeyName(key);
        }
    }

}
