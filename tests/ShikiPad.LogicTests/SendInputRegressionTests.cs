using System.Reflection;
using System.Runtime.InteropServices;

internal static class SendInputRegressionTests {
    internal static void Run() {
        VerifyNativeLayoutAndImports();
        VerifyKeyboardEvents();
        VerifyMouseEventsAndCleanup();

        // Construction and empty shutdown must not load a third-party driver or send input.
        var nativeInjector = new InputInjector();
        nativeInjector.Shutdown();
        nativeInjector.Shutdown();
    }

    private static void VerifyNativeLayoutAndImports() {
        Check(IntPtr.Size == 8, "the shipped Windows build and its tests run as x64");
        Check(Marshal.SizeOf<InputInjector.INPUT>() == 40, "INPUT matches the Win32 x64 cbSize");
        Check(Marshal.SizeOf<InputInjector.InputUnion>() == 32, "the input union includes the full mouse structure");
        Check(Marshal.SizeOf<InputInjector.KEYBDINPUT>() == 24, "KEYBDINPUT includes x64 pointer alignment");
        Check(Marshal.SizeOf<InputInjector.MOUSEINPUT>() == 32, "MOUSEINPUT includes x64 pointer alignment");
        Offset<InputInjector.INPUT>(nameof(InputInjector.INPUT.type), 0);
        Offset<InputInjector.INPUT>(nameof(InputInjector.INPUT.u), 8);
        Offset<InputInjector.InputUnion>(nameof(InputInjector.InputUnion.mi), 0);
        Offset<InputInjector.InputUnion>(nameof(InputInjector.InputUnion.ki), 0);
        Offset<InputInjector.KEYBDINPUT>(nameof(InputInjector.KEYBDINPUT.wVk), 0);
        Offset<InputInjector.KEYBDINPUT>(nameof(InputInjector.KEYBDINPUT.wScan), 2);
        Offset<InputInjector.KEYBDINPUT>(nameof(InputInjector.KEYBDINPUT.dwFlags), 4);
        Offset<InputInjector.KEYBDINPUT>(nameof(InputInjector.KEYBDINPUT.time), 8);
        Offset<InputInjector.KEYBDINPUT>(nameof(InputInjector.KEYBDINPUT.dwExtraInfo), 16);
        Offset<InputInjector.MOUSEINPUT>(nameof(InputInjector.MOUSEINPUT.dx), 0);
        Offset<InputInjector.MOUSEINPUT>(nameof(InputInjector.MOUSEINPUT.dy), 4);
        Offset<InputInjector.MOUSEINPUT>(nameof(InputInjector.MOUSEINPUT.mouseData), 8);
        Offset<InputInjector.MOUSEINPUT>(nameof(InputInjector.MOUSEINPUT.dwFlags), 12);
        Offset<InputInjector.MOUSEINPUT>(nameof(InputInjector.MOUSEINPUT.time), 16);
        Offset<InputInjector.MOUSEINPUT>(nameof(InputInjector.MOUSEINPUT.dwExtraInfo), 24);

        var imports = typeof(InputInjector).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            .Select(method => (Method: method, Import: method.GetCustomAttribute<DllImportAttribute>()))
            .Where(item => item.Import != null).ToArray();
        Check(!imports.Any(item => item.Import.Value.Contains("interception", StringComparison.OrdinalIgnoreCase)), "the assembly has no Interception native dependency");
        var sendInput = imports.Single(item => string.Equals(item.Import.Value, "user32.dll", StringComparison.OrdinalIgnoreCase) && item.Import.EntryPoint == "SendInput");
        Check(sendInput.Method.ReturnType == typeof(uint), "SendInput preserves its native UINT return count");
        Check(sendInput.Method.GetParameters().Select(parameter => parameter.ParameterType)
            .SequenceEqual(new[] { typeof(uint), typeof(InputInjector.INPUT).MakeByRefType(), typeof(int) }), "SendInput receives one correctly sized INPUT by reference");
    }

