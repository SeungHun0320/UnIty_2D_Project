using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

// MaskUI의 maskFullSprites/hitSprites를 읽어 각 Mask_* 슬롯에 Animator를 자동으로 설정합니다.
// 프레임 지속 시간은 DefaultFrameDuration 상수로 관리합니다.
public class SetupMaskAnimator
{
    private const string OutputFolder = "Assets/Animations/UI/Mask";

    // MaskUI에서 spriteFrameDuration 필드가 제거됐으므로 여기서 직접 관리합니다.
    private const float DefaultFrameDuration = 0.12f;
    private const string ConfigPath = "Assets/Animations/UI/Mask/MaskAnimatorConfig.asset";

    public static void Execute()
    {
        // 1. 씬에서 컴포넌트 탐색
        MaskUI    maskUI    = Object.FindObjectOfType<MaskUI>();
        HitFlashUI hitFlash = Object.FindObjectOfType<HitFlashUI>();
        if (maskUI == null) { Debug.LogError("[SetupMaskAnimator] MaskUI를 찾을 수 없습니다."); return; }

        // 2. MaskUI 데이터 읽기
        SerializedObject maskSO = new SerializedObject(maskUI);
        Sprite[]  fullSprites   = ReadSpriteArray(maskSO, "maskFullSprites");
        Sprite    emptySprite   = maskSO.FindProperty("maskEmpty").objectReferenceValue as Sprite;
        float     frameDuration = DefaultFrameDuration;
        Vector3[] rotations     = null; // MaskUI에서 제거됨 — 필요 시 여기에 직접 입력

        // 3. HitFlashUI 데이터 읽기
        Sprite[] hitSprites      = null;
        float    hitFrameDur     = 0.06f;
        if (hitFlash != null)
        {
            SerializedObject hitSO = new SerializedObject(hitFlash);
            hitSprites   = ReadSpriteArray(hitSO, "hitSprites");
            hitFrameDur  = hitSO.FindProperty("frameDuration").floatValue;
        }

        // 4. 출력 폴더 생성
        EnsureFolderPath(OutputFolder);

        // 5. MaskAnimatorConfig SO 로드 (없으면 스케일 커브 스킵)
        MaskAnimatorConfig config = AssetDatabase.LoadAssetAtPath<MaskAnimatorConfig>(ConfigPath);
        float[] hitFrameScales = config?.hitFrameScales;

        // 6. 애니메이션 클립 생성
        AnimationClip fullClip  = CreateFullClip(fullSprites, rotations, frameDuration);
        AnimationClip hitClip   = hitSprites != null && hitSprites.Length > 0
                                  ? CreateHitClip(hitSprites, hitFrameDur, hitFrameScales) : null;
        AnimationClip emptyClip = CreateEmptyClip(emptySprite);

        SaveClip(fullClip,  $"{OutputFolder}/MaskFull.anim");
        if (hitClip  != null) SaveClip(hitClip,  $"{OutputFolder}/MaskHit.anim");
        SaveClip(emptyClip, $"{OutputFolder}/MaskEmpty.anim");

        // 6. AnimatorController 생성
        string ctrlPath = $"{OutputFolder}/MaskSlot.controller";
        AnimatorController ctrl = CreateController(fullClip, hitClip, emptyClip, ctrlPath);

        // 7. 각 Mask_* 슬롯에 Animator 적용
        ApplyAnimatorToSlots(maskUI, ctrl);

        // 8. MaskUI의 maskAnimatorController 필드에 자동 할당
        SerializedObject maskSOFinal = new SerializedObject(maskUI);
        maskSOFinal.FindProperty("maskAnimatorController").objectReferenceValue = ctrl;
        maskSOFinal.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[SetupMaskAnimator] 완료! 각 Mask 슬롯에 Animator 적용됨.");
    }

    // ── 클립 생성 ─────────────────────────────────────────────────────────────

