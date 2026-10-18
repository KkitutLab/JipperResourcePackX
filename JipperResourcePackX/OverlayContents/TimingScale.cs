using UnityEngine;

namespace JipperResourcePackX.OverlayContents;

public class TimingScale : Feature {
    public static GameObject TimingScaleObject;
    public static TimingScaleSettings Settings => JipperSettings.Instance.TimingScale;
    public static TimingScale Instance;

    public TimingScale() : base(nameof(TimingScale), true) {
        Instance = this;
    }
    
    public override void OnEnable() {
        if (TimingScaleObject) TimingScaleObject.SetActive(Settings.ShowTimingScale);
    }
    
    public override void OnDisable() {
        if (TimingScaleObject) TimingScaleObject.SetActive(false);
    }

    public override void OnGUI() {
        SettingGUI settingGUI = Main.SettingGUI;
        settingGUI.AddSettingToggle(ref Settings.ShowTimingScale, Localization.Current["timingScale.showTimingScale"], () => {
            if (TimingScaleObject) TimingScaleObject.SetActive(Settings.ShowTimingScale);
            if (Settings.ShowTimingScale) Overlay.Instance?.UpdateTimingScale();
        });
    }
    
    public static void OnMoveToNextFloor() {
        try {
            if (!Settings.ShowTimingScale) return;
            Overlay.Instance?.UpdateTimingScale();
        } catch { }
    }
}

public class TimingScaleSettings {
    public bool ShowTimingScale = true;
}