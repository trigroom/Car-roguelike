using System.Collections.Generic;
using UnityEngine;

public class GridUtils : MonoBehaviour
{
  public static Vector2Int GetEdgePointFromCarPart(List<Vector2Int> carPartPositions) {
    Vector2Int carPartEdgePosition = carPartPositions[0];
    foreach (Vector2Int cell in carPartPositions) {
      if (cell.x < carPartEdgePosition.x)
        carPartEdgePosition.x = cell.x;
      if (cell.y < carPartEdgePosition.y)
        carPartEdgePosition.y = cell.y;
    }
    return carPartEdgePosition;
  }
  public static Vector2Int GetCarPartSize(CarPartConfig carPartConfig) {
    Vector2Int carPartSize = Vector2Int.one;
    foreach (Vector2Int cell in carPartConfig.PartForm) {
      if (cell.x + 1 > carPartSize.x)
        carPartSize.x = cell.x + 1;
      if (cell.y + 1 > carPartSize.y)
        carPartSize.y = cell.y + 1;
    }
    return carPartSize;
  }
  
  public static Vector2Int GetCarPartSize(List<Vector2Int> carPartPositions) {
      Vector2Int carPartSize = Vector2Int.one;
      foreach (Vector2Int cell in carPartPositions) {
          if (cell.x + 1 > carPartSize.x)
              carPartSize.x = cell.x + 1;
          if (cell.y + 1 > carPartSize.y)
              carPartSize.y = cell.y + 1;
      }
      return carPartSize;
  }
  
  public static bool IsPositionInGrid(Vector2Int gridPosition, int[,] gridSize)
  {
       
    return gridPosition.x >= 0 && gridPosition.x < gridSize.GetLength(0) &&
           gridPosition.y >= 0 && gridPosition.y < gridSize.GetLength(1);
        
  }
  
      public enum Direction
    {
        Top = 0,
        Right = 1,
        Bottom = 2,
        Left = 3
    }
    
    /// <summary>
    /// Получает индекс спрайта (0-15) на основе соседних клеток
    /// </summary>
    /// <param name="grid">Двумерный массив с данными тайлов</param>
    /// <param name="currentCellPos">Позиция текущей клетки</param>
    /// <param name="targetValue">Значение, с которым сравниваем соседей</param>
    /// <returns>Индекс спрайта от 0 до 15</returns>
    public static int GetTileSpriteIndex(int[,] grid, Vector2Int currentCellPos)
    {
        int targetValue = grid[currentCellPos.x, currentCellPos.y];
        // Проверяем наличие соседей в каждом направлении
        bool hasTopNeighbor = HasMatchingNeighbor(grid, currentCellPos, Direction.Top, targetValue);
        bool hasRightNeighbor = HasMatchingNeighbor(grid, currentCellPos, Direction.Right, targetValue);
        bool hasBottomNeighbor = HasMatchingNeighbor(grid, currentCellPos, Direction.Bottom, targetValue);
        bool hasLeftNeighbor = HasMatchingNeighbor(grid, currentCellPos, Direction.Left, targetValue);
        
        // Преобразуем булевы значения в битовую маску
        int bitmask = 0;
        if (hasTopNeighbor) bitmask |= 1 << 0;    // 1-й бит
        if (hasRightNeighbor) bitmask |= 1 << 1;  // 2-й бит
        if (hasBottomNeighbor) bitmask |= 1 << 2; // 3-й бит
        if (hasLeftNeighbor) bitmask |= 1 << 3;   // 4-й бит
        
        // Сопоставляем битовую маску с индексом спрайта (0-15)
        return bitmask;
    }
    
    /// <summary>
    /// Проверяет есть ли сосед с нужным значением в указанном направлении
    /// </summary>
    private static bool HasMatchingNeighbor(int[,] grid, Vector2Int currentPos, Direction direction, int targetValue)
    {
        Vector2Int neighborPos = GetNeighborPosition(currentPos, direction);
        
        // Проверяем выход за границы массива
        if(!IsPositionInGrid(neighborPos, grid))
        {
            return false;
        }
        
        // Проверяем совпадение значения
        return grid[neighborPos.x, neighborPos.y] == targetValue;
    }
    
    /// <summary>
    /// Получает позицию соседа в указанном направлении
    /// </summary>
    private static Vector2Int GetNeighborPosition(Vector2Int currentPos, Direction direction)
    {
        switch (direction)
        {
            case Direction.Top:
                return new Vector2Int(currentPos.x, currentPos.y + 1);
            case Direction.Right:
                return new Vector2Int(currentPos.x + 1, currentPos.y);
            case Direction.Bottom:
                return new Vector2Int(currentPos.x, currentPos.y - 1);
            case Direction.Left:
                return new Vector2Int(currentPos.x - 1, currentPos.y);
            default:
                return currentPos;
        }
    }
    
    public static Vector2 GetNormalizedMouseOffset(RectTransform uiElement)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiElement, 
            Input.mousePosition, 
            null, 
            out Vector2 localPoint);

        Rect rect = uiElement.rect;
        Vector2 elementSize = rect.size;

        Vector2 normalizedOffset = new Vector2(
            (localPoint.x + elementSize.x * 0.5f) / elementSize.x * 2 - 1,
            (localPoint.y + elementSize.y * 0.5f) / elementSize.y * 2 - 1
        );

        normalizedOffset.x = Mathf.Clamp(normalizedOffset.x, -1f, 1f);
        normalizedOffset.y = Mathf.Clamp(normalizedOffset.y, -1f, 1f);

        return normalizedOffset;
    }
    
    public static List<Vector2Int> GetCellsFromField( Vector2Int position, CarPartConfig carPartConfig, int rotateAngle, int[,] grid = null) {
        List<Vector2Int> returnedCells = new List<Vector2Int>();
        var carPartSize = GridUtils.GetCarPartSize(carPartConfig);
        var form = carPartConfig.PartForm;
        foreach (var cell in form) {
            var addedCell = cell;
            if(rotateAngle == 90)
                addedCell = new Vector2Int(-cell.y-1+carPartSize.y, cell.x);
            else if(rotateAngle == 180)
                addedCell = new Vector2Int(-cell.x-1+carPartSize.x, -cell.y-1+carPartSize.y);
            else if(rotateAngle == 270)
                addedCell = new Vector2Int(cell.y, -cell.x-1+carPartSize.x);
            var checkedCell = position + addedCell;
            if (grid == null || (checkedCell.y >= 0 && checkedCell.y <grid.GetLength(1) && checkedCell.x >= 0 &&
                                 checkedCell.x < grid.GetLength(0)))returnedCells.Add(checkedCell);
        }
        return returnedCells;
       
    }

    public static (float damageMultiplier, float reloadMultiplier, float attackCountdownMultiplayer) GetGunLevelMultipliers(GunConfig gunConfig, int level) {
        float damageMultiplier;
        float reloadMultiplier;
        float attackCountdownMultiplayer;

        if (gunConfig.MissleType == MissleType.laser) {
            damageMultiplier=0;
            reloadMultiplier=0;
            attackCountdownMultiplayer=0;
        }
        else if (gunConfig.MissleType == MissleType.bullet) {
            damageMultiplier=0;
            reloadMultiplier=0;
            attackCountdownMultiplayer=0;
        } 
        else {
            damageMultiplier=0;
            reloadMultiplier=0;
            attackCountdownMultiplayer=0;
        }
        
        return (damageMultiplier, reloadMultiplier, attackCountdownMultiplayer);
    }
}
