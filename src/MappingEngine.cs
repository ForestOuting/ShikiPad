using System;
using System.Globalization;

internal sealed class MappingEngine {
    private readonly KeyStroke[][] _tables;

    public MappingEngine() {
        _tables = new KeyStroke[(int)Layer.Reserved][];
        _tables[(int)Layer.Base] = Row(PhysicalKey.ArrowUp, PhysicalKey.ArrowRight, PhysicalKey.Tab, PhysicalKey.Escape, PhysicalKey.ArrowLeft, PhysicalKey.ArrowDown, PhysicalKey.Space, PhysicalKey.Enter);
        _tables[(int)Layer.R1] = Row(PhysicalKey.O, PhysicalKey.P, PhysicalKey.J, PhysicalKey.I, PhysicalKey.N, PhysicalKey.M, PhysicalKey.K, PhysicalKey.L);
        _tables[(int)Layer.L1] = Row(PhysicalKey.W, PhysicalKey.D, PhysicalKey.Q, PhysicalKey.E, PhysicalKey.A, PhysicalKey.S, PhysicalKey.Z, PhysicalKey.X);
        _tables[(int)Layer.R2] = Row(PhysicalKey.Num0, PhysicalKey.G, PhysicalKey.Y, PhysicalKey.U, PhysicalKey.Minus, PhysicalKey.Equals, PhysicalKey.B, PhysicalKey.H);
        _tables[(int)Layer.L2] = Row(PhysicalKey.R, PhysicalKey.F, PhysicalKey.T, PhysicalKey.Num1, PhysicalKey.C, PhysicalKey.V, PhysicalKey.Num3, PhysicalKey.Num2);
        _tables[(int)Layer.R1L1] = Row(PhysicalKey.Num4, PhysicalKey.Comma, PhysicalKey.Period, PhysicalKey.Num7, PhysicalKey.Num5, PhysicalKey.Num6, PhysicalKey.Num9, PhysicalKey.Num8);
        _tables[(int)Layer.R2L2] = new KeyStroke[] {
            KeyStroke.Shifted(PhysicalKey.Equals),
            KeyStroke.Of(PhysicalKey.Slash),
            KeyStroke.Shifted(PhysicalKey.Num7),
            KeyStroke.Shifted(PhysicalKey.Num8),
            KeyStroke.Shifted(PhysicalKey.Minus),
            KeyStroke.Shifted(PhysicalKey.Num6),
            KeyStroke.Shifted(PhysicalKey.Num4),
            KeyStroke.Shifted(PhysicalKey.Num5)
        };
        _tables[(int)Layer.L1R2] = new KeyStroke[] {
            KeyStroke.Of(PhysicalKey.LeftBracket),
            KeyStroke.Of(PhysicalKey.RightBracket),
            KeyStroke.Shifted(PhysicalKey.Num1),
            KeyStroke.Shifted(PhysicalKey.Slash),
            KeyStroke.Shifted(PhysicalKey.LeftBracket),
            KeyStroke.Shifted(PhysicalKey.RightBracket),
            KeyStroke.Shifted(PhysicalKey.Num2),
            KeyStroke.Shifted(PhysicalKey.Num3)
        };
        _tables[(int)Layer.R1L2] = new KeyStroke[] {
            KeyStroke.Shifted(PhysicalKey.Num9),
            KeyStroke.Shifted(PhysicalKey.Num0),
            KeyStroke.Of(PhysicalKey.Semicolon),
            KeyStroke.Of(PhysicalKey.Apostrophe),
            KeyStroke.Shifted(PhysicalKey.Comma),
            KeyStroke.Shifted(PhysicalKey.Period),
            KeyStroke.Of(PhysicalKey.Grave),
            KeyStroke.Of(PhysicalKey.Backslash)
        };
    }

