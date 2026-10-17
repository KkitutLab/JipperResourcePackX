using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using SkyHook;
using UnityEngine;
using EventType = SkyHook.EventType;

namespace JipperResourcePackX.KeyViewerContents;

public partial class KeyViewer {
    private static readonly bool StopwatchTicksAreDateTimeTicks = System.Diagnostics.Stopwatch.Frequency == TimeSpan.TicksPerSecond;

    public static long CurrentTicks {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => StopwatchTicksAreDateTimeTicks ? Stopwatch.ElapsedTicks : Stopwatch.Elapsed.Ticks;
    }

    private readonly bool[] _keyState = new bool[GhostOutIndex];
    private ConcurrentQueue<long> _pressTimes;
    private volatile bool _listening;
    private KeyBinding _keyBinding;
    private volatile int _capturedKeyCode;
    private int _lastKpsCount;
    private int _lastTotalCount;
    private static HashSet<KeyCode> _unityKeyLimitKeys = [];
    private static HashSet<ushort> _asyncKeyLimitKeys;

    private static readonly Dictionary<KeyCode, bool> _heldCache = new();
    private static int _heldCacheFrame = -1;

    private static Func<KeyCode, bool> _o5GetKey;
    private static Func<int, bool> _o5GetMouseButton;
    private static bool _o5Resolved;

