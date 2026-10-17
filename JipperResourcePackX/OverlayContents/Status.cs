using System;
using System.Collections.Generic;
using JipperResourcePackX.SettingTool;
using UnityEngine;

namespace JipperResourcePackX.OverlayContents;

public class Status : Feature {
    public static StatusSetting Settings => JipperSettings.Instance.Status;
    public static GameObject ProgressObject;
    public static GameObject ProgressBarObject;
    public static Status Instance;
    private string _progressDecimalPlacesString;
    private string _accuracyDecimalPlacesString;
    private string _xAccuracyDecimalPlacesString;
    private string _bestDecimalPlacesString;
    private string _timingDecimalPlacesString;

    public Status() : base(nameof(Status), true) {
        Instance = this;
    }
    
    public override void OnEnable() {
        if (ProgressObject) ProgressObject.SetActive(true);
        if (scrLevelMaker.instance) Overlay.Instance?.SetupLocationMain();
        if (Settings.ShowProgressBar && ProgressBarObject) ProgressBarObject.SetActive(true);
    }
    
    public override void OnDisable() {
        if (ProgressObject) ProgressObject.SetActive(false);
        if (ProgressBarObject) ProgressBarObject.SetActive(false);
    }

    public override void OnGUI() {
        SettingGUI settingGUI = Main.SettingGUI;
        Localization localization = Localization.Current;
        settingGUI.AddSettingToggle(ref Settings.ShowProgress, localization["progress.showProgress"], () => Overlay.Instance?.SetupLocationMain());
        if (Settings.ShowProgress) {
            if (Settings.ProgressColor != null && Settings.ProgressColor.SettingGUI(settingGUI, localization["progress.progressColor"]))
                Overlay.Instance?.OverlayTextManager?.UpdateProgress(Overlay.Instance);
            settingGUI.AddSettingSliderInt(ref Settings.ProgressDecimalPlaces, 2, ref _progressDecimalPlacesString, localization["progress.progressDecimalPlaces"], 0, 4,
                () => Overlay.Instance?.OverlayTextManager?.UpdateProgress(Overlay.Instance));
        }
        settingGUI.AddSettingToggle(ref Settings.ShowAccuracy, localization["progress.showAccuracy"], () => Overlay.Instance?.SetupLocationMain());
        if (Settings.ShowAccuracy) {
            if (Settings.AccuracyColor != null && Settings.AccuracyColor.SettingGUI(settingGUI, localization["progress.accuracyColor"]))
                Overlay.Instance?.UpdateAccuracy();
            settingGUI.AddSettingSliderInt(ref Settings.AccuracyDecimalPlaces, 2, ref _accuracyDecimalPlacesString, localization["progress.accuracyDecimalPlaces"], 0, 4,
                () => Overlay.Instance?.UpdateAccuracy());
            settingGUI.AddSettingEnum(ref Settings.AccuracyTextType, localization["progress.accuracyTextType"], () => {
                Overlay.Instance?.SetupLocationMain();
                Overlay.Instance?.UpdateAccuracy();
            });
        }
        settingGUI.AddSettingToggle(ref Settings.ShowXAccuracy, localization["progress.showXAccuracy"], () => Overlay.Instance?.SetupLocationMain());
        if (Settings.ShowXAccuracy) {
            if (Settings.XAccuracyColor != null && Settings.XAccuracyColor.SettingGUI(settingGUI, localization["progress.xAccuracyColor"]))
                Overlay.Instance?.UpdateAccuracy();
            settingGUI.AddSettingSliderInt(ref Settings.XAccuracyDecimalPlaces, 2, ref _xAccuracyDecimalPlacesString, localization["progress.xAccuracyDecimalPlaces"], 0, 4,
                () => Overlay.Instance?.UpdateAccuracy());
            settingGUI.AddSettingEnum(ref Settings.XAccuracyTextType, localization["progress.xAccuracyTextType"], () => {
                Overlay.Instance?.SetupLocationMain();
                Overlay.Instance?.UpdateAccuracy();
            });
        }
        if (!SafeAccess.XScoreSupported) GUI.enabled = false;
        settingGUI.AddSettingToggle(ref Settings.ShowXScore, localization["progress.showXScore"], () => Overlay.Instance?.SetupLocationMain());
        if (SafeAccess.XScoreSupported) {
            if (Settings.ShowXScore) {
                if (Settings.XScoreColor != null && Settings.XScoreColor.SettingGUI(settingGUI, localization["progress.xScoreColor"]))
                    Overlay.Instance?.UpdateAccuracy();
                settingGUI.AddSettingEnum(ref Settings.XScoreTextType, localization["progress.xScoreTextType"], () => Overlay.Instance?.UpdateAccuracy());
                settingGUI.AddSettingEnum(ref Settings.XScorePotentialTextType, localization["progress.xScorePotentialTextType"], () => {
                    Overlay.Instance?.SetupLocationMain();
                    Overlay.Instance?.UpdateAccuracy();
                });
            }
        } else {
            GUI.enabled = true;
            GUILayout.Label(localization["progress.xScoreRequireR149"]);
        }
        settingGUI.AddSettingToggle(ref Settings.ShowMusicTime, localization["progress.showMusicTime"], () => Overlay.Instance?.SetupLocationMain());
        if (Settings.ShowMusicTime && Settings.MusicTimeColor != null && Settings.MusicTimeColor.SettingGUI(settingGUI, localization["progress.musicTimeColor"]))
            Overlay.Instance?.UpdateTime();
        settingGUI.AddSettingToggle(ref Settings.ShowMapTime, localization["progress.showMapTime"], () => Overlay.Instance?.SetupLocationMain());
        settingGUI.AddSettingToggle(ref Settings.ShowMapTimeIfNotMusic, localization["progress.showMapTimeIfNotMusic"], () => Overlay.Instance?.UpdateTime());
        if (Settings.ShowMapTime && Settings.MapTimeColor != null && Settings.MapTimeColor.SettingGUI(settingGUI, localization["progress.mapTimeColor"]))
            Overlay.Instance?.UpdateTime();
        settingGUI.AddSettingEnum(ref Settings.TimeTextType, localization["progress.timeTextType"], () => Overlay.Instance?.UpdateTime());
        settingGUI.AddSettingToggle(ref Settings.ShowCheckpoint, localization["progress.showCheckpoint"], () => Overlay.Instance?.SetupLocationMain());
        settingGUI.AddSettingToggle(ref Settings.ShowBest, localization["progress.showBest"], () => Overlay.Instance?.SetupLocationMain());
        if (Settings.ShowBest) {
            if (Settings.BestColor != null && Settings.BestColor.SettingGUI(settingGUI, localization["progress.bestColor"]))
                Overlay.Instance?.OverlayTextManager?.UpdateBest(Overlay.Instance);
            settingGUI.AddSettingSliderInt(ref Settings.BestDecimalPlaces, 2, ref _bestDecimalPlacesString, localization["progress.bestDecimalPlaces"], 0, 4,
                () => Overlay.Instance?.OverlayTextManager?.UpdateBest(Overlay.Instance));
        }
        settingGUI.AddSettingToggle(ref Settings.ShowTiming, localization["progress.showTiming"], () => Overlay.Instance?.SetupLocationMain());
        if (Settings.ShowTiming) {
            if (Settings.TimingColor != null && Settings.TimingColor.SettingGUI(settingGUI, localization["progress.timingColor"]))
                Overlay.Instance?.RefreshTiming();
            settingGUI.AddSettingSliderInt(ref Settings.TimingDecimalPlaces, 5, ref _timingDecimalPlacesString, localization["progress.timingDecimalPlaces"], 0, 5,
                () => Overlay.Instance?.RefreshTiming());
            settingGUI.AddSettingEnum(ref Settings.TimingTextType, localization["progress.timingTextType"], () => {
                Overlay.Instance?.SetupLocationMain();
                Overlay.Instance?.RefreshTiming();
            });
        }
        settingGUI.AddSettingToggle(ref Settings.ShowProgressBar, localization["progress.showProgressBar"], () => {
            ProgressBarObject?.SetActive(Settings.ShowProgressBar);
        });
        if (!Settings.ShowProgressBar) return;
        if ((Settings.ProgressBarColor != null && Settings.ProgressBarColor.SettingGUI(settingGUI, localization["progress.progressBarColor"])) ||
            (Settings.ProgressBarBackgroundColor != null && Settings.ProgressBarBackgroundColor.SettingGUI(settingGUI, localization["progress.progressBarBackgroundColor"])) ||
            (Settings.ProgressBarBorderColor != null && Settings.ProgressBarBorderColor.SettingGUI(settingGUI, localization["progress.progressBarBorderColor"])))
            Overlay.Instance?.UpdateProgressBar();
    }

