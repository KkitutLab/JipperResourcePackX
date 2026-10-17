using System;
using UnityEngine;

namespace JipperResourcePackX;

public class SettingGUI {
    public static readonly SettingGUI Instance = new();

    public void AddSettingToggle(ref bool value, string text, Action onChange = null) =>
        Toggle(ref value, text, onChange);

    public void AddSettingSliderInt(ref int value, int defaultValue, ref string cacheString, string text, int min, int max, Action onChange = null) =>
        SliderInt(ref value, defaultValue, ref cacheString, text, min, max, onChange);

    public void AddSettingSliderFloat(ref float value, float defaultValue, ref string cacheString, string text, float min, float max, Action onChange = null) =>
        SliderFloat(ref value, defaultValue, ref cacheString, text, min, max, onChange);

    public void AddSettingFloat(ref float value, float defaultValue, ref string cacheString, string text, float min, Action onChange = null) =>
        FloatField(ref value, defaultValue, ref cacheString, text, min, onChange);

    public void AddSettingInt(ref int value, int defaultValue, ref string cacheString, string text, int min, Action onChange = null) =>
        IntField(ref value, defaultValue, ref cacheString, text, min, onChange);

    public void AddSettingString(ref string value, string oldValue, string text, Action onChange = null) =>
        StringField(ref value, oldValue, text, onChange);

    public void AddSettingEnum<T>(ref T value, string text, Action onChange = null) where T : struct, Enum =>
        EnumField(ref value, text, onChange);

    public void AddSettingEnum<T>(ref T value, string text, T[] values, Action onChange = null) where T : struct, Enum =>
        EnumField(ref value, text, values, onChange);

    public static void Toggle(ref bool value, string text, Action onChange = null) {
        GUILayout.BeginHorizontal();
        bool newVal = GUILayout.Toggle(value, text);
        GUILayout.EndHorizontal();
        if (newVal != value) {
            value = newVal;
            onChange?.Invoke();
        }
    }

    public static void SliderInt(ref int value, int defaultValue, ref string cacheString, string text, int min, int max, Action onChange = null) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text, GUILayout.Width(220));
        int newVal = (int)GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(180));
        if (newVal != value) {
            value = newVal;
            cacheString = value.ToString();
            onChange?.Invoke();
        }
        cacheString ??= value.ToString();
        string input = GUILayout.TextField(cacheString, GUILayout.Width(60));
        if (input != cacheString) {
            cacheString = input;
            if (int.TryParse(input, out int parsed)) {
                value = Mathf.Clamp(parsed, min, max);
                onChange?.Invoke();
            }
        }
        GUILayout.EndHorizontal();
    }

    public static void SliderFloat(ref float value, float defaultValue, ref string cacheString, string text, float min, float max, Action onChange = null) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text, GUILayout.Width(220));
        float newVal = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(180));
        if (!Mathf.Approximately(newVal, value)) {
            value = newVal;
            cacheString = value.ToString("0.##");
            onChange?.Invoke();
        }
        cacheString ??= value.ToString("0.##");
        string input = GUILayout.TextField(cacheString, GUILayout.Width(60));
        if (input != cacheString) {
            cacheString = input;
            if (float.TryParse(input, out float parsed)) {
                value = Mathf.Clamp(parsed, min, max);
                onChange?.Invoke();
            }
        }
        GUILayout.EndHorizontal();
    }

    public static void FloatField(ref float value, float defaultValue, ref string cacheString, string text, float min, Action onChange = null) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text, GUILayout.Width(220));
        cacheString ??= value.ToString("0.##");
        string input = GUILayout.TextField(cacheString, GUILayout.Width(120));
        if (input != cacheString) {
            cacheString = input;
            if (float.TryParse(input, out float parsed) && parsed >= min) {
                value = parsed;
                onChange?.Invoke();
            }
        }
        GUILayout.EndHorizontal();
    }

    public static void IntField(ref int value, int defaultValue, ref string cacheString, string text, int min, Action onChange = null) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text, GUILayout.Width(220));
        cacheString ??= value.ToString();
        string input = GUILayout.TextField(cacheString, GUILayout.Width(120));
        if (input != cacheString) {
            cacheString = input;
            if (int.TryParse(input, out int parsed) && parsed >= min) {
                value = parsed;
                onChange?.Invoke();
            }
        }
        GUILayout.EndHorizontal();
    }

    public static void StringField(ref string value, string oldValue, string text, Action onChange = null) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text, GUILayout.Width(100));
        string input = GUILayout.TextField(value ?? "", GUILayout.Width(120));
        if (input != value) {
            value = input;
            onChange?.Invoke();
        }
        GUILayout.EndHorizontal();
    }

    public static void EnumField<T>(ref T value, string text, Action onChange = null) where T : struct, Enum {
        T[] values = (T[])Enum.GetValues(typeof(T));
        EnumField(ref value, text, values, onChange);
    }

    public static void EnumField<T>(ref T value, string text, T[] values, Action onChange = null) where T : struct, Enum {
        GUILayout.BeginHorizontal();
        GUILayout.Label(text, GUILayout.Width(220));
        int curIdx = Array.IndexOf(values, value);
        if (curIdx < 0) curIdx = 0;

        string curName = value.ToString();
        if (GUILayout.Button($"< {curName} >", GUILayout.Width(180))) {
            curIdx = (curIdx + 1) % values.Length;
            value = values[curIdx];
            onChange?.Invoke();
        }
        GUILayout.EndHorizontal();
    }
}
