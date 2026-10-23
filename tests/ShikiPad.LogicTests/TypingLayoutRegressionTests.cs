using System.Reflection;
using System.Runtime.CompilerServices;

internal static class TypingLayoutRegressionTests {
    private static readonly TextInputLanguage[] Languages = {
        TextInputLanguage.Chinese, TextInputLanguage.English
    };

    internal static void Run() {
        VerifyOutputInventoryAndSharedPositions();
        VerifyProtectedTransitions();
        VerifyManualLabels();
        VerifyInvalidLookups();
        VerifyLanguageToggleCounts();
    }

    private static void VerifyProtectedTransitions() {
        // Corpus-derived constraints, independent of the literal table rows.
        // Chinese: observed within-syllable pairs + top 100 cross-syllable pairs.
        // English: top 100 in-word pairs. These are not all possible strings.
        var pairs = new Dictionary<TextInputLanguage, string> {
            [TextInputLanguage.Chinese] = "ad ag ai al am an ao as ax az ba be bi bo bu ca ce ch ci co cu da de di do du ed eg eh ei ej el en er es ex ey ez fa fe fo fu ga gb gc gd ge gf gg gh gj gk gl gm go gq gr gs gt gu gw gx gy gz ha he hi ho hu ia ib ic id ie if ig ih ij ik il im in io iq ir is it iu iw ix iy iz ji ju ka ke ko ku la le li lo lu lv ma me mi mo mu na nb nc nd ne nf ng nh ni nj nk nl nm no nq nr ns nt nu nv nw nx ny nz oc od og oh oj ol om on os ou ow ox oy oz pa pe pi po pu qi qu ra re ri ro ru sa se sh si so su ta te ti to tu ua ub uc ud ue uf ug uh ui uj ul um un uo uq ur us ut ux uy uz ve wa we wo wu xi xu ya ye yi yo yu za ze zh zi zo zu",
            [TextInputLanguage.English] = "ac ad ai al am an ar as at ay be ca ce ch co ct de di ea ec ed ee el em en er es et fo ge ha he hi ho ic ie il in io is it ke la le li ll lo ly ma me mo nc nd ne ng ni no ns nt of ol om on oo or ot ou ow pe pr ra re ri ro rs rt se sh si so ss st ta te th ti to tr ts ul un ur us ut ve wa we wh wi yo"
        };
        var mapping = new MappingEngine();
        foreach (TextInputLanguage language in Languages) {
            var positions = new Dictionary<char, (Layer layer, ActionButton button)>();
            for (Layer layer = Layer.L1; layer < Layer.Reserved; layer++) {
                for (int i = 0; i < 8; i++) {
                    KeyStroke stroke = mapping.Lookup(layer, (ActionButton)i, language);
                    if (IsLetter(stroke)) positions[(char)('a' + (int)stroke.Key - (int)PhysicalKey.A)] = (layer, (ActionButton)i);
                }
            }
            string[] protectedPairs = pairs[language].Split(' ');
            Check(protectedPairs.Distinct().Count() == (language == TextInputLanguage.Chinese ? 209 : 100),
                "complete corpus protection list for " + language);
            foreach (string pair in protectedPairs) {
                var a = positions[pair[0]];
                var b = positions[pair[1]];
                bool opposite = (a.button == ActionButton.Up && b.button == ActionButton.Down) ||
                    (a.button == ActionButton.Down && b.button == ActionButton.Up) ||
                    (a.button == ActionButton.Left && b.button == ActionButton.Right) ||
                    (a.button == ActionButton.Right && b.button == ActionButton.Left);
                Check(!(a.layer != b.layer && opposite), language + " protected pair cannot switch layer and reverse DPad: " + pair);
            }
        }
    }

