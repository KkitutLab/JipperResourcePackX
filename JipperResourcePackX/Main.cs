using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using JipperResourcePackX.KeyViewerContents;
using JipperResourcePackX.OverlayContents;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if UMM
using UnityModManagerNet;
#endif

namespace JipperResourcePackX;

public static class Main {
    public static string ModPath { get; set; } = string.Empty;
    public static bool Enabled { get; private set; }
    public static SettingGUI SettingGUI => SettingGUI.Instance;
    public static JipperSettings Settings => JipperSettings.Instance;
    public static readonly char[] SharedBuffer = new char[512];
    public static readonly List<Feature> Features = new();

    private static HarmonyLib.Harmony _harmony;
    private static GameObject _runnerGo;
    private static Coroutine _initCoroutine;
    private static bool _fullyInitialized;
    private static readonly object _initLock = new();
    private static string _sizeString;
    private static bool _fontListExpanded;
    private static string _fontSearch;
    private static Vector2 _fontScrollPosition;
    private static string[] _availableFonts;
    private static string[] _availableFontsPath;
    private static GUIStyle _toggleStyle;
    private static int _selectedTab;
    private static readonly string[] Tabs = ["Overlay", "KeyViewer", "Resource", "All Color", "Credits"];

#if UMM
    public static bool Load(UnityModManager.ModEntry modEntry) {
        try {
            ModPath = modEntry.Path;
            if (!Initialize()) return false;

            modEntry.OnToggle = (entry, value) => { OnToggle(value); return true; };
            modEntry.OnGUI = (entry) => DrawGUI();
            modEntry.OnSaveGUI = (entry) => Settings.Save();
            modEntry.OnUnload = (entry) => { Dispose(); return true; };

            return true;
        } catch (Exception ex) {
            ModLogger.Error($"UMM Load failed: {ex}");
            Dispose();
            return false;
        }
    }
#endif

#if ML
    public static bool InitializeML() {
        try {
            ModPath = Path.Combine("UserData", "JipperResourcePackX");
            if (!Initialize()) return false;

            try {
                ModMenu.ModMenuApi.Register(new ModMenu.ModEntry {
                    Id = "JipperResourcePackX",
                    DisplayName = "Jipper Resource Pack X",
                    Version = "1.6.0",
                    Author = "Jongyeol, Kkitut",
                    Enabled = Enabled,
                    OnToggle = (val) => OnToggle(val),
                    OnGUI = () => DrawGUI(),
                    OnSaveGUI = () => Settings.Save()
                });
            } catch (Exception ex) {
                ModLogger.Warn($"ModMenu registration skipped: {ex.Message}");
            }

            return true;
        } catch (Exception ex) {
            ModLogger.Error($"[JipperResourcePackX] ML Initialize failed: {ex}");
            Dispose();
            return false;
        }
    }
#endif

    public static bool Initialize() {
        try {
            if (Enabled) return true;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            MainThread.MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            try {
                AssetLoader.GameDir = Directory.GetParent(Application.dataPath)?.FullName;
            } catch { }

            // Asynchronous non-Unity tasks
            System.Threading.Tasks.Task.Run(() => {
                JipperSettings.Load();
                PlayCount.Load();
                KeyCountData.Load();
                KeyViewer.PreloadKeyCodesAsync();
                AssetLoader.StartPreloadingRawAssets();
            });

            SceneManager.sceneUnloaded += OnSceneUnloaded;

            if (_runnerGo == null) {
                _runnerGo = new GameObject("JipperResourcePackX_Runner");
                Object.DontDestroyOnLoad(_runnerGo);
                var runner = _runnerGo.AddComponent<JipperRunner>();
                _initCoroutine = runner.StartCoroutine(InitialCreateAsync());
            }

            _harmony = new HarmonyLib.Harmony("com.jongyeol.jipperresourcepackx");
            SafePatchManager.ApplyAll(_harmony);

            Enabled = true;
            sw.Stop();
            ModLogger.Log($"[Init] Core initialization finished in {sw.ElapsedMilliseconds}ms.");
            return true;
        } catch (Exception ex) {
            ModLogger.Error($"Initialization failed: {ex}");
            Dispose();
            return false;
        }
    }

