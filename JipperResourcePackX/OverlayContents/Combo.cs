using System;
using JipperResourcePackX.SettingTool;
using UnityEngine;
using UnityEngine.UI;

namespace JipperResourcePackX.OverlayContents;

public class Combo : Feature {
    public static int ComboCount;
    public static ComboSettings Settings => JipperSettings.Instance.Combo;
    public static GameObject ComboObject;
    public static RectTransform ComboTransform;
    private string _comboColorMaxString;
    public static Combo Instance;
    private static RectTransform _levelNameTransform;
    private static Vector3 _levelNameOriginalScale;
    private static Vector2 _levelNameOriginalPosition;
    private static Vector2 _levelNameOriginalSizeDelta;

    public Combo() : base(nameof(Combo), true) {
        Instance = this;
    }

    public override void OnEnable() {
        if (ComboObject) ComboObject.SetActive(Settings.ShowCombo);
    }

    public override void OnDisable() {
        if (ComboObject) ComboObject.SetActive(false);
        if (!_levelNameTransform) return;
        _levelNameTransform.localScale = _levelNameOriginalScale;
        _levelNameTransform.anchoredPosition = _levelNameOriginalPosition;
        _levelNameTransform.sizeDelta = _levelNameOriginalSizeDelta;
        _levelNameTransform = null;
    }

    private static void UpdateLevelNamePosition() {
        if (!_levelNameTransform) return;
        if (Settings.ShowCombo) {
            float size = JipperSettings.Instance.Size;
            _levelNameTransform.anchoredPosition = new Vector2(0, -20 - 7 * size);
            _levelNameTransform.localScale = new Vector3(0.5f * size, 0.5f * size);
            _levelNameTransform.sizeDelta = new Vector2(Math.Abs(_levelNameTransform.sizeDelta.x * 2.5f), _levelNameTransform.sizeDelta.y);
        } else {
            _levelNameTransform.localScale = _levelNameOriginalScale;
            _levelNameTransform.anchoredPosition = _levelNameOriginalPosition;
            _levelNameTransform.sizeDelta = _levelNameOriginalSizeDelta;
        }
    }

    public override void OnGUI() {
        SettingGUI settingGUI = Main.SettingGUI;
        settingGUI.AddSettingToggle(ref Settings.ShowCombo, Localization.Current["combo.showCombo"], () => {
            if (ComboObject) ComboObject.SetActive(Settings.ShowCombo);
            UpdateLevelNamePosition();
            if (Settings.ShowCombo) Overlay.Instance?.UpdateCombo(ComboCount, true);
        });
        if (!Settings.ShowCombo) return;
        settingGUI.AddSettingToggle(ref Settings.EnableAutoCombo, Localization.Current["combo.enableAutoCombo"]);
        settingGUI.AddSettingEnum(ref Settings.ComboJudgementTier, Localization.Current["combo.comboJudgementTier"]);
        settingGUI.AddSettingInt(ref Settings.ComboColorMax, 1000, ref _comboColorMaxString, Localization.Current["combo.comboColorMax"], 0);
        if (Settings.ComboColor != null && Settings.ComboColor.SettingGUI(settingGUI, Localization.Current["combo.comboColor"]))
            Overlay.Instance?.UpdateComboColor(ComboCount);
    }

    public static void OnHit(HitMargin hit) {
        try {
            if (!Settings.ShowCombo) return;
            ComboTier settingsTier = Settings.ComboJudgementTier;
            switch (hit) {
                case HitMargin.XPerfect:
                    Overlay.Instance?.UpdateCombo(++ComboCount, true);
                    break;
                case HitMargin.PerfectMinus or HitMargin.PerfectPlus when settingsTier >= ComboTier.Green:
                    Overlay.Instance?.UpdateCombo(++ComboCount, true);
                    Overlay.Instance?.ChangeComboText(ComboTier.Green);
                    break;
                case HitMargin.EarlyPerfect or HitMargin.LatePerfect when settingsTier == ComboTier.Yellow:
                    Overlay.Instance?.UpdateCombo(++ComboCount, true);
                    Overlay.Instance?.ChangeComboText(ComboTier.Yellow);
                    break;
                case HitMargin.Auto when Settings.EnableAutoCombo:
                    Overlay.Instance?.UpdateCombo(++ComboCount, true);
                    break;
                case HitMargin.Auto:
                case HitMargin.Midspin:
                    break;
                default:
                    Overlay.Instance?.UpdateCombo(ComboCount = 0, false);
                    Overlay.Instance?.ChangeComboText(settingsTier);
                    break;
            }
        } catch { }
    }

    public static void OnHUDTextAwake(Text ___txtLevelName) {
        try {
            if (!___txtLevelName) return;
            RectTransform transform = ___txtLevelName.GetComponent<RectTransform>();
            if (!transform) return;
            if (!_levelNameTransform) {
                _levelNameTransform = transform;
                _levelNameOriginalScale = transform.localScale;
                _levelNameOriginalPosition = transform.anchoredPosition;
                _levelNameOriginalSizeDelta = transform.sizeDelta;
            }
            if (Settings.ShowCombo) {
                float size = JipperSettings.Instance.Size;
                transform.anchoredPosition = new Vector2(0, -20 - 7 * size);
                transform.localScale = new Vector3(0.5f * size, 0.5f * size);
                transform.sizeDelta = new Vector2(Math.Abs(transform.sizeDelta.x * 2.5f), transform.sizeDelta.y);
                if (___txtLevelName.text != null) ___txtLevelName.text = ___txtLevelName.text.Replace('\n', ' ');
            }
        } catch { }
    }
}

public class ComboSettings {
    public bool ShowCombo = true;
    public bool EnableAutoCombo = true;
    public ComboTier ComboJudgementTier = ComboTier.Green;
    public int ComboColorMax = 1000;
    public ColorPerDictionary ComboColor = new([
        (0f, new Color(0.8745098039215686f, 0.7098039215686275f, 1)),
        (1f, new Color(0.7176470588235294f, 0.3490196078431373f, 1))
    ]);
}
