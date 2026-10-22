using System.Collections.Concurrent;
using System.Threading.Channels;

internal static class FaultRegressionTests {
    internal static void Run() {
        VerifyPartialShortcutCleanup();
        VerifySuccessfulOutputOrder();
        VerifyShutdownRejectsLateOutput();
        VerifyHidCancellationAndBufferReuse();
        VerifyHidDisconnect();
        VerifyHidNormalization();
    }

    private static void VerifyPartialShortcutCleanup() {
        // A shortcut temporarily releases Ctrl, then presses Shift+Win+S and restores Ctrl.
        var baseline = new FakeOutput();
        var baselineInjector = new InputInjector(baseline.Send);
        baselineInjector.KeyDown(PhysicalKey.LCtrl);
        baseline.Events.Clear();
        baselineInjector.KeyTapExact(PhysicalKey.S, true, false, false, true);
        var shortcut = baseline.Events.ToArray();
        Check(shortcut.Length == 8, "the exact shortcut keeps its original eight-stroke sequence");

        for (int failingEvent = 0; failingEvent < shortcut.Length; failingEvent++) {
            var output = new FakeOutput();
            var injector = new InputInjector(output.Send);
            injector.KeyDown(PhysicalKey.LCtrl);
            output.Failure = shortcut[failingEvent];
            Throws(() => injector.KeyTapExact(PhysicalKey.S, true, false, false, true));
            Check(output.FailedAttempts == 2, "a failed stroke is retried exactly once");

            // A failed cleanup must keep the outstanding keys available for the next cleanup.
            if (output.Failure.Value.Up && output.Held.Contains(output.Failure.Value.Key)) {
                Throws(injector.ReleaseAll);
            }
            output.Failure = null;
            injector.ReleaseAll();
            Check(output.Held.Count == 0, "cleanup releases every confirmed temporary/owned key after partial failure " + failingEvent);
            Check(!injector.IsKeyHeld(PhysicalKey.LCtrl), "successful cleanup clears logical modifier ownership");
        }

        // This is the original failure: a tap's temporary key was never in _heldKeys.
        var tapOutput = new FakeOutput();
        var tap = new InputInjector(tapOutput.Send);
        tap.KeyTap(PhysicalKey.M, false, false, false, false);
        tapOutput.Failure = tapOutput.Events.Last();
        Throws(() => tap.KeyTap(PhysicalKey.M, false, false, false, false));
        Check(tapOutput.Held.Count == 1 && !tap.IsKeyHeld(PhysicalKey.M), "failed tap release leaves only a temporary physical hold");
        tapOutput.Failure = null;
        tap.ReleaseAll();
        Check(tapOutput.Held.Count == 0, "temporary tap key is released even without a logical hold");
    }

    private static void VerifySuccessfulOutputOrder() {
        var output = new FakeOutput();
        var injector = new InputInjector(output.Send);
        injector.KeyDown(PhysicalKey.LShift);
        injector.KeyDown(PhysicalKey.LShift);
        injector.KeyUp(PhysicalKey.LShift);
        Check(output.Events.Count == 1 && output.Held.Count == 1, "modifier reference counting sends one down");
        injector.KeyUp(PhysicalKey.LShift);
        Check(output.Events.Count == 2 && output.Held.Count == 0, "last modifier owner sends one up");
        output.Events.Clear();
        injector.KeyTap(PhysicalKey.S, true, false, false, true);
        Check(output.Events.Select(e => e.Up).SequenceEqual(new[] { false, false, false, true, true, true }), "successful chord keeps press/release order");
        Check(output.Events[0].Key == output.Events[5].Key && output.Events[1].Key == output.Events[4].Key && output.Events[2].Key == output.Events[3].Key, "shortcut releases in reverse order");
        int count = output.Events.Count;
        injector.ReleaseAll();
        Check(output.Events.Count == count, "successful taps add no duplicate cleanup events");
    }

    private static void VerifyHidCancellationAndBufferReuse() {
        var streams = new ConcurrentQueue<ReportStream>();
        int opened = 0;
        var controller = new DirectHidController(() => {
            Interlocked.Increment(ref opened);
            return streams.TryDequeue(out var stream) ? stream : null;
        });
        for (int cycle = 0; cycle < 10; cycle++) {
            var stream = new ReportStream();
            streams.Enqueue(stream);
            controller.Start();
            try {
                Check(stream.ReadStarted.Wait(2000), "reader begins without a physical controller");
                controller.Start();
                Check(opened == cycle + 1, "duplicate Start does not create a second reader");
                var full = NeutralReport();
                full[8] = 0x28;
                stream.Reports.Writer.TryWrite(full);
                Wait(() => controller.State.Cross, "full report is published");
                var shortReport = new byte[] { 1, 0, 255, 127, 128, 0x48, 0, 0, 64, 128 };
                stream.Reports.Writer.TryWrite(shortReport);
                Wait(() => controller.State.Circle, "short report is parsed using its actual length");
                var snapshot = controller.State;
                Check(!snapshot.Cross && snapshot.LX == -1.0 && snapshot.LY == 1.0, "previous buffer contents do not leak into the next report");
                Check(Math.Abs(snapshot.L2 - 64.0 / 255.0) < 1e-12, "trigger normalization remains unchanged");
            } finally {
                controller.Stop();
                controller.Stop();
            }
            Check(stream.DisposeCount == 1 && stream.PendingReads == 0, "read cancellation completes before the one owning thread disposes the stream");
            Check(!stream.DisposedWhileReading && !controller.State.Connected, "stop neither closes an active read nor publishes a stale connected state");
            stream.ReadStarted.Dispose();
        }
    }

