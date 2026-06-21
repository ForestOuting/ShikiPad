using System;

internal sealed class LeftStickScrollIntegrator {
    internal const int WheelDelta = 120;
    private const double StartupCurveEnd = 0.20;
    private const double StartupWheelDeltaPerSecond = 500.0;

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

        int amount = (int)Math.Truncate(_accumulatedWheelDelta);
        if (amount == 0) return false;

        wheelDelta = amount;
        _accumulatedWheelDelta -= wheelDelta;
        return true;
    }

    internal static double NormalizeRadius(double radius, Config config) {
        double normalized = (radius - config.LeftStickEnterDeadzone) / (1.0 - config.LeftStickEnterDeadzone);
        return Clamp(normalized, 0.0, 1.0);
    }

    internal static double WheelDeltaPerSecond(double normalizedRadius, Config config) {
        double normalized = Clamp(normalizedRadius, 0.0, 1.0);
        double slowInterval = Math.Max(1.0, (double)config.ScrollSlowIntervalMs);
        double fastInterval = Math.Max(1.0, Math.Min((double)config.ScrollFastIntervalMs, slowInterval));
        double slowSpeed = WheelDelta * 1000.0 / slowInterval;
        double fastSpeed = WheelDelta * 1000.0 / fastInterval;
        double smoothStart = normalized * normalized * (3.0 - 2.0 * normalized);
        double power = Math.Pow(normalized, config.MouseScrollCurveExponent);
        double startupSpeed = StartupWheelDeltaPerSecond * SmoothStep(Clamp(normalized / StartupCurveEnd, 0.0, 1.0)) * (1.0 - normalized);
        return startupSpeed + slowSpeed * smoothStart + (fastSpeed - slowSpeed) * power;
    }

    private static double Clamp(double value, double min, double max) {
        return value < min ? min : (value > max ? max : value);
    }

    private static double SmoothStep(double value) {
        return value * value * (3.0 - 2.0 * value);
    }
}
