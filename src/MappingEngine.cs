using System;
using System.Globalization;

internal sealed class MappingEngine {
    private readonly KeyStroke[][] _tables;

    public MappingEngine() {
        _tables = new KeyStroke[(int)Layer.Reserved][];
        _tables[(int)Layer.Base] = Row(PhysicalKey.ArrowUp, PhysicalKey.ArrowRight, PhysicalKey.Space, PhysicalKey.Backspace, PhysicalKey.ArrowLeft, PhysicalKey.ArrowDown, PhysicalKey.Enter, PhysicalKey.Tab);
        _tables[(int)Layer.R1] = Row(PhysicalKey.I, PhysicalKey.N, PhysicalKey.E, PhysicalKey.L, PhysicalKey.O, PhysicalKey.T, PhysicalKey.H, PhysicalKey.U);
        _tables[(int)Layer.L1] = Row(PhysicalKey.W, PhysicalKey.D, PhysicalKey.C, PhysicalKey.R, PhysicalKey.A, PhysicalKey.S, PhysicalKey.Y, PhysicalKey.Z);
        _tables[(int)Layer.R2] = Row(PhysicalKey.M, PhysicalKey.G, PhysicalKey.J, PhysicalKey.X, PhysicalKey.Q, PhysicalKey.F, PhysicalKey.P, PhysicalKey.B);
        _tables[(int)Layer.L2] = Row(PhysicalKey.K, PhysicalKey.V, PhysicalKey.Num1, PhysicalKey.Num2, PhysicalKey.Num3, PhysicalKey.Num4, PhysicalKey.Num5, PhysicalKey.Num6);
        _tables[(int)Layer.R1L1] = Row(PhysicalKey.Num7, PhysicalKey.Num8, PhysicalKey.Num9, PhysicalKey.Num0, PhysicalKey.Minus, PhysicalKey.Equals, PhysicalKey.Comma, PhysicalKey.Period);
        _tables[(int)Layer.R2L2] = new KeyStroke[] {
            KeyStroke.Shifted(PhysicalKey.Num9),
            KeyStroke.Shifted(PhysicalKey.Num0),
            KeyStroke.Shifted(PhysicalKey.Semicolon),
            KeyStroke.Shifted(PhysicalKey.Apostrophe),
            KeyStroke.Of(PhysicalKey.LeftBracket),
            KeyStroke.Of(PhysicalKey.RightBracket),
            KeyStroke.Of(PhysicalKey.Apostrophe),
            KeyStroke.Shifted(PhysicalKey.Minus)
        };
        _tables[(int)Layer.L1R2] = new KeyStroke[] {
            KeyStroke.Shifted(PhysicalKey.LeftBracket),
            KeyStroke.Shifted(PhysicalKey.RightBracket),
            KeyStroke.Shifted(PhysicalKey.Slash),
            KeyStroke.Shifted(PhysicalKey.Num1),
            KeyStroke.Shifted(PhysicalKey.Num3),
            KeyStroke.Shifted(PhysicalKey.Num8),
            KeyStroke.Of(PhysicalKey.Slash),
            KeyStroke.Shifted(PhysicalKey.Num6)
        };
        _tables[(int)Layer.R1L2] = new KeyStroke[] {
            KeyStroke.Of(PhysicalKey.Semicolon),
            KeyStroke.Shifted(PhysicalKey.Num2),
            KeyStroke.Shifted(PhysicalKey.Equals),
            KeyStroke.Shifted(PhysicalKey.Num5),
            KeyStroke.Shifted(PhysicalKey.Num7),
            KeyStroke.Shifted(PhysicalKey.Num4),
            KeyStroke.Of(PhysicalKey.Backslash),
            KeyStroke.Of(PhysicalKey.Grave)
        };
    }

    public Layer Resolve(bool l1, bool r1, bool l2, bool r2, double l1Ms, double r1Ms, double l2Ms, double r2Ms, double comboLayerWindowMs) {
        if (!l1 && !r1 && !l2 && !r2) return Layer.Base;

        Layer layer = Layer.Reserved;
        double bestMs = double.NegativeInfinity;
        int bestRank = 0;
        double comboWindow = Math.Max(0.0, comboLayerWindowMs);

        ConsiderLayer(l1, Layer.L1, l1Ms, 1, ref layer, ref bestMs, ref bestRank);
        ConsiderLayer(r1, Layer.R1, r1Ms, 1, ref layer, ref bestMs, ref bestRank);
        ConsiderLayer(l2, Layer.L2, l2Ms, 1, ref layer, ref bestMs, ref bestRank);
        ConsiderLayer(r2, Layer.R2, r2Ms, 1, ref layer, ref bestMs, ref bestRank);
        ConsiderLayer(IsComboWithinWindow(r1, l1, r1Ms, l1Ms, comboWindow), Layer.R1L1, Math.Max(r1Ms, l1Ms), 2, ref layer, ref bestMs, ref bestRank);
        ConsiderLayer(IsComboWithinWindow(r2, l2, r2Ms, l2Ms, comboWindow), Layer.R2L2, Math.Max(r2Ms, l2Ms), 2, ref layer, ref bestMs, ref bestRank);
        ConsiderLayer(IsComboWithinWindow(l1, r2, l1Ms, r2Ms, comboWindow + 10.0), Layer.L1R2, Math.Max(l1Ms, r2Ms), 2, ref layer, ref bestMs, ref bestRank);
        ConsiderLayer(IsComboWithinWindow(r1, l2, r1Ms, l2Ms, comboWindow + 10.0), Layer.R1L2, Math.Max(r1Ms, l2Ms), 2, ref layer, ref bestMs, ref bestRank);

        return layer;
    }

    private static bool IsComboWithinWindow(bool a, bool b, double aMs, double bMs, double windowMs) {
        return a && b && Math.Abs(aMs - bMs) <= windowMs;
    }

    private static void ConsiderLayer(bool active, Layer candidate, double timestampMs, int rank, ref Layer layer, ref double bestMs, ref int bestRank) {
        if (!active) return;
        if (timestampMs > bestMs || (timestampMs == bestMs && rank >= bestRank)) {
            layer = candidate;
            bestMs = timestampMs;
            bestRank = rank;
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
