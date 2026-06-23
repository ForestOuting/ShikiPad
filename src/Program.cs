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
    private static bool _controllerSelectionExitRequested;

    public static void PrintGradientBanner() {
        EnableAnsi();
        PrintInitialControllerSurface(false, ControllerProfile.DualSense);
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
        WriteNeonRule(width, panelWidth, zh ? "ShikiPad \u542f\u52a8\u4e2d" : "ShikiPad Booting");

        WriteExtrudedLogo(width, logo, SeasonFlowStops());
        WriteEmbossedCenteredText(width, panelWidth, zh ? "\u7528\u624b\u67c4\u5199\u5b57\uff0c\u7528\u6447\u6746\u63a7\u5236\u684c\u9762" : "TYPE WITH A CONTROLLER, STEER THE DESKTOP", SeasonGlowStops(), true);
        WriteBannerStatus(width, panelWidth, zh);

        WritePanelBorder(width, panelWidth, true, new Rgb(126, 226, 244));
        WritePanelTitle(width, panelWidth, zh ? "\u542f\u52a8\u9875" : "STARTUP", new Rgb(235, 247, 252));
        WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));
        WritePanelTwinLine(width, panelWidth, zh ? "\u7b2c\u4e00\u6b65" : "Step 1", zh ? "\u9009\u62e9\u624b\u67c4\u578b\u53f7" : "Choose controller profile", zh ? "\u7b2c\u4e8c\u6b65" : "Step 2", zh ? "\u53ef\u4fdd\u5b58\u4e3a\u9ed8\u8ba4\u542f\u52a8" : "Optionally save as default", SeasonSpring(), new Rgb(245, 250, 255));
        WritePanelTwinLine(width, panelWidth, zh ? "\u8fd0\u884c\u4e2d" : "While running", zh ? "Enter \u6253\u5f00\u8bf4\u660e / \u518d\u6309\u8fd4\u56de" : "Enter opens manual / returns home", zh ? "\u9000\u51fa" : "Exit", zh ? "\u5173\u95ed\u672c\u7a97\u53e3\u5373\u53ef\u91ca\u653e\u6309\u952e" : "Close this window to release held inputs", SeasonGold(), new Rgb(245, 250, 255));
        WritePanelBorder(width, panelWidth, false, new Rgb(126, 226, 244));
        WriteSeasonDropShadow(width, panelWidth);
        FillViewportWithSignal(width, panelWidth, zh ? "ShikiPad \u5c06\u8fdb\u5165\u624b\u67c4\u9009\u62e9\u6216\u9ed8\u8ba4\u542f\u52a8" : "ShikiPad will continue to selection or default launch");
        Console.WriteLine("\x1b[0m");
    }

    private static void WriteBannerStatus(int width, int panelWidth, bool zh) {
        string text1 = zh ? "[ \u5168\u5c40\u952e\u9f20\u6620\u5c04\u5df2\u5c31\u7eea ]" : "[ GLOBAL MAPPING IS READY ]";
        string text2 = zh ? "[ Enter \u6253\u5f00\u8bf4\u660e / \u8bf4\u660e\u9875\u518d\u6309 Enter \u8fd4\u56de ]" : "[ ENTER OPENS THE MANUAL / ENTER RETURNS HOME ]";
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

        WriteHudRail(width, panelWidth, "映射说明", "Enter 返回主界面 | Esc 退出");
        WriteSignalWeave(width, panelWidth, 1, "MANUAL");
        WritePanelBorder(width, panelWidth, true, new Rgb(126, 226, 244));
        WritePanelTitle(width, panelWidth, "[ 映射说明 | Enter 返回主界面 | Esc 退出 ]", new Rgb(235, 247, 252));
        WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

        if (zh) {
            WritePanelSectionTitle(width, panelWidth, "> \u6253\u5b57\u5165\u53e3", "\u5148\u6309\u5c42\uff0c\u518d\u6309\u952e\uff1b\u8fd9\u662f ShikiPad \u6700\u6838\u5fc3\u7684\u8f93\u5165\u8282\u594f");
            WritePanelWrappedLine(width, panelWidth, "  \u5982\u4f55\u6253\u5b57", "\u6309\u4f4f L1 / R1 / L2 / R2 \u4e4b\u4e00\uff0c\u518d\u6309\u5341\u5b57\u952e\u6216\u53f3\u4fa7\u56db\u952e\uff0c\u5373\u53ef\u8f93\u5165\u5b57\u6bcd\u3001\u6570\u5b57\u548c\u6807\u70b9\u3002\u57fa\u7840\u5c42\u7528\u4e8e\u65b9\u5411\u3001\u7a7a\u683c\u3001\u9000\u683c\u3001\u56de\u8f66\u548c Tab\u3002", new Rgb(255, 200, 220), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u6309\u952e\u987a\u5e8f", xbox ? "\u2191  \u2192  X  Y  \u2190  \u2193  A  B" : "\u2191  \u2192  \u25a1  \u25b3  \u2190  \u2193  \u00d7  \u25cb", new Rgb(255, 211, 106), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelSectionTitle(width, panelWidth, "> \u5b57\u7b26\u5c42\u77e9\u9635", "\u4e0b\u65b9\u6bcf\u884c\u90fd\u6309\u540c\u4e00\u4e2a\u6309\u952e\u987a\u5e8f\u8bfb\u53d6");
            WritePanelTwinLine(width, panelWidth, "\u57fa\u7840", "\u2191 \u2192 Space Back \u2190 \u2193 Enter Tab", "R1/RB", "i n e a o t h u", new Rgb(255, 235, 180), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "L1/LB", "s r d g l c y z", "R2/RT", "m w j x q f p b", new Rgb(255, 142, 206), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "L2/LT", "k v 1 2 3 4 5 6", "R1+L1", "7 8 9 0 - = , .", new Rgb(190, 133, 255), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "R2+L2", "< ) [ { ( > } ]", "L1+R2", "` \\ ' \" ; ~ / ?", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "R1+L2", "! @ # $ % ^ & *", "Fn", "1..0/-/= \u2192 F1..F12", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelSectionTitle(width, panelWidth, "> \u6447\u6746\u4e0e\u9f20\u6807", "\u53f3\u6447\u6746\u4e3b\u63a7\u5149\u6807\uff0c\u5de6\u6447\u6746\u4e3b\u63a7\u6eda\u8f6e\u548c\u4fee\u9970\u952e");
            WritePanelTwinLine(width, panelWidth, "\u53f3\u6447\u6746", "\u9f20\u6807\u79fb\u52a8\uff1bL3 \u5de6\u952e / R3 \u53f3\u952e", "\u5de6\u6447\u6746", "\u2191/\u2193 \u6eda\u8f6e\uff0c\u63a8\u5f97\u8d8a\u6df1\u8d8a\u5feb", new Rgb(113, 255, 194), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u5de6\u6447\u6746\u4fee\u9970", "\u2190 Shift / \u2199 Ctrl / \u2198 Alt / \u2192 Win / \u2196 Esc / \u2197 Fn", new Rgb(200, 255, 220), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, xbox ? "  \u84c4\u529b\u4fee\u9970" : "  \u84c4\u529b\u4fee\u9970", xbox ? "View/Menu \u77ed\u6309\u5207\u6362\uff0c\u957f\u6309\u4fdd\u6301\uff1b\u5de6\u6447\u6746\u4f9d\u6b21\u6536\u96c6\u7ec4\u5408\u952e" : "\u89e6\u63a7\u677f\u77ed\u6309\u5207\u6362\uff0c\u957f\u6309\u4fdd\u6301\uff1b\u5de6\u6447\u6746\u4f9d\u6b21\u6536\u96c6\u7ec4\u5408\u952e", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Sony \u7cfb\u7edf\u952e", "Create/Share = Right Alt\uff0cOptions/Menu = Right Ctrl\uff0cHome = Right Shift", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelSectionTitle(width, panelWidth, "> \u8282\u594f\u4e0e\u8fd4\u56de", "\u5b57\u7b26\u5c42\u662f\u70b9\u6309\uff0c\u57fa\u7840\u5c42\u652f\u6301\u6309\u4f4f\u8fde\u53d1");
            WritePanelLine(width, panelWidth, "  \u8fd4\u56de\u4e3b\u754c\u9762", "\u518d\u6309\u4e00\u6b21 Enter\uff1b\u5173\u95ed\u8fd9\u4e2a\u7a97\u53e3\u5373\u53ef\u9000\u51fa ShikiPad", new Rgb(126, 226, 244), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u5bbd\u5bb9\u7a97\u53e3", "\u5b57\u7b26\u952e\u4e0e\u80a9\u952e\u5728 " + config.ActionLayerGraceMs + "ms \u5185\u53ef\u89c6\u4e3a\u540c\u4e00\u6b21\u5206\u5c42\u8f93\u5165", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  \u8bed\u97f3\u8f93\u5165", "\u5de6\u6447\u6746\u4fdd\u6301 Win\uff0c\u518d\u6309 R1 + \u65b9\u5757/X\uff0c\u53d1\u9001 Win + H", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
        } else {
            WritePanelSectionTitle(width, panelWidth, "> Typing Entry", "Hold a layer first, then press an action button.");
            WritePanelWrappedLine(width, panelWidth, "  How to type", "Hold L1 / R1 / L2 / R2, then press the D-pad or face buttons to emit letters, digits, and punctuation. The base layer remains available for arrows, Space, Backspace, Enter, and Tab.", new Rgb(255, 200, 220), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Button order", xbox ? "Up Right X Y Left Down A B" : "Up Right Square Triangle Left Down Cross Circle", new Rgb(255, 211, 106), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelSectionTitle(width, panelWidth, "> Character Matrix", "Read every row in the same button order.");
            WritePanelTwinLine(width, panelWidth, "Base", "arrows / Space / Back / Enter / Tab", "R1/RB", "i n e a o t h u", new Rgb(255, 235, 180), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "L1/LB", "s r d g l c y z", "R2/RT", "m w j x q f p b", new Rgb(255, 142, 206), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "L2/LT", "k v 1 2 3 4 5 6", "R1+L1", "7 8 9 0 - = , .", new Rgb(190, 133, 255), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "R2+L2", "< ) [ { ( > } ]", "L1+R2", "` \\ ' \" ; ~ / ?", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelTwinLine(width, panelWidth, "R1+L2", "! @ # $ % ^ & *", "Fn", "1..0/-/= -> F1..F12", new Rgb(255, 169, 85), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelSectionTitle(width, panelWidth, "> Sticks And Pointer", "Right stick moves the pointer; left stick handles wheel and modifiers.");
            WritePanelTwinLine(width, panelWidth, "Right stick", "Mouse; L3 left, R3 right", "Left stick", "Up/Down scrolls faster when deeper", new Rgb(113, 255, 194), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Left stick mods", "Left Shift / DownLeft Ctrl / DownRight Alt / Right Win / UpLeft Esc / UpRight Fn", new Rgb(200, 255, 220), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Clutch", xbox ? "Tap View/Menu to toggle, or long-press to hold collected modifiers." : "Tap Touchpad to toggle, or long-press to hold collected modifiers.", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Sony system keys", "Create/Share = Right Alt, Options/Menu = Right Ctrl, Home = Right Shift", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
            WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));

            WritePanelSectionTitle(width, panelWidth, "> Rhythm And Return", "Character layers tap once; base layer repeats while held.");
            WritePanelLine(width, panelWidth, "  Return home", "Press Enter again. Close this window to exit ShikiPad safely.", new Rgb(126, 226, 244), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Grace window", "Inputs within " + config.ActionLayerGraceMs + "ms resolve as one layered stroke.", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
            WritePanelLine(width, panelWidth, "  Voice typing", "Hold left-stick Win, then press R1 + Square/X to send Win + H.", new Rgb(200, 240, 255), new Rgb(245, 250, 255));
        }

        WritePanelBorder(width, panelWidth, false, new Rgb(126, 226, 244));
        FillViewportWithSignal(width, panelWidth, "Enter 返回 ShikiPad 主界面  |  Esc 退出");
        Console.WriteLine("\x1b[0m");
    }

    public static void PrintConnectedWelcome(ControllerProfile profile, Config config, string deviceName) {
        PrintHomeSurface(profile, config, deviceName, true, false);
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
        int panelWidth = Math.Min(118, Math.Max(72, width - 6));
        Console.WriteLine();
        WriteNeonRule(width, panelWidth, "ShikiPad 主界面");
        WriteExtrudedLogo(width, BuildShikiPadBlockLogo(), SeasonFlowStops());
        WriteDenseSignalBand(width, panelWidth, 2, "WELCOME");
        WriteEmbossedCenteredText(width, panelWidth, "欢迎来到 ShikiPad", SeasonGlowStops(), true);
        WriteEmbossedCenteredText(width, panelWidth, "关闭窗口也会自动释放按键", new Rgb[] { SeasonWinter(), SeasonSummer(), SeasonGold() }, false);
        WritePanelBorder(width, panelWidth, true, new Rgb(126, 226, 244));
        WritePanelTitle(width, panelWidth, "像素信号层", new Rgb(235, 247, 252));
        WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));
        WriteAvatarGallery(width, panelWidth, new string[] { "soyo", "bocchi", "kita" }, 4);
        WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));
        WritePanelTwinLine(width, panelWidth, "Enter", "打开映射说明", "Esc", "返回初始页面，再按 Esc 退出", SeasonGold(), new Rgb(245, 250, 255));
        WritePanelBorder(width, panelWidth, false, new Rgb(126, 226, 244));
        FillViewportWithSignal(width, panelWidth, "Enter 打开说明  |  Esc 返回初始页面");
        Console.WriteLine("\x1b[0m");
    }

    private static void WritePageBreak() {
        int lines = 28;
        try {
            lines = Math.Max(28, Console.WindowHeight + 6);
        } catch { }
        for (int i = 0; i < lines; i++) Console.WriteLine();
    }

    private static void PrintInitialControllerSurface(bool hasSavedDefault, ControllerProfile savedDefault) {
        try { Console.Clear(); } catch { }
        EnableAnsi();
        int width = GetConsoleWidth();
        int panelWidth = Math.Min(118, Math.Max(72, width - 6));

        Console.WriteLine();
        WriteNeonRule(width, panelWidth, "ShikiPad 输入页面");
        WriteExtrudedLogo(width, BuildShikiPadBlockLogo(), SeasonFlowStops());
        WriteDenseSignalBand(width, panelWidth, 2, "INPUT");
        WritePanelBorder(width, panelWidth, true, new Rgb(126, 226, 244));
        WritePanelTitle(width, panelWidth, "选择手柄型号", new Rgb(235, 247, 252));
        WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));
        WriteAvatarGallery(width, panelWidth, new string[] { "subaru", "nina", "anon" }, 2);
        WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));
        WriteControllerPairLine(width, panelWidth, "[1] DualSense", "[2] DualSense (BT)", SeasonSummer());
        WriteControllerPairLine(width, panelWidth, "[3] DualShock 4", "[4] DualShock 4 (BT)", new Rgb(100, 180, 255));
        WriteControllerPairLine(width, panelWidth, "[5] Xbox 360", "[6] Xbox 360 (BT)", SeasonSpring());
        WriteControllerPairLine(width, panelWidth, "[7] Xbox Series X|S", "[8] Xbox Series (BT)", SeasonGold());
        WritePanelBorder(width, panelWidth, false, new Rgb(126, 226, 244));
        FillViewportWithSignal(width, panelWidth, "选择手柄型号后按 Enter；运行中会显示 [-\\|/] 动画");
        Console.WriteLine("\x1b[0m");
    }

    private static void PrintStartupSpinner(ControllerProfile profile) {
        try { Console.Clear(); } catch { }
        EnableAnsi();
        int width = GetConsoleWidth();
        int panelWidth = Math.Min(98, Math.Max(66, width - 8));
        Console.WriteLine();
        WriteNeonRule(width, panelWidth, "ShikiPad 正在启动");
        WriteExtrudedLogo(width, BuildShikiPadBlockLogo(), SeasonFlowStops());
        WritePanelBorder(width, panelWidth, true, new Rgb(126, 226, 244));
        WritePanelTitle(width, panelWidth, "运行中", new Rgb(235, 247, 252));
        WritePanelSeparator(width, panelWidth, new Rgb(74, 94, 106));
        WritePanelLine(width, panelWidth, "  手柄型号", ControllerProfileName(profile), SeasonGold(), new Rgb(245, 250, 255));
        WritePanelLine(width, panelWidth, "  状态", "正在启动映射并等待手柄连接", SeasonSpring(), new Rgb(245, 250, 255));
        WritePanelBorder(width, panelWidth, false, new Rgb(126, 226, 244));

        string frames = "-\\|/";
        for (int i = 0; i < 22; i++) {
            int left = Math.Max(0, (width - panelWidth) / 2);
            Console.Write("\r" + new string(' ', left));
            WriteRgb(SeasonSummer(), "[" + frames[i % frames.Length] + "] ");
            WriteRgb(SeasonGold(), "ShikiPad 正在运行，连接后进入主界面...");
            Console.Write("\x1b[0m");
            Thread.Sleep(55);
        }
        Console.WriteLine();
    }

    private static void WriteDenseSignalBand(int width, int panelWidth, int rows, string seed) {
        int left = Math.Max(0, (width - panelWidth) / 2);
        int inner = Math.Max(0, panelWidth - 2);
        for (int row = 0; row < rows; row++) {
            Console.Write(new string(' ', left));
            WriteRgb(Scale(PanelInk(), 0.92), "\u2502");
            WriteTextureSegment(inner, row, row * 17 + seed.Length * 11);
            WriteRgb(Scale(PanelInk(), 0.92), "\u2502");
            Console.WriteLine();
        }
    }

    private static void WriteAvatarGallery(int width, int panelWidth, string[] keys, int extraTextureRows) {
        int left = Math.Max(0, (width - panelWidth) / 2);
        int inner = Math.Max(0, panelWidth - 2);
        PixelSprite[] sprites = new PixelSprite[keys.Length];
        for (int i = 0; i < keys.Length; i++) sprites[i] = GetPixelSprite(keys[i]);

        int spriteChars = PixelSprite.Columns * 2;
        int spriteTotal = spriteChars * sprites.Length;
        int gap = sprites.Length <= 1 ? 0 : Math.Max(2, (inner - spriteTotal) / (sprites.Length - 1));
        int contentWidth = spriteTotal + gap * Math.Max(0, sprites.Length - 1);
        if (contentWidth > inner) {
            gap = 1;
            contentWidth = Math.Min(inner, spriteTotal + gap * Math.Max(0, sprites.Length - 1));
        }
        int margin = Math.Max(0, (inner - contentWidth) / 2);

        for (int r = 0; r < extraTextureRows; r++) {
            Console.Write(new string(' ', left));
            WriteRgb(new Rgb(72, 91, 101), "\u2502");
            WriteTextureSegment(inner, r, 30 + r * 9);
            WriteRgb(new Rgb(72, 91, 101), "\u2502");
            Console.WriteLine();
        }

        for (int row = 0; row < PixelSprite.Rows; row++) {
            Console.Write(new string(' ', left));
            WriteRgb(new Rgb(72, 91, 101), "\u2502");
            WriteTextureSegment(margin, row, 100 + row * 13);
            for (int i = 0; i < sprites.Length; i++) {
                WritePixelSpriteRow(sprites[i], row, 200 + i * 23);
                if (i < sprites.Length - 1) WriteTextureSegment(gap, row, 300 + i * 31 + row);
            }
            int used = margin + contentWidth;
            WriteTextureSegment(Math.Max(0, inner - used), row, 500 + row * 7);
            WriteRgb(new Rgb(72, 91, 101), "\u2502");
            Console.WriteLine();
        }

        for (int r = 0; r < extraTextureRows; r++) {
            Console.Write(new string(' ', left));
            WriteRgb(new Rgb(72, 91, 101), "\u2502");
            WriteTextureSegment(inner, r, 700 + r * 19);
            WriteRgb(new Rgb(72, 91, 101), "\u2502");
            Console.WriteLine();
        }
    }

    private static void WriteTextureSegment(int width, int row, int seed) {
        if (width <= 0) return;
        for (int i = 0; i < width; i++) {
            int v = (i * 17 + row * 29 + seed) % 23;
            if (v == 0) WriteRgb(Scale(SeasonSummer(), 0.36), ".");
            else if (v == 1) WriteRgb(Scale(SeasonGold(), 0.32), ":");
            else if (v == 2) WriteRgb(Scale(SeasonSpring(), 0.28), "-");
            else if (v == 3 && i + 1 < width) {
                WriteRgb(Scale(SeasonSummer(), 0.25), "[]");
                i++;
            } else {
                Console.Write(' ');
            }
        }
    }

    private static void WritePixelSpriteRow(PixelSprite sprite, int row, int seed) {
        string line = sprite.Data[row];
        for (int col = 0; col < PixelSprite.Columns; col++) {
            string token = line.Substring(col * 6, 6);
            if (token == "......") {
                WriteTextureSegment(2, row, seed + col * 5);
            } else {
                Rgb color = HexToRgb(token);
                Console.Write(string.Format("\x1b[48;2;{0};{1};{2}m  \x1b[0m", color.R, color.G, color.B));
            }
        }
    }

    private static Rgb HexToRgb(string hex) {
        return new Rgb(
            Convert.ToInt32(hex.Substring(0, 2), 16),
            Convert.ToInt32(hex.Substring(2, 2), 16),
            Convert.ToInt32(hex.Substring(4, 2), 16));
    }

    private sealed class PixelSprite {
        public const int Columns = 18;
        public const int Rows = 16;
        public readonly string Key;
        public readonly string[] Data;

        public PixelSprite(string key, string[] data) {
            Key = key;
            Data = data;
        }
    }

    private static PixelSprite GetPixelSprite(string key) {
        for (int i = 0; i < PixelSprites.Length; i++) {
            if (String.Equals(PixelSprites[i].Key, key, StringComparison.OrdinalIgnoreCase)) return PixelSprites[i];
        }
        return PixelSprites[0];
    }

    private static readonly PixelSprite[] PixelSprites = new PixelSprite[] {
        new PixelSprite("subaru", new string[] {
            "............................................................................................................",
            "....................................364848243636363648363648363648..........................................",
            "........................5A6C6C48485A24364824243624364824364824364848485A....................................",
            "........................5A5A6C243648243648243648243648363648243648243648363648..............................",
            "..................5A5A6C48485A363648363648363648243648243648242436243648243648..............................",
            "..................36485A2424362436362424365A485A242436242436122436242436243648243636........................",
            "..................2436482424366C5A5A363648A27E7E484848363648122436242436242448242436........................",
            "..................2436362424366C5A6CD8C6B4D8C6B46C5A7E5A4848484848C6A290243648242436........................",
            "........................122424B49090FCD8C6FCD8C6EAC6B4B4A2905A485A6C5A5A242448242436........................",
            "........................1224366C6C6CEAC6B4EAB4B4FCD8C6C6A2A2483648121224242436242436122424..................",
            "........................2424360024246C5A6CEAC6B4C6A290A27E6C907E7E363648121224242448122436..................",
            "........................24243612242400123636365AB490A2907E7E9090A2122436121236122436242448..................",
            "..................24363624363612243624365A485A6C6C7E905A5A6C24365A24244824245A242448122436243636002424......",
            "............12362424363612242424365A6C6C7E486C7EA290A224364824244824245A24245A24245A121236122436122436......",
            "............24363612243612242436365A6C90A27E90A236365A24364824244824245A24245A12245A001236122436122424......",
            "........................1224485A5A6C487E90485A7E24364836365A12244812244824245A122448002436122436............",
        }),
        new PixelSprite("nina", new string[] {
            "............................................................................................................",
            "............................................................................................................",
            "..........................................7E36486C24485A24485A2436482436482436..............................",
            "..............................904848A2485A7E36485A24485A24365A24365A2436482436482436........................",
            "..............................6C36485A24365A24486C36487E36485A24365A24365A24365A2436482436..................",
            "........................5A24365A24365A24365A24365A24485A24486C24486C36486C24485A24365A2448..................",
            "........................4824365A24364824364824365A24484824366C485A906C6C5A2436482436482436241224............",
            "..................4824244824365A24365A36485A48485A24485A3636A2907EA27E6C4824365A3648482436242424............",
            "..................362424362436482436906C6CB4907E5A2436907E7E909090C6A2A27E485A6C4848361236241224............",
            "........................3612363612363612367E5A6C6C3648D8B4A2FCD8C6FCD8C6B4A2A2362436242424..................",
            "..............................3636362412367E5A5A905A5AEAD8C6FCD8C6FCD8C6B49090361236362424..................",
            "..................C6C6B4EAD8C6D8C6C6C6A2A2A29090A26C6CB4907EEAD8C6A29090362448242424........................",
            "..................D8B4B4EAC6C6EAD8C6FCEAD8FCD8D890909036365A122448122424....................................",
            "..................36485A48486C5A486CA27E7ED8B4B4FCD8D87E6C7E24243648485A....................................",
            "..................12244824365A36243636365A483648B49090A27E90121236A27E7E....................................",
            "............................................................................................................",
        }),
        new PixelSprite("anon", new string[] {
            "............................................................................................................",
            "............................................................................................................",
            "..............................D890A2EAC6C6EAB4C6EAC6C6EAC6C6................................................",
            "..................D8B4B4EAB4B4FCB4C6EAB4C6FCC6C6FCC6C6FCD8D8EAC6C6..........................................",
            "..................FCD8D8FCC6C6FCC6C6FCD8D8FCC6C6FCC6C6FCC6C6FCC6C6EAC6C6....................................",
            "............D8C6B4FCC6D8EAB4C6FCC6C6FCB4C6FCB4C6FCC6C6EAB4B4FCB4C6EAC6C6....................................",
            "............D8B4B4FCB4C6D8A2A2EAB4B4EAA2B4EAB4B4EAA2B4D8A2B4D8A2B4FCC6C6....................................",
            "............D8A2A2EAA2B4C6A290EAC6B4D8B4B45A5A6CC69090D89090D8A2A2FCC6C6D890A2..............................",
            "............C69090D890A2907E7ED8C6B4FCD8C6EAC6C6EAB4B4D8B4A2D890A2EAB4C6EAB4B4..............................",
            "..................C67E90C67E90FCD8C6EAB4A2FCD8C6EAC6B4A25A6CD890A2D8A2B4EAB4B4C6B4A2........................",
            "..................D8B4B4C66C90D8A2A2EAB4A2EAC6B4D8A290B47E90D8A2B4C690A2EAB4B4D8B4A2........................",
            "..................C6A2A2D890A2B45A7EA26C7EB47E90EAC6C6D8B4C6A27E90A25A7ED8A2B4EAB4C6B4907E..................",
            "............B49090EAB4C6B47E907E6C6C6C5A6C7E6C7ED8C6C6D8A2B4B4A2A2B4A2A2B49090EAA2B4EAB4B4C6A2A2............",
            "......D8A2A2D8B4B4D890A2906C7EB4A2A26C7E6CB4A2A2D8A2B4C6A2A2B4A2A2B4A2A2B4A2A2B46C90EAB4B4C6A2A2............",
            "............................................................................................................",
            "............................................................................................................",
        }),
        new PixelSprite("soyo", new string[] {
            "............................................................................................................",
            "....................................B4907EB47E6CC6907EC6907EC6907EB4A27E....................................",
            "..............................B4907EB4907EC6907EC6907EC6907EC6907ED8B490C6A290..............................",
            "........................D8B490C6907EC6907EC6907EC6907EC6A290D8A290C6907EC6A290..............................",
            "..................D8C6B4D8B4A2D8A290C6907ED8A290C6907EC6907EC6907EC6907EC6907EB4907E........................",
            "..................D8B4A2C6907EC6907EC6907EC6907EB47E7EC6907EB47E6CB47E7EC6907EB47E6C........................",
            "..................C6907EB47E7EC6A290D8B4A2C6A290B4907EB47E6CB47E6C906C6CC6907EB47E6C........................",
            "..................B4907E906C5AA2907EEAB4A29090905A5A5AB4907EC6907ED8A290B47E7EB47E6CA27E6C..................",
            "..................907E5A905A5AB4A290FCD8C6FCD8C6EAC6B4C69090B4907EA27E6CB47E6CB4907E906C48..................",
            "........................7E6C48B4907EFCD8C6FCD8D8FCD8C6C6907EA26C6C905A5AA26C6CC6907EA27E6C6C5A48............",
            "........................A27E6C905A5AB49090EAC6B4C6A290C6907EC67E7E7E5A5A7E5A5AC6907EA27E6CA27E6C............",
            "........................A27E6C906C5A6C4848483636906C5AC6907E7E5A6C5A48485A4848A27E6CB47E6CA27E6C............",
            "........................A27E6C5A484836485A906C6CD8B4A25A485A48485A5A5A6C48486C5A4848B47E6CA27E6C............",
            "..................A27E6C7E5A5A2436485A5A7E5A5A6C906C6C5A485A48486C24365A24365A24365A7E5A5AB47E6CA27E6C......",
            "............C6A290906C5A5A484848486C36486C6C6C90A26C6C48485A24365A24365A24365A24365A6C4848A27E6C............",
            "............................................................................................................",
        }),
        new PixelSprite("bocchi", new string[] {
            "............................................................................................................",
            "............................................................................................................",
            "..........................................FCC6C6FCB4B4FCB4B4FCB4B4EAA2A2....................................",
            "....................................FCC6C6FCB4C6FCB4B4FCA2B4EAA2A2FCA2B4EAA2B4EAB4B4........................",
            "..............................B4B4C6C6A2B4FCA2B4FCC6C6EAA2B4FCA2B4FCB4B4FCA2B4FCB4C6........................",
            "..................D8A290C690907E7E905A7EB4EAA2B4EAA2A2FCA2B4EAB4B4FCB4C6FCB4B4FCB4B4D8A2A2..................",
            "........................D8A27EC69048C67E6CFCB4B4EA90A2EA90A2EA90A2FCB4B4EA90A2FCA2B4D8A2A2..................",
            "........................EAB4B4D8907ED89090FCB4B4C67E90A26C6CC67E7EFCA2B4D89090EAA2A2C69090..................",
            "........................EAB4B4EA90A2D890A2FCA2B4C69090907E90D89090B47E90906C6CD87E90B47E6C..................",
            "........................EAB4B4EA90A2C66C90FCA2B4EAB4B4FCD8C6FCD8C6EAC6B4B46C7EB46C7E........................",
            "..................D8C6C6EAA2B4D890A2B46C7EEAA2B4D8907EEAC6B4FCD8C6C69090C66C7EB46C7E........................",
            "..................D8B4A2EAA2B4C66C7EC66C7ED890A2C66C7E90485AA25A6CA25A6CC66C90C69090........................",
            "..................EAA2A2EA90A2FCB4B4EA9090D87E90D87E90A25A6CB45A6CA25A6CD87E90B47E7E........................",
            "............C6A290D87E90FCB4B4FC90A2EA90A2EA9090EA90A2C67E90D87E90B45A6CD890A2C69090............6C3624......",
            "............................................................................................................",
            "............................................................................................................",
        }),
        new PixelSprite("kita", new string[] {
            "............................................................................................................",
            "......................................................B45A48................................................",
            "....................................D86C6CD84848D84836C64836C64836C64836B44836..............................",
            "..............................D85A5AD85A48D84848D83636C63636C63636C63636C63636C65A48........................",
            "..................D86C5AB44836C63636C64836D84848D84848D84836C63636D83636C63636D84836D86C6C..................",
            "..................C64836A22424C63636C63636C63636C63636C64836C64836D84848D84836D84848D85A48..................",
            "............B43636C63636902424C63636D83636B43636C65A48D86C6CC64848C63636C64836C63636C63636C65A48............",
            "............A23624A23624A22424B43636D836369048486C5A48D8B4A2B44836B43636C66C6CB43636C63636A24836............",
            "............B436247E3624B43636B45A48C63636C6907E90905AEAC6C6D87E6CB46C6C906C6CB44848C636367E3624............",
            "......B43624C63636B43636B43636903636B43636D8907EFCD8B4FCD8C6FCD8C6B4A26CB4907E902424A236246C3624............",
            "......A23624A23624B43636C636367E2424902424D89090FCD8C6D8A290EAC6B4FCD8C69048487E24246C3612..................",
            "............7E3624B43636B436367E2424902424D8907EEAC6B4FCB4A2EAC6B4B47E7E7E24246C2424903624..................",
            "............B43636B436369024247E2424903636EAB490D8906C906C5A7E36366C24249024246C2424A23624A23624............",
            "......A23636A236249024247E3648482436B46C6CFCD8C6EAA290B47E7E906C7E4824366C24247E24247E36247E3624............",
            "......7E3624B436364824363636366C6C6C904848FCD8C6FCD8C6D8B4A2C6B4B448485A362424902424........................",
            "............................................................................................................",
        }),
    };

    private static bool IsXboxProfile(ControllerProfile profile) {
        return profile == ControllerProfile.Xbox360 || profile == ControllerProfile.Xbox360BT ||
               profile == ControllerProfile.XboxSeries || profile == ControllerProfile.XboxSeriesBT;
    }

    private static void WriteHudRail(int width, int panelWidth, string leftText, string rightText) {
        int left = Math.Max(0, (width - panelWidth) / 2);
        string center = " " + leftText + " ";
        string right = " " + rightText + " ";
        int fill = Math.Max(0, panelWidth - DisplayWidth(center) - DisplayWidth(right) - 2);

        Console.Write(new string(' ', left));
        WriteRgb(PanelInk(), "\u250c");
        WriteRgb(SeasonSpring(), center);
        WriteGradientText(left + DisplayWidth(center) + 1, width, new string('\u2500', fill), SeasonFlowStops());
        WriteRgb(SeasonWinter(), right);
        WriteRgb(PanelInk(), "\u2510");
        Console.WriteLine();
    }

    private static void WriteSignalWeave(int width, int panelWidth, int rows, string seed) {
        int left = Math.Max(0, (width - panelWidth) / 2);
        int inner = Math.Max(0, panelWidth - 2);
        for (int row = 0; row < rows; row++) {
            Console.Write(new string(' ', left));
            WriteRgb(Scale(PanelInk(), 0.9), "\u2502");
            string line = row == 0 ? CenterLine(inner, "[ " + seed + " ]") : new string(' ', inner);
            WriteRgb(Scale(new Rgb(166, 205, 218), row == 0 ? 0.72 : 0.28), line);
            WriteRgb(Scale(PanelInk(), 0.9), "\u2502");
            Console.WriteLine();
        }
    }

    private static void WritePanelSectionTitle(int width, int panelWidth, string title, string hint) {
        WritePanelLine(width, panelWidth, "  " + title, hint, SeasonGold(), new Rgb(210, 244, 255));
    }

    private static void WritePanelWrappedLine(int width, int panelWidth, string label, string value, Rgb labelColor, Rgb valueColor) {
        int inner = panelWidth - 2;
        int labelWidth = Math.Min(28, Math.Max(18, inner / 3));
        int valueWidth = inner - labelWidth - 3;
        string[] lines = WrapToWidth(value, valueWidth);
        if (lines.Length == 0) lines = new string[] { "" };
        for (int i = 0; i < lines.Length; i++) {
            WritePanelLine(width, panelWidth, i == 0 ? label : "  ", lines[i], labelColor, valueColor);
        }
    }

    private static string[] WrapToWidth(string text, int width) {
        List<string> lines = new List<string>();
        if (String.IsNullOrEmpty(text)) return lines.ToArray();
        StringBuilder current = new StringBuilder();
        int used = 0;
        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            if (c == '\r') continue;
            if (c == '\n') {
                lines.Add(current.ToString());
                current.Length = 0;
                used = 0;
                continue;
            }
            int cw = CharDisplayWidth(c);
            if (used > 0 && used + cw > width) {
                lines.Add(current.ToString().TrimEnd());
                current.Length = 0;
                used = 0;
                if (c == ' ') continue;
            }
            current.Append(c);
            used += cw;
        }
        if (current.Length > 0) lines.Add(current.ToString().TrimEnd());
        return lines.ToArray();
    }

    private static void FillViewportWithSignal(int width, int panelWidth, string footer) {
        int target = 0;
        try { target = Math.Max(0, Console.WindowHeight - 2); } catch { target = 0; }
        int row = 0;
        while (target > 0) {
            int cursor = 0;
            try { cursor = Console.CursorTop; } catch { break; }
            if (cursor >= target) break;
            WriteSignalFillLine(width, panelWidth, row++);
        }
        WriteHudFooter(width, panelWidth, footer);
    }

    private static void WriteSignalFillLine(int width, int panelWidth, int row) {
        int left = Math.Max(0, (width - panelWidth) / 2);
        int inner = Math.Max(0, panelWidth - 2);
        Console.Write(new string(' ', left));
        WriteRgb(Scale(PanelInk(), 0.92), "\u2502");
        string line = row % 4 == 2 ? CenterLine(inner, ". . .") : new string(' ', inner);
        WriteRgb(Scale(new Rgb(166, 205, 218), row % 4 == 2 ? 0.24 : 0.10), line);
        WriteRgb(Scale(PanelInk(), 0.92), "\u2502");
        Console.WriteLine();
    }

    private static void WriteHudFooter(int width, int panelWidth, string footer) {
        int left = Math.Max(0, (width - panelWidth) / 2);
        Console.Write(new string(' ', left));
        WriteGradientText(left, width, "\u2570" + new string('\u2500', Math.Max(0, panelWidth - 2)) + "\u256f", SeasonFlowStops());
        Console.WriteLine();
        WriteEmbossedCenteredText(width, panelWidth, footer, SeasonGlowStops(), false);
    }

    private static bool IsChineseUi() {
        return true;
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
                    WriteRgb(Scale(baseColor, 0.38), ":");
                } else if (farShadow) {
                    WriteRgb(Scale(baseColor, 0.22), ".");
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
                WriteRgb(Scale(GradientAt(stops, t), 0.22), ".");
            }
        }
    }

    private static void WriteSeasonDropShadow(int width, int panelWidth) {
        int left = Math.Max(0, (width - panelWidth) / 2 + 2);
        Console.Write(new string(' ', left));
        WriteRgb(ShadowInk(), new string('.', Math.Max(0, panelWidth - 2)));
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
        EnableAnsi();

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

        if (debugSources) Logger.Info("debug-sources enabled");
        if (traceInput) Logger.Info("trace-input enabled");
        if (traceSendinput) Logger.Info("trace-sendinput enabled");

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        string[] selectionArgs = args;
        bool forceControllerMenuAfterRestart = false;
        while (true) {
            if (forceControllerMenuAfterRestart) {
                selectionArgs = new string[] { "--controller-menu" };
            }

            ControllerProfile controllerProfile = SelectControllerProfile(selectionArgs, root);
            if (_controllerSelectionExitRequested) return 0;
            PrintStartupSpinner(controllerProfile);
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

            PrintRunHint();
            MapperForm form = new MapperForm(config, controllerProfile, debugSources, traceInput, traceSendinput);
            Application.Run(form);
            if (!form.RestartControllerSelectionRequested) break;
            forceControllerMenuAfterRestart = true;
        }
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
            return savedDefault;
        }

        try {
            if (Console.IsInputRedirected) return ControllerProfile.DualSense;
        } catch { }

        return PromptControllerProfile(defaultPath, hasSavedDefault, savedDefault);
    }

    private static ControllerProfile PromptControllerProfile(string defaultPath, bool hasSavedDefault, ControllerProfile savedDefault) {
        EnableAnsi();
        PrintInitialControllerSurface(hasSavedDefault, savedDefault);

        while (true) {
            WriteRgb(SeasonSummer(), "选择手柄型号 [1..8，Enter = 1，Esc = 退出] > ");
            Console.Write("\x1b[0m");
            string line = ReadControllerMenuLine(true);
            if (line == null) {
                _controllerSelectionExitRequested = true;
                return ControllerProfile.DualSense;
            }
            line = line.Trim();
            if (line == "\x1b") {
                _controllerSelectionExitRequested = true;
                return ControllerProfile.DualSense;
            }

            ControllerProfile selected;
            if (TryParseMenuControllerProfile(line.Length == 0 ? "1" : line, out selected)) {
                MaybeSaveDefaultControllerProfile(defaultPath, selected);
                return selected;
            }
            WriteRgb(SeasonAutumn(), "请选择 1 到 8 之间的数字；按 Esc 可以退出。\n");
        }
    }

    private static string ReadControllerMenuLine(bool allowEsc) {
        StringBuilder sb = new StringBuilder();
        while (true) {
            ConsoleKeyInfo key;
            try {
                key = Console.ReadKey(true);
            } catch {
                return Console.ReadLine();
            }

            if (allowEsc && key.Key == ConsoleKey.Escape) {
                Console.WriteLine();
                return "\x1b";
            }
            if (key.Key == ConsoleKey.Enter) {
                Console.WriteLine();
                return sb.ToString();
            }
            if (key.Key == ConsoleKey.Backspace) {
                if (sb.Length > 0) {
                    sb.Length--;
                    Console.Write("\b \b");
                }
                continue;
            }
            if (!Char.IsControl(key.KeyChar) && sb.Length < 24) {
                sb.Append(key.KeyChar);
                Console.Write(key.KeyChar);
            }
        }
    }

    public static bool ClearSavedDefaultControllerForRuntime() {
        string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultControllerFileName);
        bool existed = false;
        try { existed = File.Exists(defaultPath); } catch { }
        ClearDefaultControllerProfile(defaultPath);
        return existed;
    }

    public static bool HasDefaultControllerForRuntime() {
        string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultControllerFileName);
        ControllerProfile ignored;
        return TryLoadDefaultControllerProfile(defaultPath, out ignored);
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
