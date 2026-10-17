using System;

namespace JipperResourcePackX.KeyViewerContents;

static class AsyncInputHook {
    internal static void Setup(HarmonyLib.Harmony harmony) {
    }

    internal static void Acquire() {
        // Do not force SkyHookManager to start: starting the hook causes AsyncInputManager.isActive
        // to be true in ADOFAI, which disables Unity input handling and breaks player inputs.
    }

    internal static void Release() {
    }
}
