using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyCarPartsMapConfig", menuName = "Scriptable Objects/EnemyCarPartsMapConfig")]
public class EnemyCarPartsMaps : ScriptableObject {
    public CarPartTypeRow[] CarPartDownLevelGrid;
    public CarPartTypeRow[] CarPartUpLevelGrid;
    public int CarId;
    public int CarLevel;
    public Vector2Int GridSize;

    public void CreateGrid(int carId, int carLevel, List<CarPartTypeRow> inputGridDownLevel, List<CarPartTypeRow> inputGridUpLevel,
        Vector2Int gridSize) {
        CarPartDownLevelGrid = inputGridDownLevel.ToArray();
        CarPartUpLevelGrid = inputGridUpLevel.ToArray();
        CarLevel = carLevel;
        CarId = carId;
        GridSize = gridSize;
    }
}
[System.Serializable]
public struct CarPartTypeRow {
    public Vector2Int gridPosition; 
    public int partId;
    public int rotate;
    public int level;

    public CarPartTypeRow(Vector2Int gridPosition, int partId, int rotate, int level) {
        this.gridPosition = gridPosition;
        this.partId = partId;
        this.rotate = rotate;
        this.level = level;
    }
}
