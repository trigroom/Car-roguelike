using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoundEnemyStuffConfig", menuName = "Scriptable Objects/RoundEnemyStuffConfig")]
public class RoundEnemyStuffConfig : ScriptableObject {
   public int[] AddedCarPartsToEnemyPool;
   public int[] RemovedCarPartsFromEnemyPool;
   public int MoneyCount;
}
