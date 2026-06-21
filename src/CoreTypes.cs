
internal enum PhysicalKey {
      None, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, Num0, Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9, Minus, Equals, LeftBracket, RightBracket, Backslash, Semicolon, Apostrophe, Comma, Period, Slash, Grave, Space, Backspace, Enter, Tab, Escape, ArrowUp, ArrowDown, ArrowLeft, ArrowRight, LShift, RShift, LCtrl, RCtrl, LAlt, RAlt, LWin, RWin, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12
    }

internal enum Layer {
    Base,
    L1,
    R1,
    L2,
    R2,
    R1L1,
    R2L2,
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
    Right,
    DownRight,
    Down,
    DownLeft,
    Left,
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
    public bool Home;
}
