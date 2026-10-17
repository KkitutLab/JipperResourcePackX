using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JipperResourcePackX;

public class ResourceChanger : Feature {
    public static Color PlanetColor = new(0.8125f, 0.70703125f, 0.96875f);
    public static Color TitleColor = new(0.56640625f, 0.46875f, 0.6328125f);
    public static Color TileColor = new(0.94921875f, 0.87109375f, 1);
    public static string ResourcePackName = "Jipper Resource Pack";
    private static Sprite _autoSprite;
    public static ResourceChanger Instance;
    public static ResourceChangerSetting Settings => JipperSettings.Instance.ResourceChanger;

    public ResourceChanger() : base(nameof(ResourceChanger), true) {
        Instance = this;
    }

    public static void PatchAll(HarmonyLib.Harmony harmony) {
        Type planetType = SafePatch.GetMethodSafe("PlanetRenderer", "SetRainbow") != null
            ? Type.GetType("PlanetRenderer, Assembly-CSharp") ?? typeof(scrPlanet)
            : typeof(scrPlanet);

        MethodInfo mPrefix = typeof(ResourceChanger).GetMethod(nameof(Prefix), BindingFlags.Public | BindingFlags.Static);
        MethodInfo mPrefixSetRainbow = typeof(ResourceChanger).GetMethod(nameof(PrefixSetRainbow), BindingFlags.Public | BindingFlags.Static);
        MethodInfo mPostfixPlanetColor = typeof(ResourceChanger).GetMethod(nameof(PostfixPlanetColor), BindingFlags.Public | BindingFlags.Static);

        foreach (string methodName in new[] { "SetPlanetColor", "SetCoreColor", "SetTailColor", "SetRingColor", "SetFaceColor" }) {
            SafePatchManager.Apply(harmony, $"ResourceChanger_{methodName}", planetType, methodName, prefix: mPrefix);
        }
        SafePatchManager.Apply(harmony, "ResourceChanger_SetRainbow", planetType, "SetRainbow", prefix: mPrefixSetRainbow);
        SafePatchManager.Apply(harmony, "ResourceChanger_LoadPlanetColor", planetType, "LoadPlanetColor", postfix: mPostfixPlanetColor);
        SafePatchManager.Apply(harmony, "ResourceChanger_SetColor", planetType, "SetColor", postfix: mPostfixPlanetColor);

        SafePatchManager.Apply(harmony, "ResourceChanger_OttoUpdate", "scnEditor", "OttoUpdate",
            postfix: typeof(ResourceChanger).GetMethod(nameof(OnEditorStart), BindingFlags.Public | BindingFlags.Static));
        SafePatchManager.Apply(harmony, "ResourceChanger_FloorStart", "scrFloor", "Start",
            postfix: typeof(ResourceChanger).GetMethod(nameof(OnFloorStart), BindingFlags.Public | BindingFlags.Static));
        SafePatchManager.Apply(harmony, "ResourceChanger_PlanetStart", "scrPlanet", "Start",
            postfix: typeof(ResourceChanger).GetMethod(nameof(OnPlanetStart), BindingFlags.Public | BindingFlags.Static));
        SafePatchManager.Apply(harmony, "ResourceChanger_SetTileColor", "scrFloor", "SetTileColor",
            prefix: typeof(ResourceChanger).GetMethod(nameof(PrefixSetTileColor), BindingFlags.Public | BindingFlags.Static));
        SafePatchManager.Apply(harmony, "ResourceChanger_LogoAwake", "scrLogoText", "Awake",
            postfix: typeof(ResourceChanger).GetMethod(nameof(OnLogoTextAwake), BindingFlags.Public | BindingFlags.Static));
        SafePatchManager.Apply(harmony, "ResourceChanger_LogoUpdateColors", "scrLogoText", "UpdateColors",
            postfix: typeof(ResourceChanger).GetMethod(nameof(PostfixLogoUpdateColors), BindingFlags.Public | BindingFlags.Static));
    }

    public override void OnEnable() {
        if (Settings.ChangeRabbit) LoadRabbit();
        if (Settings.ChangeBallColor) LoadPlanet();
        if (Settings.ChangeTileColor) LoadTileColor();
    }

