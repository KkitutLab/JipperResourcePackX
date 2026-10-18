using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JipperResourcePackX.OverlayContents;

public class Overlay {
    public static readonly string[] TimePrefixTexts = [ "음악 시간", "Music Time", "맵 시간", "Map Time" ];
    public static Overlay Instance;
    public IOverlayTextManager OverlayTextManager;
    public readonly GameObject GameObject;
    public readonly Canvas Canvas;
    public TextMeshProUGUI ProgressText;
    public TextMeshProUGUI AccuracyText;
    public TextMeshProUGUI PotentialAccuracyText;
    public TextMeshProUGUI XAccuracyText;
    public TextMeshProUGUI PotentialXAccuracyText;
    public TextMeshProUGUI XScoreText;
    public TextMeshProUGUI PotentialXScoreText;
    public TextMeshProUGUI TimeText;
    public TextMeshProUGUI MapTimeText;
    public TextMeshProUGUI CheckpointText;
    public TextMeshProUGUI AttemptText;
    public TextMeshProUGUI BestText;
    public TextMeshProUGUI TimingText;
    public TextMeshProUGUI AvgTimingText;
    public RectTransform ComboTransform;
    public TextMeshProUGUI ComboTitle;
    public TextMeshProUGUI ComboText;
    public RectTransform ComboTextTransform;
    public TextMeshProUGUI BpmText;
    public TextMeshProUGUI JudgementText;
    public TextMeshProUGUI TimingScaleText;
    public ProgressBar ProgressBar;
    public Color PurePerfectColor = new(1, 0.8549019607843137f, 0);
    public int[] Hit;
    private readonly Shader _shader = typeof(ShaderUtilities).GetProperty("ShaderRef_MobileSDF", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as Shader;

    private int _lastTime = -1;
    private int _lastMapTime = -1;
    protected string MusicTimeCache;
    protected string MapTimeCache;
    protected float MapTotalTime = -1;

    public int StartTile;
    public int NoCheckStartTile;
    public int[] Checkpoints;
    protected float LastTileBpm = -1;
    protected float LastCurBpm = -1;
    private readonly Stopwatch _stopwatch;
    protected bool SongPlaying;
    public float StartProgress;
    public bool AutoOnceEnabled;
    protected bool IsDeath;
    private int[] _scorableTiles;
    public PlayCount.Hash LastHash;
    private float _lastSavedStartProgress = -1;
    public float LastMultiplier = 1f;
    private ComboTier _current;
    private readonly Action _updateComboLocationAction;

    public Overlay() {
        Instance = this;
        _updateComboLocationAction = UpdateComboLocation;
        OnChangePlayers();
        GameObject = new GameObject("JipperResourcePackX Overlay");
        Canvas = GameObject.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = GameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        _stopwatch = new Stopwatch();
        GameObject.SetActive(false);
        InitializeStatus();
        InitializeBpm();
        InitializeTimingScale();
        InitializeJudgement();
        InitializeCombo();
        InitializeProgressBar();
        InitializeAttempt();
        UpdateSize();
        Object.DontDestroyOnLoad(GameObject);
        if(ADOBase.controller is { paused: false } && ADOBase.conductor is { isGameWorld: true }) Show(0);
    }

    public void OnChangePlayers() {
        Hit = SafeAccess.GetHitMarginsCount();
        SetupTextManager();
        if(MainThread.IsMainThread() && TimingScaleText) OverlayTextManager.SetupUnderTextLocation(this);
    }

    protected virtual void SetupTextManager() {
        OverlayTextManager = SafeAccess.IsCoopMode()
            ? new OverlayTextManagerCoop(this)
            : new OverlayTextManagerNormal();
    }

    protected virtual void InitializeStatus() {
        GameObject gameObject = new("Main");
        RectTransform transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(Canvas.transform);
        gameObject.SetActive(false);
        Status.ProgressObject = gameObject;
        transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0, 1);
        transform.anchoredPosition = new Vector2(16, -16);
        transform.sizeDelta = new Vector2(456, 100);
        SetupMainText("Progress", ref ProgressText);
        SetupMainText("Accuracy", ref AccuracyText);
        SetupMainText("PotentialAccuracy", ref PotentialAccuracyText);
        SetupMainText("XAccuracy", ref XAccuracyText);
        SetupMainText("PotentialXAccuracy", ref PotentialXAccuracyText);
        SetupMainText("XScore", ref XScoreText);
        SetupMainText("PotentialXScore", ref PotentialXScoreText);
        SetupMainText("MusicTime", ref TimeText);
        SetupMainText("MapTime", ref MapTimeText);
        SetupMainText("Checkpoint", ref CheckpointText);
        SetupMainText("Best", ref BestText);
        SetupMainText("Timing", ref TimingText);
        SetupMainText("AvgTiming", ref AvgTimingText);
    }

