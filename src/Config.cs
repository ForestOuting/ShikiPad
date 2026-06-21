using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

internal sealed class Config {
    public bool Enabled = true;
    public double MouseSensitivity = 1.0;
    public double MouseMaxSpeed = 22.0;
    public double RightStickDeadzone = 0.025;
    public string RightStickCurve = "power";
    public double RightStickCurveExponent = 3.0;
    public double MouseScrollCurveExponent = 3.0;
    public double LeftStickEnterDeadzone = 0.35;
    public double LeftStickExitDeadzone = 0.25;
    public double TriggerPressThreshold = 0.1;
    public double TriggerReleaseThreshold = 0.05;
    public int RepeatDelayMs = 180;
    public int RepeatIntervalMs = 20;
    public int BaseRepeatSlowIntervalMs = 160;
    public int BaseRepeatRampMs = 1200;
    public int ActionLayerGraceMs = 35;
    public int LayerTakeoverWindowMs = 25;
    public int ActionLayerSwitchGuardMs = 35;
    public int ComboLayerWindowMs = 25;
    public bool UseScanCode = true;
    public bool UseInterception = true;
    public int ScrollSlowIntervalMs = 120;
    public int ScrollFastIntervalMs = 12;
    public int R3FreezeMs = 60;
    public int ClutchLongPressMs = 250;
    public static Config Load(string path) {
        Config cfg = new Config();
        if (!File.Exists(path)) {
            cfg.Save(path);
            return cfg;
        }

        try {
            string text = File.ReadAllText(path);
            bool shouldSaveMigratedConfig = text.Contains("\"mouseDeadzone\"") ||
                                            text.Contains("\"touchpadSwipeThreshold\"") ||
                                            text.Contains("\"touchpadMaxSwipeMs\"") ||
                                            !text.Contains("\"rightStickDeadzone\"") ||
                                            !text.Contains("\"rightStickCurve\"") ||
                                            !text.Contains("\"rightStickCurveExponent\"") ||
                                            text.Contains("\"rightStickEpsilon\"") ||
                                            !text.Contains("\"mouseScrollCurveExponent\"") ||
                                            !text.Contains("\"leftStickEnterDeadzone\"") ||
                                            !text.Contains("\"leftStickExitDeadzone\"") ||
                                            !text.Contains("\"clutchLongPressMs\"");
            bool shouldSaveLeftStickConfig = false;
            cfg.Enabled = GetBool(text, "enabled", cfg.Enabled);
            cfg.MouseSensitivity = GetDouble(text, "mouseSensitivity", cfg.MouseSensitivity);
            cfg.MouseMaxSpeed = GetDouble(text, "mouseMaxSpeed", cfg.MouseMaxSpeed);
            cfg.RightStickDeadzone = GetDouble(text, "rightStickDeadzone", cfg.RightStickDeadzone);
            cfg.RightStickCurve = GetString(text, "rightStickCurve", cfg.RightStickCurve);
            cfg.RightStickCurveExponent = GetDouble(text, "rightStickCurveExponent", cfg.RightStickCurveExponent);
            cfg.MouseScrollCurveExponent = GetDouble(text, "mouseScrollCurveExponent", cfg.MouseScrollCurveExponent);
            cfg.LeftStickEnterDeadzone = GetDouble(text, "leftStickEnterDeadzone", cfg.LeftStickEnterDeadzone);
            cfg.LeftStickExitDeadzone = GetDouble(text, "leftStickExitDeadzone", cfg.LeftStickExitDeadzone);
            cfg.TriggerPressThreshold = GetDouble(text, "triggerPressThreshold", cfg.TriggerPressThreshold);
            cfg.TriggerReleaseThreshold = GetDouble(text, "triggerReleaseThreshold", cfg.TriggerReleaseThreshold);
            cfg.RepeatDelayMs = GetInt(text, "repeatDelayMs", cfg.RepeatDelayMs);
            cfg.RepeatIntervalMs = GetInt(text, "repeatIntervalMs", cfg.RepeatIntervalMs);
            cfg.BaseRepeatSlowIntervalMs = GetInt(text, "baseRepeatSlowIntervalMs", cfg.BaseRepeatSlowIntervalMs);
            cfg.BaseRepeatRampMs = GetInt(text, "baseRepeatRampMs", cfg.BaseRepeatRampMs);
            cfg.ActionLayerGraceMs = GetInt(text, "actionLayerGraceMs", cfg.ActionLayerGraceMs);
            cfg.LayerTakeoverWindowMs = GetInt(text, "layerTakeoverWindowMs", cfg.LayerTakeoverWindowMs);
            cfg.ActionLayerSwitchGuardMs = GetInt(text, "actionLayerSwitchGuardMs", cfg.ActionLayerSwitchGuardMs);
            cfg.ComboLayerWindowMs = GetInt(text, "comboLayerWindowMs", cfg.ComboLayerWindowMs);
            cfg.UseScanCode = GetBool(text, "useScanCode", cfg.UseScanCode);
            cfg.UseInterception = GetBool(text, "useInterception", cfg.UseInterception);
            cfg.ScrollSlowIntervalMs = GetInt(text, "scrollSlowIntervalMs", cfg.ScrollSlowIntervalMs);
            cfg.ScrollFastIntervalMs = GetInt(text, "scrollFastIntervalMs", cfg.ScrollFastIntervalMs);
            cfg.R3FreezeMs = GetInt(text, "r3FreezeMs", cfg.R3FreezeMs);
            cfg.ClutchLongPressMs = GetInt(text, "clutchLongPressMs", cfg.ClutchLongPressMs);

            if (IsInvalidPositive(cfg.MouseSensitivity)) {
                Logger.Warn("invalid mouseSensitivity; using 1.0");
                cfg.MouseSensitivity = 1.0;
                shouldSaveMigratedConfig = true;
            }
            if (IsInvalidPositive(cfg.MouseMaxSpeed)) {
                Logger.Warn("invalid mouseMaxSpeed; using 22.0");
                cfg.MouseMaxSpeed = 22.0;
                shouldSaveMigratedConfig = true;
            }
            if (Double.IsNaN(cfg.RightStickDeadzone) || Double.IsInfinity(cfg.RightStickDeadzone) || cfg.RightStickDeadzone < 0.0 || cfg.RightStickDeadzone >= 0.95) {
                Logger.Warn("invalid rightStickDeadzone; using 0.025");
                cfg.RightStickDeadzone = 0.025;
                shouldSaveMigratedConfig = true;
            }
            if (IsInvalidPositive(cfg.MouseScrollCurveExponent)) {
                Logger.Warn("invalid mouseScrollCurveExponent; using 3.0");
                cfg.MouseScrollCurveExponent = 3.0;
                shouldSaveMigratedConfig = true;
            }
            if (cfg.RightStickDeadzone == 0.0 || Math.Abs(cfg.RightStickDeadzone - 0.05) < 0.000001 || Math.Abs(cfg.RightStickDeadzone - 0.03) < 0.000001) {
                Logger.Info("migrating rightStickDeadzone to 0.025");
                cfg.RightStickDeadzone = 0.025;
                shouldSaveMigratedConfig = true;
            }

            if (!String.Equals(cfg.RightStickCurve, "power", StringComparison.Ordinal)) {
                Logger.Warn("unsupported rightStickCurve '" + cfg.RightStickCurve + "'; using power");
                cfg.RightStickCurve = "power";
                shouldSaveMigratedConfig = true;
            }
            if (cfg.RightStickCurveExponent <= 0.0 || Double.IsNaN(cfg.RightStickCurveExponent) || Double.IsInfinity(cfg.RightStickCurveExponent)) {
                Logger.Warn("invalid rightStickCurveExponent; using 3.0");
                cfg.RightStickCurveExponent = 3.0;
                shouldSaveMigratedConfig = true;
            }
            if (!text.Contains("\"baseRepeatSlowIntervalMs\"") ||
                !text.Contains("\"baseRepeatRampMs\"") ||
                !text.Contains("\"actionLayerGraceMs\"") ||
                !text.Contains("\"layerTakeoverWindowMs\"") ||
                !text.Contains("\"actionLayerSwitchGuardMs\"") ||
                !text.Contains("\"comboLayerWindowMs\"")) {
                shouldSaveMigratedConfig = true;
            }
            if (cfg.LayerTakeoverWindowMs < 0 || cfg.LayerTakeoverWindowMs > cfg.ActionLayerGraceMs) {
                int fallbackLayerTakeoverMs = Math.Min(25, Math.Max(0, cfg.ActionLayerGraceMs));
                Logger.Warn("invalid layerTakeoverWindowMs; using " + fallbackLayerTakeoverMs.ToString(CultureInfo.InvariantCulture));
                cfg.LayerTakeoverWindowMs = fallbackLayerTakeoverMs;
                shouldSaveMigratedConfig = true;
            }
            if (cfg.ActionLayerGraceMs == 80 || cfg.ActionLayerGraceMs == 20) {
                Logger.Info("migrating actionLayerGraceMs to 35");
                cfg.ActionLayerGraceMs = 35;
                shouldSaveMigratedConfig = true;
            }
            if (cfg.ActionLayerSwitchGuardMs == 120) {
                Logger.Info("migrating actionLayerSwitchGuardMs to 35");
                cfg.ActionLayerSwitchGuardMs = 35;
                shouldSaveMigratedConfig = true;
            }
            if (cfg.ComboLayerWindowMs < 0 || cfg.ComboLayerWindowMs > 500) {
                Logger.Warn("invalid comboLayerWindowMs; using 25");
                cfg.ComboLayerWindowMs = 25;
                shouldSaveMigratedConfig = true;
            }
            if (cfg.ComboLayerWindowMs == 35 || cfg.ComboLayerWindowMs == 50 || cfg.ComboLayerWindowMs == 100 || cfg.ComboLayerWindowMs == 80) {
                Logger.Info("migrating comboLayerWindowMs to 25");
                cfg.ComboLayerWindowMs = 25;
                shouldSaveMigratedConfig = true;
            }
            if (Math.Abs(cfg.MouseMaxSpeed - 18.0) < 0.000001 || Math.Abs(cfg.MouseMaxSpeed - 16.0) < 0.000001 || Math.Abs(cfg.MouseMaxSpeed - 15.0) < 0.000001 || Math.Abs(cfg.MouseMaxSpeed - 13.0) < 0.000001 || Math.Abs(cfg.MouseMaxSpeed - 12.0) < 0.000001 || Math.Abs(cfg.MouseMaxSpeed - 10.0) < 0.000001 || Math.Abs(cfg.MouseMaxSpeed - 8.0) < 0.000001 || Math.Abs(cfg.MouseMaxSpeed - 7.0) < 0.000001 || Math.Abs(cfg.MouseMaxSpeed - 28.0) < 0.000001 || Math.Abs(cfg.MouseMaxSpeed - 22.4) < 0.000001 || Math.Abs(cfg.MouseMaxSpeed - 20.0) < 0.000001 || Math.Abs(cfg.MouseMaxSpeed - 25.0) < 0.000001) {
                Logger.Info("migrating mouseMaxSpeed to 22.0");
                cfg.MouseMaxSpeed = 22.0;
                shouldSaveMigratedConfig = true;
            }
            if (Math.Abs(cfg.RightStickCurveExponent - 2.0) < 0.000001 || Math.Abs(cfg.RightStickCurveExponent - 2.5) < 0.000001 || Math.Abs(cfg.RightStickCurveExponent - 2.6) < 0.000001 || Math.Abs(cfg.RightStickCurveExponent - 2.2) < 0.000001 || Math.Abs(cfg.RightStickCurveExponent - 2.4) < 0.000001) {
                Logger.Info("migrating rightStickCurveExponent to 3.0");
                cfg.RightStickCurveExponent = 3.0;
                shouldSaveMigratedConfig = true;
            }
            if (cfg.ScrollFastIntervalMs == 6 || cfg.ScrollFastIntervalMs == 8 || cfg.ScrollFastIntervalMs == 20) {
                Logger.Info("migrating scrollFastIntervalMs to 12");
                cfg.ScrollFastIntervalMs = 12;
                shouldSaveMigratedConfig = true;
            }
            if (cfg.ScrollSlowIntervalMs == 100) {
                Logger.Info("migrating scrollSlowIntervalMs to 120");
                cfg.ScrollSlowIntervalMs = 120;
                shouldSaveMigratedConfig = true;
            }
            if (cfg.ClutchLongPressMs < 80 || cfg.ClutchLongPressMs > 1000) {
                Logger.Warn("invalid clutchLongPressMs; using 250");
                cfg.ClutchLongPressMs = 250;
                shouldSaveMigratedConfig = true;
            }
            if (Math.Abs(cfg.LeftStickEnterDeadzone - 0.50) < 0.000001 || Math.Abs(cfg.LeftStickEnterDeadzone - 0.30) < 0.000001) {
                Logger.Info("migrating leftStickEnterDeadzone to 0.35");
                cfg.LeftStickEnterDeadzone = 0.35;
                shouldSaveLeftStickConfig = true;
            }
            if (Math.Abs(cfg.LeftStickExitDeadzone - 0.45) < 0.000001 || Math.Abs(cfg.LeftStickExitDeadzone - 0.20) < 0.000001 || Math.Abs(cfg.LeftStickExitDeadzone - 0.30) < 0.000001 || Math.Abs(cfg.LeftStickExitDeadzone - 0.15) < 0.000001) {
                Logger.Info("migrating leftStickExitDeadzone to 0.25");
                cfg.LeftStickExitDeadzone = 0.25;
                shouldSaveLeftStickConfig = true;
            }
            if (shouldSaveMigratedConfig || shouldSaveLeftStickConfig) cfg.Save(path);
        } catch (Exception ex) {
            Logger.Error("config load failed: " + ex.Message);
        }

        return cfg;
    }

