using System;
using System.Collections.Concurrent;
using System.Threading;

namespace JipperResourcePackX;

public static class MainThread {
    private static readonly ConcurrentQueue<Action> Actions = new();
    public static int MainThreadId;
    private const int MaxActionsPerFrame = 64;

    public static bool IsMainThread() {
        return Thread.CurrentThread.ManagedThreadId == MainThreadId;
    }

    public static void Enqueue(Action action) {
        if (action == null) return;
        Actions.Enqueue(action);
    }

    public static void Run(Action action) {
        if (action == null) return;
        if (IsMainThread()) {
            action();
            return;
        }
        Actions.Enqueue(action);
    }

    public static void Run(object _, Action action) => Run(action);

    public static void Update() {
        int frameBatch = Math.Min(Actions.Count, MaxActionsPerFrame);
        int processed = 0;
        while (processed < frameBatch && Actions.TryDequeue(out var action)) {
            processed++;
            try {
                action();
            } catch (Exception ex) {
                ModLogger.Warn($"[MainThread] Action error: {ex.Message}");
            }
        }
    }
}