    protected void SetupMainText(string name, ref TextMeshProUGUI text) {
        GameObject gameObject2 = new(name);
        RectTransform transform = gameObject2.AddComponent<RectTransform>();
        transform.SetParent(Status.ProgressObject.transform);
        transform.anchorMin = transform.anchorMax = new Vector2(0, 1);
        transform.sizeDelta = new Vector2(456, 30);
        text = gameObject2.AddComponent<TextMeshProUGUI>();
        text.font = AssetLoader.FontAsset;
        text.fontSize = 25;
        SetupShadow(text);
    }

    public virtual void SetupLocationMain() {
        if(!GameObject.activeSelf) return;
        int y = -15;
        SetupLocationMainText(ProgressText, Status.Settings.ShowProgress, ref y);
        SetupLocationMainText(AccuracyText, Status.Settings.ShowAccuracy && Status.Settings.AccuracyTextType != PotentialTextType.Potential, ref y);
        SetupLocationMainText(PotentialAccuracyText, Status.Settings.ShowAccuracy && Status.Settings.AccuracyTextType is PotentialTextType.Potential or PotentialTextType.Both, ref y);
        SetupLocationMainText(XAccuracyText, Status.Settings.ShowXAccuracy && Status.Settings.XAccuracyTextType != PotentialTextType.Potential, ref y);
        SetupLocationMainText(PotentialXAccuracyText, Status.Settings.ShowXAccuracy && Status.Settings.XAccuracyTextType is PotentialTextType.Potential or PotentialTextType.Both, ref y);
        SetupLocationMainText(XScoreText, Status.Settings.ShowXScore && SafeAccess.XScoreSupported && Status.Settings.XScorePotentialTextType != PotentialTextType.Potential, ref y);
        SetupLocationMainText(PotentialXScoreText, Status.Settings.ShowXScore && SafeAccess.XScoreSupported && Status.Settings.XScorePotentialTextType is PotentialTextType.Potential or PotentialTextType.Both, ref y);
        SetupLocationMainText(TimeText, Status.Settings.ShowMusicTime, ref y);
        SetupLocationMainText(MapTimeText, Status.Settings.ShowMapTime, ref y);
        SetupLocationMainText(CheckpointText,
            Status.Settings.ShowCheckpoint &&
            (Checkpoints ??= scrLevelMaker.instance.listFloors.Where(floor => floor.GetComponent<ffxCheckpoint>())
                 .Select(floor => floor.seqID).ToArray()).Length > 0, ref y);
        SetupLocationMainText(BestText, Status.Settings.ShowBest, ref y);
        SetupLocationMainText(TimingText, Status.Settings.ShowTiming && Status.Settings.TimingTextType != TimingTextType.AvgTiming, ref y);
        SetupLocationMainText(AvgTimingText, Status.Settings.ShowTiming && Status.Settings.TimingTextType is TimingTextType.AvgTiming or TimingTextType.Both, ref y);
        UpdateProgress();
        SafeAccess.CalculatePercentAcc(); // UpdateAccuracy();
        UpdateTime();
        RefreshTiming();
    }

    protected static void SetupLocationMainText(TextMeshProUGUI text, bool enabled, ref int y) {
        text.enabled = enabled;
        if(!enabled) return;
        text.rectTransform.anchoredPosition = new Vector2(228, y);
        y -= 35;
    }

    private void InitializeBpm() {
        GameObject gameObject = new("BPM");
        RectTransform transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(Canvas.transform);
        transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(1, 1);
        transform.anchoredPosition = new Vector2(-16, -16);
        transform.sizeDelta = new Vector2(456, 90);
        BpmText = gameObject.AddComponent<TextMeshProUGUI>();
        BpmText.font = AssetLoader.FontAsset;
        BpmText.alignment = TextAlignmentOptions.TopRight;
        BpmText.lineSpacing = 30;
        BpmText.fontSize = 25;
        SetupShadow(BpmText);
        gameObject.SetActive(false);
        Bpm.BpmObject = gameObject;
    }