    public override void OnDisable() {
        try {
            if (Settings != null) {
                if (Settings.ChangeRabbit) UnloadRabbit();
                if (Settings.ChangeBallColor) UnloadPlanet();
                if (Settings.ChangeTileColor) UnloadTileColor();
            }
        } catch (Exception e) {
            ModLogger.Error($"[ResourceChanger] Failed to disable: {e.Message}");
        }
    }

    public override void OnGUI() {
        SettingGUI settingGUI = Main.SettingGUI;
        Localization localization = Localization.Current;
        settingGUI.AddSettingToggle(ref Settings.ChangeRabbit, localization["resourceChanger.changeRabbit"], () => {
            if (Settings.ChangeRabbit) LoadRabbit();
            else UnloadRabbit();
        });
        settingGUI.AddSettingToggle(ref Settings.ChangeBallColor, localization["resourceChanger.changeBallColor"], () => {
            if (Settings.ChangeBallColor) LoadPlanet();
            else UnloadPlanet();
        });
        settingGUI.AddSettingToggle(ref Settings.ChangeTileColor, localization["resourceChanger.changeTileColor"], () => {
            if (Settings.ChangeTileColor) LoadTileColor();
            else UnloadTileColor();
        });
    }

    private static void LoadRabbit() {
        if (ADOBase.editor) OnEditorStart();
    }

    private static List<scrPlanet> GetAllPlanets() {
        try {
            if (ADOBase.controller?.planetarySystem?.allPlanets != null)
                return ADOBase.controller.planetarySystem.allPlanets;
            if (ADOBase.controller?.playerOne?.planetarySystem?.allPlanets != null)
                return ADOBase.controller.playerOne.planetarySystem.allPlanets;
        } catch { }
        return [];
    }

    private static void LoadPlanet() {
        try {
            foreach (scrPlanet planet in GetAllPlanets()) OnPlanetStart(planet);
            scrLogoText logoText = SafeAccess.FindAnyObjectByType<scrLogoText>();
            if (logoText) {
                logoText.ColorLogoSafe(PlanetColor, true);
                logoText.ColorLogoSafe(PlanetColor, false);
            }
        } catch { }
    }

    private static void UnloadRabbit() {
        try {
            if (_autoSprite && ADOBase.editor && ADOBase.editor.autoImage) {
                ADOBase.editor.autoImage.sprite = _autoSprite;
                ADOBase.editor.Invoke("OttoUpdate", 0f);
            }
        } catch { }
    }

    private static void UnloadPlanet() {
        try {
            var ps = ADOBase.controller?.planetarySystem ?? ADOBase.controller?.playerOne?.planetarySystem;
            if (ps != null) {
                ps.planetRed?.planetRenderer?.LoadPlanetColor(true);
                ps.planetBlue?.planetRenderer?.LoadPlanetColor(false);
            }
        } catch { }
        try {
            if (scrLogoText.instance) scrLogoText.instance.UpdateColors();
        } catch { }
    }

    private static void LoadTileColor() {
        try {
            foreach (scrFloor floor in Object.FindObjectsByType<scrFloor>(FindObjectsSortMode.None)) {
                if (!floor || floor.tag != "Beat") continue;
                if (floor.floorRenderer != null) floor.floorRenderer.color = TileColor;
            }
        } catch { }
    }

    private static void UnloadTileColor() {
        try {
            foreach (scrFloor floor in Object.FindObjectsByType<scrFloor>(FindObjectsSortMode.None)) {
                if (!floor || floor.tag != "Beat") continue;
                if (floor.floorRenderer != null) floor.floorRenderer.color = new Color(0.675f, 0.675f, 0.766f, 1f);
            }
        } catch { }
    }

    public static void OnEditorStart() {
        try {
            if (!Settings.ChangeRabbit || !scnEditor.instance) return;
            Image autoImage = scnEditor.instance.autoImage;
            if (!autoImage || autoImage.sprite == AssetLoader.Auto) return;
            _autoSprite = autoImage.sprite;
            autoImage.sprite = AssetLoader.Auto;
            autoImage.color = RDC.auto ? new Color(0.5703125f, 0, 1) : new Color(0.19607843f, 0, 0.32941177f);
        } catch { }
    }
    
    public static void OnFloorStart(scrFloor __instance) {
        try {
            if (!Settings.ChangeTileColor || __instance == null || __instance.tag != "Beat") return;
            if (__instance.floorRenderer != null) __instance.floorRenderer.color = TileColor;
        } catch { }
    }
    
