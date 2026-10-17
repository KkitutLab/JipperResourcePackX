using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace JipperResourcePackX;

public static class SafePatch {
    private static readonly Dictionary<string, Type> TypeCache = new(StringComparer.Ordinal);
    private static Assembly _assemblyCSharp;
    private static Type[] _csharpTypes;

    public static Type GetTypeSafe(string typeName) {
        if (string.IsNullOrEmpty(typeName)) return null;
        if (TypeCache.TryGetValue(typeName, out var cached)) return cached;

        _assemblyCSharp ??= AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
        Type type = null;
        if (_assemblyCSharp != null) {
            type = _assemblyCSharp.GetType(typeName);
            if (type == null) {
                _csharpTypes ??= _assemblyCSharp.GetTypes();
                for (int i = 0; i < _csharpTypes.Length; i++) {
                    if (_csharpTypes[i].Name == typeName) {
                        type = _csharpTypes[i];
                        break;
                    }
                }
            }
        }

        if (type == null) {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) {
                if (a == _assemblyCSharp) continue;
                try {
                    type = a.GetType(typeName);
                    if (type != null) break;
                    var types = a.GetTypes();
                    for (int i = 0; i < types.Length; i++) {
                        if (types[i].Name == typeName) {
                            type = types[i];
                            break;
                        }
                    }
                    if (type != null) break;
                } catch { }
            }
        }

        if (type != null) TypeCache[typeName] = type;
        return type;
    }

    public static MethodBase GetMethodSafe(Type type, string methodName, Type[] args = null, bool allowStatic = true) {
        if (type == null) return null;
        BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        if (allowStatic) bf |= BindingFlags.Static;

        if (methodName == ".ctor") {
            return args != null
                ? type.GetConstructor(bf, null, args, null)
                : type.GetConstructors(bf).FirstOrDefault();
        }

        if (methodName == ".cctor") {
            return type.TypeInitializer;
        }

        if (args != null) {
            return type.GetMethod(methodName, bf, null, args, null);
        }
        return type.GetMethods(bf).FirstOrDefault(m => m.Name == methodName);
    }

    public static MethodBase GetMethodSafe(string typeName, string methodName, Type[] args = null, bool allowStatic = true) {
        var type = GetTypeSafe(typeName);
        if (type == null) {
            ModLogger.Warn($"[SafePatch] Type not found: {typeName}");
            return null;
        }
        return GetMethodSafe(type, methodName, args, allowStatic);
    }
}

public static class SafePatchManager {
    private static readonly List<MethodBase> PatchedMethods = new();

    public static bool Apply(HarmonyLib.Harmony harmony, string name, Type type, string methodName, Type[] args = null,
        MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo transpiler = null, int priority = -1) {
        if (type == null) return false;
        try {
            var target = SafePatch.GetMethodSafe(type, methodName, args, true);
            if (target == null) {
                ModLogger.Warn($"[SafePatch] Target method missing: {type.FullName}.{methodName}");
                return false;
            }

            var hPrefix = prefix != null ? new HarmonyMethod(prefix, priority) : null;
            var hPostfix = postfix != null ? new HarmonyMethod(postfix, priority) : null;
            var hTranspiler = transpiler != null ? new HarmonyMethod(transpiler, priority) : null;

            harmony.Patch(target, hPrefix, hPostfix, hTranspiler);
            PatchedMethods.Add(target);
            ModLogger.Log($"[SafePatch] {name} on {type.Name}.{methodName} applied.");
            return true;
        } catch (Exception ex) {
            ModLogger.Warn($"[SafePatch] Patch '{name}' failed on {type.FullName}.{methodName}: {ex.Message}");
            return false;
        }
    }

    public static bool Apply(HarmonyLib.Harmony harmony, string name, string typeName, string methodName, Type[] args = null,
        MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo transpiler = null, int priority = -1) {
        var type = SafePatch.GetTypeSafe(typeName);
        if (type == null) {
            ModLogger.Warn($"[SafePatch] Type not found: {typeName}");
            return false;
        }
        return Apply(harmony, name, type, methodName, args, prefix, postfix, transpiler, priority);
    }

