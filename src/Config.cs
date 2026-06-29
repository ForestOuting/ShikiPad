using System;

internal sealed class Config {
    public bool Enabled = true;
    public double MouseSensitivity = 1.0;
    public double MouseMaxSpeed = 20.0;
    public double RightStickDeadzone = 0.015;
    public string RightStickCurve = "power";
    public double RightStickCurveExponent = 3.0;
    public double MouseScrollCurveExponent = 3.0;
    public double LeftStickEnterDeadzone = 0.35;
    public double LeftStickExitDeadzone = 0.35;
    public double TriggerPressThreshold = 0.25;
    public double TriggerReleaseThreshold = 0.25;
    public int RepeatDelayMs = 300;
    public int RepeatIntervalMs = 18;
    public int BaseRepeatSlowIntervalMs = 120;
    public int BaseRepeatRampMs = 1000;
    public int ActionLayerGraceMs = 45;
    public int ActionLayerPostGraceMs = 15;
    public int LayerTakeoverWindowMs = 30;
    public int ActionLayerSwitchGuardMs = 35;
    public int ComboLayerWindowMs = 55;
    public bool UseScanCode = true;
    public int ScrollSlowIntervalMs = 300;
    public int ScrollFastIntervalMs = 18;
    public int R3FreezeMs = 60;
    public int ClutchLongPressMs = 250;
}
