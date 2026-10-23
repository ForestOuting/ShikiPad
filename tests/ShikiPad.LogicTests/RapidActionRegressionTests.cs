using System.Reflection;
using System.Runtime.CompilerServices;

// These tests drive the actual mapper with controller snapshots and a fake output
// sink. They never construct the Form, open HID, or inject keyboard input.
internal static class RapidActionRegressionTests {
    internal static void Run() {
        VerifyEveryPositionRetainsTwoReleasedPresses();
        VerifyThreePressesAndHeldSecondPress();
        VerifyIndependentLayerAndModifierWindows();
        VerifyLanguageSnapshots();
        VerifyCleanupDropsAllPendingPresses();
        VerifyBaseHoldAndRepeat();
        VerifyLayerTimingRules();
        VerifyContinuousLetterSequences();
    }

    // Validate real mapper output for every letter pair and corpus-derived
    // three-letter fragment. Whole words are diagnostic examples, not special
    // mappings. Short same-layer taps exercise the pending-action queues;
    // layer changes exercise both release-first and late-overlap transitions.
    private static void VerifyContinuousLetterSequences() {
        string fixture = Path.Combine(AppContext.BaseDirectory, "TypingSequenceCases.json");
        using var cases = System.Text.Json.JsonDocument.Parse(File.ReadAllText(fixture));
        int sequences = 0, letters = 0;
        foreach (TextInputLanguage language in Enum.GetValues<TextInputLanguage>()) {
            var positions = new Dictionary<char, (Layer layer, ActionButton button)>();
            var mapping = new MappingEngine();
            for (Layer layer = Layer.L1; layer <= Layer.R2; layer++) {
                foreach (ActionButton button in Enum.GetValues<ActionButton>()) {
                    KeyStroke stroke = mapping.Lookup(layer, button, language);
                    if (stroke.Key >= PhysicalKey.A && stroke.Key <= PhysicalKey.Z)
                        positions[(char)('a' + (int)stroke.Key - (int)PhysicalKey.A)] = (layer, button);
                }
            }
            Check(positions.Count == 26, "continuous typing locates every letter");
            for (char a = 'a'; a <= 'z'; a++) for (char b = 'a'; b <= 'z'; b++) {
                string pair = new(new[] { a, b });
                CheckSequence(pair, language, positions, false);
                CheckSequence(pair, language, positions, true);
                sequences += 2;
                letters += 4;
            }
            var group = cases.RootElement.GetProperty(language == TextInputLanguage.Chinese ? "zh" : "en");
            foreach (string kind in new[] { "trigrams", "syllables", "examples", "technical" }) {
                foreach (var value in group.GetProperty(kind).EnumerateArray()) {
                    string text = value.GetString();
                    CheckSequence(text, language, positions, true);
                    sequences++;
                    letters += text.Length;
                }
            }
        }
        Console.WriteLine($"Continuous typing: {sequences} sequences / {letters} letters passed through the actual mapper with fake output.");
    }

    private static void CheckSequence(string text, TextInputLanguage language,
        Dictionary<char, (Layer layer, ActionButton button)> positions, bool overlap) {
        var h = new Harness(language);
        var events = new List<(double time, Action<ControllerState> change)>();
        var expected = new List<TimedEvent>();
        double down = 100;
        Layer previous = Layer.Base;
        for (int index = 0; index < text.Length; index++) {
            char c = text[index];
            var (layer, button) = positions[c];
            if (previous == Layer.Base) {
                events.Add((20, s => SetLayer(s, layer)));
            } else if (previous != layer) {
                Layer before = previous;
                if (overlap) {
                    events.Add((down - 12, s => {
                        ControllerState old = State(before), next = State(layer);
                        s.L1 = old.L1 || next.L1; s.R1 = old.R1 || next.R1;
                        s.L2 = Math.Max(old.L2, next.L2); s.R2 = Math.Max(old.R2, next.R2);
                    }));
                    events.Add((down - 6, s => SetLayer(s, layer)));
                } else {
                    events.Add((down - 14, s => SetLayer(s, Layer.Base)));
                    events.Add((down - 12, s => SetLayer(s, layer)));
                }
            }
            events.Add((down, s => typeof(ControllerState).GetField(button.ToString()).SetValue(s, true)));
            events.Add((down + 8, s => typeof(ControllerState).GetField(button.ToString()).SetValue(s, false)));
            events.Add((down + 45, null));
            PhysicalKey key = (PhysicalKey)((int)PhysicalKey.A + c - 'a');
            expected.AddRange(ExpectedTap(KeyStroke.Of(key)).Select(e => new TimedEvent(down + 45, e.Key, e.Up)));
            previous = layer;
            // The following iteration chooses its interval from the actual next
            // letter, so a stable layer permits a 20 ms burst without re-timing.
            down += 20;
            if (index + 1 < text.Length && positions[text[index + 1]].layer != previous) down += 40;
        }
        var state = new ControllerState();
        foreach (var e in events.OrderBy(e => e.time)) {
            e.change?.Invoke(state);
            h.Step(e.time, state);
        }
        h.Step(down + 100, state);
        Check(h.Events.SequenceEqual(expected) && h.Held.Count == 0,
            $"continuous sequence {language}/{text}, overlap={overlap}: every letter in order, once, at its own 45 ms deadline");

        static void SetLayer(ControllerState s, Layer layer) {
            ControllerState next = State(layer);
            s.L1 = next.L1; s.R1 = next.R1; s.L2 = next.L2; s.R2 = next.R2;
        }
    }

