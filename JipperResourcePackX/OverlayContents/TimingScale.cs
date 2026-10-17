using UnityEngine;

namespace JipperResourcePackX.OverlayContents;

public class TimingScale : Feature {
    public static GameObject TimingScaleObject;
    public static TimingScale Instance;

    public TimingScale() : base(nameof(TimingScale), true) {
        Instance = this;
    }
    
    public override void OnEnable() {
        if (TimingScaleObject) TimingScaleObject.SetActive(true);
    }
    
    public override void OnDisable() {
        if (TimingScaleObject) TimingScaleObject.SetActive(false);
    }
    
    public static void OnMoveToNextFloor() {
        try {
            Overlay.Instance?.UpdateTimingScale();
        } catch { }
    }
}