    private static void VerifyKeyboardEvents() {
        var events = new List<InputInjector.INPUT>();
        var injector = new InputInjector(input => { events.Add(input); return true; });
        var keys = new (PhysicalKey Key, ushort Scan, bool Extended)[] {
            (PhysicalKey.LShift, 0x2A, false), (PhysicalKey.RShift, 0x36, false),
            (PhysicalKey.LCtrl, 0x1D, false), (PhysicalKey.RCtrl, 0x1D, true),
            (PhysicalKey.LAlt, 0x38, false), (PhysicalKey.RAlt, 0x38, true),
            (PhysicalKey.LWin, 0x5B, true), (PhysicalKey.RWin, 0x5C, true),
            (PhysicalKey.ArrowUp, 0x48, true), (PhysicalKey.ArrowDown, 0x50, true),
            (PhysicalKey.ArrowLeft, 0x4B, true), (PhysicalKey.ArrowRight, 0x4D, true),
            (PhysicalKey.Home, 0x47, true), (PhysicalKey.End, 0x4F, true),
            (PhysicalKey.Delete, 0x53, true)
        };
        foreach (var key in keys) {
            events.Clear();
            injector.KeyDown(key.Key);
            injector.KeyUp(key.Key);
            Check(events.Count == 2, key.Key + " has one down and one up");
            for (int i = 0; i < events.Count; i++) {
                var input = events[i];
                uint flags = 8u | (key.Extended ? 1u : 0u) | (i == 1 ? 2u : 0u);
                Check(input.type == 1 && input.ki.wVk == 0 && input.ki.wScan == key.Scan && input.ki.dwFlags == flags,
                    key.Key + " uses the expected physical scan code, side, extension and release flags");
                Check(input.ki.time == 0 && input.ki.dwExtraInfo == IntPtr.Zero, "keyboard events leave timestamp and extra information to Windows");
            }
            Check(!injector.IsKeyHeld(key.Key), key.Key + " releases logical ownership");
        }

        // Letters/punctuation use the active keyboard layout; every mapped key must still have a scan code.
        foreach (PhysicalKey key in Enum.GetValues<PhysicalKey>()) {
            if (key == PhysicalKey.None) continue;
            events.Clear();
            injector.KeyTap(key, false, false, false, false);
            Check(events.Count == 2 && events[0].type == 1 && events[1].type == 1, key + " produces a keyboard tap");
            Check(events[0].ki.wVk == 0 && events[0].ki.wScan != 0 && events[1].ki.wScan == events[0].ki.wScan,
                key + " has a usable matching down/up scan code");
            Check((events[0].ki.dwFlags & 8) != 0 && events[1].ki.dwFlags == (events[0].ki.dwFlags | 2),
                key + " preserves scan and extended flags on release");
        }
        events.Clear();
        injector.Shutdown();
        Check(events.Count == 0, "completed keyboard taps leave no held keys to release");
    }

    private static void VerifyMouseEventsAndCleanup() {
        var events = new List<InputInjector.INPUT>();
        var injector = new InputInjector(input => { events.Add(input); return true; });
        injector.MouseMove(0, 0);
        injector.MouseWheelDelta(0);
        Check(events.Count == 0, "zero movement and zero wheel delta produce no input");
        injector.MouseMove(-17, 23);
        injector.MouseWheelDelta(8);
        injector.MouseWheelDelta(-31);
        injector.MouseWheel(-1);
        injector.MouseButton(0, true);
        injector.MouseButton(1, true);
        injector.Shutdown();

        Check(events.Count == 8 && events.All(input => input.type == 0), "mouse output includes both held-button releases during shutdown");
        Check(events[0].mi.dwFlags == 1 && events[0].mi.dx == -17 && events[0].mi.dy == 23 && events[0].mi.mouseData == 0,
            "pointer movement preserves signed relative deltas without absolute positioning");
        int[] wheelDeltas = { 8, -31, -120 };
        for (int i = 0; i < wheelDeltas.Length; i++) {
            var wheel = events[i + 1].mi;
            Check(wheel.dwFlags == 0x800 && wheel.mouseData == wheelDeltas[i] && wheel.dx == 0 && wheel.dy == 0,
                "wheel preserves signed high-resolution delta " + wheelDeltas[i]);
        }
        Check(events.Skip(4).Select(input => input.mi.dwFlags).SequenceEqual(new uint[] { 2, 8, 4, 16 }),
            "left and right mouse buttons retain independent down/up flags and release order");
        Check(events.All(input => input.mi.time == 0 && input.mi.dwExtraInfo == IntPtr.Zero), "mouse timestamps and extra information remain unset");
        injector.Shutdown();
        Check(events.Count == 8, "repeated shutdown emits no duplicate mouse releases");
    }

    private static void Offset<T>(string field, int expected) {
        Check(Marshal.OffsetOf<T>(field).ToInt32() == expected, typeof(T).Name + "." + field + " matches the native field offset");
    }

    private static void Check(bool condition, string message) {
        if (!condition) throw new Exception("SendInput regression failed: " + message);
    }
}
