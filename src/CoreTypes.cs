
internal enum PhysicalKey {
      None, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, Num0, Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9, Minus, Equals, LeftBracket, RightBracket, Backslash, Semicolon, Apostrophe, Comma, Period, Slash, Grave, Space, Backspace, Enter, Tab, Escape, Delete, Home, End, ArrowUp, ArrowDown, ArrowLeft, ArrowRight, LShift, RShift, LCtrl, RCtrl, LAlt, RAlt, LWin, RWin, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, CapsLock
    }

internal struct KeyStroke {
    public PhysicalKey Key;
    public bool Shift;

    public KeyStroke(PhysicalKey key, bool shift) {
        Key = key;
        Shift = shift;
    }

    public bool IsNone {
        get { return Key == PhysicalKey.None; }
    }

    public static KeyStroke None {
        get { return new KeyStroke(PhysicalKey.None, false); }
    }

    public static KeyStroke Of(PhysicalKey key) {
        return new KeyStroke(key, false);
    }

    public static KeyStroke Shifted(PhysicalKey key) {
        return new KeyStroke(key, true);
    }

    public override bool Equals(object obj) {
        if (!(obj is KeyStroke)) return false;
        KeyStroke other = (KeyStroke)obj;
        return Key == other.Key && Shift == other.Shift;
    }

    public override int GetHashCode() {
        return ((int)Key * 397) ^ (Shift ? 1 : 0);
    }

    public static bool operator ==(KeyStroke a, KeyStroke b) {
        return a.Key == b.Key && a.Shift == b.Shift;
    }

    public static bool operator !=(KeyStroke a, KeyStroke b) {
        return !(a == b);
    }
}

internal enum Layer {
    Base,
    L1,
    R1,
    L2,
    R2,
    R1L1,
    R2L2,
    L1R2,
    R1L2,
    Reserved
}

internal enum ActionButton {
    Up,
    Right,
    Square,
    Triangle,
    Left,
    Down,
    Cross,
    Circle
}

internal enum StickDirection {
    None,
    Up,
    UpRight,
    DownRight,
    Down,
    DownLeft,
    UpLeft
}

internal enum ControllerProfile {
    DualSense,
    DualSenseBT,
    DualShock4,
    DualShock4BT,
    Xbox360,
    Xbox360BT,
    XboxSeries,
    XboxSeriesBT
}

internal sealed class ControllerState {
    public bool Connected;
    public double LX, LY, RX, RY, L2, R2;
    public bool Up, Right, Down, Left, Square, Triangle, Cross, Circle;
    public bool L1, R1, L3, R3, Options, Create;
    public bool TouchClick;
    public int TouchCount;
    public bool Touch1Active, Touch2Active;
    public int Touch1X, Touch1Y, Touch2X, Touch2Y;
    public bool Home;
    public bool Mute;
}
