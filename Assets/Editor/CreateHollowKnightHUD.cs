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

    [MenuItem("Tools/Hollow Knight HUD/Add or Update Geo Panel", false, 1)]
    public static void AddOrUpdateGeoPanel()
    {
        var gameHUD = UnityEngine.Object.FindObjectOfType<GameHUD>(true);
        if (gameHUD == null)
        {
            EditorUtility.DisplayDialog("Hollow Knight HUD", "씬에 HUD(GameHUD)가 없습니다. 먼저 HUD를 생성해 주세요.", "확인");
            return;
        }

        GeoUI existingGeo = gameHUD.GetComponentInChildren<GeoUI>(true);
        if (existingGeo != null)
        {
            Undo.DestroyObjectImmediate(existingGeo.gameObject);
        }

        GameObject geoPanel = CreateGeoPanel(gameHUD.transform);
        SerializedObject soHud = new SerializedObject(gameHUD);
        soHud.FindProperty("geoUI").objectReferenceValue = geoPanel.GetComponent<GeoUI>();
        soHud.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(gameHUD.gameObject.scene);
        Selection.activeGameObject = geoPanel;
        Undo.RegisterCreatedObjectUndo(geoPanel, "Add or Update Geo Panel");
        Debug.Log("[Hollow Knight HUD] 기존 HUD에 GeoPanel(이미지+텍스트)을 추가/갱신했습니다. GeoImage에 스프라이트를 할당해 주세요.");
    }

    [MenuItem("Tools/Hollow Knight HUD/Add Hit Flash to Existing HUD", false, 2)]
    public static void AddHitFlashToExistingHUD()
    {
        var gameHUD = UnityEngine.Object.FindObjectOfType<GameHUD>(true);
        if (gameHUD == null)
        {
            EditorUtility.DisplayDialog("Hollow Knight HUD", "씬에 HUD(GameHUD)가 없습니다. 먼저 HUD를 생성해 주세요.", "확인");
            return;
        }

        MaskUI maskUI = gameHUD.GetComponentInChildren<MaskUI>(true);
        if (maskUI == null)
        {
            EditorUtility.DisplayDialog("Hollow Knight HUD", "HUD 안에 MaskPanel(MaskUI)을 찾을 수 없습니다.", "확인");
            return;
        }

        Transform maskPanel = maskUI.transform;
        Transform existing = maskPanel.Find("HitFlashPanel");
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Hollow Knight HUD", "HitFlashPanel이 이미 있습니다.", "확인");
            return;
        }

        GameObject hitFlashPanel = CreateHitFlashPanel(maskPanel);
        hitFlashPanel.transform.SetAsLastSibling();
        SerializedObject soHud = new SerializedObject(gameHUD);
        soHud.FindProperty("hitFlashUI").objectReferenceValue = hitFlashPanel.GetComponent<HitFlashUI>();
        soHud.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(gameHUD.gameObject.scene);
        Selection.activeGameObject = hitFlashPanel;
        Undo.RegisterCreatedObjectUndo(hitFlashPanel, "Add Hit Flash to HUD");
        Debug.Log("[Hollow Knight HUD] 기존 HUD에 HitFlashPanel을 추가했습니다. Hit Sprites를 인스펙터에서 할당해 주세요.");
    }

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

        if (UnityEngine.Object.FindObjectOfType<GameHUD>() != null)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveOpenScenes())
                Debug.Log("[Hollow Knight HUD] 씬에 HUD가 생성되었고 씬이 저장되었습니다: " + scene.name);
        }
    }

    [MenuItem("Tools/Hollow Knight HUD/Add or Update Soul Panel", false, 3)]
    public static void AddOrUpdateSoulPanel()
    {
        var gameHUD = UnityEngine.Object.FindObjectOfType<GameHUD>(true);
        if (gameHUD == null)
        {
            EditorUtility.DisplayDialog("Hollow Knight HUD", "씬에 HUD(GameHUD)가 없습니다. 먼저 HUD를 생성해 주세요.", "확인");
            return;
        }

        SoulUI soulUI = gameHUD.GetComponentInChildren<SoulUI>(true);
        if (soulUI == null)
        {
            EditorUtility.DisplayDialog("Hollow Knight HUD", "HUD 안에 SoulUI(SoulPanel)가 없습니다.", "확인");
            return;
        }

        var frames = LoadAtlas0_308ChargingFrames();
        if (frames == null || frames.Length == 0)
        {
            EditorUtility.DisplayDialog("Hollow Knight HUD", "Assets/Resource/atlas0_308.png에서 충전 프레임을 찾지 못했습니다.", "확인");
            return;
        }

        SerializedObject so = new SerializedObject(soulUI);

        // SoulUI가 요구하는 계층/참조가 없으면 만들어서 확실히 연결합니다.
        // Frame / FillViewport(RectMask2D) / FillMaskShape(Mask) / FlowAnim
        EnsureSoulPanelReferences(soulUI, so);

        var framesProp = so.FindProperty("chargingFillFrames");
        if (framesProp != null)
        {
            framesProp.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        var durProp = so.FindProperty("chargingFrameDuration");
        if (durProp != null) durProp.floatValue = 0.06f;

        var animProp = so.FindProperty("animateWhenCharging");
        if (animProp != null) animProp.boolValue = true;

        // 만렙(풀 소울) 원형 컷: atlas0_333의 FullSoul을 fullFillSprite로 세팅
        var fullSoulSprite = LoadAtlas0_333FullSoulSprite();
        if (fullSoulSprite != null)
        {
            var fullFillSpriteProp = so.FindProperty("fullFillSprite");
            if (fullFillSpriteProp != null)
                fullFillSpriteProp.objectReferenceValue = fullSoulSprite;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(gameHUD.gameObject.scene);
        // 즉시 UI 갱신(에디터 미리보기/씬 렌더 반영용)
        soulUI.SetSoul(soulUI.GetSoul());
        Selection.activeGameObject = soulUI.gameObject;
        Debug.Log("[Hollow Knight HUD] 기존 SoulUI에 atlas0_308 충전 프레임을 다시 할당했습니다.");
    }

    [MenuItem("GameObject/UI/Hollow Knight HUD", false, 10)]
    public static void Create()
    {
        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
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
        // ---- Mask Panel (좌상단, Soul 위에 그려짐). HitFlash는 자식으로 마스크 위에 겹침 ----
        GameObject maskPanel = CreateMaskPanel(hudGo.transform);
        GameObject hitFlashPanel = CreateHitFlashPanel(maskPanel.transform);
        // ---- Geo Panel (우하단) ----
        GameObject geoPanel = CreateGeoPanel(hudGo.transform);

        // GameHUD에 참조 할당
        SerializedObject soHud = new SerializedObject(gameHUD);
        soHud.FindProperty("maskUI").objectReferenceValue = maskPanel.GetComponent<MaskUI>();
        soHud.FindProperty("hitFlashUI").objectReferenceValue = hitFlashPanel.GetComponent<HitFlashUI>();
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
        // MaskUI 필드명(slotHeight/slotWidth)과 일치시켜 null 참조를 방지합니다.
        var slotHeightProp = so.FindProperty("slotHeight");
        if (slotHeightProp != null)
            slotHeightProp.floatValue = DefaultMaskCellHeight;
        var slotWidthProp = so.FindProperty("slotWidth");
        if (slotWidthProp != null)
            slotWidthProp.floatValue = DefaultMaskCellWidth;
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

    private static GameObject CreateHitFlashPanel(Transform parent)
    {
        GameObject panel = new GameObject("HitFlashPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        SetFullStretch(rect);

        var layoutEl = panel.AddComponent<LayoutElement>();
        layoutEl.ignoreLayout = true;

        HitFlashUI hitFlash = panel.AddComponent<HitFlashUI>();
        SerializedObject so = new SerializedObject(hitFlash);
        so.FindProperty("frameDuration").floatValue = 0.06f;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(panel.transform, false);
        RectTransform iconRt = icon.AddComponent<RectTransform>();
        SetFullStretch(iconRt);
        Image img = icon.AddComponent<Image>();
        img.color = Color.white;
        img.preserveAspect = true;
        img.raycastTarget = false;

        so.Update();
        so.FindProperty("hitImage").objectReferenceValue = img;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.transform.SetAsLastSibling();
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

        // Frame(바깥 장식, 고정)
        // - vesselSprite는 Frame(Image)에 할당됩니다.
        GameObject vesselGo = new GameObject("Frame");
        vesselGo.transform.SetParent(panel.transform, false);
        RectTransform vesselRect = vesselGo.AddComponent<RectTransform>();
        SetFullStretch(vesselRect);
        Image vesselImg = vesselGo.AddComponent<Image>();
        vesselImg.color = new Color(0.15f, 0.12f, 0.2f, 0.9f);

        // FillViewport(세로로 차오르는 클립 영역)
        GameObject fillViewportGo = new GameObject("FillViewport");
        fillViewportGo.transform.SetParent(panel.transform, false);

        RectTransform fillViewportRect = fillViewportGo.AddComponent<RectTransform>();
        // bottom-anchored로 두고 높이를 sizeDelta.y로 제어합니다.
        fillViewportRect.anchorMin = new Vector2(0, 0);
        fillViewportRect.anchorMax = new Vector2(1, 0);
        fillViewportRect.pivot = new Vector2(0.5f, 0f);
        fillViewportRect.anchoredPosition = Vector2.zero;
        fillViewportRect.sizeDelta = new Vector2(0, SoulGaugeSize);

        RectMask2D rectMask = fillViewportGo.AddComponent<RectMask2D>();
        rectMask.padding = Vector4.zero;

        // FillMaskShape(원형 마스크 모양, Mask 1번: 스프라이트 알파로 클리핑)
        GameObject fillMaskShapeGo = new GameObject("FillMaskShape");
        fillMaskShapeGo.transform.SetParent(fillViewportGo.transform, false);

        RectTransform maskShapeRect = fillMaskShapeGo.AddComponent<RectTransform>();
        SetFullStretch(maskShapeRect);

        Image maskShapeImg = fillMaskShapeGo.AddComponent<Image>();
        maskShapeImg.color = Color.white;
        maskShapeImg.raycastTarget = false;
        maskShapeImg.preserveAspect = true;

        var fullSoulSpriteForMask = LoadAtlas0_333FullSoulSprite();
        if (fullSoulSpriteForMask != null)
            maskShapeImg.sprite = fullSoulSpriteForMask;

        Mask uiMask = fillMaskShapeGo.AddComponent<Mask>();
        uiMask.showMaskGraphic = false;

        // FlowAnim(가로 프레임 애니메이션 스프라이트 교체 대상)
        GameObject flowAnimGo = new GameObject("FlowAnim");
        flowAnimGo.transform.SetParent(fillMaskShapeGo.transform, false);

        RectTransform flowRect = flowAnimGo.AddComponent<RectTransform>();
        SetFullStretch(flowRect);

        Image flowAnimImg = flowAnimGo.AddComponent<Image>();
        flowAnimImg.color = new Color(0.9f, 0.95f, 1f, 0.95f); // 하얀 소울 톤
        flowAnimImg.type = Image.Type.Simple;
        flowAnimImg.preserveAspect = true;
        flowAnimImg.raycastTarget = false;

        SoulUI soulUI = panel.AddComponent<SoulUI>();
        SerializedObject so = new SerializedObject(soulUI);
        so.FindProperty("vesselImage").objectReferenceValue = vesselImg;
        so.FindProperty("fillAnimImage").objectReferenceValue = flowAnimImg;
        so.FindProperty("fillViewport").objectReferenceValue = fillViewportRect;
        so.FindProperty("maxSoul").intValue = 99;
        so.FindProperty("currentSoul").intValue = 50;

        // atlas0_308.png 스프라이트 시트의 모든 slice를 충전 애니메이션 프레임으로 자동 할당
        var frames = LoadAtlas0_308ChargingFrames();
        if (frames != null && frames.Length > 0)
        {
            var framesProp = so.FindProperty("chargingFillFrames");
            if (framesProp != null)
            {
                framesProp.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++)
                    framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }

            var durProp = so.FindProperty("chargingFrameDuration");
            if (durProp != null) durProp.floatValue = 0.06f;

            var animProp = so.FindProperty("animateWhenCharging");
            if (animProp != null) animProp.boolValue = true;
        }

        // 만렙(풀 소울) 원형 컷: atlas0_333의 FullSoul을 fullFillSprite로 세팅
        var fullSoulSprite = LoadAtlas0_333FullSoulSprite();
        if (fullSoulSprite != null)
        {
            var fullFillSpriteProp = so.FindProperty("fullFillSprite");
            if (fullFillSpriteProp != null)
                fullFillSpriteProp.objectReferenceValue = fullSoulSprite;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    /// <summary>
    /// 기존 씬의 SoulUI에 대해 FlowAnim / FillViewport / FillMaskShape 구조가 없으면 생성해서
    /// SoulUI가 요구하는 serialized references(vesselImage/fillAnimImage/fillViewport)를 연결합니다.
    /// </summary>
    private static void EnsureSoulPanelReferences(SoulUI soulUI, SerializedObject so)
    {
        if (soulUI == null) return;

        // Frame(Image)
        // Frame이 없으면 생성해서 SoulUI.vesselImage 연결합니다.
        Transform frameTf = soulUI.transform.Find("Frame");
        if (frameTf == null)
            frameTf = soulUI.transform.Find("VesselImage");

        Image vesselImg = frameTf != null ? frameTf.GetComponent<Image>() : null;

        if (vesselImg == null)
        {
            GameObject frameGo = new GameObject("Frame");
            frameGo.transform.SetParent(soulUI.transform, false);

            RectTransform rt = frameGo.AddComponent<RectTransform>();
            SetFullStretch(rt);

            vesselImg = frameGo.AddComponent<Image>();
            vesselImg.color = new Color(0.15f, 0.12f, 0.2f, 0.9f); // Frame 기본 톤

            frameTf = frameGo.transform;
            Undo.RegisterCreatedObjectUndo(frameGo, "Create Soul Frame");
        }

        if (vesselImg != null)
            so.FindProperty("vesselImage").objectReferenceValue = vesselImg;

        // FillViewport(RectTransform + RectMask2D)
        RectTransform fillViewportRect = soulUI.transform.Find("FillViewport") as RectTransform;
        if (fillViewportRect == null)
        {
            GameObject vpGo = new GameObject("FillViewport");
            vpGo.transform.SetParent(soulUI.transform, false);

            fillViewportRect = vpGo.AddComponent<RectTransform>();
            fillViewportRect.anchorMin = new Vector2(0, 0);
            fillViewportRect.anchorMax = new Vector2(1, 0);
            fillViewportRect.pivot = new Vector2(0.5f, 0f);
            fillViewportRect.anchoredPosition = Vector2.zero;
            fillViewportRect.sizeDelta = new Vector2(0, SoulGaugeSize);

            RectMask2D rectMask = vpGo.AddComponent<RectMask2D>();
            rectMask.padding = Vector4.zero;
        }
        so.FindProperty("fillViewport").objectReferenceValue = fillViewportRect;

        // FillMaskShape(Mask 1번)
        Transform maskShapeTf = soulUI.transform.Find("FillViewport/FillMaskShape");
        if (maskShapeTf == null)
        {
            GameObject maskGo = new GameObject("FillMaskShape");
            maskGo.transform.SetParent(fillViewportRect.transform, false);

            RectTransform maskRect = maskGo.AddComponent<RectTransform>();
            SetFullStretch(maskRect);

            Image maskImg = maskGo.AddComponent<Image>();
            maskImg.color = Color.white;
            maskImg.raycastTarget = false;
            maskImg.preserveAspect = true;

            var fullSoulSpriteForMask = LoadAtlas0_333FullSoulSprite();
            if (fullSoulSpriteForMask != null)
                maskImg.sprite = fullSoulSpriteForMask;

            Mask uiMask = maskGo.AddComponent<Mask>();
            uiMask.showMaskGraphic = false;
        }

        maskShapeTf = soulUI.transform.Find("FillViewport/FillMaskShape");
        if (maskShapeTf == null) return;

        // FlowAnim(Image)
        Transform flowTf = maskShapeTf.Find("FlowAnim");
        Image flowImg = flowTf != null ? flowTf.GetComponent<Image>() : null;

        if (flowImg == null)
        {
            // 기존 FillImage가 있으면 그대로 FlowAnim로 재사용합니다.
            Transform oldFillTf = soulUI.transform.Find("FillImage");
            if (oldFillTf != null)
            {
                flowImg = oldFillTf.GetComponent<Image>();
                oldFillTf.SetParent(maskShapeTf, false);
                oldFillTf.name = "FlowAnim";

                RectTransform rt = oldFillTf.GetComponent<RectTransform>();
                if (rt != null) SetFullStretch(rt);
            }
        }

        if (flowImg == null)
        {
            GameObject flowGo = new GameObject("FlowAnim");
            flowGo.transform.SetParent(maskShapeTf, false);

            RectTransform flowRect = flowGo.AddComponent<RectTransform>();
            SetFullStretch(flowRect);

            flowImg = flowGo.AddComponent<Image>();
            flowImg.color = new Color(0.9f, 0.95f, 1f, 0.95f);
            flowImg.type = Image.Type.Simple;
            flowImg.preserveAspect = true;
            flowImg.raycastTarget = false;
        }

        so.FindProperty("fillAnimImage").objectReferenceValue = flowImg;
    }

    private static Sprite[] LoadAtlas0_308ChargingFrames()
    {
        // 프로젝트 내 실제 경로: Assets/Resource/atlas0_308.png
        const string path = "Assets/Resource/atlas0_308.png";
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets == null || assets.Length == 0) return null;

        var list = new System.Collections.Generic.List<Sprite>();
        foreach (var a in assets)
        {
            if (a is Sprite s && !string.IsNullOrEmpty(s.name) && s.name.StartsWith("atlas0_308_"))
                list.Add(s);
        }

        if (list.Count == 0) return null;

        list.Sort((a, b) => ExtractAtlas0_308Index(a.name).CompareTo(ExtractAtlas0_308Index(b.name)));
        return list.ToArray();
    }

    private static Sprite LoadAtlas0_333FullSoulSprite()
    {
        // 프로젝트 내 실제 경로: Assets/Resource/atlas0_333.png
        const string path = "Assets/Resource/atlas0_333.png";
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets == null || assets.Length == 0) return null;

        foreach (var a in assets)
        {
            if (a is Sprite s && s.name == "FullSoul")
                return s;
        }

        return null;
    }

    private static int ExtractAtlas0_308Index(string spriteName)
    {
        const string prefix = "atlas0_308_";
        if (string.IsNullOrEmpty(spriteName)) return int.MaxValue;
        if (!spriteName.StartsWith(prefix)) return int.MaxValue;

        var suffix = spriteName.Substring(prefix.Length);
        return int.TryParse(suffix, out int idx) ? idx : int.MaxValue;
    }

    private static GameObject CreateGeoPanel(Transform parent)
    {
        const int geoIconSize = 36;
        const int geoSpacing = 8;
        const int rowSpacing = 4;

        GameObject panel = new GameObject("GeoPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-24, 24);
        rect.sizeDelta = new Vector2(120, 72);

        VerticalLayoutGroup panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        panelLayout.spacing = rowSpacing;
        panelLayout.childAlignment = TextAnchor.MiddleRight;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = false;
        panelLayout.childForceExpandHeight = false;

        GeoUI geoUI = panel.AddComponent<GeoUI>();

        // 첫 번째 줄: 아이콘 + 메인 지오 텍스트
        GameObject mainRow = new GameObject("MainRow");
        mainRow.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup rowLayout = mainRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = geoSpacing;
        rowLayout.childAlignment = TextAnchor.MiddleRight;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        LayoutElement mainRowLe = mainRow.AddComponent<LayoutElement>();
        mainRowLe.preferredHeight = 40;
        mainRowLe.flexibleHeight = 0f;

        GameObject imageGo = new GameObject("GeoImage");
        imageGo.transform.SetParent(mainRow.transform, false);
        RectTransform imageRect = imageGo.AddComponent<RectTransform>();
        imageRect.sizeDelta = new Vector2(geoIconSize, geoIconSize);
        LayoutElement imageLe = imageGo.AddComponent<LayoutElement>();
        imageLe.preferredWidth = geoIconSize;
        imageLe.preferredHeight = geoIconSize;
        imageLe.flexibleWidth = 0f;
        imageLe.flexibleHeight = 0f;

        Image geoImg = imageGo.AddComponent<Image>();
        geoImg.color = new Color(1f, 0.9f, 0.4f, 0.95f);
        geoImg.preserveAspect = true;
        geoImg.raycastTarget = false;

        GameObject textGo = new GameObject("GeoText");
        textGo.transform.SetParent(mainRow.transform, false);
        LayoutElement textLe = textGo.AddComponent<LayoutElement>();
        textLe.preferredWidth = 60;
        textLe.flexibleWidth = 1f;
        textLe.minHeight = 32;

        Text text = textGo.AddComponent<Text>();
        text.text = "0";
        text.fontSize = GeoFontSize;
        text.alignment = TextAnchor.MiddleRight;
        text.color = Color.white;
        if (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") != null)
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 두 번째 줄: 대기 지오 텍스트 (쌓였다가 일정 시간 후 메인에 반영)
        GameObject pendingGo = new GameObject("PendingText");
        pendingGo.transform.SetParent(panel.transform, false);
        LayoutElement pendingLe = pendingGo.AddComponent<LayoutElement>();
        pendingLe.preferredHeight = 22;
        pendingLe.flexibleWidth = 1f;

        Text pendingText = pendingGo.AddComponent<Text>();
        pendingText.text = "";
        pendingText.fontSize = 18;
        pendingText.alignment = TextAnchor.MiddleRight;
        pendingText.color = new Color(1f, 0.95f, 0.6f, 0.9f);
        if (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") != null)
            pendingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        SerializedObject so = new SerializedObject(geoUI);
        so.FindProperty("geoImage").objectReferenceValue = geoImg;
        so.FindProperty("geoText").objectReferenceValue = text;
        so.FindProperty("pendingText").objectReferenceValue = pendingText;
        so.FindProperty("currentGeo").intValue = 0;
        so.FindProperty("pendingFontSize").intValue = 18;
        so.FindProperty("commitDelay").floatValue = 1.5f;
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
