using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace JipperResourcePackX.KeyViewerContents.OtherModApi;

public static class KeyboardChatterBlockerAPI {
    public static bool IsExist;
    private static Type _mainType;
    private static PropertyInfo _selectedProfileProp;
    private static FieldInfo _selectedProfileField;

    public static void Setup() {
        if (IsExist) return;
        try {
            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "KeyboardChatterBlocker");
            if (asm == null) return;
            _mainType = asm.GetType("KeyboardChatterBlocker.Main");
            if (_mainType == null) return;

            _selectedProfileProp = _mainType.GetProperty("selectedKeyLimiterProfile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            _selectedProfileField = _mainType.GetField("selectedKeyLimiterProfile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            IsExist = true;
            ModLogger.Log("KeyboardChatterBlockerAPI is loaded.");
        } catch (Exception ex) {
            ModLogger.Warn($"Failed to setup KeyboardChatterBlockerAPI: {ex.Message}");
        }
    }

    public static void UpdateKeyLimit(List<KeyCode> keys, List<ushort> asyncKeys) {
        if (!IsExist || _mainType == null) return;
        try {
            object profile = _selectedProfileProp?.GetValue(null) ?? _selectedProfileField?.GetValue(null);
            if (profile != null) {
                var pType = profile.GetType();
                var allowedKeysProp = pType.GetProperty("allowedKeys") ?? (MemberInfo)pType.GetField("allowedKeys");
                var allowedAsyncProp = pType.GetProperty("allowedAsyncKeys") ?? (MemberInfo)pType.GetField("allowedAsyncKeys");

                if (allowedKeysProp is PropertyInfo pk) pk.SetValue(profile, keys);
                else if (allowedKeysProp is FieldInfo fk) fk.SetValue(profile, keys);

                if (allowedAsyncProp is PropertyInfo pa) pa.SetValue(profile, asyncKeys);
                else if (allowedAsyncProp is FieldInfo fa) fa.SetValue(profile, asyncKeys);
            }
        } catch (Exception ex) {
            ModLogger.Warn($"Failed to update KeyboardChatterBlocker key limit: {ex.Message}");
        }
    }
}