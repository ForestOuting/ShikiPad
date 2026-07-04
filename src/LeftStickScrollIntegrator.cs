using System;

internal sealed class LeftStickScrollIntegrator {
    internal const int WheelDelta = 120;

    private double _accumulatedWheelDelta;

    public void Reset() {
        _accumulatedWheelDelta = 0.0;
    }

    public bool TryUpdate(double radius, double deltaSec, Config config, int direction, out int wheelDelta) {
        wheelDelta = 0;
        if (direction == 0 || deltaSec <= 0.0) return false;

        double normalized = NormalizeRadius(radius, config);
        if (normalized <= 0.0) {
            Reset();
            return false;
        }

        _accumulatedWheelDelta += direction * WheelDeltaPerSecond(normalized, config) * deltaSec;

        wheelDelta = TakeRoundedWheelDelta(ref _accumulatedWheelDelta);
        return wheelDelta != 0;
    }

    internal static double NormalizeRadius(double radius, Config config) {
        double normalized = (radius - config.LeftStickEnterDeadzone) / (1.0 - config.LeftStickEnterDeadzone);
        return Clamp(normalized, 0.0, 1.0);
    }

    internal static double WheelDeltaPerSecond(double normalizedRadius, Config config) {
        double normalized = Clamp(normalizedRadius, 0.0, 1.0);
        double slowInterval = Math.Max(1.0, (double)config.ScrollSlowIntervalMs);
        double fastInterval = Math.Max(1.0, Math.Min((double)config.ScrollFastIntervalMs, slowInterval));
        double power = Math.Pow(normalized, Math.Max(0.001, config.MouseScrollCurveExponent));
        double interval = slowInterval + (fastInterval - slowInterval) * power;
        return WheelDelta * 1000.0 / Math.Max(1.0, interval);
    }

    private static int TakeRoundedWheelDelta(ref double accumulator) {
        int delta = 0;
        if (accumulator >= 0.5) {
            delta = (int)Math.Floor(accumulator + 0.5);
        } else if (accumulator <= -0.5) {
            delta = (int)Math.Ceiling(accumulator - 0.5);
        }
        accumulator -= delta;
        return delta;
    }

    private static double Clamp(double value, double min, double max) {
        return value < min ? min : (value > max ? max : value);
    }

}
