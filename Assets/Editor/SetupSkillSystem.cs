using UnityEngine;
using UnityEditor;

public class SetupSkillSystem
{
    public static void Execute()
    {
        // 1. Assets/Data/Skills 폴더 생성
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Skills"))
            AssetDatabase.CreateFolder("Assets/Data", "Skills");

        // 2. BasicAttack.asset 생성
        string assetPath = "Assets/Data/Skills/BasicAttack.asset";
        BasicAttackData existing = AssetDatabase.LoadAssetAtPath<BasicAttackData>(assetPath);
        if (existing == null)
        {
            BasicAttackData attackData = ScriptableObject.CreateInstance<BasicAttackData>();
            attackData.cooldown          = 0f;
            attackData.hitbox.delay      = 0.8f;
            attackData.hitbox.duration   = 0.4f;
            AssetDatabase.CreateAsset(attackData, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[SetupSkillSystem] BasicAttack.asset 생성 완료: " + assetPath);
        }
        else
        {
            Debug.Log("[SetupSkillSystem] BasicAttack.asset 이미 존재합니다.");
        }

        // 3. Player에 PlayerSkillController 추가
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[SetupSkillSystem] 'Player' GameObject를 찾을 수 없습니다.");
            return;
        }

        PlayerSkillController controller = player.GetComponent<PlayerSkillController>();
        if (controller == null)
        {
            controller = player.AddComponent<PlayerSkillController>();
            Debug.Log("[SetupSkillSystem] PlayerSkillController 컴포넌트 추가 완료.");
        }
        else
        {
            Debug.Log("[SetupSkillSystem] PlayerSkillController 이미 존재합니다.");
        }

        // 4. Slots[0]에 BasicAttack.asset 할당
        BasicAttackData basicAttack = AssetDatabase.LoadAssetAtPath<BasicAttackData>(assetPath);
        SerializedObject so = new SerializedObject(controller);
        SerializedProperty slots = so.FindProperty("slots");
        slots.arraySize = 1;
        slots.GetArrayElementAtIndex(0).objectReferenceValue = basicAttack;
        so.ApplyModifiedProperties();

        // 5. SpineInputController의 skillController 필드 연결
        SpineInputController inputController = player.GetComponent<SpineInputController>();
        if (inputController != null)
        {
            SerializedObject inputSO = new SerializedObject(inputController);
            SerializedProperty skillControllerProp = inputSO.FindProperty("skillController");
            skillControllerProp.objectReferenceValue = controller;
            inputSO.ApplyModifiedProperties();
            Debug.Log("[SetupSkillSystem] SpineInputController.skillController 연결 완료.");
        }

        EditorUtility.SetDirty(player);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log("[SetupSkillSystem] 세팅 완료!");
    }
}