    public static int WriteXScoreText(char[] buffer, int index, int xScore, int maxXScore) {
        index = Overlay.WriteNumber(buffer, index, xScore);
        switch (Settings.XScoreTextType) {
            case XScoreTextType.WithMax:
                buffer[index++] = '/';
                return Overlay.WriteNumber(buffer, index, maxXScore);
            case XScoreTextType.MaxMinus:
                index = Overlay.WriteText(buffer, " (MAX-", index);
                index = Overlay.WriteNumber(buffer, index, maxXScore - xScore);
                buffer[index++] = ')';
                return index;
            default:
                return index;
        }
    }

    public static float GetPotentialAccuracy(int[] hits, float acc, int seqId) {
        List<scrFloor> floors = ADOBase.lm.listFloors;
        int remaining = Math.Max(floors.Count - 1 - seqId, 0);
        (int perfect, int accurate) = SafeAccess.GetAccuracyCounts(hits);
        float rate = acc - perfect * 0.0001f;
        int count = accurate == 0 || rate <= 0 || float.IsNaN(rate) ? seqId : (int) Math.Round(accurate / rate);
        if (count < accurate) count = accurate;
        int total = count + remaining;
        return total == 0 ? 1 : (perfect + remaining) * 0.0001f + (float) (accurate + remaining) / total;
    }

