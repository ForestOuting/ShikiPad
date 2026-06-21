using System;
using System.Globalization;
using System.IO;

internal static class Logger {
    private static readonly object LockObj = new object();
    private static string _path = "logs\\shikipad.log";

    public static void Init(string root) {
        Directory.CreateDirectory(Path.Combine(root, "logs"));
        _path = Path.Combine(root, "logs", "shikipad.log");
    }

    public static void Info(string message) { Write("INFO", message); }
    public static void Warn(string message) { Write("WARN", message); }
    public static void Error(string message) { Write("ERROR", message); }

    private static void Write(string level, string message) {
        lock (LockObj) {
            File.AppendAllText(_path, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) + " [" + level + "] " + message + Environment.NewLine);
        }
    }
}
