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
}
