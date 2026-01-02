using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapConfig", menuName = "Scriptable Objects/MapConfig")]
public class MapConfig : ScriptableObject {
     [field: SerializeField]
     public int RoundOnLastMapToUnlock { get; private set; }
     [field: SerializeField]
     public int MapId { get; private set; }
     [field: SerializeField]
     public string MapDescription { get; private set; }

     [field: SerializeField]
     public Sprite MapSprite { get; private set; }

     [field: SerializeField]
     public string MapName { get; private set; }

     [field: SerializeField]
     public GameProcessConfig GameProcessConfig { get; private set; }

     [field: SerializeField]
     public MapDifficultyConfig[] MapDifficltyConfigs { get; private set; }

     public List<EnemyListConfigsList> EnemyMapConfigs;
     
     public List<MapVisualInfoConfig> MapVisualInfoConfigs;
}
[System.Serializable]
public class EnemyListConfigsList {
     public List<EnemyCarPartsMaps> CarMapsList;
}