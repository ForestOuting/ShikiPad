using System;

internal sealed class Config {
    public bool Enabled = true;
    public double MouseSensitivity = 1.0;
    public double MouseMaxSpeed = 20.0;
    public double RightStickDeadzone = 0.015;
    public string RightStickCurve = "power";
    public double RightStickCurveExponent = 3.0;
    public double RightStickSmoothingMs = 5.0;
    public double MouseScrollCurveExponent = 3.0;
    public double MouseScrollSmoothingMs = 5.0;
    public double LeftStickEnterDeadzone = 0.25;
    public double LeftStickExitDeadzone = 0.15;
    public double TriggerPressThreshold = 0.25;
    public double TriggerReleaseThreshold = 0.15;
    public int RepeatDelayMs = 300;
    public int RepeatIntervalMs = 12;
    public int BaseRepeatSlowIntervalMs = 120;
    public int BaseRepeatRampMs = 1500;
    public int ActionLayerGraceMs = 35;
    public int ActionLayerPostGraceMs = 15;
    public int LayerTakeoverWindowMs = 25;
    public int LayerOccupancyCarryCutoffMs = 15;
    public int ComboLayerWindowMs = 35;
    public bool UseScanCode = true;
    public int ScrollSlowIntervalMs = 1500;
    public int ScrollFastIntervalMs = 15;
    public double TouchGestureMoveStartThreshold = 50.0;
    public double TouchGestureThreshold = 320.0;
    public double TouchGestureRepeatDistance = 180.0;
    public int TouchGestureSideMiddleLeft = 800;
    public int TouchGestureSideMiddleRight = 1119;
    public int TouchGestureHoldMs = 150;
    public int R3FreezeMs = 60;
    public int ClutchLongPressMs = 250;
}