    private static void VerifyShutdownRejectsLateOutput() {
        var output = new FakeOutput();
        var injector = new InputInjector(output.Send);
        injector.KeyDown(PhysicalKey.M);
        injector.Shutdown();
        int count = output.Events.Count;
        Throws(() => injector.KeyDown(PhysicalKey.M));
        Throws(() => injector.KeyTap(PhysicalKey.S, true, false, false, true));
        injector.Shutdown();
        Check(output.Held.Count == 0 && output.Events.Count == count, "shutdown prevents late mapper output from re-pressing a released key");
    }

    private static void VerifyHidDisconnect() {
        var stream = new ReportStream();
        var controller = new DirectHidController(() => stream);
        controller.Start();
        try {
            var held = NeutralReport();
            held[8] = 0x18;
            stream.Reports.Writer.TryWrite(held);
            Wait(() => controller.State.Square, "held button is published before disconnect");
            stream.Reports.Writer.TryWrite(Array.Empty<byte>());
            Wait(() => !controller.State.Connected && stream.DisposeCount == 1, "end of device input publishes neutral state and closes once");
            Check(!controller.State.Square, "disconnect clears the held controller button");
        } finally {
            controller.Stop();
            stream.ReadStarted.Dispose();
        }
    }

    private static void VerifyHidNormalization() {
        var report = NeutralReport();
        report[1] = 0; report[2] = 255; report[3] = 127; report[4] = 128;
        report[5] = 0; report[6] = 255; report[8] = 1;
        Check(DirectHidController.TryParseDualSenseReportSpan(report, out var state), "span parser accepts full report");
        Check(state.LX == -1 && state.LY == 1 && state.RX < 0 && state.RY > 0, "axis direction, center and extremes stay unchanged");
        Check(state.Up && state.Right && !state.Down && !state.Left && state.L2 == 0 && state.R2 == 1, "hat diagonal and trigger endpoints stay unchanged");
        Check(!DirectHidController.TryParseDualSenseReport(null, out state) && state == null, "null report is rejected");
        Check(!DirectHidController.TryParseDualSenseReportSpan(report.AsSpan(0, 7), out state), "short report is rejected without reading outside its slice");
    }

    private static byte[] NeutralReport() {
        var report = new byte[64];
        report[0] = 1;
        report[1] = report[2] = report[3] = report[4] = 128;
        report[8] = 8;
        report[33] = report[37] = 0x80;
        return report;
    }

    private readonly record struct KeyEvent(int Key, bool Up);

    private sealed class FakeOutput {
        internal readonly HashSet<int> Held = new();
        internal readonly List<KeyEvent> Events = new();
        internal KeyEvent? Failure;
        internal int FailedAttempts;

        internal bool Send(InputInjector.INPUT input) {
            if (input.type != 1) throw new Exception("This test must only send synthetic keyboard events.");
            int identity = input.ki.wScan | ((input.ki.dwFlags & 1) != 0 ? 0x10000 : 0);
            var key = new KeyEvent(identity, (input.ki.dwFlags & 2) != 0);
            if (Failure == key) { FailedAttempts++; return false; }
            Events.Add(key);
            if (key.Up) Held.Remove(identity);
            else Held.Add(identity);
            return true;
        }
    }

    private sealed class ReportStream : Stream {
        internal readonly Channel<byte[]> Reports = Channel.CreateUnbounded<byte[]>();
        internal readonly ManualResetEventSlim ReadStarted = new();
        internal int DisposeCount;
        internal int PendingReads;
        internal bool DisposedWhileReading;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            Interlocked.Increment(ref PendingReads);
            ReadStarted.Set();
            try {
                var report = await Reports.Reader.ReadAsync(cancellationToken);
                report.AsMemory().CopyTo(buffer);
                return report.Length;
            } finally {
                Interlocked.Decrement(ref PendingReads);
            }
        }
        protected override void Dispose(bool disposing) {
            DisposedWhileReading |= PendingReads != 0;
            Interlocked.Increment(ref DisposeCount);
            base.Dispose(disposing);
        }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static void Wait(Func<bool> condition, string reason) => Check(SpinWait.SpinUntil(condition, 2000), reason);
    private static void Check(bool condition, string reason) { if (!condition) throw new Exception(reason); }
    private static void Throws(Action action) {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new Exception("Expected output failure was not reported.");
    }
}