    // MaskFull: 스프라이트 루프 + 인덱스별 회전값 (Constant 탄젠트)
    private static AnimationClip CreateFullClip(Sprite[] sprites, Vector3[] rotations, float frameDur)
    {
        AnimationClip clip = new AnimationClip { name = "MaskFull" };
        clip.frameRate = Mathf.Max(1f, Mathf.Round(1f / Mathf.Max(frameDur, 0.001f)));

        if (sprites != null && sprites.Length > 0)
        {
            int count = sprites.Length;
            string iconPath = "Icon";

            // 스프라이트 키프레임 (루프용: 마지막에 첫 프레임 반복)
            var spriteKeys = new ObjectReferenceKeyframe[count + 1];
            for (int i = 0; i <= count; i++)
                spriteKeys[i] = new ObjectReferenceKeyframe
                {
                    time  = i * frameDur,
                    value = sprites[i % count]
                };
            var spriteBinding = EditorCurveBinding.PPtrCurve(iconPath, typeof(UnityEngine.UI.Image), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeys);

            // 회전 Z 키프레임 (인덱스별 회전값 → Constant 스텝)
            if (rotations != null && rotations.Length > 0)
            {
                AnimationCurve rotCurve = new AnimationCurve();
                for (int i = 0; i <= count; i++)
                {
                    float rotZ = (rotations.Length > 0) ? rotations[i % Mathf.Min(count, rotations.Length)].z : 0f;
                    int ki = rotCurve.AddKey(new Keyframe(i * frameDur, rotZ));
                    AnimationUtility.SetKeyLeftTangentMode(rotCurve,  ki, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(rotCurve, ki, AnimationUtility.TangentMode.Constant);
                }
                clip.SetCurve(iconPath, typeof(RectTransform), "localEulerAngles.z", rotCurve);
            }
        }

        // 루프 설정
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    // MaskHit: 피격 스프라이트 시퀀스 (1회 재생) + 프레임별 스케일 커브 (Constant 스텝)
    private static AnimationClip CreateHitClip(Sprite[] sprites, float frameDur, float[] frameScales)
    {
        AnimationClip clip = new AnimationClip { name = "MaskHit" };
        clip.frameRate = Mathf.Max(1f, Mathf.Round(1f / Mathf.Max(frameDur, 0.001f)));

        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i * frameDur, value = sprites[i] };

        var binding = EditorCurveBinding.PPtrCurve("Icon", typeof(UnityEngine.UI.Image), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        // 슬롯 루트에 프레임별 Constant 스케일 커브 추가 (config SO가 있을 때만)
        if (frameScales != null && frameScales.Length > 0)
        {
            AnimationCurve scaleXY = new AnimationCurve();
            for (int i = 0; i < sprites.Length; i++)
            {
                float s = i < frameScales.Length ? frameScales[i] : 1f;
                int ki = scaleXY.AddKey(new Keyframe(i * frameDur, s));
                AnimationUtility.SetKeyLeftTangentMode(scaleXY,  ki, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(scaleXY, ki, AnimationUtility.TangentMode.Constant);
            }
            // 마지막 프레임 이후 1.0으로 복귀
            int last = scaleXY.AddKey(new Keyframe(sprites.Length * frameDur, 1f));
            AnimationUtility.SetKeyLeftTangentMode(scaleXY,  last, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(scaleXY, last, AnimationUtility.TangentMode.Constant);

            clip.SetCurve("", typeof(RectTransform), "m_LocalScale.x", scaleXY);
            clip.SetCurve("", typeof(RectTransform), "m_LocalScale.y", scaleXY);
        }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    // MaskEmpty: 빈 마스크 스프라이트 (정적)
    private static AnimationClip CreateEmptyClip(Sprite emptySprite)
    {
        AnimationClip clip = new AnimationClip { name = "MaskEmpty" };
        clip.frameRate = 12;

        if (emptySprite != null)
        {
            var keys = new[] { new ObjectReferenceKeyframe { time = 0f, value = emptySprite } };
            var binding = EditorCurveBinding.PPtrCurve("Icon", typeof(UnityEngine.UI.Image), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    // ── AnimatorController ────────────────────────────────────────────────────

    private static AnimatorController CreateController(AnimationClip fullClip, AnimationClip hitClip,
                                                        AnimationClip emptyClip, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        // 스테이트 생성
        AnimatorState fullState  = sm.AddState("Full");  fullState.motion  = fullClip;
        AnimatorState emptyState = sm.AddState("Empty"); emptyState.motion = emptyClip;
        sm.defaultState = fullState;

        // 파라미터
        ctrl.AddParameter("Hit",      AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Heal",     AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("SetEmpty", AnimatorControllerParameterType.Trigger);

        if (hitClip != null)
        {
            AnimatorState hitState = sm.AddState("Hit"); hitState.motion = hitClip;

            // Full → Hit (트리거)
            var t1 = fullState.AddTransition(hitState);
            t1.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            t1.hasExitTime = false; t1.duration = 0f;

            // Hit → Empty (재생 완료 후 자동 전환)
            var t2 = hitState.AddTransition(emptyState);
            t2.hasExitTime = true; t2.exitTime = 1f; t2.duration = 0f;
        }
        else
        {
            // Hit 클립 없으면 Full → Empty 직행
            var t1 = fullState.AddTransition(emptyState);
            t1.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            t1.hasExitTime = false; t1.duration = 0f;
        }

        // Empty → Full (회복)
        var t3 = emptyState.AddTransition(fullState);
        t3.AddCondition(AnimatorConditionMode.If, 0, "Heal");
        t3.hasExitTime = false; t3.duration = 0f;

        // Full → Empty (초기화용 즉시 전환)
        var t4 = fullState.AddTransition(emptyState);
        t4.AddCondition(AnimatorConditionMode.If, 0, "SetEmpty");
        t4.hasExitTime = false; t4.duration = 0f;

        return ctrl;
    }

    // ── 슬롯에 적용 ───────────────────────────────────────────────────────────

    private static void ApplyAnimatorToSlots(MaskUI maskUI, AnimatorController ctrl)
    {
        SerializedObject so     = new SerializedObject(maskUI);
        SerializedProperty imgs = so.FindProperty("maskImages");

        for (int i = 0; i < imgs.arraySize; i++)
        {
            var img = imgs.GetArrayElementAtIndex(i).objectReferenceValue as UnityEngine.UI.Image;
            if (img == null) continue;

            // 슬롯 = Icon의 부모 (Mask_N), 없으면 자기 자신
            Transform slot = (img.transform.parent != null && img.transform.parent != maskUI.transform)
                             ? img.transform.parent : img.transform;

            Animator anim = slot.GetComponent<Animator>() ?? slot.gameObject.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.updateMode = AnimatorUpdateMode.UnscaledTime; // UI는 timeScale 무관
            EditorUtility.SetDirty(slot.gameObject);
        }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    private static Sprite[] ReadSpriteArray(SerializedObject so, string propName)
    {
        var prop = so.FindProperty(propName);
        if (prop == null || !prop.isArray) return null;
        var arr = new Sprite[prop.arraySize];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = prop.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
        return arr;
    }

    private static void SaveClip(AnimationClip clip, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
    }

    private static void EnsureFolderPath(string path)
    {
        string[] parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