    public static Color GetTimingColor(float timing) =>
        Settings.TimingColor?.GetColor(1 - Math.Min(Math.Abs(timing), 150) / 150) ?? Color.white;

    public static void OnAccuracyChange(object __instance) {
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
        SafeAccess.RunAfter(() => Overlay.Instance?.UpdateAccuracy(index));
    }

    public static void OnProgressChange(scrPlanet __instance) {
        Overlay.Instance?.UpdateProgress(__instance);
    }

    private static scrPlanet _planet;
    public static void SwitchChosenPrefix(scrPlanet __instance) => _planet = __instance;
    public static void SwitchChosenFinalizer() => _planet = null;

    public static void GetHitMarginInDegProxyR149(float hitAngle, float refAngle, bool clockwise, float floorBpm, float conductorPitch, HitMargin __result) {
        try {
            if (!IsTimingAvailable(_planet)) return;
            float angle = (hitAngle - refAngle) * (clockwise ? 1 : -1) * 57.29578f;
            float timing = angle / 180 / floorBpm / conductorPitch * 60000;
            Overlay.Instance?.UpdateTiming(timing, _planet?.player?.playerID ?? 0);
        } catch { }
    }

    public static void GetHitMarginInSecProxyR149(double timeDiff, HitMargin __result) {
        try {
            if (!IsTimingAvailable(_planet)) return;
            float timing = (float) timeDiff * 1000;
            Overlay.Instance?.UpdateTiming(timing, _planet?.player?.playerID ?? 0);
        } catch { }
    }

    private static bool IsTimingAvailable(scrPlanet planet) =>
        planet != null && Settings.ShowTiming && !RDC.auto 
        && (planet.player != null && !planet.player.auto) 
        && (planet.currfloor == null || !planet.currfloor.nextfloor || !planet.currfloor.nextfloor.auto);

    public static void OnShowIfDebugAwake(scrShowIfDebug __instance) {
        SafeAccess.RunAfter(() => {
            try {
                if (__instance) {
                    RectTransform transform = __instance.GetComponent<RectTransform>();
                    if (transform) transform.anchoredPosition = new Vector2(300, transform.anchoredPosition.y);
                }
            } catch (Exception e) {
                ModLogger.Error($"[Status/OnShowIfDebugAwake] {e.Message}");
            }
        });
    }
}

public class StatusSetting {
    public bool ShowProgress = true;
    public ColorPerDictionary ProgressColor = new([(0f, Color.white), (1f, new Color(0.8745098f, 0.7098039f, 1f))]);
    public int ProgressDecimalPlaces = 2;
    public bool ShowAccuracy;
    public ColorPerDictionary AccuracyColor = new([(0.98f, Color.magenta), (1f, Color.white)], new Color(1, 0.85490196f, 0));
    public int AccuracyDecimalPlaces = 2;
    public PotentialTextType AccuracyTextType = PotentialTextType.Current;
    public bool ShowXAccuracy = true;
    public ColorPerDictionary XAccuracyColor = new([(0.98f, Color.white), (1f, Color.white)], new Color(1, 0.85490196f, 0));
    public int XAccuracyDecimalPlaces = 2;
    public PotentialTextType XAccuracyTextType = PotentialTextType.Current;
    public bool ShowXScore;
    public ColorPerDictionary XScoreColor = new([(0.98f, Color.white), (1f, Color.white)], new Color(1, 0.85490196f, 0));
    public XScoreTextType XScoreTextType = XScoreTextType.MaxMinus;
    public PotentialTextType XScorePotentialTextType = PotentialTextType.Current;
    public bool ShowMusicTime = true;
    public ColorPerDictionary MusicTimeColor = new([(1f, Color.white)]);
    public bool ShowMapTime;
    public ColorPerDictionary MapTimeColor = new([(1f, Color.white)]);
    public bool ShowMapTimeIfNotMusic = true;
    public TimeTextType TimeTextType = TimeTextType.Korean;
    public bool ShowCheckpoint;
    public bool ShowBest;
    public ColorPerDictionary BestColor = new([(0f, Color.white), (1f, new Color(0.8745098f, 0.7098039f, 1f))]);
    public int BestDecimalPlaces = 2;
    public bool ShowTiming;
    public ColorPerDictionary TimingColor = new([(0f, Color.red), (0.5f, new Color(0.9882353f, 1f, 0.3019608f)), (1f, new Color(0.372549f, 1f, 0.3119608f))], new Color(1, 0.85490196f, 0));
    public int TimingDecimalPlaces = 5;
    public TimingTextType TimingTextType = TimingTextType.Timing;
    public bool ShowProgressBar = true;
    public ColorPerDictionary ProgressBarColor = new([(1f, new Color(0.9215686f, 0.8039216f, 0.9764706f))]);
    public ColorPerDictionary ProgressBarBackgroundColor = new([(1f, Color.white)]);
    public ColorPerDictionary ProgressBarBorderColor = new([(1f, Color.black)]);
}