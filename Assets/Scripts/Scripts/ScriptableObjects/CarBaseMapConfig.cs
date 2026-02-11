using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CarBaseMapConfig", menuName = "Scriptable Objects/CarBaseMapConfig")]
public class CarBaseMapConfig : ScriptableObject
{
    public CarPartTypeRow[] CarPartDownLevelGrid;
    public Vector2Int GridSize;
    
    public void CreateGrid(List<CarPartTypeRow> inputGridDownLevel, Vector2Int gridSize) {
        CarPartDownLevelGrid = inputGridDownLevel.ToArray();
        GridSize = gridSize;
    }
}
