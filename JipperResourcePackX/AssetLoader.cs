using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JipperResourcePackX;

public static class AssetLoader {
    public const string AdofaiFontName = "$<ADOFAI>";
    public static TMP_FontAsset FontAsset;
    public static TMP_FontAsset DefaultFontAsset;
    public static GameObject ProgressObject;
    public static GameObject ProgressBarPrefab => ProgressObject;
    public static Sprite Auto;
    public static Sprite KeyBackground;
    public static Sprite KeyOutline;
    public static Sprite GhostRain;
    public static Texture2D SideImage;
    private static bool _requiredFontUnload;
    private static Font _sourceDefaultFont;
    private static readonly Dictionary<string, string> _fontPathCache = new(StringComparer.OrdinalIgnoreCase);
    private static string[] _manifestResourceNames;
    public static string GameDir { get; set; }

    public class RawAssets {
        public byte[] AutoBytes;
        public byte[] SideImageBytes;
        public byte[] GhostRainBytes;
        public byte[] KeyBackgroundBytes;
        public byte[] KeyOutlineBytes;
        public string DefaultFontPath;
        public string KeyCodesJson;
    }

    private static Task<RawAssets> _rawAssetsTask;
    public static Task<RawAssets> RawAssetsTask => _rawAssetsTask;

    public static void StartPreloadingRawAssets() {
        if (_rawAssetsTask != null) return;
        _rawAssetsTask = Task.Run(LoadRawAssetsBackground);
    }

    private static string[] GetResourceNames() {
        return _manifestResourceNames ??= Assembly.GetExecutingAssembly().GetManifestResourceNames();
    }

    public static byte[] LoadRawBytesFromResource(string fileName) {
        try {
            var asm = Assembly.GetExecutingAssembly();
            var resName = GetResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
            if (resName != null) {
                using var stream = asm.GetManifestResourceStream(resName);
                if (stream != null) {
                    byte[] data = new byte[stream.Length];
                    int read = 0;
                    while (read < data.Length) {
                        int r = stream.Read(data, read, data.Length - read);
                        if (r == 0) break;
                        read += r;
                    }
                    return data;
                }
            }
        } catch { }

        string assetPath = GetAssetPath(fileName);
        if (File.Exists(assetPath)) {
            try {
                return File.ReadAllBytes(assetPath);
            } catch { }
        }
        return null;
    }

