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
