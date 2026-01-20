using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapDifficultyConfig", menuName = "Scriptable Objects/MapDifficultyConfig")]
public class MapDifficultyConfig : ScriptableObject
{
    [field: SerializeField]
    public string DifficultyName { get; private set; }
    [field: SerializeField]
    public float HealthMultiplayer { get; private set; }
    [field: SerializeField]
    public float DamageMultiplayer { get; private set; }
    [field: SerializeField]
    public List<MapEventInfo> MapEvents { get; private set; }
    [field: SerializeField]
    public int ScrapForRound { get; private set; }
    [field: SerializeField]
    public int ScrapForMapComplete { get; private set; }
    [field: SerializeField]
    public float HealedHealthPartAfterEndRound { get; private set; }
    [field: SerializeField]
    public float TimeBetweenInRoundEnemySpawn { get; private set; }
    [field: SerializeField]
    public Color DifficltyUiColor { get; private set; }
    [field: SerializeField]
    public float StartMoney { get; private set; }
    [field: SerializeField]
    public int RoundsCount { get; private set; }
    [field: SerializeField]
    public bool DestroyEnemyCarsAfterDeath { get; private set; }
    [field: SerializeField]
    public EnemyCarPartsMaps[] BossCars { get; private set; }
}

public enum MapEventsType {
    None,
    Wind,
    ThunderStorm,
    Tornado,
    Bomber
}
[System.Serializable]
public class MapEventInfo {
   public MapEventsType MapEventType;
   public float MapEventFirstCount;
   public int MapEventSecondCount;
}


