using UnityEngine;

namespace JipperResourcePackX.OverlayContents;

public class Judgement : Feature {
    public static GameObject JudgementObject;
    public static JudgementSettings Settings => JipperSettings.Instance.Judgement;
    public static Judgement Instance;

    public Judgement() : base(nameof(Judgement), true) {
        Instance = this;
    }

    public override void OnEnable() {
        if (JudgementObject) JudgementObject.SetActive(Settings.ShowJudgement);
    }
    
    public override void OnDisable() {
        if (JudgementObject) JudgementObject.SetActive(false);
    }

    public override void OnGUI() {
        SettingGUI settingGUI = Main.SettingGUI;
        settingGUI.AddSettingToggle(ref Settings.ShowJudgement, Localization.Current["judgement.showJudgement"], () => {
            if (JudgementObject) JudgementObject.SetActive(Settings.ShowJudgement);
            if (Settings.ShowJudgement) Overlay.Instance?.UpdateJudgement();
        });
        if (!Settings.ShowJudgement) return;
        settingGUI.AddSettingToggle(ref Settings.LocationUp, Localization.Current["judgement.locationUp"], () => Overlay.Instance?.OverlayTextManager?.SetupUnderTextLocation(Overlay.Instance));
    }

    public static void OnHit(object __instance) {
        try {
            if (!Settings.ShowJudgement) return;
            int index = 0;
            if (SafeAccess.IsCoopMode()) {
                if (__instance is scrMarginTracker marginTracker && scrMistakesManager.marginTrackers != null) {
                    for (int i = 0; i < scrMistakesManager.marginTrackers.Length; i++) {
                        if (scrMistakesManager.marginTrackers[i] != marginTracker) continue;
                        index = i;
                        break;
                    }
                }
            }
            Overlay.Instance?.UpdateJudgement(index);
        } catch { }
    }

    public static void OnDie(int ___playerID) {
        try {
            if (!Settings.ShowJudgement) return;
            Overlay.Instance?.UpdateJudgement(___playerID);
        } catch { }
    }
}

public class JudgementSettings {
    public bool ShowJudgement = true;
    public bool LocationUp;
}