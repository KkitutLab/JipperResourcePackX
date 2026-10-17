using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace JipperResourcePackX.KeyViewerContents.OtherModApi;

public static class AdofaiTweaksAPI {
    public static bool IsExist;
    private static object _keySetting;
    private static PropertyInfo _activeKeysProp;
    private static PropertyInfo _activeAsyncKeysProp;

    public static void Setup() {
        if (IsExist) return;
        try {
            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "AdofaiTweaks");
            if (asm == null) return;
            var coreType = asm.GetType("AdofaiTweaks.AdofaiTweaks");
            if (coreType == null) return;

            var runnersMember = (MemberInfo)coreType.GetProperty("tweakRunners", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?? coreType.GetField("tweakRunners", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            object runnersObj = runnersMember switch {
                PropertyInfo p => p.GetValue(null),
                FieldInfo f => f.GetValue(null),
                _ => null
            };
            if (runnersObj is IEnumerable runners) {
                foreach (var runner in runners) {
                    var settingsProp = runner.GetType().GetProperty("Settings");
                    var settings = settingsProp?.GetValue(runner);
                    if (settings != null && settings.GetType().Name == "KeyLimiterSettings") {
                        _keySetting = settings;
                        _activeKeysProp = settings.GetType().GetProperty("ActiveKeys");
                        _activeAsyncKeysProp = settings.GetType().GetProperty("ActiveAsyncKeys");
                        IsExist = true;
                        ModLogger.Log("AdofaiTweaksAPI is loaded.");
                        return;
                    }
                }
            }
        } catch (Exception ex) {
            ModLogger.Warn($"Failed to setup AdofaiTweaksAPI: {ex.Message}");
        }
    }

    public static void UpdateKeyLimit(List<KeyCode> keys, List<ushort> asyncKeys) {
        if (!IsExist || _keySetting == null) return;
        try {
            _activeKeysProp?.SetValue(_keySetting, keys);
            _activeAsyncKeysProp?.SetValue(_keySetting, asyncKeys);
        } catch (Exception ex) {
            ModLogger.Warn($"Failed to update AdofaiTweaks key limit: {ex.Message}");
        }
    }
}