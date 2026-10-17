using System;
using System.IO;
using JipperResourcePackX.KeyViewerContents;
using JipperResourcePackX.OverlayContents;
using Newtonsoft.Json;

namespace JipperResourcePackX;

public class JipperSettings {
    public float Size = 1f;
    public string FontName = "";

    public StatusSetting Status = new();
    public BpmSettings Bpm = new();
    public ComboSettings Combo = new();
    public JudgementSettings Judgement = new();
    public AttemptSetting Attempt = new();
    public ResourceChangerSetting ResourceChanger = new();
    public KeyViewerSetting KeyViewer = new();
    public AllColorSetting AllColor = new();

    public static JipperSettings Instance { get; set; } = new();

    private static string GetSettingsPath(bool forSaving = false) {
        string userDataPath = Path.Combine("UserData", "JipperResourcePackX", "Settings.json");
        string modsPath = Path.Combine("Mods", "JipperResourcePackX", "Settings.json");

        if (!string.IsNullOrEmpty(Main.ModPath)) {
            string modPathFile = Path.Combine(Main.ModPath, "Settings.json");
            if (File.Exists(modPathFile)) return modPathFile;
        }

        if (File.Exists(userDataPath)) return userDataPath;
        if (File.Exists(modsPath)) return modsPath;

        if (forSaving) {
#if UMM
            if (!string.IsNullOrEmpty(Main.ModPath)) {
                return Path.Combine(Main.ModPath, "Settings.json");
            }
            return modsPath;
#else
            return userDataPath;
#endif
        }

        return userDataPath;
    }

    private static int _saveDirtyVersion;

    public void Save() {
        try {
            string file = GetSettingsPath(forSaving: true);
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            int currentVersion = System.Threading.Interlocked.Increment(ref _saveDirtyVersion);

            System.Threading.Tasks.Task.Run(() => {
                try {
                    string dir = Path.GetDirectoryName(file);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                        Directory.CreateDirectory(dir);
                    }
                    if (System.Threading.Volatile.Read(ref _saveDirtyVersion) == currentVersion) {
                        File.WriteAllText(file, json);
                    }
                } catch (Exception ex) {
                    ModLogger.Error($"Failed to save settings async: {ex.Message}");
                }
            });
        } catch (Exception ex) {
            ModLogger.Error($"Failed to prepare save settings: {ex.Message}");
        }
    }

    public static System.Threading.Tasks.Task<JipperSettings> LoadAsync() {
        return System.Threading.Tasks.Task.Run(() => {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var settings = Load();
            ModLogger.Log($"[Settings] Loaded settings in {sw.ElapsedMilliseconds}ms");
            return settings;
        });
    }

    public static JipperSettings Load() {
        try {
            string file = GetSettingsPath(forSaving: false);
            if (File.Exists(file)) {
                string json = File.ReadAllText(file);
                var settings = JsonConvert.DeserializeObject<JipperSettings>(json) ?? new();
                Instance = settings;
                return settings;
            }
        } catch (Exception ex) {
            ModLogger.Error($"Failed to load settings: {ex.Message}");
        }
        var def = new JipperSettings();
        Instance = def;
        return def;
    }
}
