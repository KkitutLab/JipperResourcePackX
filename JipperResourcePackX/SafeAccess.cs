using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace JipperResourcePackX;

public static class SafeAccess {
    [ThreadStatic]
    private static StringBuilder _sharedBuilder;

    public static StringBuilder GetSharedBuilder() {
        _sharedBuilder ??= new StringBuilder(256);
        _sharedBuilder.Length = 0;
        return _sharedBuilder;
    }

    public static float SetIfNaN(this float val, float fallback = 0f) => float.IsNaN(val) ? fallback : val;

    public static void SizeDeltaX(this RectTransform rect, float x) {
        if (rect != null) rect.sizeDelta = new Vector2(x, rect.sizeDelta.y);
    }

    public static void ColorLogoSafe(this scrLogoText text, Color color, bool isFire) {
        if (text != null) text.ColorLogo(color, isFire);
    }

    public static Color GetOrange() => Color.orange;
    public static Color GetSkyBlue() => new Color(0.5294118f, 0.8078432f, 0.9215687f, 1f);
    public static Color GetPurple() => new Color(0.627451f, 0.1254902f, 0.9411765f, 1f);

    public static int ReleaseNumber {
        get {
            try { return Releases.releaseNumber; } catch { return 149; }
        }
    }

    public static bool XScoreSupported => true;

    public static int[] GetHitMarginsCount() {
        try {
            if (scrMistakesManager.marginTrackers != null && scrMistakesManager.marginTrackers.Length > 0)
                return scrMistakesManager.marginTrackers[0]?.hitMarginsCount ?? new int[16];
        } catch { }
        return new int[16];
    }

    public static void CalculatePercentAcc() {
        try {
            if (scrMistakesManager.marginTrackers != null) {
                foreach (var tracker in scrMistakesManager.marginTrackers)
                    tracker?.CalculatePercentAcc();
            }
            scrPlayerManager.instance?.mistakesManager?.CalculateTotalAccuracy();
        } catch { }
    }

    public static float GetPercentAcc() {
        try {
            if (scrPlayerManager.instance?.mistakesManager != null)
                return scrPlayerManager.instance.mistakesManager.percentAcc;
            if (scrController.instance?.mistakesManager != null)
                return scrController.instance.mistakesManager.percentAcc;
        } catch { }
        return 1f;
    }

    public static float GetPercentXAcc() {
        try {
            if (scrPlayerManager.instance?.mistakesManager != null)
                return scrPlayerManager.instance.mistakesManager.percentXAcc;
            if (scrController.instance?.mistakesManager != null)
                return scrController.instance.mistakesManager.percentXAcc;
        } catch { }
        return 1f;
    }

    public static double GetPlanetSpeed(scrController controller) {
        try {
            if (controller?.playerOne?.planetarySystem != null)
                return controller.playerOne.planetarySystem.speed;
            if (controller?.planetarySystem != null)
                return controller.planetarySystem.speed;
            if (controller != null)
                return controller.d_speed;
        } catch { }
        return 1.0;
    }

    public static bool IsCoopMode() {
        try {
            return scrPlayerManager.playerCount > 1 || scrController.coopMode;
        } catch {
            return false;
        }
    }

    public static float GetPotentialXAccuracy(int index, float xacc, int seqId, int remaining) {
        try {
            if (scrMistakesManager.marginTrackers != null && index >= 0 && index < scrMistakesManager.marginTrackers.Length) {
                float val = scrMistakesManager.marginTrackers[index].maxPossibleXAcc;
                return float.IsNaN(val) ? 1f : val;
            }
        } catch { }
        int total = seqId + remaining;
        return total == 0 ? 1f : (xacc * seqId + remaining) / total;
    }

    public static int GetJudgedTiles(int[] hits, int seqID) {
        if (hits != null && hits.Length > 14)
            return seqID - hits[12 /* Auto */] - hits[14 /* MidSpin */];
        return seqID;
    }

    public static (int, int) GetAccuracyCounts(int[] hits) {
        if (hits == null || hits.Length < 13) return (0, 0);
        int midspin = hits.Length > 14 ? hits[14] : 0;
        int perfect = hits[3] + hits[4] + hits[5] + hits[12] + midspin;
        int accurate = perfect + hits[2] + hits[6];
        return (perfect, accurate);
    }