    public static void EnsureInitialized() {
        if (_fullyInitialized) return;
        lock (_initLock) {
            if (_fullyInitialized) return;
            try {
                if (_runnerGo != null && _initCoroutine != null) {
                    var runner = _runnerGo.GetComponent<JipperRunner>();
                    if (runner != null) runner.StopCoroutine(_initCoroutine);
                    _initCoroutine = null;
                }
                var sw = System.Diagnostics.Stopwatch.StartNew();
                JipperSettings.Load();
                AssetLoader.InitializeAssets();
                if (Overlay.Instance == null) _ = new Overlay();
                InitFeatures();
                _fullyInitialized = true;
                sw.Stop();
                ModLogger.Log($"[Init] Synchronous fallback initialization finished in {sw.ElapsedMilliseconds}ms.");
            } catch (Exception ex) {
                ModLogger.Error($"EnsureInitialized error: {ex}");
            }
        }
    }

    private static IEnumerator InitialCreateAsync() {
        yield return null;
        if (_fullyInitialized) yield break;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        ModLogger.Log("[Init] Starting async resource loading and overlay setup...");

        AssetLoader.StartPreloadingRawAssets();
        if (AssetLoader.RawAssetsTask != null) {
            while (!AssetLoader.RawAssetsTask.IsCompleted) {
                yield return null;
            }
        }
        if (_fullyInitialized) yield break;

        var stepSw = System.Diagnostics.Stopwatch.StartNew();
        AssetLoader.InitializeAssets();
        stepSw.Stop();
        ModLogger.Log($"[Init] [AssetLoader] Asset initialization completed in {stepSw.ElapsedMilliseconds}ms.");
        yield return null;
        if (_fullyInitialized) yield break;

        stepSw.Restart();
        if (Overlay.Instance == null) _ = new Overlay();
        stepSw.Stop();
        ModLogger.Log($"[Init] [Overlay] Overlay UI created in {stepSw.ElapsedMilliseconds}ms.");
        yield return null;
        if (_fullyInitialized) yield break;

        stepSw.Restart();
        InitFeatures();
        stepSw.Stop();
        ModLogger.Log($"[Init] [Features] {Features.Count} features enabled in {stepSw.ElapsedMilliseconds}ms.");

        _fullyInitialized = true;
        _initCoroutine = null;
        sw.Stop();
        ModLogger.Log($"[Init] Full background setup completed in {sw.ElapsedMilliseconds}ms.");
    }

    private static void InitFeatures() {
        if (Features.Count == 0) {
            Features.Add(new AllColor());
            Features.Add(new Status());
            Features.Add(new Bpm());
            Features.Add(new Combo());
            Features.Add(new Judgement());
            Features.Add(new TimingScale());
            Features.Add(new Attempt());
            Features.Add(new ResourceChanger());
            Features.Add(new KeyViewer());
        }

        foreach (var feature in Features) {
            if (feature.Enabled) {
                try { feature.OnEnable(); } catch (Exception ex) { ModLogger.Warn($"Failed to enable {feature.Name}: {ex.Message}"); }
            }
        }
    }

    public static void Dispose() {
        try {
            Enabled = false;
            _fullyInitialized = false;
            if (_runnerGo != null && _initCoroutine != null) {
                var runner = _runnerGo.GetComponent<JipperRunner>();
                if (runner != null) runner.StopCoroutine(_initCoroutine);
                _initCoroutine = null;
            }

            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            Settings?.Save();
            PlayCount.Save();
            PlayCount.Dispose();

            foreach (var feature in Features) {
                try { feature.OnDisable(); } catch { }
            }
            Features.Clear();

            try { Overlay.Instance?.Destroy(); } catch { }
            Overlay.Instance = null;
            AssetLoader.UnloadAssets();

            if (_harmony != null) {
                SafePatchManager.UnpatchAll(_harmony);
                _harmony = null;
            }

            if (_runnerGo != null) {
                Object.Destroy(_runnerGo);
                _runnerGo = null;
            }
        } catch (Exception ex) {
            ModLogger.Error($"Dispose error: {ex}");
        }
    }

    public static void OnToggle(bool value) {
        if (value == Enabled) return;
        if (value) {
            Initialize();
            EnsureInitialized();
        } else {
            Dispose();
        }
    }

    public static void DrawGUI() {
        EnsureInitialized();
        _selectedTab = GUILayout.Toolbar(_selectedTab, Tabs);
        GUILayout.Space(10f);

        switch (_selectedTab) {
            case 0:
                DrawOverlayGUI();
                break;
            case 1:
                KeyViewer.Instance?.OnGUI();
                break;
            case 2:
                ResourceChanger.Instance?.OnGUI();
                break;
            case 3:
                Features.FirstOrDefault(f => f is AllColor)?.OnGUI();
                break;
            case 4:
                DrawCreditsGUI();
                break;
        }
    }

