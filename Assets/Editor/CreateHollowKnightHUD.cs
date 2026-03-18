using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 메뉴에서 "GameObject > UI > Hollow Knight HUD" 선택 시
/// 캔버스와 스크립트 구조에 맞는 HUD 계층을 자동 생성합니다.
/// "Tools > Hollow Knight HUD > Create in Scene and Save" 로 씬에 생성 후 저장합니다.
/// </summary>
public static class CreateHollowKnightHUD
{
    private const int DefaultMaskCount = 5;
    private const float DefaultMaskCellHeight = 70f;  // 55x70 / 70x55 마스크용
    private const float DefaultMaskCellWidth = 55f;
    private const int MaskSpacing = 8;
    private const int SoulGaugeSize = 80;
    private const int GeoFontSize = 24;

    [MenuItem("Tools/Hollow Knight HUD/Create in Scene and Save", false, 0)]
    public static void CreateInSceneAndSave()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Hollow Knight HUD", "씬을 먼저 열어 주세요.", "확인");
            return;
        }

        Create();

        if (Object.FindObjectOfType<GameHUD>() != null)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveOpenScenes())
                Debug.Log("[Hollow Knight HUD] 씬에 HUD가 생성되었고 씬이 저장되었습니다: " + scene.name);
        }
    }

    [MenuItem("GameObject/UI/Hollow Knight HUD", false, 10)]
    public static void Create()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            canvas = CreateCanvas();
        else
        {
            canvas = canvas.rootCanvas;
            FixCanvasRect(canvas);
        }

        if (canvas.transform.childCount > 0)
        {
            var existing = canvas.GetComponentInChildren<GameHUD>(true);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[Hollow Knight HUD] 씬에 이미 HUD가 있습니다: " + existing.name);
                return;
            }
        }

        Transform root = canvas.transform;
        GameObject hudGo = new GameObject("HUD");
        hudGo.transform.SetParent(root, false);

        RectTransform hudRect = hudGo.AddComponent<RectTransform>();
        SetFullStretch(hudRect);
        CanvasGroup cg = hudGo.AddComponent<CanvasGroup>();
        GameHUD gameHUD = hudGo.AddComponent<GameHUD>();

        // 렌더 순서(형제 순서)가 중요:
        // - 먼저 있는(위에 있는) 오브젝트가 뒤에 그려지고
        // - 나중에 있는(아래 있는) 오브젝트가 위에 그려집니다.
        // SoulPanel이 MaskPanel 뒤로 가려지게 하려면 SoulPanel을 먼저 만들고,
        // MaskPanel을 나중에 만들어서 위에 그려지게 합니다.

        // ---- Soul Panel (마스크 뒤, 아래쪽 위치) ----
        GameObject soulPanel = CreateSoulPanel(hudGo.transform);
        // ---- Mask Panel (좌상단, Soul 위에 그려짐) ----
        GameObject maskPanel = CreateMaskPanel(hudGo.transform);
        // ---- Geo Panel (우하단) ----
        GameObject geoPanel = CreateGeoPanel(hudGo.transform);

        // GameHUD에 참조 할당
        SerializedObject soHud = new SerializedObject(gameHUD);
        soHud.FindProperty("maskUI").objectReferenceValue = maskPanel.GetComponent<MaskUI>();
        soHud.FindProperty("soulUI").objectReferenceValue = soulPanel.GetComponent<SoulUI>();
        soHud.FindProperty("geoUI").objectReferenceValue = geoPanel.GetComponent<GeoUI>();
        soHud.FindProperty("hudCanvasGroup").objectReferenceValue = cg;
        soHud.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = hudGo;
        Undo.RegisterCreatedObjectUndo(hudGo, "Create Hollow Knight HUD");
    }

    private static void FixCanvasRect(Canvas canvas)
    {
        var rt = canvas.GetComponent<RectTransform>();
        if (rt == null) return;
        if (rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one || rt.localScale == Vector3.zero)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }
    }

    private static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("Canvas");
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        return c;
    }

    private static GameObject CreateMaskPanel(Transform parent)
    {
        GameObject panel = new GameObject("MaskPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(24, -24);
        float maskW = DefaultMaskCellWidth * DefaultMaskCount + MaskSpacing * (DefaultMaskCount - 1);
        rect.sizeDelta = new Vector2(maskW, DefaultMaskCellHeight + 16);

        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = MaskSpacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        MaskUI maskUI = panel.AddComponent<MaskUI>();
        SerializedObject so = new SerializedObject(maskUI);
        so.FindProperty("maxMasks").intValue = DefaultMaskCount;
        so.FindProperty("maskCellHeight").floatValue = DefaultMaskCellHeight;
        so.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < DefaultMaskCount; i++)
        {
            // 슬롯(컨테이너)
            GameObject slot = new GameObject("Mask_" + i);
            slot.transform.SetParent(panel.transform, false);
            RectTransform slotRt = slot.AddComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(DefaultMaskCellWidth, DefaultMaskCellHeight);

            var le = slot.AddComponent<LayoutElement>();
            le.preferredWidth = DefaultMaskCellWidth;
            le.preferredHeight = DefaultMaskCellHeight;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            // 실제 아이콘(Image) - 슬롯 안에서 Stretch + PreserveAspect
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(slot.transform, false);
            RectTransform iconRt = icon.AddComponent<RectTransform>();
            SetFullStretch(iconRt);

            Image img = icon.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.9f);
            img.preserveAspect = true;
        }

        return panel;
    }

    private static GameObject CreateSoulPanel(Transform parent)
    {
        GameObject panel = new GameObject("SoulPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(24, -24 - (int)DefaultMaskCellHeight - 24);
        rect.sizeDelta = new Vector2(SoulGaugeSize, SoulGaugeSize);

        // 용기 배경 이미지 (나중에 스프라이트 할당)
        GameObject vesselGo = new GameObject("VesselImage");
        vesselGo.transform.SetParent(panel.transform, false);
        RectTransform vesselRect = vesselGo.AddComponent<RectTransform>();
        SetFullStretch(vesselRect);
        Image vesselImg = vesselGo.AddComponent<Image>();
        vesselImg.color = new Color(0.15f, 0.12f, 0.2f, 0.9f);

        // 채움 이미지 (Mask/Filled로 게이지 표시) — 용기 모양 스프라이트 넣으면 용기 안이 차오르는 느낌
        GameObject fillGo = new GameObject("FillImage");
        fillGo.transform.SetParent(panel.transform, false);
        RectTransform fillRect = fillGo.AddComponent<RectTransform>();
        SetFullStretch(fillRect);
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.9f, 0.95f, 1f, 0.95f);  // 하얀 소울 톤
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Vertical;
        fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;  // 세로로 아래→위 채움
        fillImg.fillAmount = 0.5f;

        SoulUI soulUI = panel.AddComponent<SoulUI>();
        SerializedObject so = new SerializedObject(soulUI);
        so.FindProperty("vesselImage").objectReferenceValue = vesselImg;
        so.FindProperty("fillImage").objectReferenceValue = fillImg;
        so.FindProperty("fillDirection").enumValueIndex = (int)SoulUI.FillDirection.VerticalBottom;
        so.FindProperty("maxSoul").intValue = 99;
        so.FindProperty("currentSoul").intValue = 50;
        so.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    private static GameObject CreateGeoPanel(Transform parent)
    {
        GameObject panel = new GameObject("GeoPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-24, 24);
        rect.sizeDelta = new Vector2(120, 40);

        GeoUI geoUI = panel.AddComponent<GeoUI>();

        GameObject textGo = new GameObject("GeoText");
        textGo.transform.SetParent(panel.transform, false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        SetFullStretch(textRect);

        Text text = textGo.AddComponent<Text>();
        text.text = "0";
        text.fontSize = GeoFontSize;
        text.alignment = TextAnchor.MiddleRight;
        text.color = Color.white;
        if (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") != null)
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        SerializedObject so = new SerializedObject(geoUI);
        so.FindProperty("geoText").objectReferenceValue = text;
        so.FindProperty("currentGeo").intValue = 0;
        so.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    private static void SetFullStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
