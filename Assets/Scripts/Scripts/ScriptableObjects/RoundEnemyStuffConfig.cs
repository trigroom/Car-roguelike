using UnityEngine;

[CreateAssetMenu(fileName = "RoundEnemyStuffConfig", menuName = "Scriptable Objects/RoundEnemyStuffConfig")]
public class RoundEnemyStuffConfig : ScriptableObject {
   public EnemyCarPartsMaps[] CarPresets;
   public int[] CarParts;
   public int MoneyCount;
}
