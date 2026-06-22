using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

internal sealed class Config {
    private const int CurrentConfigVersion = 8;
    private const double DefaultMouseScrollCurveExponent = 3.0;
    private const int DefaultScrollSlowIntervalMs = 180;
    private const int DefaultScrollFastIntervalMs = 18;
    private const int DefaultComboLayerWindowMs = 35;
    private const double DefaultTriggerPressThreshold = 0.0;
    private const double DefaultTriggerReleaseThreshold = 0.0;
    private const int DefaultLayerTakeoverWindowMs = 30;
    private const int DefaultActionLayerPostGraceMs = 25;
    private const int DefaultRepeatIntervalMs = 32;
    private const int DefaultBaseRepeatSlowIntervalMs = 240;
    private const int DefaultBaseRepeatRampMs = 2500;
    private const double DefaultRightStickDeadzone = 0.03;

    public int ConfigVersion = CurrentConfigVersion;
    public bool Enabled = true;
    public double MouseSensitivity = 1.0;
    public double MouseMaxSpeed = 20.0;
    public double RightStickDeadzone = DefaultRightStickDeadzone;
    public string RightStickCurve = "power";
    public double RightStickCurveExponent = 3.0;
    public double MouseScrollCurveExponent = DefaultMouseScrollCurveExponent;
    public double LeftStickEnterDeadzone = 0.35;
    public double LeftStickExitDeadzone = 0.25;
    public double TriggerPressThreshold = DefaultTriggerPressThreshold;
    public double TriggerReleaseThreshold = DefaultTriggerReleaseThreshold;
    public int RepeatDelayMs = 300;
    public int RepeatIntervalMs = DefaultRepeatIntervalMs;
    public int BaseRepeatSlowIntervalMs = DefaultBaseRepeatSlowIntervalMs;
    public int BaseRepeatRampMs = DefaultBaseRepeatRampMs;
    public int ActionLayerGraceMs = 35;
    public int ActionLayerPostGraceMs = DefaultActionLayerPostGraceMs;
    public int LayerTakeoverWindowMs = DefaultLayerTakeoverWindowMs;
    public int ActionLayerSwitchGuardMs = 35;
    public int ComboLayerWindowMs = DefaultComboLayerWindowMs;
    public bool UseScanCode = true;
    public bool UseInterception = true;
    public int ScrollSlowIntervalMs = DefaultScrollSlowIntervalMs;
    public int ScrollFastIntervalMs = DefaultScrollFastIntervalMs;
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
            JsonObject json = JsonObject.Parse(text);
            bool shouldSaveConfig = false;