    public void Save(string path) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        Write(sb, "enabled", Enabled, true);
        Write(sb, "mouseSensitivity", MouseSensitivity, true);
        Write(sb, "mouseMaxSpeed", MouseMaxSpeed, true);
        Write(sb, "rightStickDeadzone", RightStickDeadzone, true);
        Write(sb, "rightStickCurve", RightStickCurve, true);
        Write(sb, "rightStickCurveExponent", RightStickCurveExponent, true);
        Write(sb, "mouseScrollCurveExponent", MouseScrollCurveExponent, true);
        Write(sb, "leftStickEnterDeadzone", LeftStickEnterDeadzone, true);
        Write(sb, "leftStickExitDeadzone", LeftStickExitDeadzone, true);
        Write(sb, "triggerPressThreshold", TriggerPressThreshold, true);
        Write(sb, "triggerReleaseThreshold", TriggerReleaseThreshold, true);
        Write(sb, "repeatDelayMs", RepeatDelayMs, true);
        Write(sb, "repeatIntervalMs", RepeatIntervalMs, true);
        Write(sb, "baseRepeatSlowIntervalMs", BaseRepeatSlowIntervalMs, true);
        Write(sb, "baseRepeatRampMs", BaseRepeatRampMs, true);
        Write(sb, "actionLayerGraceMs", ActionLayerGraceMs, true);
        Write(sb, "layerTakeoverWindowMs", LayerTakeoverWindowMs, true);
        Write(sb, "actionLayerSwitchGuardMs", ActionLayerSwitchGuardMs, true);
        Write(sb, "comboLayerWindowMs", ComboLayerWindowMs, true);
        Write(sb, "useScanCode", UseScanCode, true);
        Write(sb, "useInterception", UseInterception, true);
        Write(sb, "scrollSlowIntervalMs", ScrollSlowIntervalMs, true);
        Write(sb, "scrollFastIntervalMs", ScrollFastIntervalMs, true);
        Write(sb, "r3FreezeMs", R3FreezeMs, true);
        Write(sb, "clutchLongPressMs", ClutchLongPressMs, false);

