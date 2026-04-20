using UnityEditor;

public class ForceReimport
{
    public static void Execute()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }
}