    public static bool ApplyAll(HarmonyLib.Harmony harmony) {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int initialCount = PatchedMethods.Count;
        try {
            Apply(harmony, "Main_OnGameStart1", "scnGame", "Play",
                postfix: typeof(Main).GetMethod(nameof(Main.OnGameStart1), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Main_OnGameStart2", "scrPressToStart", "ShowText",
                postfix: typeof(Main).GetMethod(nameof(Main.OnGameStart2), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Main_OnChangeState", "StateBehaviour", "ChangeState", new[] { typeof(Enum) },
                postfix: typeof(Main).GetMethod(nameof(Main.OnChangeState), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Main_WipeToBlack", "scrUIController", "WipeToBlack",
                postfix: typeof(Main).GetMethod(nameof(Main.OnGameStop), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Main_ResetScene", "scnEditor", "ResetScene",
                postfix: typeof(Main).GetMethod(nameof(Main.OnGameStop), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Main_StartLoadingScene", "scrController", "StartLoadingScene",
                postfix: typeof(Main).GetMethod(nameof(Main.OnGameStop), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Main_SetPlayerCount", "scrMistakesManager", "SetPlayerCount",
                postfix: typeof(Main).GetMethod(nameof(Main.UpdatePlayersCount), BindingFlags.Public | BindingFlags.Static));

            if (!Apply(harmony, "Bpm_Hit_Player", "scrPlayer", "Hit",
                postfix: typeof(OverlayContents.Bpm).GetMethod(nameof(OverlayContents.Bpm.OnHit), BindingFlags.Public | BindingFlags.Static))) {
                Apply(harmony, "Bpm_Hit_Controller", "scrController", "Hit",
                    postfix: typeof(OverlayContents.Bpm).GetMethod(nameof(OverlayContents.Bpm.OnHit), BindingFlags.Public | BindingFlags.Static));
            }

            Apply(harmony, "Combo_AddHit", "scrMarginTracker", "AddHit",
                postfix: typeof(OverlayContents.Combo).GetMethod(nameof(OverlayContents.Combo.OnHit), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Combo_AwakeRewind", "scrController", "Awake_Rewind",
                postfix: typeof(OverlayContents.Combo).GetMethod(nameof(OverlayContents.Combo.OnHUDTextAwake), BindingFlags.Public | BindingFlags.Static));

            Apply(harmony, "Judgement_AddHit", "scrMarginTracker", "AddHit",
                postfix: typeof(OverlayContents.Judgement).GetMethod(nameof(OverlayContents.Judgement.OnHit), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Judgement_Reset", "scrMarginTracker", "Reset",
                postfix: typeof(OverlayContents.Judgement).GetMethod(nameof(OverlayContents.Judgement.OnHit), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Judgement_Die", "scrPlayer", "Die",
                postfix: typeof(OverlayContents.Judgement).GetMethod(nameof(OverlayContents.Judgement.OnDie), BindingFlags.Public | BindingFlags.Static));

            Apply(harmony, "Status_AccChange", "scrMarginTracker", "CalculatePercentAcc",
                postfix: typeof(OverlayContents.Status).GetMethod(nameof(OverlayContents.Status.OnAccuracyChange), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Status_ProgressChange", "scrPlanet", "MoveToNextFloor",
                postfix: typeof(OverlayContents.Status).GetMethod(nameof(OverlayContents.Status.OnProgressChange), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Status_SwitchChosenPrefix", "scrPlanet", "SwitchChosen",
                prefix: typeof(OverlayContents.Status).GetMethod(nameof(OverlayContents.Status.SwitchChosenPrefix), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Status_SwitchChosenFinalizer", "scrPlanet", "SwitchChosen",
                postfix: typeof(OverlayContents.Status).GetMethod(nameof(OverlayContents.Status.SwitchChosenFinalizer), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Status_GetHitMarginInDeg", "scrMisc", "GetHitMarginInDeg",
                postfix: typeof(OverlayContents.Status).GetMethod(nameof(OverlayContents.Status.GetHitMarginInDegProxyR149), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Status_GetHitMarginInSec", "scrMisc", "GetHitMarginInSec",
                postfix: typeof(OverlayContents.Status).GetMethod(nameof(OverlayContents.Status.GetHitMarginInSecProxyR149), BindingFlags.Public | BindingFlags.Static));
            Apply(harmony, "Status_ShowIfDebugAwake", "scrShowIfDebug", "Awake",
                postfix: typeof(OverlayContents.Status).GetMethod(nameof(OverlayContents.Status.OnShowIfDebugAwake), BindingFlags.Public | BindingFlags.Static));

            Apply(harmony, "TimingScale_MoveToNextFloor", "scrPlanet", "MoveToNextFloor",
                postfix: typeof(OverlayContents.TimingScale).GetMethod(nameof(OverlayContents.TimingScale.OnMoveToNextFloor), BindingFlags.Public | BindingFlags.Static));

            ResourceChanger.PatchAll(harmony);
            KeyViewerContents.KeyViewer.SetupOtherMods();

            sw.Stop();
            int total = PatchedMethods.Count - initialCount;
            ModLogger.Log($"[SafePatch] Successfully applied {total} safe patches in {sw.ElapsedMilliseconds}ms.");
            return true;
        } catch (Exception ex) {
            ModLogger.Error($"CRITICAL: SafePatchManager.ApplyAll encountered error: {ex}");
            return false;
        }
    }

    public static void UnpatchAll(HarmonyLib.Harmony harmony) {
        try {
            harmony.UnpatchSelf();
        } catch (Exception ex) {
            ModLogger.Warn($"[SafePatch] Error during unpatch: {ex.Message}");
        }
        PatchedMethods.Clear();
    }
}