    private void InitializeJudgement() {
        GameObject gameObject = new("Judgement");
        RectTransform transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(Canvas.transform);
        transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 0);
        transform.sizeDelta = new Vector2(1000, 30);
        JudgementText = gameObject.AddComponent<TextMeshProUGUI>();
        JudgementText.font = AssetLoader.FontAsset;
        JudgementText.fontSize = 25;
        JudgementText.alignment = TextAlignmentOptions.Bottom;
        JudgementText.color = new Color(0.8509804f, 0.345098f, 1);
        SetupShadow(JudgementText);
        gameObject.SetActive(false);
        Judgement.JudgementObject = gameObject;
    }

    private void InitializeCombo() {
        GameObject gameObject = new("Combo");
        RectTransform transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(Canvas.transform);
        transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 1);
        transform.sizeDelta = new Vector2(300, 200);
        Combo.ComboTransform = transform;
        GameObject gameObject2 = new("ComboTitle");
        transform = gameObject2.AddComponent<RectTransform>();
        transform.SetParent(Combo.ComboTransform);
        transform.anchorMin = transform.anchorMax = new Vector2(0.5f, 0.45f);
        transform.pivot = new Vector2(0.5f, 0);
        ComboTitle = gameObject2.AddComponent<TextMeshProUGUI>();
        ComboTitle.font = AssetLoader.FontAsset;
        ComboTitle.fontSize = 40;
        ComboTitle.text = "Perfect";
        ComboTitle.alignment = TextAlignmentOptions.Center;
        ContentSizeFitter fitter = gameObject2.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        SetupDarkShadow(ComboTitle);
        ComboTransform = transform;
        gameObject2 = new GameObject("ComboValue");
        transform = gameObject2.AddComponent<RectTransform>();
        transform.SetParent(Combo.ComboTransform);
        transform.anchorMin = transform.anchorMax = new Vector2(0.5f, 0.45f);
        transform.anchoredPosition = Vector2.zero;
        ComboTextTransform = transform;
        ComboText = gameObject2.AddComponent<TextMeshProUGUI>();
        ComboText.font = AssetLoader.FontAsset;
        ComboText.fontSize = 108;
        ComboText.alignment = TextAlignmentOptions.Top;
        fitter = gameObject2.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        SetupDarkShadow(ComboText);
        gameObject.SetActive(false);
        Combo.ComboObject = gameObject;
    }

    private void InitializeProgressBar() {
        GameObject gameObject = Object.Instantiate(AssetLoader.ProgressBarPrefab);
        RectTransform transform = gameObject.GetComponent<RectTransform>();
        transform.SetParent(Canvas.transform);
        transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 1);
        transform.anchoredPosition = new Vector2(0, -10);
        transform.sizeDelta = new Vector2(642, 18);
        ProgressBar = new ProgressBar(transform);
        gameObject.SetActive(false);
        Status.ProgressBarObject = gameObject;
    }

    private void InitializeTimingScale() {
        GameObject gameObject = new("TimingScale");
        RectTransform transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(Canvas.transform);
        transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 0);
        transform.sizeDelta = new Vector2(300, 30);
        TimingScaleText = gameObject.AddComponent<TextMeshProUGUI>();
        TimingScaleText.font = AssetLoader.FontAsset;
        TimingScaleText.fontSize = 20;
        TimingScaleText.alignment = TextAlignmentOptions.Bottom;
        SetupShadow(TimingScaleText);
        gameObject.SetActive(false);
        TimingScale.TimingScaleObject = gameObject;
    }

    private void InitializeAttempt() {
        GameObject gameObject = new("Attempt");
        RectTransform transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(Canvas.transform);
        transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 0);
        transform.anchoredPosition = new Vector2(310, 35);
        transform.sizeDelta = new Vector2(300, 30);
        AttemptText = gameObject.AddComponent<TextMeshProUGUI>();
        AttemptText.font = AssetLoader.FontAsset;
        AttemptText.fontSize = 25;
        AttemptText.alignment = TextAlignmentOptions.BottomLeft;
        SetupShadow(AttemptText);
        gameObject.SetActive(false);
        Attempt.AttemptObject = gameObject;
    }

    public void UpdateSize() {
        Transform transform = GameObject.transform;
        int count = transform.childCount;
        float size = JipperSettings.Instance.Size;
        Vector3 scale = new(size, size, 1);
        for(int i = 0; i < count; i++) transform.GetChild(i).localScale = scale;
        RectTransform txtLevelName = ADOBase.controller?.txtLevelName?.GetComponent<RectTransform>();
        if(txtLevelName) {
            txtLevelName.anchoredPosition = new Vector2(0, -20 - 7 * size);
            txtLevelName.localScale = new Vector3(0.5f * size, 0.5f * size);
        }
        Combo.ComboTransform.anchoredPosition = new Vector2(0, -43 - 14 * size);
        OverlayTextManager.SetupUnderTextLocation(this);
    }

    public virtual void UpdateFont() {
        ProgressText.font = AssetLoader.FontAsset;
        AccuracyText.font = AssetLoader.FontAsset;
        PotentialAccuracyText.font = AssetLoader.FontAsset;
        XAccuracyText.font = AssetLoader.FontAsset;
        PotentialXAccuracyText.font = AssetLoader.FontAsset;
        XScoreText.font = AssetLoader.FontAsset;
        PotentialXScoreText.font = AssetLoader.FontAsset;
        TimeText.font = AssetLoader.FontAsset;
        MapTimeText.font = AssetLoader.FontAsset;
        CheckpointText.font = AssetLoader.FontAsset;
        BestText.font = AssetLoader.FontAsset;
        TimingText.font = AssetLoader.FontAsset;
        AvgTimingText.font = AssetLoader.FontAsset;
        BpmText.font = AssetLoader.FontAsset;
        JudgementText.font = AssetLoader.FontAsset;
        ComboTitle.font = AssetLoader.FontAsset;
        ComboText.font = AssetLoader.FontAsset;
        TimingScaleText.font = AssetLoader.FontAsset;
        AttemptText.font = AssetLoader.FontAsset;
    }

    private void SetupShadow(TextMeshProUGUI text) => Shadow(text, 0.5f);

    private void SetupDarkShadow(TextMeshProUGUI text) => Shadow(text, 0.7f);

    private void Shadow(TextMeshProUGUI text, float a) {
        Task.Yield().GetAwaiter().OnCompleted(() => {
            try {
                Material baseMaterial = text.fontSharedMaterial ?? text.fontMaterial;
                Material material = new(baseMaterial);
                if(_shader) material.shader = _shader;
                material.EnableKeyword(ShaderUtilities.Keyword_Outline);
                material.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.01f);
                material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, a));
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 1f);
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -1f);
                material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
                material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
                text.fontSharedMaterial = material;
            } catch (Exception e) {
                ModLogger.Error($"[Overlay/Shadow] {e.Message}");
            }
        });
    }

    public void UpdateAccuracy(int index = -1) {
        if(!GameObject.activeSelf) return;
        OverlayTextManager.UpdateAccuracy(this, index);
    }
    
    public virtual void UpdateProgress(scrPlanet planet = null) {
        if(!GameObject.activeSelf) return;
        OverlayTextManager.CacheProgress(planet);
        if(Status.Settings.ShowProgress) OverlayTextManager.UpdateProgress(this);
        if(Status.Settings.ShowCheckpoint) UpdateCheckPointText();
        if(Status.Settings.ShowProgressBar) UpdateProgressBar();
        if(Status.Settings.ShowBest) OverlayTextManager.UpdateBest(this);
    }

    public void UpdateProgressBar() {
        try {
            if(!ProgressBar.LineTransform) return;
            OverlayTextManager.UpdateProgressBar(this);
        } catch (Exception e) {
            ModLogger.Error($"[Overlay/UpdateProgressBar] {e.Message}");
        }
    }

    private void UpdateCheckPointText() {
        if(Checkpoints.Length == 0) return;
        OverlayTextManager.UpdateCheckpoint(this);
    }

    public void UpdateAttempts() {
        char[] buffer = Main.SharedBuffer;
        int index = 0;

        if(Attempt.Settings.ShowAttempt) {
            index = WriteText(buffer, "Attempt ", index);
            index = WriteNumber(buffer, index, PlayCount.GetData(LastHash)?.GetAttempts(StartProgress, LastMultiplier) ?? 0);
            buffer[index++] = '\n';
        }
        if(Attempt.Settings.ShowFullAttempt) {
            index = WriteText(buffer, "Full Attempt ", index);
            index = WriteNumber(buffer, index, PlayCount.GetData(LastHash)?.GetAttempts() ?? 0);
            buffer[index++] = '\n';
        }

        AttemptText.SetCharArray(buffer, 0, index > 0 ? index - 1 : 0);
    }

    public void UpdateJudgement(int index = -1) {
        if(!GameObject.activeSelf || !Judgement.Settings.ShowJudgement) return;
        OverlayTextManager.UpdateJudgement(this, index);
    }
    
    public virtual void UpdateTime() {
        try {
            if(!GameObject.activeSelf || !Status.Instance.Enabled || IsDeath) return;
            bool requireMusicToMap = false;
            if(Status.Settings.ShowMusicTime) {
                AudioSource song = scrConductor.instance?.song;
                if(!song?.clip && Status.Settings.ShowMapTimeIfNotMusic) requireMusicToMap = true;
                else if (song != null) {
                    float time = song.time;
                    float totalTime = song.clip != null ? song.clip.length : 0;
                    if(time == 0 && SongPlaying) time = totalTime;
                    else if(time > 0) SongPlaying = true;
                    if(_lastTime != (int) time) {
                        MusicTimeCache ??= GetTimeString(totalTime);
                        int length = WriteTimeText(time, totalTime, false, MusicTimeCache);
                        TimeText.SetCharArray(Main.SharedBuffer, 0, length);
                        _lastTime = (int) time;
                        TimeText.color = Status.Settings.MusicTimeColor.GetColor(totalTime > 0 ? time / totalTime : 0);
                    }
                }
            }
            if(Status.Settings.ShowMapTime || requireMusicToMap) {
                if (scrController.instance == null || scrConductor.instance == null) return;
                float time = scrController.instance.state == States.Start ? 0 : (float) (scrConductor.instance.addoffset + scrConductor.instance.songposition_minusi);
                float totalTime = GetMapTotalTime();
                if(time < 0) time = 0;
                else if(time > totalTime) time = totalTime;
                if((!Status.Settings.ShowMapTime || _lastMapTime == (int) time) &&
                   (!requireMusicToMap || _lastTime == (int) time)) return;
                MapTimeCache ??= GetTimeString(totalTime);
                int length = WriteTimeText(time, totalTime, true, MapTimeCache);
                if(Status.Settings.ShowMapTime) {
                    MapTimeText.SetCharArray(Main.SharedBuffer, 0, length);
                    _lastMapTime = (int) time;
                    MapTimeText.color = Status.Settings.MapTimeColor.GetColor(totalTime > 0 ? time / totalTime : 0);
                }
                if(requireMusicToMap) {
                    TimeText.SetCharArray(Main.SharedBuffer, 0, length);
                    _lastTime = (int) time;
                    TimeText.color = Status.Settings.MusicTimeColor.GetColor(totalTime > 0 ? time / totalTime : 0);
                }
            }
        } catch (Exception ex) {
            ModLogger.Error($"[Overlay/UpdateTime] {ex.Message}");
        }
    }
    
    public int GetRemainingTiles(int seqID) {
        List<scrFloor> floors = ADOBase.lm?.listFloors;
        if (floors == null || floors.Count == 0) return 0;
        int last = floors.Count - 1;
        if(seqID >= last) return 0;
        if(!SafeAccess.XScoreSupported) return last - seqID;
        int[] scorableTiles = _scorableTiles ??= BuildScorableTiles(floors);
        return scorableTiles[last] - scorableTiles[seqID];
    }

    private static int[] BuildScorableTiles(List<scrFloor> floors) {
        int[] scorableTiles = new int[floors.Count];
        int count = 0;
        for(int i = 1; i < floors.Count; i++) {
            scrFloor floor = floors[i];
            if(floor != null && !floor.midSpin && !floor.auto) count++;
            scorableTiles[i] = count;
        }
        return scorableTiles;
    }

    protected float GetMapTotalTime() {
        try {
            if(MapTotalTime == -1) {
                var floors = scrLevelMaker.instance?.listFloors;
                MapTotalTime = floors != null && floors.Count > 0 ? (float) floors[^1].entryTime : 0;
            }
        } catch {
            MapTotalTime = 0;
        }
        return MapTotalTime;
    }

    private static string GetTimeString(float time) {
        int timeInt = (int) time;
        StringBuilder sb = SafeAccess.GetSharedBuilder();

        if(timeInt >= 3600) sb.Append(timeInt / 3600).Append(':').Append(timeInt % 3600 / 60);
        else sb.Append(timeInt / 60);

        sb.Append(':').Append(timeInt % 60);
        return sb.ToString();
    }

    private static int WriteTimeText(float time, float totalTime, bool isMap, string timeCache) {
        bool hour = totalTime >= 3600;
        bool isEnglish = Status.Settings.TimeTextType == TimeTextType.English;
        int requirePrefix = (isMap ? 2 : 0) | (isEnglish ? 1 : 0);
        char[] buffer = Main.SharedBuffer;

        int index = WriteText(buffer, "<color=white>", 0);
        index = WriteText(buffer, TimePrefixTexts[requirePrefix], index);
        index = WriteText(buffer, " |</color> ", index);

        index = WriteTime(buffer, index, (int) time, hour);
        buffer[index++] = '~';
        return WriteText(buffer, timeCache, index);
    }

    public static int WriteText(char[] buffer, string value, int index) {
        value.CopyTo(0, buffer, index, value.Length);
        return index + value.Length;
    }

    public static int WriteBuilder(char[] buffer, StringBuilder sb, int index) {
        sb.CopyTo(0, buffer, index, sb.Length);
        return index + sb.Length;
    }

    public static int WriteLabel(char[] buffer, string label) {
        int index = WriteText(buffer, "<color=white>", 0);
        index = WriteText(buffer, label, index);
        return WriteText(buffer, " |</color> ", index);
    }

    public static int WritePercent(char[] buffer, int index, double value, int decimals) {
        index = WriteRounded(buffer, index, value, decimals);
        buffer[index++] = '%';
        return index;
    }

    private static int WriteTime(char[] buffer, int index, int time, bool hour) {
        if(hour) {
            index = WriteNumber(buffer, index, time / 3600);
            buffer[index++] = ':';
            index = WriteTwoDigits(buffer, index, time % 3600 / 60);
        } else index = WriteNumber(buffer, index, time / 60);
        buffer[index++] = ':';
        return WriteTwoDigits(buffer, index, time % 60);
    }

    public static int WriteNumber(char[] buffer, int index, int value) {
        if(value < 0) {
            buffer[index++] = '-';
            value = -value;
        }
        int end = index + 1;
        for(int i = value / 10; i > 0; i /= 10) end++;
        for(int i = end - 1; i >= index; i--) {
            buffer[i] = (char) ('0' + value % 10);
            value /= 10;
        }
        return end;
    }

    public static int WriteTwoDigits(char[] buffer, int index, int value) {
        buffer[index] = (char) ('0' + value / 10);
        buffer[index + 1] = (char) ('0' + value % 10);
        return index + 2;
    }

    public static int WriteRounded(char[] buffer, int index, double value, int decimals) {
        if(double.IsNaN(value)) return WriteText(buffer, "NaN", index);
        if(value < 0) {
            buffer[index++] = '-';
            value = -value;
        }
        if(double.IsInfinity(value)) return WriteText(buffer, "Infinity", index);
        long scale = 1;
        for(int i = 0; i < decimals; i++) scale *= 10;
        long scaled = (long) Math.Round(value * scale);
        index = WriteNumber(buffer, index, (int) (scaled / scale));
        long fraction = scaled % scale;
        if(fraction == 0) return index;
        buffer[index++] = '.';
        int end = index + decimals;
        for(int i = end - 1; i >= index; i--) {
            buffer[i] = (char) ('0' + fraction % 10);
            fraction /= 10;
        }
        while(buffer[end - 1] == '0') end--;
        return end;
    }

    public void UpdateCombo(int combo, bool bump) {
        if(!GameObject.activeSelf || !Combo.Settings.ShowCombo) return;
        ComboText.SetCharArray(Main.SharedBuffer, 0, WriteNumber(Main.SharedBuffer, 0, combo));
        ComboText.color = UpdateComboColor(combo);
        if(bump) {
            _stopwatch.Restart();
            UpdateComboSize();
        } else {
            _stopwatch.Stop();
            ComboText.fontSize = 78;
            ComboTransform.anchoredPosition = new Vector2(0, 43.505f);
        }
    }

    public virtual Color UpdateComboColor(int combo) {
        if(combo > Combo.Settings.ComboColorMax) combo = Combo.Settings.ComboColorMax;
        return Combo.Settings.ComboColor.GetColor((float) combo / Combo.Settings.ComboColorMax);
    }

    public void UpdateComboSize() {
        if(!_stopwatch.IsRunning || !GameObject.activeSelf) return;
        double t = _stopwatch.Elapsed.TotalMilliseconds / 500;
        if(t > 1) {
            t = 1;
            _stopwatch.Stop();
        }
        ComboText.fontSize = 30 * OutExpoChange(t) + 78;
        Task.Yield().GetAwaiter().OnCompleted(_updateComboLocationAction);
    }

    private void UpdateComboLocation() {
        try {
            ComboTransform.anchoredPosition = new Vector2(0, ComboTextTransform.sizeDelta.y / 2);
        } catch (Exception e) {
            ModLogger.Error($"[Overlay/UpdateComboLocation] {e.Message}");
        }
    }
    
    // ReSharper disable once CompareOfFloatsByEqualityOperator
    private static float OutExpoChange(double t) => (float) (t == 1 ? 0 : Math.Pow(2, -10 * t));

    public virtual void UpdateBpm() {
        try {
            if(!GameObject.activeSelf || !Bpm.Settings.ShowBpm) return;
            scrFloor floor = scrController.instance?.currFloor ?? scrController.instance?.firstFloor;
            scrConductor conductor = scrConductor.instance;
            if (conductor?.song == null) return;
            float bpm = (float) (conductor.bpm * conductor.song.pitch * SafeAccess.GetPlanetSpeed(scrController.instance));
            float cbpm = floor != null && floor.nextfloor ? (float) (60.0 / (floor.nextfloor.entryTime - floor.entryTime) * conductor.song.pitch) : bpm;
            float kps = cbpm / 60;
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if(LastTileBpm == bpm && LastCurBpm == cbpm) return;
            char[] buffer = Main.SharedBuffer;
            int decimals = Bpm.Settings.DecimalPlaces;
            int index = WriteText(buffer, "<color=white>TBPM | <color=#", 0);
            index = WriteColorHex(buffer, index, Bpm.Settings.BpmColor.GetColor(bpm / Bpm.Settings.BpmColorMax));
            buffer[index++] = '>';
            index = WriteRounded(buffer, index, bpm, decimals);
            index = WriteText(buffer, "</color>\nCBPM |</color> ", index);
            index = WriteRounded(buffer, index, cbpm, decimals);
            index = WriteText(buffer, "\n<color=white>KPS |</color> ", index);
            index = WriteRounded(buffer, index, kps, decimals);
            BpmText.SetCharArray(buffer, 0, index);
            if(LastCurBpm != cbpm) BpmText.color = Bpm.Settings.BpmColor.GetColor(cbpm / Bpm.Settings.BpmColorMax);
            // ReSharper restore CompareOfFloatsByEqualityOperator
            LastTileBpm = bpm;
            LastCurBpm = cbpm;
        } catch (Exception ex) {
            ModLogger.Error($"[Overlay/UpdateBpm] {ex.Message}");
        }
    }

    private const string HexDigits = "0123456789ABCDEF";

    public static string ColorToHex(Color color) {
        char[] chars = new char[8];
        return new string(chars, 0, WriteColorHex(chars, 0, color));
    }

    public static int WriteColorHex(char[] buffer, int index, Color color) {
        WriteHexByte(buffer, index, Mathf.RoundToInt(color.r * 255));
        WriteHexByte(buffer, index + 2, Mathf.RoundToInt(color.g * 255));
        WriteHexByte(buffer, index + 4, Mathf.RoundToInt(color.b * 255));
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if(color.a == 1) return index + 6;
        WriteHexByte(buffer, index + 6, Mathf.RoundToInt(color.a * 255));
        return index + 8;
    }

    private static void WriteHexByte(char[] chars, int index, int value) {
        if(value < 0) value = 0;
        else if(value > 255) value = 255;
        chars[index] = HexDigits[value >> 4];
        chars[index + 1] = HexDigits[value & 0xF];
    }

    public void UpdateTiming(float timing, int player = -1) {
        if(!Status.Settings.ShowTiming || !GameObject.activeSelf) return;
        OverlayTextManager?.UpdateTiming(this, timing, player);
    }

    public void RefreshTiming() {
        if(!GameObject.activeSelf || OverlayTextManager == null) return;
        OverlayTextManager.RefreshTiming(this);
    }

    public void UpdateTimingScale() {
        try {
            if(!GameObject.activeSelf || !TimingScale.Settings.ShowTimingScale) return;
            var currFloor = scrController.instance?.currFloor;
            if (currFloor == null) return;
            char[] buffer = Main.SharedBuffer;
            int index = WriteText(buffer, "Timing Scale - ", 0);
            index = WriteRounded(buffer, index, currFloor.marginScale * 100, 2);
            buffer[index++] = '%';
            TimingScaleText.SetCharArray(buffer, 0, index);
        } catch (Exception ex) {
            ModLogger.Error($"[Overlay/UpdateTimingScale] {ex.Message}");
        }
    }

    public void ChangeComboText(ComboTier tier) {
        if(_current >= tier) return;
        ComboTitle.text = tier == ComboTier.Green ? "Perfect" : "Combo";
        _current = tier;
    }
    
    public virtual void Show(int floor) {
        try {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if(_lastSavedStartProgress != -1) {
                if(!AutoOnceEnabled) PlayCount.SetBest(LastHash, _lastSavedStartProgress, OverlayTextManager?.GetProgress() ?? 0, LastMultiplier);
                _lastSavedStartProgress = -1;
            }

            if(scrController.checkpointsUsed == 0) {
                if(SafeAccess.ReleaseNumber >= 149) {
                    _current = ComboTier.White;
                    ComboTitle.text = "XPerfect";
                } else {
                    _current = ComboTier.Green;
                    ComboTitle.text = "Perfect";
                }
            }
            
            PlayCount.Hash hash = PlayCount.GetMapHash();
            if(LastHash != hash) {
                LastHash = hash;
                Checkpoints = null;
                MapTimeCache = null;
                MapTotalTime = -1;
                _scorableTiles = null;
            }
            MusicTimeCache = null;
            
            if(scnEditor.instance) {
                if(scrController.checkpointsUsed == 0) NoCheckStartTile = floor;
            } else if(!GCS.practiceMode) {
                NoCheckStartTile = 0;
            } else {
                NoCheckStartTile = floor;
            }
            
            AutoOnceEnabled = RDC.auto || (ADOBase.controller != null && ADOBase.controller.noFail);
            StartTile = floor;
            int floorCount = ADOBase.lm?.listFloors?.Count ?? 1;
            if (floorCount <= 0) floorCount = 1;
            _lastSavedStartProgress = StartProgress = (float) floor / floorCount;
            float pitch = ADOBase.conductor?.song != null ? ADOBase.conductor.song.pitch : 1f;
            LastMultiplier = (float) (pitch * SafeAccess.GetPlanetSpeed(scrController.instance));
            if(!AutoOnceEnabled) PlayCount.AddAttempts(LastHash, StartProgress, LastMultiplier);
            SetupTextManager();
            
            GameObject.SetActive(true);
            SongPlaying = false;
            IsDeath = false;
            
            if(Status.Instance.Enabled) SetupLocationMain();
            if(Judgement.Instance.Enabled && Judgement.Settings.ShowJudgement) {
                if (Judgement.JudgementObject) Judgement.JudgementObject.SetActive(true);
                UpdateJudgement();
            } else if (Judgement.JudgementObject) {
                Judgement.JudgementObject.SetActive(false);
            }
            if(Combo.Instance.Enabled && Combo.Settings.ShowCombo) {
                if (Combo.ComboObject) Combo.ComboObject.SetActive(true);
                UpdateCombo(0, false);
            } else if (Combo.ComboObject) {
                Combo.ComboObject.SetActive(false);
            }
            if(Bpm.Instance.Enabled && Bpm.Settings.ShowBpm) {
                if (Bpm.BpmObject) Bpm.BpmObject.SetActive(true);
                UpdateBpm();
            } else if (Bpm.BpmObject) {
                Bpm.BpmObject.SetActive(false);
            }
            if(TimingScale.Instance.Enabled && TimingScale.Settings.ShowTimingScale) {
                if (TimingScale.TimingScaleObject) TimingScale.TimingScaleObject.SetActive(true);
                UpdateTimingScale();
            } else if (TimingScale.TimingScaleObject) {
                TimingScale.TimingScaleObject.SetActive(false);
            }
            if(Attempt.Instance.Enabled && (Attempt.Settings.ShowAttempt || Attempt.Settings.ShowFullAttempt)) {
                if (Attempt.AttemptObject) Attempt.AttemptObject.SetActive(true);
                UpdateAttempts();
            } else if (Attempt.AttemptObject) {
                Attempt.AttemptObject.SetActive(false);
            }
            Combo.ComboCount = 0;
        } catch (Exception ex) {
            ModLogger.Error($"[Overlay/Show] {ex.Message}");
        }
    }

    public void Death() {
        try {
            IsDeath = true;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if(AutoOnceEnabled || _lastSavedStartProgress == -1 || OverlayTextManager == null) return;
            PlayCount.SetBest(LastHash, _lastSavedStartProgress, OverlayTextManager.GetProgress(), LastMultiplier);
            _lastSavedStartProgress = -1;
            OverlayTextManager.SetBest(OverlayTextManager.GetProgress());
        } catch (Exception ex) {
            ModLogger.Error($"[Overlay/Death] {ex.Message}");
        }
    }

    public void Clear() {
        try {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if(AutoOnceEnabled || _lastSavedStartProgress == -1 || OverlayTextManager == null) return;
            PlayCount.SetBest(LastHash, _lastSavedStartProgress, 1, LastMultiplier);
            _lastSavedStartProgress = -1;
            OverlayTextManager.SetBest(1);
        } catch (Exception ex) {
            ModLogger.Error($"[Overlay/Clear] {ex.Message}");
        }
    }
    
    public void Hide() {
        if((object) GameObject == null || !GameObject.activeSelf) return;
        GameObject.SetActive(false);
        try {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if(!AutoOnceEnabled && _lastSavedStartProgress != -1) {
                PlayCount.SetBest(LastHash, _lastSavedStartProgress, OverlayTextManager.GetProgress(), LastMultiplier);
                _lastSavedStartProgress = -1;
            }
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if(StartProgress == OverlayTextManager.GetProgress() && !AutoOnceEnabled) PlayCount.RemoveAttempts(LastHash, StartProgress, LastMultiplier);
        } catch (Exception e) {
            ModLogger.Error($"[Overlay/Hide] {e.Message}");
        }
        StartProgress = StartTile = NoCheckStartTile = -1;
        OverlayTextManager = null;
    }

    public void Destroy() {
        try {
            if (GameObject != null) Object.Destroy(GameObject);
        } catch { }
        Status.ProgressObject = null;
        Status.ProgressBarObject = null;
        Bpm.BpmObject = null;
        Combo.ComboObject = null;
        Judgement.JudgementObject = null;
        TimingScale.TimingScaleObject = null;
        Attempt.AttemptObject = null;
        Instance = null;
        GC.SuppressFinalize(this);
    }
}
