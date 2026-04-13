using UnityEngine;
using UnityEditor;

public class ListAtlasSprites
{
    public static string Execute()
    {
        string path = "Assets/Resources/atlas0_300.png";
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path);
        var sb = new System.Text.StringBuilder();
        foreach (var obj in sprites)
        {
            if (obj is Sprite sprite)
                sb.AppendLine(sprite.name);
        }
        string result = sb.ToString();
        Debug.Log("[ListAtlasSprites]\n" + result);
        return result;
    }
}
