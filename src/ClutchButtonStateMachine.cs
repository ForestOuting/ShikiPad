using System;

internal sealed class ClutchButtonStateMachine {
    public bool Toggled;
    public bool Held;

    private bool _prevDown;
    private bool _longPress;
    private bool _startedToggled;
    private double _downMs;

    public bool Active {
        get { return Held || Toggled; }
    }

    public void Update(bool down, double nowMs, int longPressMs) {
        int thresholdMs = Math.Max(1, longPressMs);

        if (down && !_prevDown) {
            Held = true;
            _longPress = false;
            _startedToggled = Toggled;
            _downMs = nowMs;
        } else if (down && _prevDown) {
            if (!_longPress && nowMs - _downMs >= thresholdMs) _longPress = true;
        } else if (!down && _prevDown) {
            double heldMs = nowMs - _downMs;
            if (!_longPress && heldMs < thresholdMs) {
                Toggled = !_startedToggled;
            } else {
                Toggled = false;
            }
            Held = false;
            _longPress = false;
            _downMs = 0;
        }

        _prevDown = down;
    }

    public void Reset() {
        Toggled = false;
        Held = false;
        _prevDown = false;
        _longPress = false;
        _startedToggled = false;
        _downMs = 0;
    }
}