    private static void VerifyEveryPositionRetainsTwoReleasedPresses() {
        foreach (TextInputLanguage language in Enum.GetValues<TextInputLanguage>()) {
            for (Layer layer = Layer.Base; layer < Layer.Reserved; layer++) {
                foreach (ActionButton button in Enum.GetValues<ActionButton>()) {
                    var h = new Harness(language);
                    h.Step(20, State(layer));
                    h.Step(100, State(layer, button));
                    h.Step(110, State(layer));
                    h.Step(120, State(layer, button));
                    h.Step(130, State(layer));
                    h.Step(144.99, State(layer));
                    Check(h.Events.Count == 0, "neither quick press outputs before its own decision window");
                    h.Step(145, State(layer));
                    h.AssertTap(145, layer, button, language);
                    h.Step(164.99, State(layer));
                    Check(!h.Events.Any(e => e.Time > 145), "second released press keeps its full 45 ms window");
                    h.Step(165, State(layer));
                    h.AssertTap(165, layer, button, language);
                    h.Step(300, State(layer));
                    Check(!h.Events.Any(e => e.Time > 165) && h.Held.Count == 0,
                        "two released presses give exactly two complete outputs: " + language + " " + layer + " " + button);
                }
            }
        }
    }

    private static void VerifyThreePressesAndHeldSecondPress() {
        var three = new Harness();
        three.Step(20, State(Layer.R1));
        foreach (double down in new[] { 100.0, 112.0, 124.0 }) {
            three.Step(down, State(Layer.R1, ActionButton.Down));
            three.Step(down + 5, State(Layer.R1));
        }
        foreach (double deadline in new[] { 145.0, 157.0, 169.0 }) {
            three.Step(deadline, State(Layer.R1));
            three.AssertTap(deadline, Layer.R1, ActionButton.Down);
        }
        Check(three.Events.Count == ExpectedTap(three.Stroke(Layer.R1, ActionButton.Down)).Length * 3,
            "three quick taps are neither merged nor duplicated");

        var held = new Harness();
        held.Step(20, State(Layer.R1));
        held.Step(100, State(Layer.R1, ActionButton.Down));
        held.Step(110, State(Layer.R1));
        held.Step(120, State(Layer.R1, ActionButton.Down));
        held.Step(145, State(Layer.R1, ActionButton.Down));
        held.AssertTap(145, Layer.R1, ActionButton.Down);
        held.Step(146, State(Layer.R1, ActionButton.Down));
        held.Step(164.99, State(Layer.R1, ActionButton.Down));
        Check(!held.Events.Any(e => e.Time > 145), "held second press cannot take the old layer-change fallback at 146 ms");
        held.Step(165, State(Layer.R1, ActionButton.Down));
        held.AssertTap(165, Layer.R1, ActionButton.Down);
        held.Step(500, State(Layer.R1, ActionButton.Down));
        held.Step(510, State(Layer.R1));
        Check(!held.Events.Any(e => e.Time > 165) && held.Held.Count == 0, "character holds stay suppressed until release");
    }