    public Layer Resolve(bool l1, bool r1, bool l2, bool r2, double l1Ms, double r1Ms, double l2Ms, double r2Ms, double comboLayerWindowMs) {
        if (!l1 && !r1 && !l2 && !r2) return Layer.Base;

        double comboWindow = Math.Max(0.0, comboLayerWindowMs);

        Layer latestLayer = Layer.Reserved;
        Layer previousLayer = Layer.Reserved;
        double latestMs = double.NegativeInfinity;
        double previousMs = double.NegativeInfinity;
        int latestOrder = 0;
        int previousOrder = 0;

        ConsiderRecentLayer(l1, Layer.L1, l1Ms, 3, ref latestLayer, ref latestMs, ref latestOrder, ref previousLayer, ref previousMs, ref previousOrder);
        ConsiderRecentLayer(r1, Layer.R1, r1Ms, 4, ref latestLayer, ref latestMs, ref latestOrder, ref previousLayer, ref previousMs, ref previousOrder);
        ConsiderRecentLayer(l2, Layer.L2, l2Ms, 1, ref latestLayer, ref latestMs, ref latestOrder, ref previousLayer, ref previousMs, ref previousOrder);
        ConsiderRecentLayer(r2, Layer.R2, r2Ms, 2, ref latestLayer, ref latestMs, ref latestOrder, ref previousLayer, ref previousMs, ref previousOrder);

        if (latestLayer != Layer.Reserved && previousLayer != Layer.Reserved && latestMs - previousMs <= comboWindow) {
            Layer comboLayer = ComboFor(previousLayer, latestLayer);
            if (comboLayer != Layer.Reserved) return comboLayer;
        }

        return latestLayer;
    }

    private static Layer ComboFor(Layer a, Layer b) {
        if ((a == Layer.R1 && b == Layer.L1) || (a == Layer.L1 && b == Layer.R1)) return Layer.R1L1;
        if ((a == Layer.R2 && b == Layer.L2) || (a == Layer.L2 && b == Layer.R2)) return Layer.R2L2;
        if ((a == Layer.L1 && b == Layer.R2) || (a == Layer.R2 && b == Layer.L1)) return Layer.L1R2;
        if ((a == Layer.R1 && b == Layer.L2) || (a == Layer.L2 && b == Layer.R1)) return Layer.R1L2;
        return Layer.Reserved;
    }

    private static void ConsiderRecentLayer(bool active, Layer candidate, double timestampMs, int order, ref Layer latestLayer, ref double latestMs, ref int latestOrder, ref Layer previousLayer, ref double previousMs, ref int previousOrder) {
        if (!active) return;
        if (timestampMs > latestMs || (timestampMs == latestMs && order >= latestOrder)) {
            previousLayer = latestLayer;
            previousMs = latestMs;
            previousOrder = latestOrder;
            latestLayer = candidate;
            latestMs = timestampMs;
            latestOrder = order;
        } else if (timestampMs > previousMs || (timestampMs == previousMs && order >= previousOrder)) {
            previousLayer = candidate;
            previousMs = timestampMs;
            previousOrder = order;
        }
    }

    public KeyStroke Lookup(Layer layer, ActionButton action) {
        if (layer == Layer.Reserved) return KeyStroke.None;
        int li = (int)layer;
        int ai = (int)action;
        if (li < 0 || li >= _tables.Length || ai < 0 || ai >= 8) return KeyStroke.None;
        return _tables[li][ai];
    }

    private static KeyStroke[] Row(params PhysicalKey[] keys) {
        KeyStroke[] row = new KeyStroke[keys.Length];
        for (int i = 0; i < keys.Length; i++) row[i] = KeyStroke.Of(keys[i]);
        return row;
    }

    public static string KeyName(PhysicalKey key) {
        if (key >= PhysicalKey.Num0 && key <= PhysicalKey.Num9) return ((int)(key - PhysicalKey.Num0)).ToString(CultureInfo.InvariantCulture);
        return key.ToString();
    }

    public static string KeyName(KeyStroke stroke) {
        if (stroke.IsNone) return "None";
        if (!stroke.Shift) return KeyName(stroke.Key);
        return ShiftedKeyName(stroke.Key);
    }

    private static string ShiftedKeyName(PhysicalKey key) {
        switch (key) {
            case PhysicalKey.Num1: return "!";
            case PhysicalKey.Num2: return "@";
            case PhysicalKey.Num3: return "#";
            case PhysicalKey.Num4: return "$";
            case PhysicalKey.Num5: return "%";
            case PhysicalKey.Num6: return "^";
            case PhysicalKey.Num7: return "&";
            case PhysicalKey.Num8: return "*";
            case PhysicalKey.Num9: return "(";
            case PhysicalKey.Num0: return ")";
            case PhysicalKey.Minus: return "_";
            case PhysicalKey.Equals: return "+";
            case PhysicalKey.LeftBracket: return "{";
            case PhysicalKey.RightBracket: return "}";
            case PhysicalKey.Backslash: return "|";
            case PhysicalKey.Semicolon: return ":";
            case PhysicalKey.Apostrophe: return "\"";
            case PhysicalKey.Comma: return "<";
            case PhysicalKey.Period: return ">";
            case PhysicalKey.Slash: return "?";
            case PhysicalKey.Grave: return "~";
            default: return "Shift+" + KeyName(key);
        }
    }

}
