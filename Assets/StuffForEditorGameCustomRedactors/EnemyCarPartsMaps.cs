using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyCarPartsMapConfig", menuName = "Scriptable Objects/EnemyCarPartsMapConfig")]
public class EnemyCarPartsMaps : ScriptableObject
{
    public CarPartTypeRow[] CarPartDownLevelGrid ;
    public CarPartTypeRow[] CarPartUpLevelGrid ;
    public int CarId;
    public Vector2Int GridSize;
    
    public void CreateGrid(int carId,List<CarPartTypeRow> inputGridDownLevel, List<CarPartTypeRow> inputGridUpLevel, Vector2Int gridSize) {
        CarPartDownLevelGrid = inputGridDownLevel.ToArray();
        CarPartUpLevelGrid = inputGridUpLevel.ToArray();
        CarId = carId;
        GridSize = gridSize;
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