            cfg.ConfigVersion = GetInt(json, "configVersion", 1);
            cfg.Enabled = GetBool(json, "enabled", cfg.Enabled);
            cfg.MouseSensitivity = GetDouble(json, "mouseSensitivity", cfg.MouseSensitivity);
            cfg.MouseMaxSpeed = GetDouble(json, "mouseMaxSpeed", cfg.MouseMaxSpeed);
            cfg.RightStickDeadzone = GetDouble(json, "rightStickDeadzone", cfg.RightStickDeadzone);
            cfg.RightStickCurve = GetString(json, "rightStickCurve", cfg.RightStickCurve);
            cfg.RightStickCurveExponent = GetDouble(json, "rightStickCurveExponent", cfg.RightStickCurveExponent);
            cfg.MouseScrollCurveExponent = GetDouble(json, "mouseScrollCurveExponent", cfg.MouseScrollCurveExponent);
            cfg.LeftStickEnterDeadzone = GetDouble(json, "leftStickEnterDeadzone", cfg.LeftStickEnterDeadzone);
            cfg.LeftStickExitDeadzone = GetDouble(json, "leftStickExitDeadzone", cfg.LeftStickExitDeadzone);
            cfg.TriggerPressThreshold = GetDouble(json, "triggerPressThreshold", cfg.TriggerPressThreshold);
            cfg.TriggerReleaseThreshold = GetDouble(json, "triggerReleaseThreshold", cfg.TriggerReleaseThreshold);
            cfg.RepeatDelayMs = GetInt(json, "repeatDelayMs", cfg.RepeatDelayMs);
            cfg.RepeatIntervalMs = GetInt(json, "repeatIntervalMs", cfg.RepeatIntervalMs);
            cfg.BaseRepeatSlowIntervalMs = GetInt(json, "baseRepeatSlowIntervalMs", cfg.BaseRepeatSlowIntervalMs);
            cfg.BaseRepeatRampMs = GetInt(json, "baseRepeatRampMs", cfg.BaseRepeatRampMs);
            cfg.ActionLayerGraceMs = GetInt(json, "actionLayerGraceMs", cfg.ActionLayerGraceMs);
            cfg.ActionLayerPostGraceMs = GetInt(json, "actionLayerPostGraceMs", cfg.ActionLayerPostGraceMs);
            cfg.LayerTakeoverWindowMs = GetInt(json, "layerTakeoverWindowMs", cfg.LayerTakeoverWindowMs);
            cfg.ActionLayerSwitchGuardMs = GetInt(json, "actionLayerSwitchGuardMs", cfg.ActionLayerSwitchGuardMs);
            cfg.ComboLayerWindowMs = GetInt(json, "comboLayerWindowMs", cfg.ComboLayerWindowMs);
            cfg.UseScanCode = GetBool(json, "useScanCode", cfg.UseScanCode);
            cfg.UseInterception = GetBool(json, "useInterception", cfg.UseInterception);
            cfg.ScrollSlowIntervalMs = GetInt(json, "scrollSlowIntervalMs", cfg.ScrollSlowIntervalMs);
            cfg.ScrollFastIntervalMs = GetInt(json, "scrollFastIntervalMs", cfg.ScrollFastIntervalMs);
            cfg.R3FreezeMs = GetInt(json, "r3FreezeMs", cfg.R3FreezeMs);
            cfg.ClutchLongPressMs = GetInt(json, "clutchLongPressMs", cfg.ClutchLongPressMs);

            if (cfg.ConfigVersion < CurrentConfigVersion) {
                MigrateDefaultsToCurrent(cfg);
                shouldSaveConfig = true;
            }
            if (cfg.ConfigVersion != CurrentConfigVersion) {
                cfg.ConfigVersion = CurrentConfigVersion;
                shouldSaveConfig = true;
            }

            if (!json.ContainsKey("rightStickDeadzone") && TryGetDouble(json, "mouseDeadzone", out cfg.RightStickDeadzone)) {
                Logger.Info("migrating mouseDeadzone to rightStickDeadzone");
                shouldSaveConfig = true;
            }

            if (HasRemovedLegacyKeys(json) || HasMissingCurrentKeys(json)) {
                shouldSaveConfig = true;
            }

            if (IsInvalidPositive(cfg.MouseSensitivity)) {
                Logger.Warn("invalid mouseSensitivity; using 1.0");
                cfg.MouseSensitivity = 1.0;
                shouldSaveConfig = true;
            }
            if (IsInvalidPositive(cfg.MouseMaxSpeed)) {
                Logger.Warn("invalid mouseMaxSpeed; using 20.0");
                cfg.MouseMaxSpeed = 20.0;
                shouldSaveConfig = true;
            }
            if (Double.IsNaN(cfg.RightStickDeadzone) || Double.IsInfinity(cfg.RightStickDeadzone) || cfg.RightStickDeadzone < 0.0 || cfg.RightStickDeadzone >= 0.95) {
                Logger.Warn("invalid rightStickDeadzone; using " + DefaultRightStickDeadzone.ToString("0.###", CultureInfo.InvariantCulture));
                cfg.RightStickDeadzone = DefaultRightStickDeadzone;
                shouldSaveConfig = true;
            }
            if (IsInvalidPositive(cfg.MouseScrollCurveExponent)) {
                Logger.Warn("invalid mouseScrollCurveExponent; using " + DefaultMouseScrollCurveExponent.ToString("0.###", CultureInfo.InvariantCulture));
                cfg.MouseScrollCurveExponent = DefaultMouseScrollCurveExponent;
                shouldSaveConfig = true;
            }

