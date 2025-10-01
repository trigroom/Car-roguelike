
struct CellsFieldComponent
{
    public int[,] occupedCells;
    public BuildingView currentBuildingView;
    public CellView currentCellView;
    public int currentMovedBuildingEntity;
    public InteractTowerState currentInteractWithTowerState;
    public enum InteractTowerState
    {
        none,
        showMenu,
        moving,
        buildingNewTower
    }

}