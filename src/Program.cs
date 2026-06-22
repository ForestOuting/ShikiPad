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
        PrintFullGradientBanner(width, panelWidth);
    }

    public static void PrintRunHint() {
        EnableAnsi();
        Console.Write("\x1b[0m");
    }

    private static void PrintFullGradientBanner(int width, int panelWidth) {
        bool zh = IsChineseUi();
        string[] logo = BuildShikiPadBlockLogo();

        try { Console.Clear(); } catch { }
        Console.WriteLine();

        WriteNeonRule(width, panelWidth, zh ? "ShikiPad \u63a7\u5236\u754c\u9762" : "ShikiPad Control Surface");

        int left = (width - panelWidth) / 2;
        Console.Write(new string(' ', left));
        WriteGradientText(left, width, RepeatPattern("\u2508", panelWidth), SeasonFlowStops());
        Console.WriteLine();

        WriteExtrudedLogo(width, logo, SeasonFlowStops());
        Console.WriteLine();
        WriteBannerStatus(width, panelWidth, zh);

        Console.Write(new string(' ', left));
        WriteGradientText(left, width, "\u2570" + new string('\u2500', panelWidth - 2) + "\u256f", SeasonFlowStops());
        Console.WriteLine();
        WriteSeasonDropShadow(width, panelWidth);
        Console.WriteLine("\x1b[0m");
    }

    private static void WriteBannerStatus(int width, int panelWidth, bool zh) {
        string text1 = zh ? "\u25c7  \u5168\u5c40\u952e\u9f20\u6620\u5c04\u5df2\u5c31\u7eea  \u25c7" : "\u25c7  GLOBAL MAPPING IS READY  \u25c7";
        string text2 = zh ? "\u2014\u2014  \u6309\u4e0b Enter \u952e\u5c55\u5f00\u8be6\u7ec6\u8bf4\u660e  \u2014\u2014" : "\u2014\u2014  PRESS ENTER FOR DETAILED MANUAL  \u2014\u2014";
        WriteEmbossedCenteredText(width, panelWidth, text1, SeasonFlowStops(), true);
        WriteEmbossedCenteredText(width, panelWidth, text2, SeasonFlowStops(), false);
    }

    private static string[] BuildShikiPadBlockLogo() {
        return new string[] {
            BlockLogo(" ########  ##    ## #### ##   ##  #### #######  ######  ######  "),
            BlockLogo("##         ##    ##  ##  ##  ##    ##  ##    ## ##   ##     ##  "),
            BlockLogo("##         ##    ##  ##  ## ##     ##  ##    ##      ##     ##  "),
            BlockLogo(" #######   ########  ##  ####      ##  #######  #######  ###### "),
            BlockLogo("      ##   ##    ##  ##  ####      ##  ##       ##   ## ##   ## "),
            BlockLogo("       ##  ##    ##  ##  ## ##     ##  ##       ##   ## ##   ## "),
            BlockLogo("########   ##    ##  ##  ##  ##    ##  ##       ##   ## ##   ## "),
            BlockLogo(" ########  ##    ## #### ##   ##  #### ##        ######  ###### ")
        };
    }

    private static string BlockLogo(string pattern) {
        return pattern.Replace('#', '\u2588');
    }

    public static void PrintDetailedManual(ControllerProfile profile, Config config) {
        try { Console.Clear(); } catch { }
        EnableAnsi();
        int width = GetConsoleWidth();
        int panelWidth = Math.Min(112, Math.Max(72, width - 6));
        bool zh = IsChineseUi();
        bool xbox = profile == ControllerProfile.Xbox360 || profile == ControllerProfile.XboxSeries ||
                    profile == ControllerProfile.Xbox360BT || profile == ControllerProfile.XboxSeriesBT;

        Console.WriteLine();
        WritePanelBorder(width, panelWidth, true, new Rgb(126, 226, 244));
        WritePanelTitle(width, panelWidth, zh ? "\u25c7 \u8be6\u7ec6\u6620\u5c04\u4e0e\u64cd\u4f5c\u8bf4\u660e \u25c7" : "\u25c7 DETAILED MAPPING MANUAL \u25c7", new Rgb(235, 247, 252));
        WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

        if (zh) {
            WritePanelLine(width, panelWidth, "  \u3010 \u65b0\u624b\u5fc5\u8bfb\uff1a\u5982\u4f55\u6253\u5b57\uff1f \u3011", "", new Rgb(255, 120, 150), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u2728 \u6838\u5fc3\u539f\u7406", "\u6309\u4f4f\u3010L/R\u80a9\u952e\u3011\u540c\u65f6\u6309\u3010\u53f3\u4fa7\u56db\u952e/\u5341\u5b57\u952e\u3011\uff0c\u5373\u53ef\u8f93\u5165\u5b57\u6bcd\u3001\u6570\u5b57\u548c\u6807\u70b9", new Rgb(255, 200, 220), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelLine(width, panelWidth, "  \u3010 \u5b57\u7b26\u8f93\u5165\u5c42\uff08\u6253\u5b57\u9762\u677f\uff09 \u3011", "\u57fa\u7840\u6309\u952e\uff1a" + (xbox ? "D-pad(\u5341\u5b57\u952e) + X Y A B" : "D-pad(\u5341\u5b57\u952e) + \u25a1 \u25b3 \u00d7 \u25cb"), new Rgb(255, 211, 106), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 \u57fa\u7840\u5c42 (\u4e0d\u6309\u80a9\u952e)", xbox ? "D-pad=\u65b9\u5411\u952e, X=\u7a7a\u683c, Y=\u9000\u683c, A=\u56de\u8f66, B=Tab" : "D-pad=\u65b9\u5411\u952e, \u25a1=\u7a7a\u683c, \u25b3=\u9000\u683c, \u00d7=\u56de\u8f66, \u25cb=Tab", new Rgb(255, 235, 180), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 L1 \u5c42 (\u6309\u4f4f L1)", "s r d g l c y z", new Rgb(255, 142, 206), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 R1 \u5c42 (\u6309\u4f4f R1)", "i n e a o t h u (\u5bf9\u5e948\u4e2a\u57fa\u7840\u952e\u987a\u5e8f:\u4e0a\u53f3 \u25a1\u25b3 \u5de6\u4e0b \u00d7\u25cb)", new Rgb(255, 142, 206), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 L2 \u5c42 (\u6309\u4f4f L2)", "k v 1 2 3 4 5 6", new Rgb(190, 133, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 R2 \u5c42 (\u6309\u4f4f R2)", "m w j x q f p b", new Rgb(190, 133, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 R1+L1 \u7ec4\u5408\u5c42", "7 8 9 0 - = , . (\u540c\u65f6\u6309\u4f4f\u4e24\u4e2a\u80a9\u952e)", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 R2+L2 \u7ec4\u5408\u5c42", "< ) [ { ( > } ]", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 L1+R2 \u7ec4\u5408\u5c42", "` \\ ' \" ; ~ / ?", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 R1+L2 \u7ec4\u5408\u5c42", "! @ # $ % ^ & *", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelLine(width, panelWidth, "  \u3010 \u6447\u6746\u4e0e\u9f20\u6807\u64cd\u4f5c \u3011", "", new Rgb(113, 255, 194), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 \u53f3\u6447\u6746", "\u63a7\u5236\u9f20\u6807\u79fb\u52a8\uff1bL3=\u5de6\u952e\uff0cR3=\u53f3\u952e(\u77ed\u6682\u9632\u6f02\u79fb)", new Rgb(200, 255, 220), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 \u5de6\u6447\u6746 (\u6eda\u8f6e)", "\u5411\u4e0a/\u5411\u4e0b\u63a8\u52a8\u4e3a\u9f20\u6807\u6eda\u8f6e\u3002\u63a8\u52a8\u8d8a\u6df1\u6eda\u52a8\u8d8a\u5feb", new Rgb(200, 255, 220), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 \u5de6\u6447\u6746 (\u4fee\u9970\u952e)", "\u5176\u4ed6\u65b9\u5411\u6620\u5c04: \u2197 Fn, \u2192 Win, \u2198 Alt, \u2199 Ctrl, \u2190 Shift, \u2196 Esc", new Rgb(200, 255, 220), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelLine(width, panelWidth, "  \u3010 \u9ad8\u7ea7\u673a\u5236\u4e0e\u5173\u95ed \u3011", "", new Rgb(126, 226, 244), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, xbox ? "  \u25b6 \u84c4\u529b\u4fee\u9970 (View/Menu)" : "  \u25b6 \u84c4\u529b\u4fee\u9970 (\u89e6\u63a7\u677f)", "\u77ed\u6309\u5207\u6362\uff0c\u957f\u6309\u4fdd\u6301\uff1b\u5de6\u6447\u6746\u53ef\u4f9d\u6b21\u6536\u96c6 Ctrl/Shift/Esc/Alt/Win", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 \u5bbd\u5bb9\u7a97\u53e3", "\u5b57\u7b26\u952e\u4e0e\u80a9\u952e\u524d\u540e\u5dee\u5728 " + config.ActionLayerGraceMs + "ms \u5185\uff0c\u4ecd\u4f1a\u5224\u5b9a\u4e3a\u540c\u4e00\u6b21\u5206\u5c42\u8f93\u5165", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 \u5982\u4f55\u5173\u95ed\u8f6f\u4ef6\uff1f", "\u76f4\u63a5\u70b9\u51fb\u672c\u7a97\u53e3\u53f3\u4e0a\u89d2\u7684 X \u5173\u95ed\u5373\u53ef\uff0c\u6240\u6709\u6309\u952e\u4f1a\u81ea\u52a8\u91ca\u653e", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
        } else {
            WritePanelLine(width, panelWidth, "  [ QUICK START ]", "", new Rgb(255, 120, 150), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u2728 How to type", "Hold L/R shoulder buttons and press D-Pad or Action keys to type.", new Rgb(255, 200, 220), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelLine(width, panelWidth, "  [ JOYSTICKS & MOUSE ]", "", new Rgb(113, 255, 194), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 Right stick", "Move mouse; L3 left click, R3 right click with brief freeze", new Rgb(200, 255, 220), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 Left stick (Scroll)", "Up/Down pushes scroll wheel. Deeper push means faster scroll", new Rgb(200, 255, 220), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 Left stick (Mods)", "UpRight Fn, Right Win, DownRight Alt, DownLeft Ctrl, Left Shift, UpLeft Esc", new Rgb(200, 255, 220), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));
            
            WritePanelLine(width, panelWidth, "  [ CHARACTER LAYERS ]", "Base Action Keys = " + (xbox ? "D-pad + X Y A B" : "D-pad + Square Triangle Cross Circle"), new Rgb(255, 211, 106), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 Base Layer", xbox ? "D-pad=arrows, X=Space, Y=Backspace, A=Enter, B=Tab" : "D-pad=arrows, Square=Space, Triangle=Backspace, Cross=Enter, Circle=Tab", new Rgb(255, 235, 180), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 R1 Layer", "i n e a o t h u", new Rgb(255, 142, 206), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 L1 Layer", "s r d g l c y z", new Rgb(255, 142, 206), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 R2 Layer", "m w j x q f p b", new Rgb(190, 133, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 L2 Layer", "k v 1 2 3 4 5 6", new Rgb(190, 133, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 R1+L1 Combo", "7 8 9 0 - = , .", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 R2+L2 Combo", "< ) [ { ( > } ]", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 L1+R2 Combo", "` \\ ' \" ; ~ / ?", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 R1+L2 Combo", "! @ # $ % ^ & *", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelLine(width, panelWidth, "  [ ADVANCED CONTROLS ]", "", new Rgb(126, 226, 244), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, xbox ? "  \u25b6 Clutch (View/Menu)" : "  \u25b6 Clutch (Touchpad)", "Short tap toggles; long press holds. Collect Ctrl/Shift/Esc/Alt/Win.", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 Grace Windows", "Action and layer inputs within " + config.ActionLayerGraceMs + "ms resolve as one layered stroke.", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u25b6 How to Exit", "Simply close this window to exit and release all keys automatically.", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
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

    private static void WriteGradientText(int absoluteX, int consoleWidth, string text, Rgb[] stops) {
        int logoWidth = 64;
        int logoLeft = Math.Max(0, (consoleWidth - 67) / 2);
        for (int i = 0; i < text.Length; i++) {
            int col = (absoluteX + i) - logoLeft;
            double t = Math.Max(0.0, Math.Min(col, logoWidth - 1)) / (double)(logoWidth - 1);
            WriteRgb(GradientAt(stops, t), text[i].ToString());
        }
    }

    private static void WriteEmbossedCenteredText(int width, int panelWidth, string text, Rgb[] stops, bool bold) {
        int left = (width - panelWidth) / 2;
        string line = CenterLine(panelWidth, text);
        Console.Write(new string(' ', left + 1));
        WriteGradientShadowGlyphs(left + 1, width, line, stops);
        Console.Write("\r");
        Console.Write(new string(' ', left));
        if (bold) Console.Write("\x1b[1m");
        WriteGradientText(left, width, line, stops);
        if (bold) Console.Write("\x1b[22m");
        Console.WriteLine();
    }

    private static void WriteGradientShadowGlyphs(int absoluteX, int consoleWidth, string text, Rgb[] stops) {
        int logoWidth = 64;
        int logoLeft = Math.Max(0, (consoleWidth - 67) / 2);
        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            if (c == ' ') {
                Console.Write(' ');
            } else {
                int col = (absoluteX + i) - logoLeft;
                double t = Math.Max(0.0, Math.Min(col, logoWidth - 1)) / (double)(logoWidth - 1);
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

    private static void WriteSeasonPanelBorder(int width, int panelWidth, bool top) {
        int left = (width - panelWidth) / 2;
        string line = (top ? "\u256d" : "\u2570") + new string('\u2500', panelWidth - 2) + (top ? "\u256e" : "\u256f");
        Console.Write(new string(' ', left));
        WriteGradientText(left, width, line, SeasonFlowStops());
        Console.WriteLine();
    }

    private static void WriteSeasonPanelSeparator(int width, int panelWidth) {
        int left = (width - panelWidth) / 2;
        Console.Write(new string(' ', left));
        WriteRgb(PanelInk(), "\u2502");
        WriteGradientText(left + 1, width, new string('\u2504', panelWidth - 2), SeasonFlowStops());
        WriteRgb(PanelInk(), "\u2502");
        Console.WriteLine();
    }

    private static void WriteSeasonPanelTitle(int width, int panelWidth, string title) {
        int left = (width - panelWidth) / 2;
        Console.Write(new string(' ', left));
        WriteRgb(PanelInk(), "\u2502");
        Console.Write("\x1b[1m");
        WriteGradientText(left + 1, width, CenterLine(panelWidth - 2, title), SeasonFlowStops());
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

    private static bool _shutdownReleaseRegistered;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsole();

    [STAThread]
    private static int Main(string[] args) {
        AllocConsole();
        StreamWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        Console.SetOut(writer);
        try { Console.Title = "ShikiPad"; } catch { }
        PrintGradientBanner();

        string root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        Directory.SetCurrentDirectory(root);
        Logger.Init(root);
        Config config = new Config();
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

}
