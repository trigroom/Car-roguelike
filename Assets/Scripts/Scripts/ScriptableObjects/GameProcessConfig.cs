using UnityEngine;

[CreateAssetMenu(fileName = "GameProcessConfig", menuName = "Scriptable Objects/GameProcessConfig")]
public class GameProcessConfig : ScriptableObject {
   public RoundEnemyStuffConfig[] GameRounds;
}
