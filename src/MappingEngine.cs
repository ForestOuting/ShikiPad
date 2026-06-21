using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

internal sealed class MappingEngine {
    private readonly PhysicalKey[][] _tables;

    public MappingEngine() {
        _tables = new PhysicalKey[7][];
        _tables[(int)Layer.Base] = new PhysicalKey[] { PhysicalKey.ArrowUp, PhysicalKey.ArrowRight, PhysicalKey.Space, PhysicalKey.Backspace, PhysicalKey.ArrowLeft, PhysicalKey.ArrowDown, PhysicalKey.Enter, PhysicalKey.Tab };
        _tables[(int)Layer.R1] = new PhysicalKey[] { PhysicalKey.I, PhysicalKey.N, PhysicalKey.E, PhysicalKey.A, PhysicalKey.O, PhysicalKey.T, PhysicalKey.H, PhysicalKey.U };
        _tables[(int)Layer.L1] = new PhysicalKey[] { PhysicalKey.S, PhysicalKey.R, PhysicalKey.D, PhysicalKey.G, PhysicalKey.L, PhysicalKey.C, PhysicalKey.Y, PhysicalKey.Z };
        _tables[(int)Layer.R2] = new PhysicalKey[] { PhysicalKey.M, PhysicalKey.W, PhysicalKey.J, PhysicalKey.X, PhysicalKey.Q, PhysicalKey.F, PhysicalKey.P, PhysicalKey.B };
        _tables[(int)Layer.L2] = new PhysicalKey[] { PhysicalKey.K, PhysicalKey.V, PhysicalKey.Num1, PhysicalKey.Num2, PhysicalKey.Num3, PhysicalKey.Num4, PhysicalKey.Num5, PhysicalKey.Num6 };
        _tables[(int)Layer.R1L1] = new PhysicalKey[] { PhysicalKey.Num7, PhysicalKey.Num8, PhysicalKey.Num9, PhysicalKey.Num0, PhysicalKey.Minus, PhysicalKey.Equals, PhysicalKey.Comma, PhysicalKey.Period };
        _tables[(int)Layer.R2L2] = new PhysicalKey[] { PhysicalKey.Apostrophe, PhysicalKey.Slash, PhysicalKey.Semicolon, PhysicalKey.LeftBracket, PhysicalKey.RightBracket, PhysicalKey.Backslash, PhysicalKey.Grave, PhysicalKey.None };
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

    public PhysicalKey Lookup(Layer layer, ActionButton action) {
        if (layer == Layer.Reserved) return PhysicalKey.None;
        int li = (int)layer;
        int ai = (int)action;
        if (li < 0 || li >= _tables.Length || ai < 0 || ai >= 8) return PhysicalKey.None;
        return _tables[li][ai];
    }

    public static string KeyName(PhysicalKey key) {
        if (key >= PhysicalKey.Num0 && key <= PhysicalKey.Num9) return ((int)(key - PhysicalKey.Num0)).ToString(CultureInfo.InvariantCulture);
        return key.ToString();
    }

}
