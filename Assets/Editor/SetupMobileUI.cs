using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class SetupMobileUI
{
    public static void Execute()
    {
        // Canvas 찾기
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) { Debug.LogError("[SetupMobileUI] Canvas를 찾지 못했습니다."); return; }

        Canvas canvas = canvasGo.GetComponent<Canvas>();

        // ── MobileUI 루트 생성 ────────────────────────────────────────────────
        GameObject mobileRoot = new GameObject("MobileUI");
        mobileRoot.transform.SetParent(canvasGo.transform, false);
        RectTransform rootRt = mobileRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        // ── 버튼 색상 (반투명 흰색) ───────────────────────────────────────────
        Color btnColor = new Color(1f, 1f, 1f, 0.25f);

        // ── 왼쪽 이동 버튼 ────────────────────────────────────────────────────
        CreateButton(mobileRoot, "BtnLeft",  "◀",
            anchorMin: new Vector2(0f,   0f),
            anchorMax: new Vector2(0f,   0f),
            pivot:     new Vector2(0f,   0f),
            pos:       new Vector2(20f,  20f),
            size:      new Vector2(130f, 130f),
            color:     btnColor);

        // ── 오른쪽 이동 버튼 ──────────────────────────────────────────────────
        CreateButton(mobileRoot, "BtnRight", "▶",
            anchorMin: new Vector2(0f,   0f),
            anchorMax: new Vector2(0f,   0f),
            pivot:     new Vector2(0f,   0f),
            pos:       new Vector2(165f, 20f),
            size:      new Vector2(130f, 130f),
            color:     btnColor);

        // ── 점프 버튼 (오른쪽 하단) ───────────────────────────────────────────
        CreateButton(mobileRoot, "BtnJump", "JUMP",
            anchorMin: new Vector2(1f,   0f),
            anchorMax: new Vector2(1f,   0f),
            pivot:     new Vector2(1f,   0f),
            pos:       new Vector2(-20f, 20f),
            size:      new Vector2(130f, 130f),
            color:     new Color(0.4f, 0.8f, 1f, 0.35f));

        // ── 공격 버튼 (J, slot 0) ─────────────────────────────────────────────
        CreateButton(mobileRoot, "BtnAttack", "ATK",
            anchorMin: new Vector2(1f,   0f),
            anchorMax: new Vector2(1f,   0f),
            pivot:     new Vector2(1f,   0f),
            pos:       new Vector2(-165f, 20f),
            size:      new Vector2(130f,  130f),
            color:     new Color(1f, 0.5f, 0.2f, 0.35f));

        // ── 스킬 버튼 (K, slot 1) ─────────────────────────────────────────────
        CreateButton(mobileRoot, "BtnSkill", "SKL",
            anchorMin: new Vector2(1f,   0f),
            anchorMax: new Vector2(1f,   0f),
            pivot:     new Vector2(1f,   0f),
            pos:       new Vector2(-310f, 20f),
            size:      new Vector2(130f,  130f),
            color:     new Color(0.5f, 0.3f, 1f, 0.35f));

        // ── 힐 버튼 (L, slot 2, Hold) ─────────────────────────────────────────
        CreateButton(mobileRoot, "BtnHeal", "HEAL",
            anchorMin: new Vector2(1f,   0f),
            anchorMax: new Vector2(1f,   0f),
            pivot:     new Vector2(1f,   0f),
            pos:       new Vector2(-455f, 20f),
            size:      new Vector2(130f,  130f),
            color:     new Color(0.2f, 1f, 0.4f, 0.35f));

        // ── Player에 MobileInputController 추가 ──────────────────────────────
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[SetupMobileUI] Player를 찾지 못했습니다."); return; }

        MobileInputController mic = player.GetComponent<MobileInputController>();
        if (mic == null)
            mic = player.AddComponent<MobileInputController>();

        // ── Player에 InputRouter 추가 ─────────────────────────────────────────
        InputRouter router = player.GetComponent<InputRouter>();
        if (router == null)
            router = player.AddComponent<InputRouter>();

        // InputRouter 필드 연결
        SerializedObject soRouter = new SerializedObject(router);
        soRouter.FindProperty("keyboardController").objectReferenceValue = player.GetComponent<SpineInputController>();
        soRouter.FindProperty("mobileController").objectReferenceValue   = mic;
        soRouter.FindProperty("mobileUIRoot").objectReferenceValue       = mobileRoot;
        soRouter.ApplyModifiedProperties();

        // ── MobileButton 이벤트 연결 ──────────────────────────────────────────
        ConnectButton(mobileRoot, "BtnLeft",   mic, "OnLeftDown",  "OnLeftUp");
        ConnectButton(mobileRoot, "BtnRight",  mic, "OnRightDown", "OnRightUp");
        ConnectButton(mobileRoot, "BtnJump",   mic, "OnJumpDown",  null);
        ConnectButton(mobileRoot, "BtnAttack", mic, "OnSlot0Down", null);
        ConnectButton(mobileRoot, "BtnSkill",  mic, "OnSlot1Down", null);
        ConnectButton(mobileRoot, "BtnHeal",   mic, "OnSlot2Down", "OnSlot2Up");

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(mobileRoot);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SetupMobileUI] 완료 — 모바일 UI 생성 및 이벤트 연결 성공");
    }

    // ── 헬퍼: 버튼 GameObject 생성 ────────────────────────────────────────────
    private static GameObject CreateButton(GameObject parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin     = anchorMin;
        rt.anchorMax     = anchorMax;
        rt.pivot         = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta     = size;

        Image img = go.AddComponent<Image>();
        img.color = color;

        // 레이블 텍스트
        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        go.AddComponent<MobileButton>();
        return go;
    }

    // ── 헬퍼: MobileButton UnityEvent에 메서드 연결 ────────────────────────────
    private static void ConnectButton(GameObject root, string btnName,
        MobileInputController target, string pressMethod, string releaseMethod)
    {
        Transform t = root.transform.Find(btnName);
        if (t == null) { Debug.LogWarning($"[SetupMobileUI] {btnName}을 찾지 못했습니다."); return; }

        MobileButton btn = t.GetComponent<MobileButton>();
        if (btn == null) return;

        SerializedObject so = new SerializedObject(btn);

        if (!string.IsNullOrEmpty(pressMethod))
            AddPersistentListener(so.FindProperty("onPress"), target, pressMethod);

        if (!string.IsNullOrEmpty(releaseMethod))
            AddPersistentListener(so.FindProperty("onRelease"), target, releaseMethod);

        so.ApplyModifiedProperties();
    }

    private static void AddPersistentListener(SerializedProperty eventProp,
        MonoBehaviour target, string methodName)
    {
        SerializedProperty calls = eventProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
        calls.arraySize++;
        SerializedProperty call = calls.GetArrayElementAtIndex(calls.arraySize - 1);

        call.FindPropertyRelative("m_Target").objectReferenceValue  = target;
        call.FindPropertyRelative("m_MethodName").stringValue        = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex           = 1; // void
        call.FindPropertyRelative("m_CallState").enumValueIndex      = 2; // RuntimeOnly
    }
}
