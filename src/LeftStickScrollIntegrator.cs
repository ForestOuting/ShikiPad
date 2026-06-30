using System;

internal sealed class LeftStickScrollIntegrator {
    internal const int WheelDelta = 120;

    private double _accumulatedWheelDelta;
    private double _smoothedWheelDeltaPerSecond;

    public void Reset() {
        _accumulatedWheelDelta = 0.0;
        _smoothedWheelDeltaPerSecond = 0.0;
    }

    public bool TryUpdate(double radius, double deltaSec, Config config, int direction, out int wheelDelta) {
        wheelDelta = 0;
        if (direction == 0 || deltaSec <= 0.0) return false;

        double normalized = NormalizeRadius(radius, config);
        if (normalized <= 0.0) {
            Reset();
            return false;
        }

        double targetWheelDeltaPerSecond = direction * WheelDeltaPerSecond(normalized, config);
        double smoothingMs = config.ScrollVelocitySmoothingMs * (1.0 - normalized);
        double smoothing = SmoothingAlpha(deltaSec, smoothingMs);
        _smoothedWheelDeltaPerSecond += (targetWheelDeltaPerSecond - _smoothedWheelDeltaPerSecond) * smoothing;
        _accumulatedWheelDelta += _smoothedWheelDeltaPerSecond * deltaSec;

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
        double power = Math.Pow(normalized, Math.Max(0.001, config.MouseScrollCurveExponent));
        return slowSpeed + (fastSpeed - slowSpeed) * power;
    }

    private static double Clamp(double value, double min, double max) {
        return value < min ? min : (value > max ? max : value);
    }

    private static double SmoothingAlpha(double deltaSec, double smoothingMs) {
        if (deltaSec <= 0.0) return 0.0;
        if (smoothingMs <= 0.001) return 1.0;
        return Clamp(1.0 - Math.Exp(-deltaSec * 1000.0 / smoothingMs), 0.0, 1.0);
    }

}