    private static void EnsureO5Input() {
        if (_o5Resolved) return;
        _o5Resolved = true;
        try {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                if (string.Equals(asm.GetName().Name, "O5Kit", StringComparison.OrdinalIgnoreCase)) {
                    Type type = asm.GetType("O5Kit.Input.O5Input");
                    if (type != null) {
                        MethodInfo mKey = type.GetMethod("GetKey", BindingFlags.Public | BindingFlags.Static, null, [typeof(KeyCode)], null);
                        if (mKey != null) _o5GetKey = (Func<KeyCode, bool>)Delegate.CreateDelegate(typeof(Func<KeyCode, bool>), mKey);

                        MethodInfo mMouse = type.GetMethod("GetMouseButton", BindingFlags.Public | BindingFlags.Static, null, [typeof(int)], null);
                        if (mMouse != null) _o5GetMouseButton = (Func<int, bool>)Delegate.CreateDelegate(typeof(Func<int, bool>), mMouse);
                    }
                    break;
                }
            }
        } catch { }
    }

    internal static bool IsKeyHeld(KeyCode key) {
        if ((int)key == 0) return false;
        int frame = Time.frameCount;
        if (frame != _heldCacheFrame) {
            _heldCache.Clear();
            _heldCacheFrame = frame;
        }
        if (_heldCache.TryGetValue(key, out bool cached)) return cached;

        bool val = false;
        try {
            EnsureO5Input();
            if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse2) {
                int btn = (int)(key - KeyCode.Mouse0);
                if (_o5GetMouseButton != null) {
                    val = _o5GetMouseButton(btn);
                }
                if (!val) {
                    val = Input.GetMouseButton(btn);
                }
                if (!val) {
                    val = Input.GetKey(key);
                }
            } else if (key >= KeyCode.Mouse3 && key <= KeyCode.Mouse6) {
                val = Input.GetKey(key);
            } else {
                if (_o5GetKey != null) {
                    val = _o5GetKey(key);
                }
                if (!val) {
                    val = Input.GetKey(key);
                }
            }
        } catch {
            val = false;
        }

        _heldCache[key] = val;
        return val;
    }

    private void RebuildKeyBinding() {
        if(Keys == null) return;
        Dictionary<KeyLabel, List<int>> labels = new();
        Dictionary<ushort, List<int>> natives = new();
        AddKeyCodes(GetKeyCode(), 0);
        AddKeyCodes(GetFootKeyCode(), HandOutIndex);
        AddKeyCodes(GetGhostKeyCode(), FootOutIndex);
        _keyBinding = new KeyBinding(ToArrayMap(labels), ToArrayMap(natives));
        return;

        void AddKeyCodes(KeyCode[] keyCodes, int offset) {
            for(int i = 0; i < keyCodes.Length; i++) {
                KeyCode keyCode = keyCodes[i];
                if(keyCode == KeyCode.None) continue;
                int index = i + offset;
                if((int) keyCode < 0x1000) {
                    KeyLabel label = SkyHookKeyMapper.UnityKeyToSkyHookKey(keyCode);
                    if(label == KeyLabel.Unknown) continue;
                    if(!labels.TryGetValue(label, out List<int> list)) labels[label] = list = [];
                    list.Add(index);
                } else {
                    ushort nativeKeyCode = (ushort) ((int) keyCode - 0x1000);
                    if(!natives.TryGetValue(nativeKeyCode, out List<int> list)) natives[nativeKeyCode] = list = [];
                    list.Add(index);
                }
            }
        }
    }

    private static Dictionary<T, int[]> ToArrayMap<T>(Dictionary<T, List<int>> map) {
        Dictionary<T, int[]> result = new(map.Count);
        foreach(KeyValuePair<T, List<int>> pair in map) result[pair.Key] = pair.Value.ToArray();
        return result;
    }

    private void StartEventListener() {
        try {
            _listening = true;
            if (SkyHookManager.Instance != null) {
                SkyHookManager.Instance.requireFocus = false;
            }
            SkyHookManager.KeyUpdated?.AddListener(OnKeyEvent);
        } catch (Exception ex) {
            ModLogger.Warn($"[KeyViewer] StartEventListener failed: {ex.Message}");
        }
    }

    private void StopEventListener() {
        try {
            SkyHookManager.KeyUpdated?.RemoveListener(OnKeyEvent);
        } catch { }
        _listening = false;
        _heldCache.Clear();
    }

    private void OnKeyEvent(SkyHookEvent hookEvent) {
        if (!_listening) return;
        if (hookEvent.Type == EventType.KeyPressed && _selectedKey != -1 && _changeState != 1) {
            KeyCode capturedKey = SkyHookKeyMapper.SkyHookKeyToUnityKey(hookEvent.Label);
            _capturedKeyCode = capturedKey switch {
                KeyCode.None => hookEvent.Key + 0x1000,
                KeyCode.Less => (int) KeyCode.Comma,
                KeyCode.Greater => (int) KeyCode.Equals,
                KeyCode.Pipe => (int) KeyCode.Backslash,
                _ => (int) capturedKey
            };
        }
    }

    private bool Work(int[] indexes, bool pressed, long currentTicks) {
        KeyViewerSetting settings = Settings;
        KeyCountData countData = KeyCountData.Instance;
        bool counted = false;
        foreach(int index in indexes) {
            if(index >= FootOutIndex) {
                if(!settings.useRain || !settings.useGhostRain) continue;
                Key ghostKey = Keys[index - FootOutIndex];
                if(ghostKey == null || _keyState[index] == pressed) continue;
                _keyState[index] = pressed;
                if(!pressed) {
                    ghostKey.LastGhostRain?.Finish(currentTicks);
                    continue;
                }
                RawRain ghostRain = ghostKey.LastGhostRain = RawRain.GetOrNewRawRain(ghostKey, currentTicks, true);
                RainManager.RawRainQueue.Enqueue(ghostRain);
                continue;
            }
            Key key = Keys[index];
            if(key == null || _keyState[index] == pressed) continue;
            _keyState[index] = pressed;
            key.UpdateRequestKey(pressed);
            if(index >= HandOutIndex) {
                if(!pressed) continue;
                countData.Count[index]++;
                countData.TotalCount++;
                _pressTimes?.Enqueue(currentTicks);
                counted = true;
                continue;
            }
            if(!pressed) {
                key.LastRain?.Finish(currentTicks);
                continue;
            }
            int countIndex = index == 9 && settings.KeyViewerStyle == KeyviewerStyle.Key10 ? 10 : index;
            key.Value.Text = (++countData.Count[countIndex]).ToString();
            countData.TotalCount++;
            _pressTimes?.Enqueue(currentTicks);
            counted = true;
            if(!settings.useRain) continue;
            RawRain rawRain = key.LastRain = RawRain.GetOrNewRawRain(key, currentTicks, false);
            RainManager.RawRainQueue.Enqueue(rawRain);
        }
        return counted;
    }

    private sealed class KeyBinding(Dictionary<KeyLabel, int[]> labelMap, Dictionary<ushort, int[]> nativeMap) {
        public readonly Dictionary<KeyLabel, int[]> LabelMap = labelMap;
        public readonly Dictionary<ushort, int[]> NativeMap = nativeMap;
    }

    internal void ProcessInputs() {
        if (!Enabled) return;
        long now = CurrentTicks;
        bool counted = false;

        void PollKeys(KeyCode[] keyCodes, int offset) {
            if (keyCodes == null) return;
            for (int i = 0; i < keyCodes.Length; i++) {
                KeyCode kc = keyCodes[i];
                if (kc == KeyCode.None) continue;
                int index = i + offset;
                if (index < 0 || index >= _keyState.Length) continue;

                bool isDown = IsKeyHeld(kc);
                if (_keyState[index] != isDown) {
                    counted |= Work([index], isDown, now);
                }
            }
        }

        PollKeys(GetKeyCode(), 0);
        PollKeys(GetFootKeyCode(), HandOutIndex);
        PollKeys(GetGhostKeyCode(), FootOutIndex);

        if (counted && Enabled) KeyCountData.Instance.Save();
    }

    public class KeyViewerUpdater : MonoBehaviour {
        private void Update() {
            KeyViewer instance = Instance;
            if(instance == null) return;
            instance.ProcessInputs();
            ConcurrentQueue<long> pressTimes = instance._pressTimes;
            if(pressTimes == null) return;
            long currentTicks = CurrentTicks;
            while(pressTimes.TryPeek(out long result) && currentTicks - result > TimeSpan.TicksPerSecond) pressTimes.TryDequeue(out _);
            if(instance.Kps != null) {
                int kpsCount = pressTimes.Count;
                if(kpsCount != instance._lastKpsCount) {
                    instance._lastKpsCount = kpsCount;
                    instance.Kps.Value.TMP.text = kpsCount.ToString();
                }
            }
            if(instance.Total == null) return;
            int totalCount = KeyCountData.Instance.TotalCount;
            if(totalCount == instance._lastTotalCount) return;
            instance._lastTotalCount = totalCount;
            instance.Total.Value.TMP.text = totalCount.ToString();
        }
    }
}
