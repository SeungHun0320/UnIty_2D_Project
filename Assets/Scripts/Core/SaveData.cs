// 저장 슬롯 하나의 전체 데이터입니다. JSON으로 직렬화됩니다.
[System.Serializable]
public class SaveData
{
    public string sceneName;
    public float  currentHealth;
    public float  maxHealth;
    public int    currentSoul;
    public int    geoAmount;
    public string savedAt;   // "yyyy-MM-dd HH:mm"
    public float  playtime;  // 초 단위 누적 플레이 시간
    
    // 플레이어 트랜스폼 정보
    public float[] playerPosition = new float[3];  // [x, y, z]
    public float[] playerRotation = new float[4];  // [x, y, z, w] - Quaternion
}

// Title 씬 슬롯 버튼 미리보기용 경량 정보입니다.
public class SlotInfo
{
    public bool   isEmpty = true;
    public string sceneName;
    public string savedAt;
    public float  playtime;
}