    public static string LoadStringFromResource(string fileName) {
        try {
            var asm = Assembly.GetExecutingAssembly();
            var resName = GetResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
            if (resName != null) {
                using var stream = asm.GetManifestResourceStream(resName);
                if (stream != null) {
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }
        } catch { }

        string assetPath = GetAssetPath(fileName);
        if (File.Exists(assetPath)) {
            try {
                return File.ReadAllText(assetPath);
            } catch { }
        }
        return null;
    }

    private static RawAssets LoadRawAssetsBackground() {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var raw = new RawAssets {
            AutoBytes = LoadRawBytesFromResource("Auto.png"),
            SideImageBytes = LoadRawBytesFromResource("SideImage.png"),
            GhostRainBytes = LoadRawBytesFromResource("GhostRain.png"),
            KeyBackgroundBytes = LoadRawBytesFromResource("KeyBackground.png"),
            KeyOutlineBytes = LoadRawBytesFromResource("KeyOutline.png"),
            KeyCodesJson = LoadStringFromResource("KeyCodes.json")
        };

        try {
            string fontPath = GetAssetPath("Maplestory OTF Bold.otf");
            if (!File.Exists(fontPath)) {
                byte[] fontBytes = LoadRawBytesFromResource("Maplestory OTF Bold.otf");
                if (fontBytes != null && fontBytes.Length > 0) {
                    string dir = Path.GetDirectoryName(fontPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                        Directory.CreateDirectory(dir);
                    }
                    File.WriteAllBytes(fontPath, fontBytes);
                }
            }
            if (File.Exists(fontPath)) raw.DefaultFontPath = fontPath;
        } catch (Exception ex) {
            ModLogger.Warn($"[AssetLoader] Background font preparation failed: {ex.Message}");
        }

        sw.Stop();
        ModLogger.Log($"[AssetLoader] Preloaded raw asset bytes in {sw.ElapsedMilliseconds}ms.");
        return raw;
    }

    public static void InitializeAssets() {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        StartPreloadingRawAssets();
        RawAssets raw = _rawAssetsTask.GetAwaiter().GetResult();

        if (Auto == null) Auto = CreateSpriteFromBytes(raw.AutoBytes);
        if (SideImage == null) SideImage = CreateTextureFromBytes(raw.SideImageBytes);
        if (GhostRain == null) GhostRain = CreateSpriteFromBytes(raw.GhostRainBytes, new Vector4(2, 2, 2, 2));
        if (KeyBackground == null) KeyBackground = CreateSpriteFromBytes(raw.KeyBackgroundBytes, new Vector4(15, 15, 15, 15));
        if (KeyOutline == null) KeyOutline = CreateSpriteFromBytes(raw.KeyOutlineBytes, new Vector4(15, 15, 15, 15));

        if (ProgressObject == null) ProgressObject = CreateProgressBarPrefab();

        if (DefaultFontAsset == null) LoadDefaultFont(raw.DefaultFontPath);

        if (!string.IsNullOrEmpty(JipperSettings.Instance.FontName)) {
            if (JipperSettings.Instance.FontName == AdofaiFontName) {
                UnloadCustomFont(RDString.fontData.fontTMP);
            } else {
                LoadCustomFont(JipperSettings.Instance.FontName);
            }
        }
        sw.Stop();
        ModLogger.Log($"[AssetLoader] Initialized Unity asset objects in {sw.ElapsedMilliseconds}ms.");
    }

    public static void UnloadAssets() {
        UnloadCustomFont(null);
        if (DefaultFontAsset != null) {
            Object.Destroy(DefaultFontAsset);
            DefaultFontAsset = null;
        }
        if (_sourceDefaultFont != null) {
            Object.Destroy(_sourceDefaultFont);
            _sourceDefaultFont = null;
        }
        if (ProgressObject != null) {
            Object.Destroy(ProgressObject);
            ProgressObject = null;
        }
    }

    public static GameObject CreateProgressBarPrefab() {
        GameObject root = new("ProgressBar", typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(642, 18);

        GameObject border = new("borderLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        border.transform.SetParent(root.transform, false);
        RectTransform borderRt = border.GetComponent<RectTransform>();
        borderRt.anchorMin = borderRt.anchorMax = borderRt.pivot = new Vector2(0.5f, 0.5f);
        borderRt.anchoredPosition = Vector2.zero;
        borderRt.sizeDelta = new Vector2(642, 18);
        border.GetComponent<Image>().color = Color.black;

        GameObject bg = new("background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(638, 14);
        bg.GetComponent<Image>().color = Color.white;

        GameObject line = new("line", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        line.transform.SetParent(root.transform, false);
        RectTransform lineRt = line.GetComponent<RectTransform>();
        lineRt.anchorMin = lineRt.anchorMax = lineRt.pivot = new Vector2(0f, 0.5f);
        lineRt.anchoredPosition = new Vector2(2, 0);
        lineRt.sizeDelta = new Vector2(100, 14);
        line.GetComponent<Image>().color = new Color(0.92f, 0.8f, 0.98f, 1f);

        root.SetActive(false);
        Object.DontDestroyOnLoad(root);
        return root;
    }

    public static Texture2D CreateTextureFromBytes(byte[] data) {
        if (data == null || data.Length == 0) return Texture2D.whiteTexture;
        try {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(data)) return tex;
        } catch { }
        return Texture2D.whiteTexture;
    }

    public static Sprite CreateSpriteFromBytes(byte[] data, Vector4 border = default) {
        var tex = CreateTextureFromBytes(data);
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    public static Texture2D LoadTextureFromResource(string fileName) {
        byte[] data = LoadRawBytesFromResource(fileName);
        return CreateTextureFromBytes(data);
    }

    public static Sprite LoadSpriteFromResource(string fileName, Vector4 border = default) {
        byte[] data = LoadRawBytesFromResource(fileName);
        return CreateSpriteFromBytes(data, border);
    }

    public static string GetAssetPath(string filename) {
        try {
            string gameDir = GameDir;
            if (string.IsNullOrEmpty(gameDir)) {
                try {
                    gameDir = GameDir = Directory.GetParent(Application.dataPath)?.FullName;
                } catch { }
            }
            if (!string.IsNullOrEmpty(gameDir)) {
                string pGameMods = Path.Combine(gameDir, "Mods", "JipperResourcePackX", filename);
                if (File.Exists(pGameMods)) return pGameMods;

                string pGameUser = Path.Combine(gameDir, "UserData", "JipperResourcePackX", filename);
                if (File.Exists(pGameUser)) return pGameUser;
            }
        } catch { }

        if (!string.IsNullOrEmpty(Main.ModPath)) {
            string p = Path.Combine(Main.ModPath, filename);
            if (File.Exists(p)) return p;
        }

        string pMods = Path.Combine("Mods", "JipperResourcePackX", filename);
        if (File.Exists(pMods)) return pMods;

        string pUserData = Path.Combine("UserData", "JipperResourcePackX", filename);
        if (File.Exists(pUserData)) return pUserData;

        return Path.Combine("UserData", "JipperResourcePackX", filename);
    }

    private static void LoadDefaultFont(string fontPath = null) {
        fontPath ??= GetAssetPath("Maplestory OTF Bold.otf");
        if (!string.IsNullOrEmpty(fontPath) && File.Exists(fontPath)) {
            try {
                _sourceDefaultFont = new Font(fontPath);
                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(_sourceDefaultFont);
                if (fontAsset != null) {
                    try {
                        if (RDConstants.data != null) {
                            fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset> {
                                RDConstants.data.latinFontTMPro,
                                RDConstants.data.koreanFontTMPro,
                                RDConstants.data.japaneseFontTMPro,
                                RDConstants.data.chineseFontTMPro
                            };
                        }
                    } catch { }
                    FontAsset = DefaultFontAsset = fontAsset;
                    return;
                }
            } catch (Exception ex) {
                ModLogger.Warn($"[AssetLoader] Failed to load Maplestory OTF font: {ex.Message}");
            }
        }

        try {
            FontAsset = DefaultFontAsset = RDString.fontData.fontTMP;
        } catch { }
    }

    public static void LoadCustomFont(string fontName, string fontPath = null) {
        fontPath ??= GetPathForFontName(fontName);
        if (string.IsNullOrEmpty(fontPath) && !string.IsNullOrEmpty(fontName)) fontPath = fontName;

        try {
            Font font = new(fontPath);
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
            if (!fontAsset) {
                ModLogger.Warn("[AssetLoader] Failed to create font asset for font: " + fontName);
                return;
            }
            try {
                fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset> {
                    RDConstants.data.latinFontTMPro,
                    RDConstants.data.koreanFontTMPro,
                    RDConstants.data.japaneseFontTMPro,
                    RDConstants.data.chineseFontTMPro
                };
            } catch { }

            UnloadCustomFont(fontAsset);
            _requiredFontUnload = true;
        } catch (Exception ex) {
            ModLogger.Warn($"[AssetLoader] Error loading font {fontName}: {ex.Message}");
        }
    }

    private static string GetPathForFontName(string fontName) {
        if (string.IsNullOrEmpty(fontName)) return null;
        if (_fontPathCache.TryGetValue(fontName, out string cached)) return cached;

        try {
            string[] names = Font.GetOSInstalledFontNames();
            string[] paths = Font.GetPathsToOSFonts();
            for (int i = 0; i < names.Length && i < paths.Length; i++) {
                if (!_fontPathCache.ContainsKey(names[i])) {
                    _fontPathCache[names[i]] = paths[i];
                }
            }
            if (_fontPathCache.TryGetValue(fontName, out string found)) return found;
        } catch { }
        return null;
    }

    public static void UnloadCustomFont(TMP_FontAsset fontAsset) {
        if (_requiredFontUnload && FontAsset != null && FontAsset != DefaultFontAsset) {
            Object.Destroy(FontAsset);
        }
        _requiredFontUnload = false;
        FontAsset = fontAsset ?? DefaultFontAsset;
    }
}