    private static void VerifyOutputInventoryAndSharedPositions() {
        var mapping = new MappingEngine();
        KeyStroke[] legacy = NonBaseStrokes(mapping, null);
        Check(legacy.Length == 64 && legacy.Distinct().Count() == 64,
            "legacy layout has 64 distinct physical key/Shift outputs");

        PhysicalKey[] expectedBase = {
            PhysicalKey.ArrowUp, PhysicalKey.ArrowRight, PhysicalKey.Tab, PhysicalKey.Escape,
            PhysicalKey.ArrowLeft, PhysicalKey.ArrowDown, PhysicalKey.Space, PhysicalKey.Enter
        };
        foreach (TextInputLanguage language in Languages) {
            KeyStroke[] strokes = NonBaseStrokes(mapping, language);
            Check(strokes.All(stroke => !stroke.IsNone), language + " fills every non-Base position");
            Check(Inventory(strokes).SequenceEqual(Inventory(legacy)),
                language + " preserves the complete legacy physical key/Shift multiset");
            KeyStroke[] letters = strokes.Where(IsLetter).ToArray();
            Check(letters.Length == 26 && letters.Select(stroke => stroke.Key).Distinct().Count() == 26,
                language + " contains all 26 letters exactly once");
            Check(letters.All(stroke => !stroke.Shift), language + " stores ordinary lowercase letter strokes");
            for (int index = 0; index < expectedBase.Length; index++) {
                KeyStroke expected = KeyStroke.Of(expectedBase[index]);
                Check(mapping.Lookup(Layer.Base, (ActionButton)index, language) == expected,
                    language + " preserves Base action " + index);
            }
        }

        int changedLetters = 0;
        for (Layer layer = Layer.L1; layer < Layer.Reserved; layer++) {
            for (int index = 0; index < 8; index++) {
                KeyStroke chinese = mapping.Lookup(layer, (ActionButton)index, TextInputLanguage.Chinese);
                KeyStroke english = mapping.Lookup(layer, (ActionButton)index, TextInputLanguage.English);
                KeyStroke original = mapping.Lookup(layer, (ActionButton)index);
                if (!IsLetter(original)) {
                    Check(chinese == original && english == original,
                        "every original number/symbol retains its physical position: " + layer + "/" + index);
                }
                if (!IsLetter(chinese) || !IsLetter(english)) {
                    Check(chinese == english, "numbers and symbols share position " + layer + "/" + index);
                } else if (chinese != english) {
                    changedLetters++;
                }
            }
        }
        Check(changedLetters > 0, "Chinese and English use distinct letter layouts");
    }

    private static void VerifyManualLabels() {
        var mapping = new MappingEngine();
        Type program = typeof(MappingEngine).Assembly.GetType("Program", true);
        MethodInfo manualKeys = program.GetMethod("ManualKeys", BindingFlags.NonPublic | BindingFlags.Static);
        Check(manualKeys != null, "manual has a pure table-backed label provider");
        foreach (TextInputLanguage language in Languages) {
            for (Layer layer = Layer.Base; layer < Layer.Reserved; layer++) {
                string[] labels = (string[])manualKeys.Invoke(null, new object[] { mapping, layer, language });
                Check(labels.Length == 8, "manual shows eight positions for " + language + "/" + layer);
                for (int index = 0; index < labels.Length; index++) {
                    KeyStroke stroke = mapping.Lookup(layer, (ActionButton)index, language);
                    Check(labels[index] == MappingEngine.KeyLabel(stroke),
                        "manual label uses the active mapping at " + language + "/" + layer + "/" + index);
                    Check(!string.IsNullOrWhiteSpace(labels[index]) && labels[index] != "None",
                        "manual label is visible at " + language + "/" + layer + "/" + index);
                }
            }
        }
        // These are easy to display incorrectly if only the physical key name is used.
        Check(MappingEngine.KeyLabel(KeyStroke.Shifted(PhysicalKey.Slash)) == "?", "shifted slash label");
        Check(MappingEngine.KeyLabel(KeyStroke.Shifted(PhysicalKey.Equals)) == "+", "shifted equals label");
        Check(MappingEngine.KeyLabel(KeyStroke.Of(PhysicalKey.A)) == "a", "letter label");
        Check(MappingEngine.KeyLabel(KeyStroke.Of(PhysicalKey.Num0)) == "0", "digit label");
    }