    private static void DrawOverlayGUI() {
        SettingGUI.AddSettingSliderFloat(ref Settings.Size, 1, ref _sizeString, Localization.Current["size"], 0, 2, () => Overlay.Instance?.UpdateSize());

        _toggleStyle ??= new GUIStyle {
            fixedWidth = 10f,
            normal = new GUIStyleState { textColor = Color.white },
            fontSize = 15,
            margin = new RectOffset(4, 2, 6, 6)
        };

        GUILayout.BeginHorizontal();
        _fontListExpanded = GUILayout.Toggle(_fontListExpanded, _fontListExpanded ? "◢" : "▶", _toggleStyle);
        if (GUILayout.Button(Localization.Current["font"], GUI.skin.label)) _fontListExpanded = !_fontListExpanded;
        GUILayout.FlexibleSpace();
        GUILayout.Label(string.IsNullOrEmpty(Settings.FontName) ? Localization.Current["font.default"] : Settings.FontName.Equals(AssetLoader.AdofaiFontName) ? Localization.Current["font.adofai"] : Settings.FontName);
        GUILayout.EndHorizontal();

        if (_fontListExpanded) {
            GUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            GUILayout.BeginVertical();
            _fontSearch = GUILayout.TextField(_fontSearch ?? "");
            _fontScrollPosition = GUILayout.BeginScrollView(_fontScrollPosition, GUI.skin.box, GUILayout.Height(150));
            bool isDefault = string.IsNullOrEmpty(Settings.FontName);
            if (GUILayout.Button(isDefault ? $"<b>{Localization.Current["font.default"]}</b>" : Localization.Current["font.default"])) SelectFont(null);
            if (GUILayout.Button(Settings.FontName == AssetLoader.AdofaiFontName ? $"<b>{Localization.Current["font.adofai"]}</b>" : Localization.Current["font.adofai"])) SelectFont(AssetLoader.AdofaiFontName);
            foreach ((string fontName, string fontPath) in GetFilteredFonts()) {
                bool selected = fontName == Settings.FontName;
                if (GUILayout.Button(selected ? $"<b>{fontName}</b>" : fontName)) SelectFont(fontName, fontPath);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10f);
        Status.Instance?.OnGUI();
        GUILayout.Space(5f);
        Bpm.Instance?.OnGUI();
        GUILayout.Space(5f);
        Combo.Instance?.OnGUI();
        GUILayout.Space(5f);
        Judgement.Instance?.OnGUI();
        GUILayout.Space(5f);
        Attempt.Instance?.OnGUI();
    }

    private static IEnumerable<(string Name, string Path)> GetFilteredFonts() {
        if (_availableFonts == null) {
            try {
                _availableFonts = Font.GetOSInstalledFontNames();
                _availableFontsPath = Font.GetPathsToOSFonts();
            } catch {
                _availableFonts = [];
                _availableFontsPath = [];
            }
        }
        var fonts = _availableFonts.Zip(_availableFontsPath, (name, path) => (name, path));
        if (string.IsNullOrEmpty(_fontSearch)) return fonts.OrderBy(f => f.name, StringComparer.OrdinalIgnoreCase);
        return fonts.Where(f => f.name.Contains(_fontSearch, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.name.StartsWith(_fontSearch, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(f => f.name, StringComparer.OrdinalIgnoreCase);
    }

    private static void SelectFont(string fontName, string fontPath = null) {
        Settings.FontName = fontName;
        if (string.IsNullOrEmpty(fontName)) AssetLoader.UnloadCustomFont(AssetLoader.DefaultFontAsset);
        else if (fontName.Equals(AssetLoader.AdofaiFontName)) AssetLoader.UnloadCustomFont(RDString.fontData.fontTMP);
        else AssetLoader.LoadCustomFont(fontName, fontPath);
        Overlay.Instance?.UpdateFont();
        if (KeyViewer.Instance != null && KeyViewer.Instance.Enabled) {
            KeyViewer.Instance.ResetKeyViewer();
            KeyViewer.Instance.ResetFootKeyViewer();
        }
        Settings.Save();
    }

    private static void DrawCreditsGUI() {
        GUILayout.Label($"<b><color=yellow>※ {Localization.Current["credit.modified"]}</color></b>");
        GUILayout.Space(5f);
        GUILayout.BeginHorizontal();
        if (AssetLoader.SideImage != null && GUILayout.Button(AssetLoader.SideImage, GUI.skin.label))
            Application.OpenURL("https://github.com/Jongye0l/JipperResourcePack");
        GUILayout.BeginVertical();
        GUILayout.Space(5f);
        URLLabel("Jipper Resource Pack X", "https://github.com/Jongye0l/JipperResourcePack");
        URLLabel(Localization.Current["credit.developer"] + ": Jongyeol, Kkitut", "https://github.com/Kkitut");
        URLLabel(Localization.Current["credit.design"] + ": Jipper", "https://www.youtube.com/@jipper1214");
        URLLabel(Localization.Current["credit.translator"] + ": changhyeon", "https://www.youtube.com/@changhyeon7492");
        URLLabel(Localization.Current["credit.translator"] + ": Phrygia", "https://www.youtube.com/@sjk04_");
        URLLabel(Localization.Current["credit.tester"] + ": dofaer", "https://www.youtube.com/@%EB%8F%84%ED%8E%98");
        URLLabel(Localization.Current["credit.tester"] + ": NTure", "https://www.youtube.com/@NTure_1253");
        GUILayout.Space(15f);
        GUILayout.Label(Localization.Current["credit.reference"]);
        URLLabel("Overlayer (By. c3nb)", "https://github.com/c3nb/Overlayer");
        URLLabel("ShowBPM (By. Flower)", "https://github.com/FLOWERs-Modding/ADOFAI_ShowBPM");
        URLLabel("ProgressDisplayer2 (By. Flower)", "https://github.com/FLOWERs-Modding/ADOFAI_ProgressDisplayer2");
        URLLabel("AdofaiTweaks (By. PizzaLovers007)", "https://github.com/PizzaLovers007/AdofaiTweaks");
        URLLabel("MovingManN (By. Kkitut)", "https://github.com/Jongye0l/JIpper-Overlayer/blob/main/Scripts/MovingManN.js");
        URLLabel("MoreTimeTags (By. Jongyeol)", "https://github.com/Jongye0l/MoreTimeTags");
        URLLabel("BetterCalibration (By. Jongyeol)", "https://github.com/Jongye0l/BetterCalibration");
        URLLabel("AdvancedCombo (By. Jongyeol)", "https://github.com/Jongye0l/AdvancedCombo");
        GUILayout.Space(15f);
        GUILayout.Label(Localization.Current["credit.font"]);
        URLLabel("Maplestory OTF Bold", "https://fontmeme.com/ktype/maplestory-font/");
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private static void URLLabel(string label, string url) {
        if (GUILayout.Button(label, GUI.skin.label)) Application.OpenURL(url);
    }

    public static void OnGameStart1(int seqID) {
        try {
            EnsureInitialized();
            if (GCS.practiceMode) return;
            Overlay.Instance?.Show(seqID);
        } catch { }
    }

    public static void OnGameStart2() {
        try {
            EnsureInitialized();
            if (!GCS.practiceMode && scnGame.instance) return;
            Overlay.Instance?.Show(scrController.instance?.currentSeqID ?? 0);
        } catch { }
    }

    public static void OnChangeState(Enum newState) {
        try {
            if (!_fullyInitialized) return;
            switch ((States) newState) {
                case States.Fail2:
                    Overlay.Instance?.Death();
                    break;
                case States.Won:
                    Overlay.Instance?.Clear();
                    break;
            }
        } catch { }
    }

    public static void OnGameStop() {
        try {
            if (!_fullyInitialized) return;
            Overlay.Instance?.Hide();
        } catch { }
    }

    public static void UpdatePlayersCount() {
        try {
            if (!_fullyInitialized) return;
            Overlay.Instance?.OnChangePlayers();
        } catch { }
    }

    private static void OnSceneUnloaded(Scene _) {
        try {
            if (!_fullyInitialized) return;
            Overlay.Instance?.Hide();
        } catch (Exception e) {
            ModLogger.Error($"[Main/OnSceneUnloaded] {e.Message}");
        }
    }

    private class JipperRunner : MonoBehaviour {
        private void Update() {
            try {
                MainThread.Update();
            } catch (Exception ex) {
                ModLogger.Error($"[JipperRunner] MainThread update error: {ex.Message}");
            }

            if (!_fullyInitialized) return;

            try {
                if (Overlay.Instance?.GameObject != null && Overlay.Instance.GameObject.activeSelf) {
                    Overlay.Instance.UpdateTime();
                    Overlay.Instance.UpdateComboSize();
                }
            } catch { }

            float dt = Time.deltaTime;
            foreach (var feature in Features) {
                try {
                    feature.OnUpdate(dt);
                } catch { }
            }
        }
    }
}
