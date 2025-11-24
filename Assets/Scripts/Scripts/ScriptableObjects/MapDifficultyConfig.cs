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
    public MapEventsType[] MapEvents { get; private set; }
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
}

public enum MapEventsType {
    None,
    StrongWind
}


