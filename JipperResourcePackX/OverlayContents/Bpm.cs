using System;
using JipperResourcePackX.SettingTool;
using UnityEngine;

namespace JipperResourcePackX.OverlayContents;

public class Bpm : Feature {
    public static GameObject BpmObject;
    public static BpmSettings Settings => JipperSettings.Instance.Bpm;
    private string _bpmColorMaxString;
    private string _decimalPlacesString;
    public static Bpm Instance;

    public Bpm() : base("BPM", true) {
        Instance = this;
    }

    public override void OnEnable() {
        if (BpmObject) BpmObject.SetActive(true);
    }
    
    public override void OnDisable() {
        if (BpmObject) BpmObject.SetActive(false);
    }

    public override void OnGUI() {
        SettingGUI settingGUI = Main.SettingGUI;
        settingGUI.AddSettingFloat(ref Settings.BpmColorMax, 8000, ref _bpmColorMaxString, Localization.Current["bpm.bpmColorMax"], 0);
        if(Settings.BpmColor != null && Settings.BpmColor.SettingGUI(settingGUI, Localization.Current["bpm.bpmColor"])) Overlay.Instance?.UpdateBpm();
        settingGUI.AddSettingSliderInt(ref Settings.DecimalPlaces, 2, ref _decimalPlacesString, Localization.Current["bpm.decimalPlaces"], 0, 4, () => Overlay.Instance?.UpdateBpm());
    }

    public static void OnHit() {
        try {
            Overlay.Instance?.UpdateBpm();
        } catch { }
    }
}

public class BpmSettings {
    public float BpmColorMax = 8000;
    public int DecimalPlaces = 2;
    public ColorPerDictionary BpmColor = new([
        (0f, Color.white),
        (1f, Color.magenta)
    ]);
}