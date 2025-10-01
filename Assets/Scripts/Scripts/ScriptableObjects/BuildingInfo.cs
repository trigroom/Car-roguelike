using UnityEngine;

[CreateAssetMenu(fileName = "BuildingInfo", menuName = "Scriptable Objects/BuildingInfo")]
public class BuildingInfo : ScriptableObject
{
    public Vector2[] occupedCells;
    public int cost;
    public string towerName;
    public BuildingView buildingView;
}
