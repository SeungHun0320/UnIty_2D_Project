using System.IO;
using UnityEngine;

// IDataRepository의 로컬 JSON 파일 구현체입니다.
// 파일 경로: Application.persistentDataPath/save_slot{n}.json
public class JsonRepository : IDataRepository
{
    private string GetPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");

    public void Save(SaveData data, int slot) =>
        File.WriteAllText(GetPath(slot), JsonUtility.ToJson(data, true));

    public SaveData Load(int slot)
    {
        var path = GetPath(slot);
        return File.Exists(path) ? JsonUtility.FromJson<SaveData>(File.ReadAllText(path)) : null;
    }

    public void Delete(int slot)
    {
        var path = GetPath(slot);
        if (File.Exists(path)) File.Delete(path);
    }

    public bool Exists(int slot) => File.Exists(GetPath(slot));
}
