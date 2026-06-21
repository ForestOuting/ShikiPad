using System;

internal sealed class RightStickMouseIntegrator {
    private const double StartupCurveStart = 0.04;
    private const double StartupCurveEnd = 0.25;
    private const double StartupSpeedRatio = 0.012;

    private double _accumX;
    private double _accumY;

    public void Reset() {
        _accumX = 0.0;
        _accumY = 0.0;
    }

    public bool TryUpdate(double x, double y, double deltaSec, Config config, out int dx, out int dy) {
        dx = 0;
        dy = 0;

        double actualRadius = Math.Sqrt(x * x + y * y);
        double radius = Clamp(actualRadius, 0.0, 1.0);
        if (radius <= config.RightStickDeadzone) {
            Reset();
            return false;
        }

        double normalizedRadius = Clamp((radius - config.RightStickDeadzone) / (1.0 - config.RightStickDeadzone), 0.0, 1.0);
        double dirX = x / actualRadius;
        double dirY = y / actualRadius;
        double powerRatio = Math.Pow(normalizedRadius, config.RightStickCurveExponent);
        double startupT = Clamp((normalizedRadius - StartupCurveStart) / (StartupCurveEnd - StartupCurveStart), 0.0, 1.0);
        double startupRatio = StartupSpeedRatio * SmoothStep(startupT) * (1.0 - normalizedRadius);
        double speedRatio = Math.Max(powerRatio, startupRatio);
        double speed = config.MouseMaxSpeed * deltaSec * 120.0 * config.MouseSensitivity;
        double rawDx = dirX * speedRatio * speed;
        double rawDy = dirY * speedRatio * speed;
        if (Math.Abs(rawDx) + Math.Abs(rawDy) < 0.000001) return false;

        _accumX += rawDx;
        _accumY += rawDy;
        dx = TakeRoundedMouseDelta(ref _accumX);
        dy = TakeRoundedMouseDelta(ref _accumY);
        return dx != 0 || dy != 0;
    }

    private static int TakeRoundedMouseDelta(ref double accumulator) {
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

    private static double SmoothStep(double value) {
        return value * value * (3.0 - 2.0 * value);
    }
}
