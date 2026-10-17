using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace JipperResourcePackX.KeyViewerContents;

public class KeyCountData {
    public static KeyCountData Instance;
    public readonly int[] Count = new int[KeyViewer.FootOutIndex];
    public int TotalCount;
    private int _saveDirtyVersion;

    public static void Load() {
        if(Instance != null) return;
        Instance = new KeyCountData();
        _ = Task.Run(() => {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try {
                if (string.IsNullOrEmpty(Main.ModPath)) return;
                string path = Path.Combine(Main.ModPath, "KeyCount.dat");
                bool loaded = false;
                if(File.Exists(path) && Instance.Read(path)) loaded = true;
                else {
                    path += ".bak";
                    if(File.Exists(path) && Instance.Read(path)) loaded = true;
                }
                sw.Stop();
                if (loaded) {
                    ModLogger.Log($"[KeyCount] Loaded key count data ({Instance.TotalCount} total presses) in {sw.ElapsedMilliseconds}ms.");
                } else {
                    ModLogger.Log($"[KeyCount] Initialized new key count data in {sw.ElapsedMilliseconds}ms.");
                }
            } catch (Exception ex) {
                ModLogger.Warn($"[KeyCount] Failed to load key count: {ex.Message}");
            }
        });
    }

    private bool Read(string path) {
        try {
            byte[] data = new byte[KeyViewer.FootOutIndex * 4 + 8];
            using(FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read)) 
                if(!ReadExactly(fs, data)) return false;
            TotalCount = BitConverter.ToInt32(data, 4);
            data.AsSpan(8).CopyTo(MemoryMarshal.AsBytes(Count.AsSpan()));
            return true;
        } catch {
            return false;
        }
    }

    private static bool ReadExactly(FileStream fs, byte[] buffer) {
        int offset = 0;
        while(offset < buffer.Length) {
            int count = fs.Read(buffer, offset, buffer.Length - offset);
            if(count == 0) return false;
            offset += count;
        }
        return true;
    }

    public void Save() {
        if(Interlocked.Increment(ref _saveDirtyVersion) == 1) Task.Run(SaveData);
    }

    private async void SaveData() {
        try {
            string path = Path.Combine(Main.ModPath, "KeyCount.dat");
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
            do {
                _saveDirtyVersion = 1;
                if(File.Exists(path)) {
                    try {
                        File.Delete(path + ".bak");
                        File.Move(path, path + ".bak");
                    } catch { }
                }
                byte[] data = new byte[KeyViewer.FootOutIndex * 4 + 8];
                BitConverter.TryWriteBytes(data.AsSpan(4), TotalCount);
                MemoryMarshal.AsBytes(Count.AsSpan()).CopyTo(data.AsSpan(8));
                await using(FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true)) {
                    await fs.WriteAsync(data, 0, data.Length);
                }
                await Task.Delay(1000);
            } while(Interlocked.CompareExchange(ref _saveDirtyVersion, 0, 1) != 1);
        } catch (Exception e) {
            ModLogger.Error($"Fail to save key count: {e}");
            _saveDirtyVersion = 0;
        }
    }
}