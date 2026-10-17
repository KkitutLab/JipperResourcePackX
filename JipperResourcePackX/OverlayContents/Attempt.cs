using UnityEngine;

namespace JipperResourcePackX.OverlayContents;

public class Attempt : Feature {
    public static AttemptSetting Settings => JipperSettings.Instance.Attempt;
    public static Attempt Instance;
    public static GameObject AttemptObject;

    public Attempt() : base(nameof(Attempt), true) {
        Instance = this;
    }

    public override void OnEnable() {
        if (AttemptObject) AttemptObject.SetActive(true);
        if (scrLevelMaker.instance) Overlay.Instance?.UpdateAttempts();
    }

    public override void OnGUI() {
        SettingGUI settingGUI = Main.SettingGUI;
        Localization localization = Localization.Current;
        settingGUI.AddSettingToggle(ref Settings.ShowAttempt, localization["attempt.showAttempt"]);
        settingGUI.AddSettingToggle(ref Settings.ShowFullAttempt, localization["attempt.showFullAttempt"]);
    }

    public override void OnDisable() {
        if (AttemptObject) AttemptObject.SetActive(false);
    }
}

public class AttemptSetting {
    public bool ShowAttempt = true;
    public bool ShowFullAttempt = true;
}