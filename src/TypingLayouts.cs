using System;

// These tables select physical keystrokes only. They do not change the Windows IME.
internal static class TypingLayouts {
    // Rows: L1, R1, L2, R2, R1+L1, R2+L2, L1+R2, R1+L2.
    // Columns: Up, Right, Square, Triangle, Left, Down, Cross, Circle.
    // All 26 letters optimized separately; every number and symbol keeps its original position.
    private static readonly string[] ChineseRows = new string[] {
        "exildang", "jyschouz", "vbw1kq32", "0tmp-=rf",
        "4,.75698", "+/&*_^$%", "[]!?{}@#", "();'<>`\\"
    };

    private static readonly string[] EnglishRows = new string[] {
        "mfduclyo", "rsainteh", "vpk1gw32", "0bzj-=xq",
        "4,.75698", "+/&*_^$%", "[]!?{}@#", "();'<>`\\"
    };

    public static string Name(TextInputLanguage language) {
        return language == TextInputLanguage.English ? "英文" : "中文全拼";
    }

    public static KeyStroke[][] CreateTables(TextInputLanguage language, KeyStroke[][] original) {
        string[] rows = language == TextInputLanguage.English ? EnglishRows : ChineseRows;
        KeyStroke[][] tables = new KeyStroke[original.Length][];
        tables[(int)Layer.Base] = (KeyStroke[])original[(int)Layer.Base].Clone();
        for (int row = 0; row < rows.Length; row++) {
            if (rows[row].Length != 8) throw new InvalidOperationException("A typing layout row must contain eight keys.");
            KeyStroke[] strokes = new KeyStroke[8];
            for (int column = 0; column < 8; column++) strokes[column] = FromCharacter(rows[row][column]);
            tables[(int)Layer.L1 + row] = strokes;
        }
        return tables;
    }

    private static KeyStroke FromCharacter(char character) {
        if (character >= 'a' && character <= 'z') return KeyStroke.Of((PhysicalKey)((int)PhysicalKey.A + character - 'a'));
        if (character >= '0' && character <= '9') return KeyStroke.Of((PhysicalKey)((int)PhysicalKey.Num0 + character - '0'));
        switch (character) {
            case '-': return KeyStroke.Of(PhysicalKey.Minus);
            case '=': return KeyStroke.Of(PhysicalKey.Equals);
            case '[': return KeyStroke.Of(PhysicalKey.LeftBracket);
            case ']': return KeyStroke.Of(PhysicalKey.RightBracket);
            case '\\': return KeyStroke.Of(PhysicalKey.Backslash);
            case ';': return KeyStroke.Of(PhysicalKey.Semicolon);
            case '\'': return KeyStroke.Of(PhysicalKey.Apostrophe);
            case ',': return KeyStroke.Of(PhysicalKey.Comma);
            case '.': return KeyStroke.Of(PhysicalKey.Period);
            case '/': return KeyStroke.Of(PhysicalKey.Slash);
            case '`': return KeyStroke.Of(PhysicalKey.Grave);
            case '!': return KeyStroke.Shifted(PhysicalKey.Num1);
            case '@': return KeyStroke.Shifted(PhysicalKey.Num2);
            case '#': return KeyStroke.Shifted(PhysicalKey.Num3);
            case '$': return KeyStroke.Shifted(PhysicalKey.Num4);
            case '%': return KeyStroke.Shifted(PhysicalKey.Num5);
            case '^': return KeyStroke.Shifted(PhysicalKey.Num6);
            case '&': return KeyStroke.Shifted(PhysicalKey.Num7);
            case '*': return KeyStroke.Shifted(PhysicalKey.Num8);
            case '(': return KeyStroke.Shifted(PhysicalKey.Num9);
            case ')': return KeyStroke.Shifted(PhysicalKey.Num0);
            case '_': return KeyStroke.Shifted(PhysicalKey.Minus);
            case '+': return KeyStroke.Shifted(PhysicalKey.Equals);
            case '{': return KeyStroke.Shifted(PhysicalKey.LeftBracket);
            case '}': return KeyStroke.Shifted(PhysicalKey.RightBracket);
            case '<': return KeyStroke.Shifted(PhysicalKey.Comma);
            case '>': return KeyStroke.Shifted(PhysicalKey.Period);
            case '?': return KeyStroke.Shifted(PhysicalKey.Slash);
            default: throw new InvalidOperationException("Unsupported typing layout character: " + character);
        }
    }
}