    public static T FindAnyObjectByType<T>() where T : UnityEngine.Object {
#pragma warning disable CS0618
        return UnityEngine.Object.FindAnyObjectByType<T>() ?? UnityEngine.Object.FindObjectOfType<T>();
#pragma warning restore CS0618
    }

    public static void LoadScene(string name) {
        try {
            if (ADOBase.loader != null) ADOBase.loader.LoadScene(name);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(name);
        } catch {
            UnityEngine.SceneManagement.SceneManager.LoadScene(name);
        }
    }

    public static void RunAfter(Action action) {
        if (action == null) return;
        Task.Yield().GetAwaiter().OnCompleted(action);
    }

    private static int DigitCount(int value) {
        return value < 10 ? 1 :
            value < 100 ? 2 :
            value < 1000 ? 3 :
            value < 10000 ? 4 :
            value < 100000 ? 5 :
            value < 1000000 ? 6 :
            value < 10000000 ? 7 :
            value < 100000000 ? 8 :
            value < 1000000000 ? 9 : 10;
    }

    private static void AppendBalance(StringBuilder sb, int count) {
        if (count <= 0) return;
        sb.Append("<color=#0000>").Append('0', count).Append("</color>");
    }

    public static void AppendHitMarginText(StringBuilder sb, int[] hits, string prefix, string postfix) {
        if (hits == null || hits.Length < 13) return;
        bool competitive = Persistence.enableCompetitiveMode;
        int diff = DigitCount(hits[11]) + DigitCount(hits[0]) + DigitCount(hits[1]) + DigitCount(hits[2])
                   - DigitCount(hits[6]) - DigitCount(hits[7]) - DigitCount(hits[8]) - DigitCount(hits[10]);
        if (competitive) diff += DigitCount(hits[3]) - DigitCount(hits[5]);
        AppendBalance(sb, -diff);
        sb.Append(prefix).Append(hits[11])
            .Append(" <color=red>").Append(hits[0])
            .Append(" <color=#FF6F4E>").Append(hits[1])
            .Append(" <color=#A0FF4E>").Append(hits[2])
            .Append(" <color=#60FF4E>");
        if (competitive)
            sb.Append(hits[3]).Append(" <color=#FFF>").Append(hits[4] + hits[12]).Append("</color> ").Append(hits[5]);
        else sb.Append(hits[3] + hits[4] + hits[5] + hits[12]);
        sb.Append("</color> ").Append(hits[6])
            .Append("</color> ").Append(hits[7])
            .Append("</color> ").Append(hits[8])
            .Append("</color> ").Append(hits[10])
            .Append(postfix);
        AppendBalance(sb, diff);
    }

    public static void AppendHitMarginTextWithoutColor(StringBuilder sb, int[] hits, string prefix, string postfix) {
        if (hits == null || hits.Length < 13) return;
        bool competitive = Persistence.enableCompetitiveMode;
        int diff = DigitCount(hits[11]) + DigitCount(hits[0]) + DigitCount(hits[1]) + DigitCount(hits[2])
                   - DigitCount(hits[6]) - DigitCount(hits[7]) - DigitCount(hits[8]) - DigitCount(hits[10]);
        if (competitive) diff += DigitCount(hits[3]) - DigitCount(hits[5]);
        AppendBalance(sb, -diff);
        sb.Append(prefix).Append(hits[11])
            .Append(' ').Append(hits[0])
            .Append(' ').Append(hits[1])
            .Append(' ').Append(hits[2])
            .Append(' ');
        if (competitive)
            sb.Append(hits[3]).Append(' ').Append(hits[4] + hits[12]).Append(' ').Append(hits[5]);
        else sb.Append(hits[3] + hits[4] + hits[5] + hits[12]);
        sb.Append(' ').Append(hits[6])
            .Append(' ').Append(hits[7])
            .Append(' ').Append(hits[8])
            .Append(' ').Append(hits[10])
            .Append(postfix);
        AppendBalance(sb, diff);
    }
}
