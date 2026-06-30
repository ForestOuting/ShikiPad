using System;

internal sealed class RightStickMouseIntegrator {

    private double _accumX;
    private double _accumY;
    private double _velocityX;
    private double _velocityY;

    public void Reset() {
        _accumX = 0.0;
        _accumY = 0.0;
        _velocityX = 0.0;
        _velocityY = 0.0;
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
        double maxSpeed = Math.Max(1.0, config.MouseMaxSpeed * 120.0 * config.MouseSensitivity);
        double slowInterval = Math.Max(1.0, (double)config.MouseSlowIntervalMs);
        double slowSpeed = Math.Min(maxSpeed, 1000.0 / slowInterval);
        double speed = slowSpeed + (maxSpeed - slowSpeed) * powerRatio;
        double targetVelocityX = dirX * speed;
        double targetVelocityY = dirY * speed;
        double smoothingMs = config.MouseVelocitySmoothingMs * (1.0 - powerRatio);
        double smoothing = SmoothingAlpha(deltaSec, smoothingMs);
        _velocityX += (targetVelocityX - _velocityX) * smoothing;
        _velocityY += (targetVelocityY - _velocityY) * smoothing;
        double rawDx = _velocityX * deltaSec;
        double rawDy = _velocityY * deltaSec;

        _accumX += rawDx;
        _accumY += rawDy;
        dx = TakeWholeMouseDelta(ref _accumX);
        dy = TakeWholeMouseDelta(ref _accumY);
        return dx != 0 || dy != 0;
    }

    private static int TakeWholeMouseDelta(ref double accumulator) {
        int delta = 0;
        if (accumulator >= 1.0) {
            delta = (int)Math.Floor(accumulator);
        } else if (accumulator <= -1.0) {
            delta = (int)Math.Ceiling(accumulator);
        }
        accumulator -= delta;
        return delta;
    }

    private static double SmoothingAlpha(double deltaSec, double smoothingMs) {
        if (deltaSec <= 0.0) return 0.0;
        if (smoothingMs <= 0.001) return 1.0;
        return Clamp(1.0 - Math.Exp(-deltaSec * 1000.0 / smoothingMs), 0.0, 1.0);
    }

    private static double Clamp(double value, double min, double max) {
        return value < min ? min : (value > max ? max : value);
    }

}
