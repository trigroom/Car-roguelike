using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoundEnemyStuffConfig", menuName = "Scriptable Objects/RoundEnemyStuffConfig")]
public class RoundEnemyStuffConfig : ScriptableObject {
   public List<float> EnemyCarChances;
   public int[] AddedCarPartsToEnemyPool;
   public int[] RemovedCarPartsFromEnemyPool;
   public int MoneyCount;
}