    private static void VerifyIndependentLayerAndModifierWindows() {
        // The new layer is too late for press 1, but exactly at press 2's 40 ms boundary.
        var layers = new Harness();
        layers.Step(100, State(Layer.Base, ActionButton.Down));
        layers.Step(110, State(Layer.Base));
        layers.Step(120, State(Layer.Base, ActionButton.Down));
        layers.Step(130, State(Layer.Base));
        layers.Step(145, State(Layer.Base));
        layers.AssertTap(145, Layer.Base, ActionButton.Down);
        layers.Step(160, State(Layer.R1));
        layers.Step(165, State(Layer.R1));
        layers.Step(195, State(Layer.R1));
        Check(!layers.Events.Any(e => e.Time > 145), "second press waits through the new layer's inclusive 35 ms combo window");
        layers.Step(195.01, State(Layer.R1));
        layers.AssertTap(195.01, Layer.R1, ActionButton.Down);

        foreach (double offset in new[] { 45.0, 45.01 }) {
            var modifiers = new Harness();
            modifiers.Step(20, State(Layer.R1));
            modifiers.Step(100, State(Layer.R1, ActionButton.Down));
            modifiers.Step(110, State(Layer.R1));
            modifiers.Step(120, State(Layer.R1, ActionButton.Down));
            modifiers.Step(130, State(Layer.R1));
            modifiers.Step(145, State(Layer.R1));
            modifiers.AssertTap(145, Layer.R1, ActionButton.Down);
            modifiers.Modifier(120 + offset, PhysicalKey.LCtrl);
            modifiers.Step(120 + offset, State(Layer.R1));
            modifiers.AssertTap(120 + offset, Layer.R1, ActionButton.Down, ctrl: offset == 45);
            Check(modifiers.Held.Count == 0, "second press owns its modifier boundary and releases the chord");
        }

        var queuedModifier = new Harness();
        queuedModifier.Step(20, State(Layer.R1));
        queuedModifier.Step(100, State(Layer.R1, ActionButton.Down));
        queuedModifier.Step(110, State(Layer.R1));
        queuedModifier.Step(120, State(Layer.R1, ActionButton.Down));
        queuedModifier.Step(130, State(Layer.R1));
        queuedModifier.Modifier(140, PhysicalKey.LCtrl);
        queuedModifier.Step(145, State(Layer.R1));
        queuedModifier.AssertTap(145, Layer.R1, ActionButton.Down, ctrl: true);
        queuedModifier.Step(165, State(Layer.R1));
        queuedModifier.AssertTap(165, Layer.R1, ActionButton.Down, ctrl: true);
    }

    private static void VerifyLanguageSnapshots() {
        var h = new Harness();
        h.Step(20, State(Layer.R1));
        h.Step(100, State(Layer.R1, ActionButton.Down));
        h.Step(110, State(Layer.R1));
        h.ToggleLanguage();
        h.Step(120, State(Layer.R1, ActionButton.Down));
        h.Step(130, State(Layer.R1));
        Check((TextInputLanguage)h.HoldField(ActionButton.Down, "PendingLanguage") == TextInputLanguage.English,
            "a new press snapshots the selected language");
        h.Step(145, State(Layer.R1));
        h.AssertTap(145, Layer.R1, ActionButton.Down, TextInputLanguage.Chinese);
        h.ToggleLanguage();
        h.Step(165, State(Layer.R1));
        h.AssertTap(165, Layer.R1, ActionButton.Down, TextInputLanguage.English);

        // Changing language while a character button remains held must not emit again.
        h.Step(200, State(Layer.R1, ActionButton.Down));
        h.Step(245, State(Layer.R1, ActionButton.Down));
        h.AssertTap(245, Layer.R1, ActionButton.Down, TextInputLanguage.Chinese);
        int count = h.Events.Count;
        h.ToggleLanguage();
        h.Step(300, State(Layer.R1, ActionButton.Down));
        h.Step(310, State(Layer.R1));
        Check(h.Events.Count == count, "switching language cannot reinterpret an already-output held action");
        h.Step(320, State(Layer.R1, ActionButton.Down));
        h.Step(330, State(Layer.R1));
        h.Step(365, State(Layer.R1));
        h.AssertTap(365, Layer.R1, ActionButton.Down, TextInputLanguage.English);
    }

