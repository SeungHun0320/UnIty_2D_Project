using System;
using UnityEngine;

// 히트박스 타이밍을 묶어 재사용하는 직렬화 가능한 구조체입니다.
[Serializable]
public class HitboxConfig
{
    [Min(0f)] public float delay    = 0.8f;
    [Min(0f)] public float duration = 0.4f;
}
