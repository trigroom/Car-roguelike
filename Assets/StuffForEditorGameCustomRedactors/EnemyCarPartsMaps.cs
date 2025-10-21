using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyCarPartsMapConfig", menuName = "Scriptable Objects/EnemyCarPartsMapConfig")]
public class EnemyCarPartsMaps : ScriptableObject
{
    public CarPartTypeRow[] CarPartDownLevelGrid ;
    public CarPartTypeRow[] CarPartUpLevelGrid ;
    public int CarId;
    
    public void CreateGrid(int carId,List<CarPartTypeRow> inputGridDownLevel, List<CarPartTypeRow> inputGridUpLevel) {
        CarPartDownLevelGrid = inputGridDownLevel.ToArray();
        CarPartUpLevelGrid = inputGridUpLevel.ToArray();
        CarId = carId;
    }
}
[System.Serializable]
public struct CarPartTypeRow {
    public Vector2Int gridPosition;
    public int partId;
    public int rotate;

    public CarPartTypeRow(Vector2Int gridPosition, int partId, int rotate) {
        this.gridPosition = gridPosition;
        this.partId = partId;
        this.rotate = rotate;
    }
}