    private static void VerifyCleanupDropsAllPendingPresses() {
        var h = new Harness();
        h.Step(20, State(Layer.R1));
        h.Step(100, State(Layer.R1, ActionButton.Down));
        h.Step(110, State(Layer.R1));
        h.Step(120, State(Layer.R1, ActionButton.Down));
        h.Step(130, State(Layer.R1));
        h.Call("ReleaseRuntimeHolds");
        h.Step(500, State(Layer.Base));
        Check(h.Events.Count == 0 && h.Held.Count == 0, "runtime cleanup clears current and queued pending actions without ghost output");
        h.Step(600, State(Layer.Base, ActionButton.Down));
        h.Step(610, State(Layer.Base));
        h.Step(645, State(Layer.Base));
        h.AssertTap(645, Layer.Base, ActionButton.Down);
    }

    private static void VerifyBaseHoldAndRepeat() {
        var h = new Harness();
        h.Step(100, State(Layer.Base, ActionButton.Down));
        h.Step(110, State(Layer.Base));
        h.Step(120, State(Layer.Base, ActionButton.Down));
        h.Step(145, State(Layer.Base, ActionButton.Down));
        h.AssertTap(145, Layer.Base, ActionButton.Down);
        h.Step(165, State(Layer.Base, ActionButton.Down));
        Check(h.Events.Count(e => e.Time == 165) == 1 && h.Held.Count == 1, "second Base press becomes an ordinary held key after its own deadline");
        h.ToggleLanguage();
        h.Step(464.99, State(Layer.Base, ActionButton.Down));
        Check(!h.Events.Any(e => e.Time > 165), "Base repeat delay starts at the actual second press output");
        h.Step(465, State(Layer.Base, ActionButton.Down));
        Check(h.Events.Count(e => e.Time == 465 && !e.Up) == 1, "Base directional repeat still starts after 300 ms");
        h.Step(470, State(Layer.Base));
        Check(h.Events.Count(e => e.Time == 470 && e.Up) == 1 && h.Held.Count == 0, "Base release balances the held output");

        var bound = new Harness();
        bound.Step(100, State(Layer.Base, ActionButton.Down));
        bound.Modifier(120, PhysicalKey.LCtrl);
        bound.Step(145, State(Layer.Base, ActionButton.Down));
        bound.AssertTap(145, Layer.Base, ActionButton.Down, ctrl: true);
        bound.Step(445, State(Layer.Base, ActionButton.Down));
        bound.AssertTap(445, Layer.Base, ActionButton.Down, ctrl: true);
        bound.Step(450, State(Layer.Base));
        Check(bound.Held.Count == 0 && !bound.Events.Any(e => e.Time == 450), "modifier-bound Base repeat remains complete chord pulses");
    }

