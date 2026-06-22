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
    private const string DefaultControllerFileName = "shikipad.default";
    private const int DefaultControllerGraceMs = 1200;

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
        string text2 = zh ? "\u2014\u2014  \u8fd0\u884c\u540e\u6309 Enter \u6253\u5f00\u8bf4\u660e\uff0c\u8bf4\u660e\u9875\u518d\u6309 Enter \u8fd4\u56de  \u2014\u2014" : "\u2014\u2014  PRESS ENTER FOR THE MANUAL, THEN ENTER AGAIN TO RETURN  \u2014\u2014";
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
        int panelWidth = Math.Min(116, Math.Max(72, width - 6));
        bool zh = IsChineseUi();
        bool xbox = profile == ControllerProfile.Xbox360 || profile == ControllerProfile.XboxSeries ||
                    profile == ControllerProfile.Xbox360BT || profile == ControllerProfile.XboxSeriesBT;

        Console.WriteLine();
        WritePanelBorder(width, panelWidth, true, new Rgb(126, 226, 244));
        WritePanelTitle(width, panelWidth, zh ? "\u25c7 \u6620\u5c04\u8bf4\u660e  |  Enter \u8fd4\u56de ShikiPad \u4e3b\u754c\u9762 \u25c7" : "\u25c7 MAPPING MANUAL  |  ENTER RETURNS TO SHIKIPAD HOME \u25c7", new Rgb(235, 247, 252));
        WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

        if (zh) {
            WritePanelLine(width, panelWidth, "  \u5982\u4f55\u6253\u5b57", "\u6309\u4f4f L1/R1/L2/R2 \u4e4b\u4e00\uff0c\u518d\u6309\u5341\u5b57\u952e\u6216\u53f3\u4fa7\u56db\u952e", new Rgb(255, 200, 220), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u6309\u952e\u987a\u5e8f", xbox ? "\u2191 \u2192 X Y \u2190 \u2193 A B" : "\u2191 \u2192 \u25a1 \u25b3 \u2190 \u2193 \u00d7 \u25cb", new Rgb(255, 211, 106), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelTwinLine(width, panelWidth, "\u57fa\u7840", xbox ? "\u2191 \u2192 Space Back \u2190 \u2193 Enter Tab" : "\u2191 \u2192 Space Back \u2190 \u2193 Enter Tab", "R1/RB", "i n e a o t h u", new Rgb(255, 235, 180), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "L1/LB", "s r d g l c y z", "R2/RT", "m w j x q f p b", new Rgb(255, 142, 206), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "L2/LT", "k v 1 2 3 4 5 6", "R1+L1", "7 8 9 0 - = , .", new Rgb(190, 133, 255), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "R2+L2", "< ) [ { ( > } ]", "L1+R2", "` \\ ' \" ; ~ / ?", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "R1+L2", "! @ # $ % ^ & *", "Fn", "1..0/-/= \u2192 F1..F12", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelTwinLine(width, panelWidth, "\u53f3\u6447\u6746", "\u9f20\u6807\u79fb\u52a8\uff1bL3 \u5de6\u952e / R3 \u53f3\u952e", "\u5de6\u6447\u6746", "\u2191/\u2193 \u6eda\u8f6e\uff0c\u63a8\u5f97\u8d8a\u6df1\u8d8a\u5feb", new Rgb(113, 255, 194), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u5de6\u6447\u6746\u4fee\u9970", "\u2190 Shift / \u2199 Ctrl / \u2198 Alt / \u2192 Win / \u2196 Esc / \u2197 Fn", new Rgb(200, 255, 220), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, xbox ? "  \u84c4\u529b\u4fee\u9970" : "  \u84c4\u529b\u4fee\u9970", xbox ? "View/Menu \u77ed\u6309\u5207\u6362\uff0c\u957f\u6309\u4fdd\u6301\uff1b\u5de6\u6447\u6746\u4f9d\u6b21\u6536\u96c6\u7ec4\u5408\u952e" : "\u89e6\u63a7\u677f\u77ed\u6309\u5207\u6362\uff0c\u957f\u6309\u4fdd\u6301\uff1b\u5de6\u6447\u6746\u4f9d\u6b21\u6536\u96c6\u7ec4\u5408\u952e", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelLine(width, panelWidth, "  \u8fd4\u56de\u4e3b\u754c\u9762", "\u518d\u6309\u4e00\u6b21 Enter\uff1b\u5173\u95ed\u8fd9\u4e2a\u7a97\u53e3\u5373\u53ef\u9000\u51fa ShikiPad", new Rgb(126, 226, 244), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u5bbd\u5bb9\u7a97\u53e3", "\u5b57\u7b26\u952e\u4e0e\u80a9\u952e\u5728 " + config.ActionLayerGraceMs + "ms \u5185\u53ef\u89c6\u4e3a\u540c\u4e00\u6b21\u5206\u5c42\u8f93\u5165", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
        } else {
            WritePanelLine(width, panelWidth, "  How to type", "Hold L1/R1/L2/R2, then press D-pad or action buttons.", new Rgb(255, 200, 220), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Button order", xbox ? "Up Right X Y Left Down A B" : "Up Right Square Triangle Left Down Cross Circle", new Rgb(255, 211, 106), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelTwinLine(width, panelWidth, "Base", "arrows / Space / Back / Enter / Tab", "R1/RB", "i n e a o t h u", new Rgb(255, 235, 180), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "L1/LB", "s r d g l c y z", "R2/RT", "m w j x q f p b", new Rgb(255, 142, 206), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "L2/LT", "k v 1 2 3 4 5 6", "R1+L1", "7 8 9 0 - = , .", new Rgb(190, 133, 255), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "R2+L2", "< ) [ { ( > } ]", "L1+R2", "` \\ ' \" ; ~ / ?", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "R1+L2", "! @ # $ % ^ & *", "Fn", "1..0/-/= -> F1..F12", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelTwinLine(width, panelWidth, "Right stick", "Mouse; L3 left, R3 right", "Left stick", "Up/Down scrolls faster when deeper", new Rgb(113, 255, 194), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Left stick mods", "Left Shift / DownLeft Ctrl / DownRight Alt / Right Win / UpLeft Esc / UpRight Fn", new Rgb(200, 255, 220), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Clutch", xbox ? "Tap View/Menu to toggle, or long-press to hold collected modifiers." : "Tap Touchpad to toggle, or long-press to hold collected modifiers.", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelLine(width, panelWidth, "  Return home", "Press Enter again. Close this window to exit ShikiPad safely.", new Rgb(126, 226, 244), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Grace window", "Inputs within " + config.ActionLayerGraceMs + "ms resolve as one layered stroke.", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
        }

        WritePanelBorder(width, panelWidth, false, new Rgb(126, 226, 244));
        Console.WriteLine("\x1b[0m");
    }

    public static void PrintConnectedWelcome(ControllerProfile profile, Config config, string deviceName) {
        PrintHomeSurface(profile, config, deviceName, true, true);
    }

    public static void PrintRunningHome(ControllerProfile profile, Config config, string deviceName, bool connected) {
        PrintHomeSurface(profile, config, deviceName, connected, false);
    }

    private static void PrintHomeSurface(ControllerProfile profile, Config config, string deviceName, bool connected, bool pageBreak) {
        EnableAnsi();
        if (pageBreak) {
            WritePageBreak();
        } else {
            try { Console.Clear(); } catch { }
        }

        int width = GetConsoleWidth();
        int panelWidth = Math.Min(112, Math.Max(72, width - 6));
        bool zh = IsChineseUi();
        Console.WriteLine();
        WriteNeonRule(width, panelWidth, zh ? "ShikiPad \u5df2\u5c31\u7eea" : "ShikiPad Is Ready");
        WriteExtrudedLogo(width, BuildShikiPadBlockLogo(), SeasonFlowStops());
        Console.WriteLine();
        WriteEmbossedCenteredText(width, panelWidth, zh ? "\u6b22\u8fce\u6765\u5230 ShikiPad" : "WELCOME TO SHIKIPAD", SeasonGlowStops(), true);
        Console.WriteLine();
        WritePanelBorder(width, panelWidth, true, new Rgb(126, 226, 244));
        WritePanelTitle(width, panelWidth, zh ? "\u4e3b\u754c\u9762" : "HOME", new Rgb(235, 247, 252));
        WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));
        WritePanelLine(width, panelWidth, zh ? "  \u624b\u67c4" : "  Controller", String.IsNullOrEmpty(deviceName) ? ControllerProfileName(profile) : deviceName, new Rgb(113, 255, 194), new Rgb(245, 250, 255));
        WritePanelLine(width, panelWidth, zh ? "  \u72b6\u6001" : "  Status", connected ? (zh ? "\u5df2\u8fde\u63a5\uff0c\u952e\u9f20\u6620\u5c04\u6b63\u5728\u8fd0\u884c" : "Connected; keyboard and mouse mapping is running") : (zh ? "\u7b49\u5f85\u624b\u67c4\u8fde\u63a5" : "Waiting for controller connection"), new Rgb(255, 211, 106), new Rgb(245, 250, 255));
        WritePanelLine(width, panelWidth, zh ? "  \u8bf4\u660e" : "  Manual", zh ? "\u6309 Enter \u6253\u5f00\u6620\u5c04\u8bf4\u660e\uff1b\u8bf4\u660e\u9875\u518d\u6309 Enter \u8fd4\u56de" : "Press Enter for the manual; press Enter again to return", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
        WritePanelLine(width, panelWidth, zh ? "  \u9000\u51fa" : "  Exit", zh ? "\u5173\u95ed\u8fd9\u4e2a\u7a97\u53e3\uff0cShikiPad \u4f1a\u81ea\u52a8\u91ca\u653e\u6309\u952e" : "Close this window; ShikiPad releases held inputs automatically", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
        WritePanelBorder(width, panelWidth, false, new Rgb(126, 226, 244));
        Console.WriteLine("\x1b[0m");
    }

    private static void WritePageBreak() {
        int lines = 28;
        try {
            lines = Math.Max(28, Console.WindowHeight + 6);
        } catch { }
        for (int i = 0; i < lines; i++) Console.WriteLine();
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

    private static void WritePanelTwinLine(int width, int panelWidth, string leftLabel, string leftValue, string rightLabel, string rightValue, Rgb labelColor, Rgb valueColor) {
        int left = (width - panelWidth) / 2;
        int inner = panelWidth - 2;
        int gap = 3;
        int column = (inner - gap) / 2;
        string leftText = leftLabel + ": " + leftValue;
        string rightText = rightLabel + ": " + rightValue;

        Console.Write(new string(' ', left));
        WriteRgb(new Rgb(72, 91, 101), "\u2502");
        WriteTwinCell(leftText, column, labelColor, valueColor);
        WriteRgb(new Rgb(72, 91, 101), " \u2506 ");
        WriteTwinCell(rightText, inner - column - gap, labelColor, valueColor);
        WriteRgb(new Rgb(72, 91, 101), "\u2502");
        Console.WriteLine();
    }

    private static void WriteTwinCell(string text, int width, Rgb labelColor, Rgb valueColor) {
        int colon = text.IndexOf(": ", StringComparison.Ordinal);
        string label = colon >= 0 ? text.Substring(0, colon + 2) : "";
        string value = colon >= 0 ? text.Substring(colon + 2) : text;
        string combined = label + value;
        if (DisplayWidth(combined) > width) combined = TrimToWidth(combined, width);

        int labelChars = Math.Min(label.Length, combined.Length);
        string visibleLabel = combined.Substring(0, labelChars);
        string visibleValue = combined.Substring(labelChars);
        Console.Write("\x1b[1m");
        WriteRgb(labelColor, visibleLabel);
        Console.Write("\x1b[22m");
        WriteRgb(valueColor, PadRight(visibleValue, width - DisplayWidth(visibleLabel)));
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

        ControllerProfile controllerProfile = SelectControllerProfile(args, root);
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

    private static ControllerProfile SelectControllerProfile(string[] args, string root) {
        ControllerProfile fromArgs;
        if (TryGetControllerProfileArg(args, out fromArgs)) return fromArgs;
        string defaultPath = Path.Combine(root, DefaultControllerFileName);
        bool forceMenu = HasAnyArg(args, "--controller-menu", "--choose-controller", "--select-controller");
        if (HasAnyArg(args, "--clear-default-controller", "--reset-default-controller", "--forget-controller")) {
            ClearDefaultControllerProfile(defaultPath);
            PrintDefaultControllerCleared();
            forceMenu = true;
        }

        ControllerProfile savedDefault;
        bool hasSavedDefault = TryLoadDefaultControllerProfile(defaultPath, out savedDefault);
        if (hasSavedDefault && !forceMenu) {
            bool inputRedirected = false;
            try { inputRedirected = Console.IsInputRedirected; } catch { }
            if (inputRedirected || !ShouldOpenControllerMenuForDefault(savedDefault)) {
                return savedDefault;
            }
        }

        try {
            if (Console.IsInputRedirected) return ControllerProfile.DualSense;
        } catch { }

        return PromptControllerProfile(defaultPath, hasSavedDefault, savedDefault);
    }

    private static ControllerProfile PromptControllerProfile(string defaultPath, bool hasSavedDefault, ControllerProfile savedDefault) {
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
        if (hasSavedDefault) {
            WriteSeasonPanelSeparator(width, panelWidth);
            WritePanelLine(width, panelWidth, zh ? "  当前默认启动" : "  Saved default", ControllerProfileName(savedDefault), SeasonGold(), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, zh ? "  退出默认启动" : "  Clear default", zh ? "输入 0 后回车，之后每次启动都会重新选择手柄" : "Type 0 and press Enter to ask every time again", SeasonAutumn(), new Rgb(245, 250, 255));
        }
        WriteSeasonPanelBorder(width, panelWidth, false);
        WriteSeasonDropShadow(width, panelWidth);
        Console.WriteLine();

        while (true) {
            WriteRgb(SeasonSummer(), hasSavedDefault
                ? (zh ? "选择手柄型号 [1..8，Enter = 1，0 = 退出默认启动] > " : "Select controller profile [1..8, Enter = 1, 0 = clear default] > ")
                : (zh ? "选择手柄型号 [1..8，Enter = 1] > " : "Select controller profile [1..8, Enter = 1] > "));
            Console.Write("\x1b[0m");
            string line = Console.ReadLine();
            if (line == null) return ControllerProfile.DualSense;
            line = line.Trim();
            if (line == "0") {
                ClearDefaultControllerProfile(defaultPath);
                hasSavedDefault = false;
                WriteRgb(SeasonGold(), zh ? "已关闭默认启动。现在请选择本次要使用的手柄。\n" : "Default launch cleared. Choose a controller for this run.\n");
                continue;
            }

            ControllerProfile selected;
            if (TryParseMenuControllerProfile(line.Length == 0 ? "1" : line, out selected)) {
                MaybeSaveDefaultControllerProfile(defaultPath, selected);
                return selected;
            }
            WriteRgb(SeasonAutumn(), zh ? "请选择 1 到 8 之间的数字，或输入 0 关闭默认启动。\n" : "Please choose 1 to 8, or 0 to clear the saved default.\n");
        }
    }

    private static bool ShouldOpenControllerMenuForDefault(ControllerProfile savedDefault) {
        EnableAnsi();
        int width = GetConsoleWidth();
        int panelWidth = Math.Min(104, Math.Max(66, width - 6));
        bool zh = IsChineseUi();
        WriteSeasonPanelBorder(width, panelWidth, true);
        WriteSeasonPanelTitle(width, panelWidth, zh ? "◇ 默认手柄启动 ◇" : "◇ DEFAULT CONTROLLER LAUNCH ◇");
        WriteSeasonPanelSeparator(width, panelWidth);
        WritePanelLine(width, panelWidth, zh ? "  默认手柄" : "  Default", ControllerProfileName(savedDefault), SeasonGold(), new Rgb(245, 250, 255));
        WritePanelLine(width, panelWidth, zh ? "  自动继续" : "  Auto start", zh ? "无需输入；如需重新选择或退出默认启动，请按 C" : "No input needed; press C to choose or clear default", SeasonSummer(), new Rgb(245, 250, 255));
        WriteSeasonPanelBorder(width, panelWidth, false);
        WriteSeasonDropShadow(width, panelWidth);
        Console.WriteLine();

        DateTime end = DateTime.UtcNow.AddMilliseconds(DefaultControllerGraceMs);
        while (DateTime.UtcNow < end) {
            try {
                if (Console.KeyAvailable) {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    char c = Char.ToUpperInvariant(key.KeyChar);
                    if (c == 'C' || c == 'M' || key.Key == ConsoleKey.Enter) return true;
                    if (c == 'D' || key.Key == ConsoleKey.Delete || key.Key == ConsoleKey.Backspace) {
                        return true;
                    }
                }
            } catch {
                return false;
            }
            Thread.Sleep(50);
        }
        return false;
    }

    private static void MaybeSaveDefaultControllerProfile(string defaultPath, ControllerProfile profile) {
        bool zh = IsChineseUi();
        WriteRgb(SeasonGold(), zh ? "将「" + ControllerProfileName(profile) + "」设为默认启动？[Enter/Y = 保存，N = 仅本次] > " : "Save \"" + ControllerProfileName(profile) + "\" as the default launch profile? [Enter/Y = yes, N = once] > ");
        Console.Write("\x1b[0m");
        string line = Console.ReadLine();
        if (line == null) return;
        line = line.Trim();
        if (line.Length > 0 && line.StartsWith("n", StringComparison.OrdinalIgnoreCase)) return;
        SaveDefaultControllerProfile(defaultPath, profile);
    }

    private static bool TryParseMenuControllerProfile(string value, out ControllerProfile profile) {
        value = (value ?? "").Trim();
        if (value == "1") { profile = ControllerProfile.DualSense; return true; }
        if (value == "2") { profile = ControllerProfile.DualSenseBT; return true; }
        if (value == "3") { profile = ControllerProfile.DualShock4; return true; }
        if (value == "4") { profile = ControllerProfile.DualShock4BT; return true; }
        if (value == "5") { profile = ControllerProfile.Xbox360; return true; }
        if (value == "6") { profile = ControllerProfile.Xbox360BT; return true; }
        if (value == "7") { profile = ControllerProfile.XboxSeries; return true; }
        if (value == "8") { profile = ControllerProfile.XboxSeriesBT; return true; }
        return TryParseControllerProfile(value, out profile);
    }

    private static bool TryLoadDefaultControllerProfile(string defaultPath, out ControllerProfile profile) {
        profile = ControllerProfile.DualSense;
        try {
            if (!File.Exists(defaultPath)) return false;
            string value = File.ReadAllText(defaultPath, Encoding.UTF8).Trim();
            return TryParseControllerProfile(value, out profile);
        } catch {
            return false;
        }
    }

    private static void SaveDefaultControllerProfile(string defaultPath, ControllerProfile profile) {
        bool zh = IsChineseUi();
        try {
            File.WriteAllText(defaultPath, ControllerProfileKey(profile) + Environment.NewLine, Encoding.UTF8);
            WriteRgb(SeasonSpring(), zh ? "已保存默认启动。以后直接运行 ShikiPad 即会自动使用这个手柄型号。\n" : "Default launch profile saved.\n");
        } catch (Exception ex) {
            WriteRgb(SeasonAutumn(), (zh ? "默认启动保存失败：" : "Could not save default profile: ") + ex.Message + "\n");
        }
    }

    private static void ClearDefaultControllerProfile(string defaultPath) {
        try {
            if (File.Exists(defaultPath)) File.Delete(defaultPath);
        } catch { }
    }

    private static void PrintDefaultControllerCleared() {
        EnableAnsi();
        bool zh = IsChineseUi();
        WriteRgb(SeasonGold(), zh ? "默认启动已关闭；ShikiPad 会重新显示手柄选择。\n" : "Default launch has been cleared; ShikiPad will show controller selection.\n");
        Console.Write("\x1b[0m");
    }

    private static string ControllerProfileKey(ControllerProfile profile) {
        switch (profile) {
            case ControllerProfile.DualSenseBT: return "dualsensebt";
            case ControllerProfile.DualShock4: return "dualshock4";
            case ControllerProfile.DualShock4BT: return "dualshock4bt";
            case ControllerProfile.Xbox360: return "xbox360";
            case ControllerProfile.Xbox360BT: return "xbox360bt";
            case ControllerProfile.XboxSeries: return "xboxseries";
            case ControllerProfile.XboxSeriesBT: return "xboxseriesbt";
            default: return "dualsense";
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

    private static bool HasAnyArg(string[] args, params string[] values) {
        for (int i = 0; i < values.Length; i++) {
            if (HasArg(args, values[i])) return true;
        }
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
