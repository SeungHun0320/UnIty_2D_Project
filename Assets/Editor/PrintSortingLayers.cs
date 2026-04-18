using UnityEngine;
using UnityEditorInternal;

public class PrintSortingLayers
{
    public static void Execute()
    {
        var layers = SortingLayer.layers;
        foreach (var l in layers)
            Debug.Log($"[SortingLayer] {l.name} (id={l.id})");
    }
}