            if (String.Equals(cfg.RightStickCurve, "power", StringComparison.OrdinalIgnoreCase)) {
                cfg.RightStickCurve = "power";
            } else {
                Logger.Warn("unsupported rightStickCurve '" + cfg.RightStickCurve + "'; using power");
                cfg.RightStickCurve = "power";
                shouldSaveConfig = true;
            }
            if (cfg.RightStickCurveExponent <= 0.0 || Double.IsNaN(cfg.RightStickCurveExponent) || Double.IsInfinity(cfg.RightStickCurveExponent)) {
                Logger.Warn("invalid rightStickCurveExponent; using 3.0");
                cfg.RightStickCurveExponent = 3.0;
                shouldSaveConfig = true;
            }
            if (cfg.LayerTakeoverWindowMs < 0 || cfg.LayerTakeoverWindowMs > cfg.ActionLayerGraceMs) {
                int fallbackLayerTakeoverMs = Math.Min(DefaultLayerTakeoverWindowMs, Math.Max(0, cfg.ActionLayerGraceMs));
                Logger.Warn("invalid layerTakeoverWindowMs; using " + fallbackLayerTakeoverMs.ToString(CultureInfo.InvariantCulture));
                cfg.LayerTakeoverWindowMs = fallbackLayerTakeoverMs;
                shouldSaveConfig = true;
            }
            if (cfg.ComboLayerWindowMs < 0 || cfg.ComboLayerWindowMs > 500) {
                Logger.Warn("invalid comboLayerWindowMs; using " + DefaultComboLayerWindowMs.ToString(CultureInfo.InvariantCulture));
                cfg.ComboLayerWindowMs = DefaultComboLayerWindowMs;
                shouldSaveConfig = true;
            }
            if (cfg.ClutchLongPressMs < 80 || cfg.ClutchLongPressMs > 1000) {
                Logger.Warn("invalid clutchLongPressMs; using 250");
                cfg.ClutchLongPressMs = 250;
                shouldSaveConfig = true;
            }
            if (Double.IsNaN(cfg.LeftStickEnterDeadzone) || Double.IsInfinity(cfg.LeftStickEnterDeadzone) || cfg.LeftStickEnterDeadzone <= 0.0 || cfg.LeftStickEnterDeadzone >= 1.0) {
                Logger.Warn("invalid leftStickEnterDeadzone; using 0.35");
                cfg.LeftStickEnterDeadzone = 0.35;
                shouldSaveConfig = true;
            }
            if (Double.IsNaN(cfg.LeftStickExitDeadzone) || Double.IsInfinity(cfg.LeftStickExitDeadzone) || cfg.LeftStickExitDeadzone < 0.0 || cfg.LeftStickExitDeadzone >= cfg.LeftStickEnterDeadzone) {
                Logger.Warn("invalid leftStickExitDeadzone; using 0.25");
                cfg.LeftStickExitDeadzone = 0.25;
                shouldSaveConfig = true;
            }
            if (Double.IsNaN(cfg.TriggerPressThreshold) || Double.IsInfinity(cfg.TriggerPressThreshold) ||
                Double.IsNaN(cfg.TriggerReleaseThreshold) || Double.IsInfinity(cfg.TriggerReleaseThreshold) ||
                cfg.TriggerPressThreshold < 0.0 || cfg.TriggerPressThreshold > 1.0 ||
                cfg.TriggerReleaseThreshold < 0.0 || cfg.TriggerReleaseThreshold > cfg.TriggerPressThreshold) {
                Logger.Warn("invalid trigger thresholds; using " +
                            DefaultTriggerPressThreshold.ToString("0.###", CultureInfo.InvariantCulture) + " / " +
                            DefaultTriggerReleaseThreshold.ToString("0.###", CultureInfo.InvariantCulture));
                cfg.TriggerPressThreshold = DefaultTriggerPressThreshold;
                cfg.TriggerReleaseThreshold = DefaultTriggerReleaseThreshold;
                shouldSaveConfig = true;
            }
            if (cfg.RepeatDelayMs < 0) {
                Logger.Warn("invalid repeatDelayMs; using 300");
                cfg.RepeatDelayMs = 300;
                shouldSaveConfig = true;
            }
            if (cfg.RepeatIntervalMs <= 0) {
                Logger.Warn("invalid repeatIntervalMs; using " + DefaultRepeatIntervalMs.ToString(CultureInfo.InvariantCulture));
                cfg.RepeatIntervalMs = DefaultRepeatIntervalMs;
                shouldSaveConfig = true;
            }
            if (cfg.BaseRepeatSlowIntervalMs <= 0) {
                Logger.Warn("invalid baseRepeatSlowIntervalMs; using " + DefaultBaseRepeatSlowIntervalMs.ToString(CultureInfo.InvariantCulture));
                cfg.BaseRepeatSlowIntervalMs = DefaultBaseRepeatSlowIntervalMs;
                shouldSaveConfig = true;
            }
            if (cfg.BaseRepeatRampMs <= 0) {
                Logger.Warn("invalid baseRepeatRampMs; using " + DefaultBaseRepeatRampMs.ToString(CultureInfo.InvariantCulture));
                cfg.BaseRepeatRampMs = DefaultBaseRepeatRampMs;
                shouldSaveConfig = true;
            }
            if (cfg.ActionLayerGraceMs < 0) {
                Logger.Warn("invalid actionLayerGraceMs; using 35");
                cfg.ActionLayerGraceMs = 35;
                shouldSaveConfig = true;
            }
            if (cfg.ActionLayerPostGraceMs < 0) {
                Logger.Warn("invalid actionLayerPostGraceMs; using " + DefaultActionLayerPostGraceMs.ToString(CultureInfo.InvariantCulture));
                cfg.ActionLayerPostGraceMs = DefaultActionLayerPostGraceMs;
                shouldSaveConfig = true;
            }
            if (cfg.ActionLayerSwitchGuardMs < 0) {
                Logger.Warn("invalid actionLayerSwitchGuardMs; using 35");
                cfg.ActionLayerSwitchGuardMs = 35;
                shouldSaveConfig = true;
            }
            if (cfg.ScrollSlowIntervalMs <= 0) {
                Logger.Warn("invalid scrollSlowIntervalMs; using " + DefaultScrollSlowIntervalMs.ToString(CultureInfo.InvariantCulture));
                cfg.ScrollSlowIntervalMs = DefaultScrollSlowIntervalMs;
                shouldSaveConfig = true;
            }
            if (cfg.ScrollFastIntervalMs <= 0) {
                Logger.Warn("invalid scrollFastIntervalMs; using " + DefaultScrollFastIntervalMs.ToString(CultureInfo.InvariantCulture));
                cfg.ScrollFastIntervalMs = DefaultScrollFastIntervalMs;
                shouldSaveConfig = true;
            }
            if (cfg.ScrollFastIntervalMs > cfg.ScrollSlowIntervalMs) {
                Logger.Warn("scrollFastIntervalMs is slower than scrollSlowIntervalMs; clamping to slow interval");
                cfg.ScrollFastIntervalMs = cfg.ScrollSlowIntervalMs;
                shouldSaveConfig = true;
            }
            if (cfg.R3FreezeMs < 0) {
                Logger.Warn("invalid r3FreezeMs; using 60");
                cfg.R3FreezeMs = 60;
                shouldSaveConfig = true;
            }

