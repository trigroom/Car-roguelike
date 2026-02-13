using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridUtils : MonoBehaviour {
    public const int InWorldCellSize = 16;
    private static List<CarPartType> _containerPartTypes = new() { CarPartType.CarStuffContainer, CarPartType.WheelsContainer };
    private static List<CarPartType> _weaponPartTypes = new() { CarPartType.MeleeWeapon, CarPartType.Gun };
    public static bool IsContainerType (CarPartConfig carPartConfig) => _containerPartTypes.Contains(carPartConfig.PartType);
    public static bool IsWeaponType (CarPartConfig carPartConfig) => _weaponPartTypes.Contains(carPartConfig.PartType);
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
  
  public static int GetRandomIndexFromChancesList(List<float> chances)
  {
      float total = chances.Sum();
    
      float randomValue = Random.value;
      float cumulative = 0f;
    
      for (int i = 0; i < chances.Count; i++)
      {
          cumulative += chances[i] / total;
          if (randomValue <= cumulative)
              return i;
      }
    
      return 0;
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
    
    public static int GetTileSpriteIndex(int[,] grid, Vector2Int currentCellPos)
    {
        int targetValue = grid[currentCellPos.x, currentCellPos.y];
     
        bool hasTopNeighbor = HasMatchingNeighbor(grid, currentCellPos, Direction.Top, targetValue);
        bool hasRightNeighbor = HasMatchingNeighbor(grid, currentCellPos, Direction.Right, targetValue);
        bool hasBottomNeighbor = HasMatchingNeighbor(grid, currentCellPos, Direction.Bottom, targetValue);
        bool hasLeftNeighbor = HasMatchingNeighbor(grid, currentCellPos, Direction.Left, targetValue);
        
        int bitmask = 0;
        if (hasTopNeighbor) bitmask |= 1 << 0;    // 1-й бит
        if (hasRightNeighbor) bitmask |= 1 << 1;  // 2-й бит
        if (hasBottomNeighbor) bitmask |= 1 << 2; // 3-й бит
        if (hasLeftNeighbor) bitmask |= 1 << 3;   // 4-й бит
        
        // Сопоставляем битовую маску с индексом спрайта (0-15)
        return bitmask;
    }
    
    private static bool HasMatchingNeighbor(int[,] grid, Vector2Int currentPos, Direction direction, int targetValue)
    {
        Vector2Int neighborPos = GetNeighborPosition(currentPos, direction);
        
        // Проверяем выход за границы массива
        if(!IsPositionInGrid(neighborPos, grid))
            return false;
        
        // Проверяем совпадение значения
        return grid[neighborPos.x, neighborPos.y] == targetValue;
    }

    public static List<int> GetNeighbors(List<Vector2Int> needPositions, int[,] grid) {
        HashSet<int> uniqueNeighbors = new HashSet<int>();

        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        foreach (var needPosition in needPositions) {
            
            if (needPosition.y + 1 < cols && grid[needPosition.x, needPosition.y + 1] != 0) 
                uniqueNeighbors.Add( grid[needPosition.x, needPosition.y + 1]);

            if (needPosition.y - 1 >= 0 && grid[needPosition.x, needPosition.y - 1] != 0) 
                uniqueNeighbors.Add(grid[needPosition.x, needPosition.y - 1]);

            if (needPosition.x - 1 >= 0 && grid[needPosition.x - 1, needPosition.y] != 0) 
                uniqueNeighbors.Add(grid[needPosition.x - 1, needPosition.y]);

            if (needPosition.x + 1 < rows && grid[needPosition.x + 1, needPosition.y] != 0) 
                uniqueNeighbors.Add(grid[needPosition.x + 1, needPosition.y]);
        }

        return new List<int>(uniqueNeighbors);
    }

    private static bool IsEdgeContainerCell(Vector2Int needPosition, int[,] grid) {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        if (needPosition.y + 1 >= cols || grid[needPosition.x, needPosition.y + 1] == 0)
            return true;

        if (needPosition.y - 1 < 0 || grid[needPosition.x, needPosition.y - 1] == 0)
            return true;
        if (grid.GetLength(0) <= 2) return false;
        if (needPosition.x - 1 < 0 || grid[needPosition.x - 1, needPosition.y] == 0)
            return true;

        if (needPosition.x + 1 >= rows || grid[needPosition.x + 1, needPosition.y] == 0)
            return true;

        return false;
    }
    
    public static List<Vector2Int> GetNonZeroNeighbors8Dir(int[,] grid, List<Vector2Int> needCells, int radius) {
        List<Vector2Int> result = new List<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        foreach (var cell in needCells) {
            for (int dx = -radius; dx <= radius; dx++) {
                for (int dy = -radius; dy <= radius; dy++) {
                    if (dx == 0 && dy == 0) continue;

                    Vector2Int neighbor = new Vector2Int(cell.x + dx, cell.y + dy);

                    if (neighbor.x >= 0 && neighbor.x < rows && neighbor.y >= 0 && neighbor.y < cols) {
                        if (grid[neighbor.x, neighbor.y] != 0 && !visited.Contains(neighbor) && !needCells.Contains(neighbor)) {
                            result.Add(neighbor);
                            visited.Add(neighbor);
                        }
                    }
                }
            }
        }

        return result;
    }
    
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
        float damageMultiplier=1;
        float reloadMultiplier=1;
        float attackCountdownMultiplayer=1;

        if (gunConfig.MissleType == MissleType.laser) {
            damageMultiplier+=level*0.18f;
            reloadMultiplier+=level*0.1f;
            attackCountdownMultiplayer+=level*0.15f;
        }
        else if (gunConfig.MissleType == MissleType.bullet) {
            damageMultiplier+=level*0.15f;
            reloadMultiplier+=level*0.15f;
            attackCountdownMultiplayer+=level*0.1f;
        } 
        else {
            damageMultiplier+=level*0.17f;
            reloadMultiplier+=level*0.08f;
            attackCountdownMultiplayer+=level*0.12f;
        }
        
        return (damageMultiplier, reloadMultiplier, attackCountdownMultiplayer);
    }
    
    public static (float damageMultiplier, float damageBySpeedMultiplier) GetMeleeLevelMultipliers(MeleeWeaponConfig meleeConfig, int level) {
        float damageMultiplier=1;
        float damageBySpeedMultiplier=1;

        if ((meleeConfig.Damage/1000f) > meleeConfig.DamageAddedFromOneSpeed) {
            damageMultiplier+=level*0.2f;
        }
        else if ((meleeConfig.Damage/8000f) > meleeConfig.DamageAddedFromOneSpeed) {
            damageMultiplier+=level*0.1f;
            damageBySpeedMultiplier+=level*0.1f;
        } 
        else {
            damageBySpeedMultiplier+=level*0.2f;
        }
        
        return (damageMultiplier, damageBySpeedMultiplier);
    }

    public static float GetUpgradeCost(int level, CarPartConfig carPartConfig, int[] moduleIds) {
        float costMultiplayer = 1;
        foreach (var moduleId in moduleIds) {
            if(moduleId == -1)continue;
            var moduleConfig = BetweenScenesService.Instance._carStuffListsConfig.ModuleConfigs[moduleId];
            foreach (ModuleBuff moduleBuff in moduleConfig.ModuleBuffs) {
                if (moduleBuff.Buff == Buff.UpgradeCost)
                    costMultiplayer += moduleBuff.BuffCount;
            }
        }
        
        return Mathf.FloorToInt(carPartConfig.UpgradedPartCost * (1+level*0.2f))*costMultiplayer;
    }
    
    public static float GetArmorLevelMultipliers(int level) {
      //  float healthMultiplier=1;
        float defenceMultiplier=1;

       // healthMultiplier *= level * 0.15f;
        defenceMultiplier += level * 0.15f;
        
        return defenceMultiplier;
    }
    public static float GetHealthLevelMultiplier(int level) => 1 +level * 0.1f ;
       
    public static float GetMaxWeightLevelMultiplier(int level) => 1 +level * 0.08f ;
    public static (float firstMultiplayer, float secondMultiplayer)GetSupportLevelMultipliers(int level, SupportModuleType supportModuleType) {
       
        float firstCountMultiplayer=1;
        float secondCountMultiplayer=1;
        switch (supportModuleType) {
            case SupportModuleType.Fuel or SupportModuleType.Battery:
                firstCountMultiplayer+=level*0.13f;
                break;
            case SupportModuleType.Money :
                firstCountMultiplayer+=level*0.15f;
                break;
            case SupportModuleType.Ammo :
                firstCountMultiplayer+=level*0.1f;
                break;
            case SupportModuleType.Turbine :
                firstCountMultiplayer+=level*0.05f;
                secondCountMultiplayer+=level*0.15f;
                break;
            case SupportModuleType.Heater or SupportModuleType.FireExtinguisher:
                firstCountMultiplayer+=level*0.1f;
                break;
            case SupportModuleType.ElectricGenerator :
                firstCountMultiplayer+=level*0.1f;
                secondCountMultiplayer-=level*0.05f;
                break;
        }
        
        return (firstCountMultiplayer, secondCountMultiplayer);
    }

    public static (float baseSpeedMultiplier, float baseRotateSpeedMultiplier, float baseAccelerationMultiplier, float baseMaxFuelMultiplier)
        GetCarLevelMultipliers(int level) {
        float baseSpeedMultiplier = 1 + level * 0.15f;
        float baseRotateSpeedMultiplier = 1 + level * 0.15f;
        float baseAccelerationMultiplier = 1 + level * 0.15f;
        float baseMaxFuelMultiplier = 1 + level * 0.15f;

        return (baseSpeedMultiplier, baseRotateSpeedMultiplier, baseAccelerationMultiplier, baseMaxFuelMultiplier);
    }

    public static List<Vector2> FindBorderContour(int[,] grid) {
        List<Vector2> contour = new List<Vector2>();
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        Vector2 start = FindStartPoint(grid);
        if (start.x == -1) return contour;

        Vector2Int[] directions = {
            new (1, 0), // вправо
            new (0, -1), // вниз  
            new (-1, 0), // влево
            new (0, 1) // вверх
        };

        Vector2 current = start;
        int currentDir = 0; 
        contour.Add(current);
        do {
            Vector2Int dir = directions[currentDir];
            var lastDir = currentDir;
            currentDir = IsBorderEdge(grid, current, dir);
            if (lastDir != currentDir) 
                contour.Add(current);
                
            current += directions[currentDir];
        } while (contour.Count == 1 || Vector2.Distance(contour[0] , contour[^1]) > 0.1f);

        for (int i = 0; i < contour.Count; i++) 
            contour[i] -= new Vector2(width/2f-0.5f, height/2f-0.5f);
        
        return contour;
    }

    private static Vector2 FindStartPoint(int[,] grid)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != 0 && (y == height - 1 || grid[x, y + 1] == 0))
                {
                    return new Vector2(x-0.5f, y+0.5f);
                }
            }
        }
        return new Vector2(-1, -1);
    }

    private static int IsBorderEdge(int[,] grid, Vector2 currentPos, Vector2Int dir) {
        if (dir.x != 0) {
            float newY = currentPos.y;
            float newX = currentPos.x;

            int checkedX = dir.x == 1 ? Mathf.FloorToInt(newX + dir.x) : Mathf.CeilToInt(newX + dir.x);

            bool firstCellIsEmpty = !IsPositionInGrid(new Vector2Int(checkedX, Mathf.CeilToInt(newY)), grid) ||
                                    grid[checkedX, Mathf.CeilToInt(newY)] == 0;
            bool secondCellIsEmpty = !IsPositionInGrid(new Vector2Int(checkedX, Mathf.FloorToInt(newY)), grid) ||
                                     grid[checkedX, Mathf.FloorToInt(newY)] == 0;

            if (firstCellIsEmpty != secondCellIsEmpty)
                return dir.x == 1 ? 0 : 2;
            else {
                int checkedXsecond = dir.x == 1 ? Mathf.CeilToInt(newX - dir.x) : Mathf.FloorToInt(newX - dir.x);
                bool upCellIsEmpty = !IsPositionInGrid(new Vector2Int(checkedXsecond, Mathf.CeilToInt(newY)), grid) ||
                                     grid[checkedXsecond, Mathf.CeilToInt(newY)] == 0;

                if (firstCellIsEmpty && secondCellIsEmpty) {
                    if (upCellIsEmpty)
                        return 1;
                    else
                        return 3;
                } else if (!firstCellIsEmpty && !secondCellIsEmpty) {
                    if (upCellIsEmpty)
                        return 3;
                    else
                        return 1;
                }
            }
        } else // Вертикальное движение
        {
            float newY = currentPos.y;
            float newX = currentPos.x;

            int checkedY = dir.y == 1 ? Mathf.FloorToInt(newY + dir.y) : Mathf.CeilToInt(newY + dir.y);

            bool firstCellIsEmpty = !IsPositionInGrid(new Vector2Int(Mathf.CeilToInt(newX), checkedY), grid) ||
                                    grid[Mathf.CeilToInt(newX), checkedY] == 0;
            bool secondCellIsEmpty = !IsPositionInGrid(new Vector2Int(Mathf.FloorToInt(newX), checkedY), grid) ||
                                     grid[Mathf.FloorToInt(newX), checkedY] == 0;

            if (firstCellIsEmpty != secondCellIsEmpty)
                return dir.y == 1 ? 3 : 1;
            else {
                int checkedYsecond = dir.y == 1 ? Mathf.CeilToInt(newY - dir.y) : Mathf.FloorToInt(newY - dir.y);
                bool upCellIsEmpty = !IsPositionInGrid(new Vector2Int(Mathf.CeilToInt(newX), checkedYsecond), grid) ||
                                     grid[Mathf.CeilToInt(newX), checkedYsecond] == 0;
                if (firstCellIsEmpty && secondCellIsEmpty) {
                    if (upCellIsEmpty)
                        return 2;
                    else
                        return 0;
                } else if (!firstCellIsEmpty && !secondCellIsEmpty) {
                    if (upCellIsEmpty)
                        return 0;
                    else
                        return 2;
                }
            }
        }

        return 0;
    }
    
    public static Vector2Int FindMedianCell(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0)
            return Vector2Int.zero;

        // Сортируем отдельно по X и Y
        var sortedX = cells.OrderBy(c => c.x).ToList();
        var sortedY = cells.OrderBy(c => c.y).ToList();

        int medianX, medianY;

        // Находим медиану по X
        if (sortedX.Count % 2 == 0)
        {
            // Четное количество - берем среднее двух центральных (округляем в меньшую сторону)
            int mid = sortedX.Count / 2;
            medianX = (sortedX[mid - 1].x + sortedX[mid].x) / 2;
        }
        else
        {
            // Нечетное количество - берем центральную
            medianX = sortedX[sortedX.Count / 2].x;
        }

        // Находим медиану по Y
        if (sortedY.Count % 2 == 0)
        {
            int mid = sortedY.Count / 2;
            medianY = (sortedY[mid - 1].y + sortedY[mid].y) / 2;
        }
        else
        {
            medianY = sortedY[sortedY.Count / 2].y;
        }

        return new Vector2Int(medianX, medianY);
    }

    public static List<int> FindContactedValues(int[,] grid, int mainValue) {
        List<int> contactedValues = new List<int>();

        // Проверяем валидность входных данных
        if (grid == null) {
            Debug.LogWarning("Grid is null");
            return contactedValues;
        }

        // Получаем размеры grid
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        // Находим первую позицию с нужным значением
        Vector2Int? startPos = FindFirstPositionWithValue(grid, mainValue);

        // Если значение не найдено в grid, возвращаем пустой список
        if (!startPos.HasValue) {
            return contactedValues;
        }

        // Используем очередь для BFS обхода
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // Добавляем начальную позицию
        queue.Enqueue(startPos.Value);
        visited.Add(startPos.Value);

        // Направления для проверки соседей (вверх, вправо, вниз, влево)
        Vector2Int[] directions = new Vector2Int[] {
            new Vector2Int(-1, 0), // вверх
            new Vector2Int(1, 0), // вниз
            new Vector2Int(0, -1), // влево
            new Vector2Int(0, 1) // вправо
        };

        // BFS обход
        while (queue.Count > 0) {
            Vector2Int currentPos = queue.Dequeue();
            int currentValue = grid[currentPos.x, currentPos.y];

            // Добавляем текущее значение в результат (если еще не добавлено)
            if (!contactedValues.Contains(currentValue)) {
                contactedValues.Add(currentValue);
            }

            // Проверяем всех соседей
            foreach (Vector2Int direction in directions) {
                Vector2Int neighborPos = new Vector2Int(currentPos.x + direction.x, currentPos.y + direction.y);

                // Проверяем, находится ли сосед в пределах grid
                if (neighborPos.x >= 0 && neighborPos.x < rows && neighborPos.y >= 0 && neighborPos.y < cols) {
                    int neighborValue = grid[neighborPos.x, neighborPos.y];

                    // Если сосед не посещен и его значение не равно 0
                    if (!visited.Contains(neighborPos) && neighborValue != 0) {
                        visited.Add(neighborPos);
                        queue.Enqueue(neighborPos);
                    }
                }
            }
        }

        return contactedValues;
    }

    // Находит первую позицию с заданным значением в grid
    private static Vector2Int? FindFirstPositionWithValue(int[,] grid, int value) {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        for (int i = 0; i < rows; i++) {
            for (int j = 0; j < cols; j++) {
                if (grid[i, j] == value) {
                    return new Vector2Int(i, j);
                }
            }
        }

        return null;
    }
    
    public static void PrintGrid(int[,] grid)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        
        string gridStr = "Grid:\n";
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                gridStr += grid[i, j] + " ";
            }
            gridStr += "\n";
        }
        Debug.Log(gridStr);
    }

    public static float GetSellValueForCarPart(CarPartConfig carPartConfig, int level, int[] moduleIds) {
        float modulesCost = 0;
        if (moduleIds != null)
            foreach (var moduleId in moduleIds) {
                if (moduleId == -1) continue;
                var moduleConfig = BetweenScenesService.Instance._carStuffListsConfig.ModuleConfigs[moduleId];
                modulesCost += moduleConfig.Cost;
            }

        return carPartConfig.MoneyCost / 2 + Mathf.Max(level - 1, 0) * carPartConfig.UpgradedPartCost + modulesCost;
    }

    public static (float damageMultiplaier, float attackSpeedMultiplayer, float healthMultiplayer, int targetPiercingAddedCount, float
        reloadSpeedMultiplayer, float maxWeightMultiplier,float wheelRotateSpeedMultiplier, float magSizeMultiplier) GetModulesMultipliers(int[] moduleIds) {
        float damageMultiplayer = 1;
        float attackSpeedMultiplayer = 1;
        float healthMultiplayer = 1;
        int targetPiercingAddedCount = 0;
        float reloadSpeedMultiplayer = 1;
        float maxWeightMultiplier = 1;
        float wheelRotateSpeedMultiplier = 1;
        float magSizeMultiplier = 1;

        if (moduleIds != null)
            foreach (var moduleId in moduleIds) {
                if (moduleId == -1) continue;
                var moduleConfig = BetweenScenesService.Instance._carStuffListsConfig.ModuleConfigs[moduleId];
                foreach (var moduleBuff in moduleConfig.ModuleBuffs) {
                    switch (moduleBuff.Buff) {
                        case Buff.Damage:
                            damageMultiplayer += moduleBuff.BuffCount;
                            break;
                        case Buff.AttackSpeed:
                            attackSpeedMultiplayer += moduleBuff.BuffCount;
                            break;
                        case Buff.Health:
                            healthMultiplayer += moduleBuff.BuffCount;
                            break;
                        case Buff.TargetPierceing:
                            targetPiercingAddedCount += (int)moduleBuff.BuffCount;
                            break;
                        case Buff.ReloadSpeed:
                            reloadSpeedMultiplayer += moduleBuff.BuffCount;
                            break;
                        case Buff.MaxMagazineSize:
                            magSizeMultiplier += moduleBuff.BuffCount;
                            break;
                        case Buff.MaxWeight:
                            maxWeightMultiplier += moduleBuff.BuffCount;
                            break;
                        case Buff.CarRotateSpeed:
                            wheelRotateSpeedMultiplier += moduleBuff.BuffCount;
                            break;
                    }
                }
            }

        return (damageMultiplayer, attackSpeedMultiplayer, healthMultiplayer, targetPiercingAddedCount, reloadSpeedMultiplayer,
            maxWeightMultiplier, wheelRotateSpeedMultiplier, magSizeMultiplier);
    }
    
    public static ( Sprite upSprite, Vector2 size, Vector2 position) GetUpperPartSizesForUI(Vector2 cellSizeDelta, CarPartConfig carPartConfig) {
        Vector2 cellSize = Vector2.zero;
        Vector2 needPosition = Vector2.zero;
        Sprite upSprite = null;
        if (carPartConfig.PartType == CarPartType.Gun && carPartConfig is GunConfig gunConfig) {
            if (gunConfig.UpPartSprite != null) {
                var carPartSize = GetCarPartSize(carPartConfig);
                cellSizeDelta = new Vector2(Mathf.Max(cellSizeDelta.x, cellSizeDelta.y), Mathf.Max(cellSizeDelta.x, cellSizeDelta.y));
                cellSize = cellSizeDelta / (Mathf.Max(carPartSize.x, carPartSize.y) * InWorldCellSize) *
                           gunConfig.UpPartSprite.rect.size;
                needPosition = cellSizeDelta / Mathf.Max(carPartSize.x, carPartSize.y) * gunConfig.UpPartPosition;
                upSprite = gunConfig.UpPartSprite != null ? gunConfig.UpPartSprite : null;
            }
        }

        if (carPartConfig.PartType == CarPartType.MeleeWeapon && carPartConfig is MeleeWeaponConfig meleeConfig) {
            if (meleeConfig.UpSprite != null) {
                var carPartSize = GetCarPartSize(carPartConfig);
                cellSizeDelta = new Vector2(Mathf.Max(cellSizeDelta.x, cellSizeDelta.y), Mathf.Max(cellSizeDelta.x, cellSizeDelta.y));
                cellSize = cellSizeDelta / (Mathf.Max(carPartSize.x, carPartSize.y) * InWorldCellSize) *
                           meleeConfig.UpSprite.rect.size;
                needPosition = cellSizeDelta / Mathf.Max(carPartSize.x, carPartSize.y) * meleeConfig.UpSpritePosition;
                upSprite = meleeConfig.UpSprite != null ? meleeConfig.UpSprite : null;
            }
        }

        return (upSprite, cellSize, needPosition);
    }
    
    static private int gridSize = 20;
    static private int minBuildings = 10;
    static private int maxBuildings = 15;

    static public void GenerateRandomBuildings(ref int[,] mapGrid) {
        int buildingCount = Random.Range(minBuildings, maxBuildings + 1);
        List<Building> placedBuildings = new List<Building>();
        for (int i = 0; i < buildingCount; i++) {
            Building building = CreateRandomBuilding();

            if (TryPlaceBuilding(building, placedBuildings)) {
             //   minPoints.Add(new Vector2(building.X*10-102.5f, building.Y*10-102.5f));
               // maxPoints.Add(new Vector2((building.X+building.Width-1)*10-97.5f, (building.Y+building.Height-1)*10-97.5f));
                placedBuildings.Add(building);
                PlaceBuildingOnGrid(building, ref mapGrid);
            }
        }
    }

    static private Building CreateRandomBuilding() {
        // Случайные размеры здания (минимальный размер 2x2)
        int width = Random.Range(3, 8);
        int height = Random.Range(3, 8);

        var needAxis = Random.value > 0.5f;
        // Случайная позиция с отступом от границ
        int x=0;
        int y=0;
        if (needAxis) {
            x = Random.value > 0.5f
                ? Random.Range(2, (gridSize - width - 1) / 2 - 2-width/2)
                : Random.Range((gridSize - width - 1) / 2 + 2+width/2, gridSize - width - 1);
            y = Random.Range(2, gridSize - height - 1);
        } else {
            x = Random.Range(2, gridSize - width - 1);
            y = Random.value > 0.5f
                ? Random.Range(2, (gridSize - height - 1) / 2 - 2-height/2)
                : Random.Range((gridSize - height - 1) / 2 + 2+height/2, gridSize - height - 1);
        }
     


        // Случайная форма (прямоугольник, Г-образная, П-образная)
        return new Building(x, y, width, height, GetRandomShape(width, height));
    }

    static private bool[,] GetRandomShape(int width, int height) {
        bool[,] shape = new bool[width, height];
     
        bool isVerticalCentre = Random.value > 0.5f;
        bool isHorizontalCentre = Random.value > 0.5f;
        int xStartPosition = Random.Range(isHorizontalCentre ? 1 : 0, isHorizontalCentre ? width - 1 : width);
        int xEndPosition = Random.Range(xStartPosition + 1, isHorizontalCentre ? width : width + 1);
        int yStartPosition = Random.Range(isVerticalCentre ? 1 : 0, isVerticalCentre ? height - 1 : height);
        int yEndPosition = Random.Range(yStartPosition + 1, isVerticalCentre ? height : height + 1);
        for (int i = 0; i < width; i++)
        for (int j = 0; j < height; j++)
            shape[i, j] = ((isHorizontalCentre && i >= xStartPosition && i <= xEndPosition) ||
                           (!isHorizontalCentre && (i < xStartPosition || i > xEndPosition))) ||
                          ((isVerticalCentre && j >= yStartPosition && j <= yEndPosition) ||
                           (!isVerticalCentre && (j < yStartPosition || j > yEndPosition)));
    
        return shape;
    }

    static private bool TryPlaceBuilding(Building building, List<Building> existingBuildings) {
        // Проверяем границы
        if (building.X < 1 || building.Y < 1 || building.X + building.Width >= gridSize - 1 || building.Y + building.Height >= gridSize - 1)
            return false;

        // Проверяем пересечения с существующими зданиями
        foreach (var existing in existingBuildings) {
            if (BuildingsIntersect(building, existing))
                return false;
        }

        return true;
    }

    static private bool BuildingsIntersect(Building a, Building b) {
        // Проверяем пересечение с зазором в 1 клетку
        return a.X - 1 < b.X + b.Width && a.X + a.Width + 1 > b.X && a.Y - 1 < b.Y + b.Height && a.Y + a.Height + 1 > b.Y;
    }

    static private void PlaceBuildingOnGrid(Building building, ref int[,] mapGrid) {
        for (int i = 0; i < building.Width; i++) {
            for (int j = 0; j < building.Height; j++) {
                if (building.Shape[i, j]) {
                    int x = building.X + i;
                    int y = building.Y + j;

                    if (x >= 0 && x < gridSize && y >= 0 && y < gridSize)
                        mapGrid[x, y] = 1;
                }
            }
        }
    }
    //
    static public List<CarPartTypeRow> GetPlacedCarPatsOnCarBase(CarBaseMapConfig carBaseConfig, int carId,
        Dictionary<CarPartType, List<int>> carPartsToPlace, int moneyLimit) {
        List<int> mainParts = new List<int>();
        var carConfig = BetweenScenesService.Instance._carStuffListsConfig.CarConfigs[carId];
        int carCellsSum = 0;
        foreach (var carPartId in carConfig.StartCarParts) {
            if (BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs[carPartId].PartType == CarPartType.Main) {
                mainParts.Add(carPartId);
              //  carCellsSum -= BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs[carPartId].PartForm.Length;
            }
        }

        List<CarPartTypeRow> carPartUpLevelGrid = new List<CarPartTypeRow>();

        int[,] downGrid = new int[carBaseConfig.GridSize.x, carBaseConfig.GridSize.y];
        int[,] upGrid = new int[carBaseConfig.GridSize.x, carBaseConfig.GridSize.y];
        foreach (var carPartBase in carBaseConfig.CarPartDownLevelGrid) {
            var cells = GetCellsFromField(carPartBase.gridPosition,
                BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs[carPartBase.partId], carPartBase.rotate, downGrid);
            carCellsSum += cells.Count;
            foreach (var cell in cells)
                downGrid[cell.x, cell.y] = 1;
        }

        (List<Vector2Int> middleCells, List<Vector2Int> edgeCells) = GetEdgeAndMiddleCells(downGrid);
        int edgeCellsCount = edgeCells.Count;
        PlaceCarPartToCarBase(CarPartType.Main, 0, ref moneyLimit,
            mainParts, ref upGrid, middleCells, edgeCells, downGrid, ref carPartUpLevelGrid);
        int carType = Random.Range(0, 3);
        switch (carType) {
            //tank
            case 0:
                PlaceCarPartToCarBase(CarPartType.MeleeWeapon, Mathf.RoundToInt(edgeCellsCount * 0.4f), ref moneyLimit,
                    carPartsToPlace[CarPartType.MeleeWeapon], ref upGrid, middleCells, edgeCells, downGrid, ref carPartUpLevelGrid);
                PlaceCarPartToCarBase(CarPartType.Armor, Mathf.RoundToInt(edgeCellsCount * 0.6f), ref moneyLimit,
                    carPartsToPlace[CarPartType.Armor], ref upGrid, middleCells, edgeCells, downGrid, ref carPartUpLevelGrid);
                PlaceCarPartToCarBase(CarPartType.Gun, Mathf.RoundToInt((carCellsSum - edgeCellsCount) * 0.7f), ref moneyLimit,
                    carPartsToPlace[CarPartType.Gun], ref upGrid, middleCells, edgeCells, downGrid, ref carPartUpLevelGrid);
                PlaceCarPartToCarBase(CarPartType.Support, Mathf.RoundToInt((carCellsSum - edgeCellsCount) * 0.3f), ref moneyLimit,
                    carPartsToPlace[CarPartType.Support], ref upGrid, middleCells, edgeCells, downGrid, ref carPartUpLevelGrid);
                break;
            //range
            case 1:
                PlaceCarPartToCarBase(CarPartType.Armor, edgeCellsCount, ref moneyLimit, carPartsToPlace[CarPartType.Armor], ref upGrid,
                    middleCells, edgeCells, downGrid, ref carPartUpLevelGrid);
                PlaceCarPartToCarBase(CarPartType.Gun, Mathf.RoundToInt((carCellsSum - edgeCellsCount) * 0.65f), ref moneyLimit,
                    carPartsToPlace[CarPartType.Gun], ref upGrid, middleCells, edgeCells, downGrid, ref carPartUpLevelGrid);
                PlaceCarPartToCarBase(CarPartType.Support, Mathf.RoundToInt((carCellsSum - edgeCellsCount) * 0.35f), ref moneyLimit,
                    carPartsToPlace[CarPartType.Support], ref upGrid, middleCells, edgeCells, downGrid, ref carPartUpLevelGrid);

                break;
            //damager
            case 2:
                PlaceCarPartToCarBase(CarPartType.Gun, Mathf.RoundToInt((carCellsSum - edgeCellsCount) * 0.85f), ref moneyLimit,
                    carPartsToPlace[CarPartType.Gun], ref upGrid, middleCells, edgeCells, downGrid, ref carPartUpLevelGrid);
                PlaceCarPartToCarBase(CarPartType.MeleeWeapon, Mathf.RoundToInt(edgeCellsCount * 0.8f), ref moneyLimit,
                    carPartsToPlace[CarPartType.MeleeWeapon], ref upGrid, middleCells, edgeCells, downGrid, ref carPartUpLevelGrid);
                PlaceCarPartToCarBase(CarPartType.Armor, Mathf.RoundToInt(edgeCellsCount * 0.2f), ref moneyLimit,
                    carPartsToPlace[CarPartType.Armor], ref upGrid, middleCells, edgeCells, downGrid, ref carPartUpLevelGrid);
                PlaceCarPartToCarBase(CarPartType.Support, Mathf.RoundToInt((carCellsSum - edgeCellsCount) * 0.15f), ref moneyLimit,
                    carPartsToPlace[CarPartType.Support], ref upGrid, middleCells, edgeCells, downGrid, ref carPartUpLevelGrid);
                break;
        }

        //other car part types + main
        return carPartUpLevelGrid;
    }
    private static bool CanPlaceCarPartOnField(CarPartConfig form, Vector2Int position, int[,] grid,
        int[,] gridContainers, int degrees) /*need caar part containers grid*/ {
        var formCells = GetCellsFromField(position, form,degrees, grid);
        if (formCells.Count != form.PartForm.Length) return false;
        foreach (var cell in formCells) 
            if (grid[cell.x, cell.y] != 0 || gridContainers[cell.x, cell.y] == 0) return false;
        

        return true;
    }

    private static (List<Vector2Int> middleCells, List<Vector2Int> edgeCells) GetEdgeAndMiddleCells(int[,] grid) {
        List<Vector2Int> middleCells = new List<Vector2Int>();
        List<Vector2Int> edgeCells = new List<Vector2Int>();
        for (int x = 0; x < grid.GetLength(0); x++) {
            for (int y = 0; y < grid.GetLength(1); y++) {
                if (grid[x, y] != 0) {
                    Vector2Int cellPosition = new (x, y);
                 
                    if (IsEdgeContainerCell(cellPosition, grid))
                        edgeCells.Add(cellPosition);
                    else
                        middleCells.Add(cellPosition);
                }
            }
        }
        return (middleCells, edgeCells);
    }
    public static Dictionary<CarPartRare, int> _maxLevelsForRare = new Dictionary<CarPartRare, int>() {
        {CarPartRare.Low, 3},
        {CarPartRare.Medium, 5},
        {CarPartRare.High, 7},
        {CarPartRare.VeryHigh, 9}
    };

    private static void PlaceCarPartToCarBase(CarPartType partType, int needCount, ref int moneyLimit, List<int> carPartsToPlace,
        ref int[,] upGrid, List<Vector2Int> middleCells, List<Vector2Int> edgeCells, int[,] downGrid,
        ref List<CarPartTypeRow> carPartUpLevelGrid) {
        if (carPartsToPlace.Count == 0) return;
        while (((needCount > 0 && moneyLimit > 0) && partType != CarPartType.Main) ||
               (partType == CarPartType.Main && carPartsToPlace.Count > 0)) {
            bool canPlaceOnEdge = partType is CarPartType.Armor or CarPartType.MeleeWeapon;
            var currentCarPartConfig = partType != CarPartType.Main
                ? BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs[carPartsToPlace[Random.Range(0, carPartsToPlace.Count)]]
                : BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs[carPartsToPlace[0]];
            var carPartSize = GetCarPartSize(currentCarPartConfig);
            for (int j = 0; j < 3; j++) {
                Vector2Int randomPosition = Vector2Int.zero;
                int rotateAngle = 0;
                if (canPlaceOnEdge || middleCells.Count == 0) {
                    if (edgeCells.Count == 0) {
                        needCount -= currentCarPartConfig.PartForm.Length;
                        break;
                    }

                    randomPosition = edgeCells[Random.Range(0, edgeCells.Count)];
                    if (randomPosition.y + 1 >= upGrid.GetLength(1) || downGrid[randomPosition.x, randomPosition.y + 1] == 0) {
                        rotateAngle = 0;
                        if (carPartSize.y > 1)
                            randomPosition = new Vector2Int(randomPosition.x, randomPosition.y - (carPartSize.y - 1));
                    } else if (randomPosition.y - 1 < 0 || downGrid[randomPosition.x, randomPosition.y - 1] == 0)
                        rotateAngle = 180;

                    else if (randomPosition.x + 1 >= upGrid.GetLength(0) || downGrid[randomPosition.x + 1, randomPosition.y] == 0) {
                        rotateAngle = 90;
                        if (carPartSize.x > 1)
                            randomPosition = new Vector2Int(randomPosition.x - (carPartSize.x - 1), randomPosition.y);
                    } else if (randomPosition.x - 1 < 0 || downGrid[randomPosition.x- 1, randomPosition.y ] == 0)
                        rotateAngle = 270;
                } else {
                    float randomValue = Random.value;
                    randomPosition = middleCells[Random.Range(0, middleCells.Count)];
                    if (randomValue > 0.92f)
                        rotateAngle = 180;
                    else if (randomValue > 0.85f)
                        rotateAngle = 90;
                    else if (randomValue > 0.78f)
                        rotateAngle = 270;
                }

                if (CanPlaceCarPartOnField(currentCarPartConfig, randomPosition, upGrid, downGrid, rotateAngle)) {
                    //place car part on grid
                    var formCells = GetCellsFromField(randomPosition, currentCarPartConfig, rotateAngle, downGrid);
                    foreach (var cell in formCells) {
                        upGrid[cell.x, cell.y] = 1;
                        middleCells.Remove(cell);
                        edgeCells.Remove(cell);
                    }

                    carPartUpLevelGrid.Add(new CarPartTypeRow(randomPosition, currentCarPartConfig.Id, rotateAngle, 0));
                    if (partType == CarPartType.Main)
                        carPartsToPlace.RemoveAt(0);
                    else
                        moneyLimit -= (int)currentCarPartConfig.MoneyCost;
                    needCount -= currentCarPartConfig.PartForm.Length;
                    break;
                }

                if (j == 2)
                    needCount -= currentCarPartConfig.PartForm.Length;
            }
        }
    }
    private static int[,] PlaceCarPartOnGrid(int[,] mapGrid,Vector2Int[] carPart, int carPartId, Vector2Int needEdgePosition) {
        foreach (var needPosition in carPart) {
            mapGrid[needEdgePosition.x+needPosition.x, needEdgePosition.y + needPosition.y] = carPartId;
        }
        return mapGrid;
    }

    private class Building {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public bool[,] Shape { get; }

        public Building(int x, int y, int width, int height, bool[,] shape) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Shape = shape;
        }
    }
}


