using System;

internal sealed class LeftStickScrollIntegrator {
    internal const int WheelDelta = 120;
    private const int WheelQuantum = 4;
    private const int MaxWheelDeltaPerFrame = 120;

    private double _accumulatedWheelDelta;
    private int _lastDirection;
    private bool _hasActiveDirection;

    public void Reset() {
        _accumulatedWheelDelta = 0.0;
        _lastDirection = 0;
        _hasActiveDirection = false;
    }

    public bool TryUpdate(double radius, double deltaSec, Config config, int direction, out int wheelDelta) {
        wheelDelta = 0;
        if (direction == 0 || deltaSec <= 0.0) return false;

        double clampedRadius = Clamp(radius, 0.0, 1.0);
        if (clampedRadius <= config.LeftStickExitDeadzone) {
            Reset();
            return false;
        }

        double normalized = NormalizeRadius(clampedRadius, config);
        if (normalized <= 0.0) {
            Reset();
            return false;
        }

        if (!_hasActiveDirection || _lastDirection != direction) {
            _hasActiveDirection = true;
            _lastDirection = direction;
            _accumulatedWheelDelta = 0.0;
        }

        _accumulatedWheelDelta += direction * WheelDeltaPerSecond(normalized, config) * deltaSec;
        wheelDelta = TakeQuantizedWheelDelta(ref _accumulatedWheelDelta);
        return wheelDelta != 0;
    }

    internal static double NormalizeRadius(double radius, Config config) {
        double enterDeadzone = Clamp(config.LeftStickEnterDeadzone, 0.0, 0.99);
        double normalized = (radius - enterDeadzone) / (1.0 - enterDeadzone);
        return Clamp(normalized, 0.0, 1.0);
    }

    internal static double ScrollIntervalMs(double normalizedRadius, Config config) {
        double normalized = Clamp(normalizedRadius, 0.0, 1.0);
        double slowInterval = Math.Max(1.0, (double)config.ScrollSlowIntervalMs);
        double fastInterval = Math.Max(1.0, Math.Min((double)config.ScrollFastIntervalMs, slowInterval));
        double power = Math.Pow(normalized, Math.Max(0.001, config.MouseScrollCurveExponent));
        return slowInterval * Math.Pow(fastInterval / slowInterval, power);
    }

    internal static double WheelDeltaPerSecond(double normalizedRadius, Config config) {
        return WheelDelta * 1000.0 / Math.Max(1.0, ScrollIntervalMs(normalizedRadius, config));
    }

    private static int TakeQuantizedWheelDelta(ref double accumulator) {
        int delta = 0;
        double halfQuantum = WheelQuantum * 0.5;
        if (accumulator >= halfQuantum) {
            delta = (int)Math.Floor((accumulator + halfQuantum) / WheelQuantum) * WheelQuantum;
            if (delta > MaxWheelDeltaPerFrame) delta = MaxWheelDeltaPerFrame;
        } else if (accumulator <= -halfQuantum) {
            delta = (int)Math.Ceiling((accumulator - halfQuantum) / WheelQuantum) * WheelQuantum;
            if (delta < -MaxWheelDeltaPerFrame) delta = -MaxWheelDeltaPerFrame;
        }
        accumulator -= delta;
        return delta;
    }

    private static double Clamp(double value, double min, double max) {
        return value < min ? min : (value > max ? max : value);
    }

}