        sb.AppendLine("}");
        File.WriteAllText(path, sb.ToString());
    }

    private static void Write(StringBuilder sb, string key, bool value, bool comma) {
        sb.Append("  \"").Append(key).Append("\": ").Append(value ? "true" : "false");
        if (comma) sb.Append(",");
        sb.AppendLine();
    }

    private static void Write(StringBuilder sb, string key, double value, bool comma) {
        sb.Append("  \"").Append(key).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture));
        if (comma) sb.Append(",");
        sb.AppendLine();
    }

    private static void Write(StringBuilder sb, string key, string value, bool comma) {
        sb.Append("  \"").Append(key).Append("\": \"").Append(value.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\"");
        if (comma) sb.Append(",");
        sb.AppendLine();
    }

    private static void Write(StringBuilder sb, string key, int value, bool comma) {
        sb.Append("  \"").Append(key).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture));
        if (comma) sb.Append(",");
        sb.AppendLine();
    }

    private static bool GetBool(string text, string key, bool fallback) {
        Match m = Regex.Match(text, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
        return m.Success ? String.Equals(m.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase) : fallback;
    }

    private static int GetInt(string text, string key, int fallback) {
        Match m = Regex.Match(text, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
        int value;
        return m.Success && Int32.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : fallback;
    }

    private static double GetDouble(string text, string key, double fallback) {
        Match m = Regex.Match(text, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)");
        double value;
        return m.Success && Double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : fallback;
    }

    private static string GetString(string text, string key, string fallback) {
        Match m = Regex.Match(text, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
        return m.Success ? m.Groups[1].Value : fallback;
    }

    private static bool IsInvalidPositive(double value) {
        return value <= 0.0 || Double.IsNaN(value) || Double.IsInfinity(value);
    }
}