    private static void VerifyInvalidLookups() {
        var mapping = new MappingEngine();
        int[] invalidLayers = { int.MinValue, -1, (int)Layer.Reserved, (int)Layer.Reserved + 1, int.MaxValue };
        int[] invalidActions = { int.MinValue, -1, 8, int.MaxValue };
        foreach (int layer in invalidLayers) {
            Check(mapping.Lookup((Layer)layer, ActionButton.Up).IsNone, "invalid legacy layer returns None: " + layer);
            foreach (TextInputLanguage language in Languages) {
                Check(mapping.Lookup((Layer)layer, ActionButton.Up, language).IsNone,
                    "invalid language-table layer returns None: " + language + "/" + layer);
            }
        }
        for (Layer layer = Layer.Base; layer < Layer.Reserved; layer++) {
            foreach (int action in invalidActions) {
                Check(mapping.Lookup(layer, (ActionButton)action).IsNone,
                    "invalid legacy action returns None: " + layer + "/" + action);
                foreach (TextInputLanguage language in Languages) {
                    Check(mapping.Lookup(layer, (ActionButton)action, language).IsNone,
                        "invalid language-table action returns None: " + language + "/" + layer + "/" + action);
                }
            }
        }
        foreach (int language in new[] { int.MinValue, -1, 2, int.MaxValue }) {
            Check(mapping.Lookup(Layer.Base, ActionButton.Up, (TextInputLanguage)language).IsNone,
                "invalid language returns None: " + language);
        }
    }

    private static void VerifyLanguageToggleCounts() {
        // Do not construct the Form: its constructor creates the HID and native output services.
        Type mapper = typeof(MappingEngine).Assembly.GetType("MapperForm", true);
        object instance = RuntimeHelpers.GetUninitializedObject(mapper);
        FieldInfo languageField = mapper.GetField("_inputLanguage", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo visibleField = mapper.GetField("_manualVisible", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo apply = mapper.GetMethod("ApplyLanguageToggleCount", BindingFlags.NonPublic | BindingFlags.Instance);
        Check(languageField != null && visibleField != null && apply != null, "language state and toggle processor exist");
        Check(default(TextInputLanguage) == TextInputLanguage.Chinese,
            "zero-initialized language state defaults to Chinese");
        Check((TextInputLanguage)languageField.GetValue(instance) == TextInputLanguage.Chinese,
            "fresh language state starts in Chinese");
        Check(TypingLayouts.Name(TextInputLanguage.Chinese) == "中文全拼", "Chinese language label");
        Check(TypingLayouts.Name(TextInputLanguage.English) == "英文", "English language label");

        // A queued key is consumed by count parity, regardless of which console page is open.
        // Extreme signed values also cover the parity after an Interlocked.Increment overflow.
        int[] counts = { 0, 1, 2, 3, 4, 100, 101, int.MaxValue, int.MinValue, -1, -2 };
        foreach (bool manualVisible in new[] { false, true }) {
            visibleField.SetValue(instance, manualVisible);
            foreach (TextInputLanguage initial in Languages) {
                foreach (int count in counts) {
                    languageField.SetValue(instance, initial);
                    bool changed = (bool)apply.Invoke(instance, new object[] { count });
                    bool odd = (count & 1) != 0;
                    TextInputLanguage expected = odd
                        ? (initial == TextInputLanguage.Chinese ? TextInputLanguage.English : TextInputLanguage.Chinese)
                        : initial;
                    Check(changed == odd, "toggle result reflects queued parity: " + count);
                    Check((TextInputLanguage)languageField.GetValue(instance) == expected,
                        "toggle count selects language: " + initial + "/" + count);
                    Check((bool)visibleField.GetValue(instance) == manualVisible,
                        "language switching preserves the current console page");
                }
            }
        }
        languageField.SetValue(instance, TextInputLanguage.Chinese);
        apply.Invoke(instance, new object[] { 1 });
        apply.Invoke(instance, new object[] { 1 });
        Check((TextInputLanguage)languageField.GetValue(instance) == TextInputLanguage.Chinese,
            "two separately consumed switches return to Chinese");
    }

    private static KeyStroke[] NonBaseStrokes(MappingEngine mapping, TextInputLanguage? language) {
        var strokes = new List<KeyStroke>(64);
        for (Layer layer = Layer.L1; layer < Layer.Reserved; layer++) {
            for (int index = 0; index < 8; index++) {
                strokes.Add(language.HasValue
                    ? mapping.Lookup(layer, (ActionButton)index, language.Value)
                    : mapping.Lookup(layer, (ActionButton)index));
            }
        }
        return strokes.ToArray();
    }

    private static string[] Inventory(IEnumerable<KeyStroke> strokes) {
        return strokes.Select(stroke => ((int)stroke.Key) + ":" + stroke.Shift)
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static bool IsLetter(KeyStroke stroke) {
        return stroke.Key >= PhysicalKey.A && stroke.Key <= PhysicalKey.Z;
    }

    private static void Check(bool condition, string message) {
        if (!condition) throw new InvalidOperationException("Typing layout regression: " + message);
    }
}