            if (shouldSaveConfig) cfg.Save(path);
        } catch (Exception ex) {
            Logger.Error("config load failed: " + ex.Message);
        }

        return cfg;
    }

    public void Save(string path) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        Write(sb, "configVersion", ConfigVersion, true);
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
        Write(sb, "actionLayerPostGraceMs", ActionLayerPostGraceMs, true);
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

    private static bool GetBool(JsonObject json, string key, bool fallback) {
        bool value;
        return json.TryGetBool(key, out value) ? value : fallback;
    }

    private static int GetInt(JsonObject json, string key, int fallback) {
        int value;
        return json.TryGetInt(key, out value) ? value : fallback;
    }

    private static double GetDouble(JsonObject json, string key, double fallback) {
        double value;
        return json.TryGetDouble(key, out value) ? value : fallback;
    }

    private static bool TryGetDouble(JsonObject json, string key, out double value) {
        return json.TryGetDouble(key, out value);
    }

    private static string GetString(JsonObject json, string key, string fallback) {
        string value;
        return json.TryGetString(key, out value) ? value : fallback;
    }

    private static bool IsInvalidPositive(double value) {
        return value <= 0.0 || Double.IsNaN(value) || Double.IsInfinity(value);
    }

    private static void MigrateDefaultsToCurrent(Config cfg) {
        if (Math.Abs(cfg.MouseScrollCurveExponent - 3.5) < 0.000001) {
            cfg.MouseScrollCurveExponent = DefaultMouseScrollCurveExponent;
        }
        if (cfg.ScrollSlowIntervalMs == 160 || cfg.ScrollSlowIntervalMs == 120) {
            cfg.ScrollSlowIntervalMs = DefaultScrollSlowIntervalMs;
        }
        if (cfg.ScrollFastIntervalMs == 12) {
            cfg.ScrollFastIntervalMs = DefaultScrollFastIntervalMs;
        }
        if (cfg.LayerTakeoverWindowMs == 25) {
            cfg.LayerTakeoverWindowMs = DefaultLayerTakeoverWindowMs;
        }
        if (cfg.ComboLayerWindowMs == 35) {
            cfg.ComboLayerWindowMs = DefaultComboLayerWindowMs;
        }
        if (cfg.ComboLayerWindowMs == 30) {
            cfg.ComboLayerWindowMs = DefaultComboLayerWindowMs;
        }
        if (Math.Abs(cfg.TriggerPressThreshold - 0.1) < 0.000001 &&
            Math.Abs(cfg.TriggerReleaseThreshold - 0.05) < 0.000001) {
            cfg.TriggerPressThreshold = DefaultTriggerPressThreshold;
            cfg.TriggerReleaseThreshold = DefaultTriggerReleaseThreshold;
        }
        if (cfg.RepeatIntervalMs == 20) {
            cfg.RepeatIntervalMs = DefaultRepeatIntervalMs;
        }
        if (cfg.BaseRepeatSlowIntervalMs == 220) {
            cfg.BaseRepeatSlowIntervalMs = DefaultBaseRepeatSlowIntervalMs;
        }
        if (cfg.BaseRepeatRampMs == 1200) {
            cfg.BaseRepeatRampMs = DefaultBaseRepeatRampMs;
        }
        if (Math.Abs(cfg.RightStickDeadzone - 0.025) < 0.000001 ||
            Math.Abs(cfg.RightStickDeadzone - 0.055) < 0.000001) {
            cfg.RightStickDeadzone = DefaultRightStickDeadzone;
        }
        Logger.Info("migrated config defaults to version " + CurrentConfigVersion.ToString(CultureInfo.InvariantCulture));
    }

    private static bool HasRemovedLegacyKeys(JsonObject json) {
        return json.ContainsKey("mouseDeadzone") ||
               json.ContainsKey("touchpadSwipeThreshold") ||
               json.ContainsKey("touchpadMaxSwipeMs") ||
               json.ContainsKey("rightStickEpsilon");
    }

    private static bool HasMissingCurrentKeys(JsonObject json) {
        string[] keys = new string[] {
            "configVersion",
            "enabled",
            "mouseSensitivity",
            "mouseMaxSpeed",
            "rightStickDeadzone",
            "rightStickCurve",
            "rightStickCurveExponent",
            "mouseScrollCurveExponent",
            "leftStickEnterDeadzone",
            "leftStickExitDeadzone",
            "triggerPressThreshold",
            "triggerReleaseThreshold",
            "repeatDelayMs",
            "repeatIntervalMs",
            "baseRepeatSlowIntervalMs",
            "baseRepeatRampMs",
            "actionLayerGraceMs",
            "actionLayerPostGraceMs",
            "layerTakeoverWindowMs",
            "actionLayerSwitchGuardMs",
            "comboLayerWindowMs",
            "useScanCode",
            "useInterception",
            "scrollSlowIntervalMs",
            "scrollFastIntervalMs",
            "r3FreezeMs",
            "clutchLongPressMs"
        };
        for (int i = 0; i < keys.Length; i++) {
            if (!json.ContainsKey(keys[i])) return true;
        }
        return false;
    }

    private sealed class JsonObject {
        private readonly Dictionary<string, JsonValue> _values = new Dictionary<string, JsonValue>(StringComparer.Ordinal);

        public bool ContainsKey(string key) {
            return _values.ContainsKey(key);
        }

        public bool TryGetBool(string key, out bool value) {
            JsonValue item;
            if (_values.TryGetValue(key, out item) && item.Kind == JsonValueKind.Bool) {
                value = item.BoolValue;
                return true;
            }
            value = false;
            return false;
        }

        public bool TryGetInt(string key, out int value) {
            JsonValue item;
            if (_values.TryGetValue(key, out item) && item.Kind == JsonValueKind.Number) {
                double rounded = Math.Round(item.NumberValue);
                if (Math.Abs(item.NumberValue - rounded) < 0.000001 &&
                    rounded >= Int32.MinValue &&
                    rounded <= Int32.MaxValue) {
                    value = (int)rounded;
                    return true;
                }
            }
            value = 0;
            return false;
        }

        public bool TryGetDouble(string key, out double value) {
            JsonValue item;
            if (_values.TryGetValue(key, out item) && item.Kind == JsonValueKind.Number) {
                value = item.NumberValue;
                return true;
            }
            value = 0.0;
            return false;
        }

        public bool TryGetString(string key, out string value) {
            JsonValue item;
            if (_values.TryGetValue(key, out item) && item.Kind == JsonValueKind.String) {
                value = item.StringValue;
                return true;
            }
            value = null;
            return false;
        }

        public static JsonObject Parse(string text) {
            JsonObject obj = new JsonObject();
            int index = 0;
            SkipWhite(text, ref index);
            Expect(text, ref index, '{');
            SkipWhite(text, ref index);
            if (TryConsume(text, ref index, '}')) return obj;

            while (index < text.Length) {
                SkipWhite(text, ref index);
                string key = ParseString(text, ref index);
                SkipWhite(text, ref index);
                Expect(text, ref index, ':');
                SkipWhite(text, ref index);
                obj._values[key] = ParseValue(text, ref index);
                SkipWhite(text, ref index);
                if (TryConsume(text, ref index, '}')) return obj;
                Expect(text, ref index, ',');
            }

            throw new FormatException("unterminated JSON object");
        }

        private static JsonValue ParseValue(string text, ref int index) {
            if (index >= text.Length) throw new FormatException("unexpected end of JSON");
            char c = text[index];
            if (c == '"') return JsonValue.ForString(ParseString(text, ref index));
            if (c == 't') {
                ExpectLiteral(text, ref index, "true");
                return JsonValue.ForBool(true);
            }
            if (c == 'f') {
                ExpectLiteral(text, ref index, "false");
                return JsonValue.ForBool(false);
            }
            if (c == 'n') {
                ExpectLiteral(text, ref index, "null");
                return JsonValue.ForNull();
            }
            if (c == '{' || c == '[') {
                SkipComposite(text, ref index);
                return JsonValue.ForNull();
            }
            return JsonValue.ForNumber(ParseNumber(text, ref index));
        }

        private static void SkipComposite(string text, ref int index) {
            char open = text[index];
            char close = open == '{' ? '}' : ']';
            int depth = 0;
            while (index < text.Length) {
                char c = text[index];
                if (c == '"') {
                    ParseString(text, ref index);
                    continue;
                }
                if (c == open) depth++;
                if (c == close) {
                    depth--;
                    index++;
                    if (depth == 0) return;
                    continue;
                }
                index++;
            }
            throw new FormatException("unterminated JSON composite value");
        }

        private static double ParseNumber(string text, ref int index) {
            int start = index;
            if (index < text.Length && text[index] == '-') index++;
            while (index < text.Length && Char.IsDigit(text[index])) index++;
            if (index < text.Length && text[index] == '.') {
                index++;
                while (index < text.Length && Char.IsDigit(text[index])) index++;
            }
            if (index < text.Length && (text[index] == 'e' || text[index] == 'E')) {
                index++;
                if (index < text.Length && (text[index] == '+' || text[index] == '-')) index++;
                while (index < text.Length && Char.IsDigit(text[index])) index++;
            }
            if (index == start) throw new FormatException("expected JSON value");

            double value;
            string raw = text.Substring(start, index - start);
            if (!Double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) {
                throw new FormatException("invalid JSON number '" + raw + "'");
            }
            return value;
        }

        private static string ParseString(string text, ref int index) {
            Expect(text, ref index, '"');
            StringBuilder sb = new StringBuilder();
            while (index < text.Length) {
                char c = text[index++];
                if (c == '"') return sb.ToString();
                if (c != '\\') {
                    sb.Append(c);
                    continue;
                }
                if (index >= text.Length) throw new FormatException("unterminated JSON escape");
                char esc = text[index++];
                switch (esc) {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (index + 4 > text.Length) throw new FormatException("invalid JSON unicode escape");
                        string hex = text.Substring(index, 4);
                        int code;
                        if (!Int32.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code)) {
                            throw new FormatException("invalid JSON unicode escape");
                        }
                        sb.Append((char)code);
                        index += 4;
                        break;
                    default:
                        throw new FormatException("invalid JSON escape '\\" + esc + "'");
                }
            }
            throw new FormatException("unterminated JSON string");
        }

        private static void SkipWhite(string text, ref int index) {
            while (index < text.Length && Char.IsWhiteSpace(text[index])) index++;
        }

        private static bool TryConsume(string text, ref int index, char expected) {
            if (index < text.Length && text[index] == expected) {
                index++;
                return true;
            }
            return false;
        }

        private static void Expect(string text, ref int index, char expected) {
            if (!TryConsume(text, ref index, expected)) {
                throw new FormatException("expected '" + expected + "'");
            }
        }

        private static void ExpectLiteral(string text, ref int index, string expected) {
            if (index + expected.Length > text.Length || String.CompareOrdinal(text, index, expected, 0, expected.Length) != 0) {
                throw new FormatException("expected '" + expected + "'");
            }
            index += expected.Length;
        }
    }

    private enum JsonValueKind {
        Null,
        Bool,
        Number,
        String
    }

    private struct JsonValue {
        public JsonValueKind Kind;
        public bool BoolValue;
        public double NumberValue;
        public string StringValue;

        public static JsonValue ForNull() {
            JsonValue value = new JsonValue();
            value.Kind = JsonValueKind.Null;
            return value;
        }

        public static JsonValue ForBool(bool data) {
            JsonValue value = new JsonValue();
            value.Kind = JsonValueKind.Bool;
            value.BoolValue = data;
            return value;
        }

        public static JsonValue ForNumber(double data) {
            JsonValue value = new JsonValue();
            value.Kind = JsonValueKind.Number;
            value.NumberValue = data;
            return value;
        }

        public static JsonValue ForString(string data) {
            JsonValue value = new JsonValue();
            value.Kind = JsonValueKind.String;
            value.StringValue = data;
            return value;
        }
    }
}
