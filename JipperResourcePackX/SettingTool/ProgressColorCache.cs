using Newtonsoft.Json;
using UnityEngine;

namespace JipperResourcePackX.SettingTool;

public class ProgressColorCache(float progress, Color color) : ColorCache(color) {
    public float Progress = progress;
    [JsonIgnore] public string ProgressString;
}