    private static void VerifyLayerTimingRules() {
        var invalidCombo = new Harness();
        invalidCombo.Step(100, State(Layer.R1));
        ControllerState r1r2 = new() { R1 = true, R2 = 1, Down = true };
        invalidCombo.Step(200, r1r2);
        invalidCombo.Step(244.99, r1r2);
        Check(invalidCombo.Events.Count == 0, "R2 has no extra output before the action's 45 ms window");
        invalidCombo.Step(245, r1r2);
        invalidCombo.AssertTap(245, Layer.R2, ActionButton.Down);
        invalidCombo.Step(400, r1r2);
        Check((Layer)invalidCombo.Field("_previousActionLayer") == Layer.R2 && !invalidCombo.Events.Any(e => e.Time > 245),
            "held R1 plus later R2 never matures into a combo");

        foreach (double arrival in new[] { 40.0, 40.01, 41.0 }) {
            var combo = new Harness();
            combo.Step(100, State(Layer.Base, ActionButton.Down));
            combo.Step(120, State(Layer.R1, ActionButton.Down));
            combo.Step(135, State(Layer.R1, ActionButton.Down));
            combo.Step(100 + arrival, State(Layer.R1L1, ActionButton.Down));
            combo.Step(145, State(Layer.R1L1, ActionButton.Down));
            if (arrival <= 40) {
                combo.AssertTap(145, Layer.R1L1, ActionButton.Down);
            } else {
                Check(combo.Events.Count == 1 && combo.Held.Count == 1 &&
                    combo.Events[0].Key == Identity(PhysicalKey.ArrowDown),
                    "a legal combo formed after the 40 ms grace restores the original Base layer");
            }
            combo.Step(150, State(Layer.R1L1));
            Check(combo.Held.Count == 0, "combo boundary and late-combo cancellation release normally");
        }

        foreach (double arrival in new[] { 39.99, 40.0, 40.01 }) {
            var h = new Harness();
            h.Step(100, State(Layer.Base, ActionButton.Down));
            h.Step(108, State(Layer.Base));
            h.Step(100 + arrival, State(Layer.R2));
            h.Step(145, State(Layer.R2));
            if (arrival <= 40) {
                Check(h.Events.Count == 0, "a newly acquired single layer waits beyond the ordinary 45 ms decision");
                h.Step(135 + arrival, State(Layer.R2));
                Check(h.Events.Count == 0, "the single-layer settle wait includes exactly 35 ms");
                h.Step(135.01 + arrival, State(Layer.R2));
                h.AssertTap(135.01 + arrival, Layer.R2, ActionButton.Down);
            } else {
                h.AssertTap(145, Layer.Base, ActionButton.Down);
                h.Step(180, State(Layer.R2));
                Check(!h.Events.Any(e => e.Time > 145), "an arrival beyond 40 ms neither takes over nor delays the Base action");
            }
            Check(h.Held.Count == 0, "released action leaves no held key at either grace boundary");
        }

        foreach (double gap in new[] { 34.99, 35, 35.01 }) {
            var h = new Harness();
            h.Step(100, State(Layer.R1));
            h.Step(100 + gap, State(Layer.R1L1, ActionButton.Down));
            h.Step(146 + gap, State(Layer.R1L1, ActionButton.Down));
            h.AssertTap(146 + gap, gap <= 35 ? Layer.R1L1 : Layer.L1, ActionButton.Down);
        }
        foreach (double gap in new[] { 15.0, 15.01 }) {
            var h = new Harness();
            h.Step(100, State(Layer.R1));
            h.Step(200, State(Layer.Base));
            h.Step(200 + gap, State(Layer.Base, ActionButton.Down));
            h.Step(210 + gap, State(Layer.Base));
            h.Step(246 + gap, State(Layer.Base));
            h.AssertTap(246 + gap, gap <= 15 ? Layer.R1 : Layer.Base, ActionButton.Down);
        }
        foreach (double body in new[] { 29.99, 30.0 }) {
            var h = new Harness();
            h.Step(100, State(Layer.Base, ActionButton.Down));
            h.Step(110, State(Layer.R1, ActionButton.Down));
            h.Step(110 + body, new() { R1 = true, R2 = 1, Down = true });
            h.Step(180, new() { R1 = true, R2 = 1, Down = true });
            h.AssertTap(180, body < 30 ? Layer.R2 : Layer.R1, ActionButton.Down);
        }
        foreach (double body in new[] { 30.0, 30.01 }) {
            var h = new Harness();
            h.Step(20, State(Layer.R1));
            h.Step(100, State(Layer.R1, ActionButton.Down));
            h.Step(100 + body, new() { R1 = true, R2 = 1, Down = true });
            h.Step(180, new() { R1 = true, R2 = 1, Down = true });
            h.AssertTap(180, body <= 30 ? Layer.R2 : Layer.R1, ActionButton.Down);
        }
    }

    private static ControllerState State(Layer layer, ActionButton? button = null) {
        var s = new ControllerState {
            L1 = layer is Layer.L1 or Layer.R1L1 or Layer.L1R2,
            R1 = layer is Layer.R1 or Layer.R1L1 or Layer.R1L2,
            L2 = layer is Layer.L2 or Layer.R2L2 or Layer.R1L2 ? 1 : 0,
            R2 = layer is Layer.R2 or Layer.R2L2 or Layer.L1R2 ? 1 : 0
        };
        if (button.HasValue) typeof(ControllerState).GetField(button.Value.ToString()).SetValue(s, true);
        return s;
    }

    private readonly record struct StrokeEvent(int Key, bool Up);
    private readonly record struct TimedEvent(double Time, int Key, bool Up);

    private static int Identity(PhysicalKey key) => ExpectedTap(KeyStroke.Of(key))[0].Key;

    private static StrokeEvent[] ExpectedTap(KeyStroke stroke, bool ctrl = false) {
        var events = new List<StrokeEvent>();
        var injector = new InputInjector(input => { events.Add(Decode(input)); return true; });
        if (ctrl) injector.KeyDown(PhysicalKey.LCtrl);
        injector.KeyTap(stroke.Key, stroke.Shift, false, false, false);
        if (ctrl) injector.KeyUp(PhysicalKey.LCtrl);
        return events.ToArray();
    }

