using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyCarPartsMapConfig", menuName = "Scriptable Objects/EnemyCarPartsMapConfig")]
public class EnemyCarPartsMaps : ScriptableObject
{
    public CarPartTypeRow[] CarPartTypeGrid ;
    
    public void CreateGrid(CarPartType[,] inputGrid)
    {
        CarPartTypeGrid = new CarPartTypeRow[inputGrid.GetLength(0)];
        
        int rows = inputGrid.GetLength(0);
        int cols = inputGrid.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            CarPartType[] carPartTypes = new CarPartType[cols];
            for (int j = 0; j < cols; j++)
            {
                carPartTypes[j] = inputGrid[i, j];
              
            }
              CarPartTypeGrid[i] = (new CarPartTypeRow(carPartTypes));
        }
        
        /*
        LockedCellGroups = new Dictionary<int, List<Vector2Int>>();
        int rows = inputGrid.GetLength(0);
        int cols = inputGrid.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                LockedCellGroups.TryAdd(inputGrid[i, j], new List<Vector2Int>()); 
                LockedCellGroups[inputGrid[i, j]].Add(new Vector2Int(i, j));
            }
            
        }*/
    }
}
[System.Serializable]
public struct CarPartTypeRow
{
    public CarPartType[] types;
    public CarPartTypeRow(CarPartType[] types)
    {
        this.types = types;
    }
}
