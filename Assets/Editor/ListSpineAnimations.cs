using UnityEngine;
using UnityEditor;
using Spine.Unity;

public class ListSpineAnimations
{
    public static string Execute()
    {
        string assetPath = "Assets/Spine/Spine2D Knight Character Animation Pack/Exports/json_export/Knight_SkeletonData.asset";
        var skeletonData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(assetPath);
        if (skeletonData == null)
        {
            Debug.LogError("[ListSpineAnimations] SkeletonDataAsset을 찾을 수 없습니다: " + assetPath);
            return "NOT FOUND";
        }

        var data = skeletonData.GetSkeletonData(false);
        if (data == null)
        {
            Debug.LogError("[ListSpineAnimations] SkeletonData 로드 실패.");
            return "LOAD FAILED";
        }

        System.Text.StringBuilder sb = new();
        foreach (var anim in data.Animations)
            sb.AppendLine(anim.Name);

        string result = sb.ToString();
        Debug.Log("[ListSpineAnimations] 애니메이션 목록:\n" + result);
        return result;
    }
}