    private static StrokeEvent Decode(InputInjector.INPUT input) {
        Check(input.type == 1, "action tests only produce fake keyboard events");
        return new StrokeEvent(input.ki.wScan | ((input.ki.dwFlags & 1) != 0 ? 0x10000 : 0), (input.ki.dwFlags & 2) != 0);
    }

    private static void Check(bool condition, string message) {
        if (!condition) throw new Exception("Rapid-action regression: " + message);
    }

    private sealed class Harness {
        private static readonly Type MapperType = typeof(MapperForm);
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly object _mapper = RuntimeHelpers.GetUninitializedObject(MapperType);
        private readonly MappingEngine _mapping = new();
        internal readonly List<TimedEvent> Events = new();
        internal readonly HashSet<int> Held = new();
        private double _now;

        internal Harness(TextInputLanguage language = TextInputLanguage.Chinese) {
            var injector = new InputInjector(input => {
                var stroke = Decode(input);
                Events.Add(new TimedEvent(_now, stroke.Key, stroke.Up));
                if (stroke.Up) Held.Remove(stroke.Key); else Held.Add(stroke.Key);
                return true;
            });
            Set("_config", new Config());
            Set("_injector", injector);
            Set("_mapping", _mapping);
            Set("_inputLanguage", language);
            Set("_holds", Array.CreateInstance(MapperType.GetNestedType("ButtonHold", BindingFlags.NonPublic), 8));
            Set("_prevDown", new bool[8]);
            foreach (string name in new[] { "_desiredLeftStickKeys", "_pressedLeftStickKeys", "_pendingLeftStickModifierTaps", "_accumulatedModifiers" })
                Set(name, new List<PhysicalKey>());
            foreach (string name in new[] { "_leftStickScroll", "_rightStickMouse", "_clutchButton", "_touchGesture" }) {
                FieldInfo field = MapperType.GetField(name, PrivateInstance);
                field.SetValue(_mapper, Activator.CreateInstance(field.FieldType, true));
            }
        }

        internal void Step(double now, ControllerState state) {
            _now = now;
            // OnTick's shoulder edge bookkeeping. Actual trigger, layer, action,
            // pending resolution and output implementations are invoked below.
            foreach (var (field, active) in new[] { ("l1", state.L1), ("r1", state.R1) }) {
                bool previous = (bool)Field("_prev" + field.ToUpperInvariant());
                if (active && !previous) {
                    Set("_" + field + "DownMs", now);
                    Set("_" + field + "UpMs", 0.0);
                    Set("_" + field + "ConsumedByCombo", false);
                }
                if (!active && previous) {
                    Set("_" + field + "UpMs", now);
                    Set("_" + field + "ConsumedByCombo", false);
                }
                Set("_prev" + field.ToUpperInvariant(), active);
            }
            Call("UpdateTriggers", state, now);
            Call("MarkActiveLayerOverlap");
            Call("UpdateActionButtons", state, now);
            Call("ClearInactiveLayerOverlapFlags", state);
        }

        internal void Modifier(double now, PhysicalKey key) => Call("RegisterModifierBinding", key, now);
        internal void ToggleLanguage() => Check((bool)Call("ApplyLanguageToggleCount", 1), "one toggle changes language");
        internal KeyStroke Stroke(Layer layer, ActionButton button, TextInputLanguage language = TextInputLanguage.Chinese) => _mapping.Lookup(layer, button, language);
        internal object Field(string name) => MapperType.GetField(name, PrivateInstance).GetValue(_mapper);
        private void Set(string name, object value) => MapperType.GetField(name, PrivateInstance).SetValue(_mapper, value);
        internal object Call(string name, params object[] values) => MapperType.GetMethod(name, PrivateInstance).Invoke(_mapper, values);
        internal object HoldField(ActionButton button, string name) {
            object hold = ((Array)Field("_holds")).GetValue((int)button);
            return hold.GetType().GetField(name).GetValue(hold);
        }
        internal void AssertTap(double now, Layer layer, ActionButton button,
            TextInputLanguage language = TextInputLanguage.Chinese, bool ctrl = false) {
            StrokeEvent[] expected = ExpectedTap(Stroke(layer, button, language), ctrl);
            StrokeEvent[] actual = Events.Where(e => e.Time == now).Select(e => new StrokeEvent(e.Key, e.Up)).ToArray();
            Check(actual.SequenceEqual(expected), $"output at {now} follows {language}/{layer}/{button}; expected {string.Join(',', expected)}, actual {string.Join(',', actual)}");
        }
    }
}