    public static void OnPlanetStart(scrPlanet __instance) {
        if (!Settings.ChangeBallColor || SafeAccess.IsCoopMode() || __instance == null) return;
        try {
            var pr = __instance.planetRenderer;
            if (pr != null) {
                pr.DisableAllSpecialPlanets();
                if (pr.sprite != null) pr.sprite.sprite = ADOBase.gc.tex_planetWhite;
                pr.SetPlanetColor(PlanetColor);
                pr.SetTailColor(PlanetColor);
                pr.SetRainbow(false);
            }
        } catch { }
    }

    public static void Prefix(ref Color color) {
        try {
            if (Settings.ChangeBallColor && !SafeAccess.IsCoopMode()) color = PlanetColor;
        } catch { }
    }

    public static void PrefixSetRainbow(ref bool enabled) {
        try {
            if (Settings.ChangeBallColor && !SafeAccess.IsCoopMode()) enabled = false;
        } catch { }
    }

    public static void PostfixPlanetColor(object __instance) {
        try {
            if (!Settings.ChangeBallColor || SafeAccess.IsCoopMode() || __instance == null) return;
            if (__instance is PlanetRenderer pr) {
                pr.DisableAllSpecialPlanets();
                if (pr.sprite != null) pr.sprite.sprite = ADOBase.gc.tex_planetWhite;
                pr.SetPlanetColor(PlanetColor);
                pr.SetTailColor(PlanetColor);
                pr.SetRainbow(false);
            } else if (__instance is scrPlanet sp && sp.planetRenderer != null) {
                sp.planetRenderer.DisableAllSpecialPlanets();
                if (sp.planetRenderer.sprite != null) sp.planetRenderer.sprite.sprite = ADOBase.gc.tex_planetWhite;
                sp.planetRenderer.SetPlanetColor(PlanetColor);
                sp.planetRenderer.SetTailColor(PlanetColor);
                sp.planetRenderer.SetRainbow(false);
            }
        } catch { }
    }

    public static void PrefixSetTileColor(ref Color color, scrFloor __instance) {
        try {
            if (Settings.ChangeTileColor && __instance != null && __instance.tag == "Beat") color = TileColor;
        } catch { }
    }

    public static void PostfixLogoUpdateColors(scrLogoText __instance) {
        try {
            if (!Settings.ChangeBallColor || SafeAccess.IsCoopMode() || __instance == null) return;
            __instance.ColorLogoSafe(PlanetColor, true);
            __instance.ColorLogoSafe(PlanetColor, false);
        } catch { }
    }
    
    public static void OnLogoTextAwake(scrLogoText __instance) {
        try {
            if (__instance == null || (object)__instance.gameObject == null) return;
            RectTransform rectTransform = __instance.gameObject.GetComponent<RectTransform>();
            if (rectTransform != null) {
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, 0.75f);
                if (Settings.ChangeBallColor && !SafeAccess.IsCoopMode()) {
                    __instance.ColorLogoSafe(PlanetColor, true);
                    __instance.ColorLogoSafe(PlanetColor, false);
                }
                Transform hitSpace = rectTransform.parent?.parent?.Find("Hit Space");
                if (hitSpace != null && !hitSpace.Find("JipperResourcepack Logo")) {
                    var edu = hitSpace.Find("Education Edition");
                    if (edu != null) {
                        GameObject gameObject = Object.Instantiate(edu.gameObject, hitSpace);
                        gameObject.SetActive(true);
                        gameObject.name = "JipperResourcepack Logo";
                        Text text = gameObject.GetComponent<Text>();
                        if (text != null) {
                            text.text = ResourcePackName;
                            text.color = TitleColor;
                            text.fontSize = 100;
                        }
                        var rt = gameObject.GetComponent<RectTransform>();
                        if (rt != null) rt.anchoredPosition = new Vector2(-50, 330);
                    }
                }
            }
        } catch (Exception ex) {
            ModLogger.Warn($"[ResourceChanger] OnLogoTextAwake error: {ex.Message}");
        }
    }
}

public class ResourceChangerSetting {
    public bool ChangeRabbit = true;
    public bool ChangeBallColor = true;
    public bool ChangeTileColor = true;
}