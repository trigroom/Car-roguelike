using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

sealed class UiControlSystem : IEcsRunSystem, IEcsInitSystem
{
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;

    private EcsPoolInject<BuildingCellComponent> _buildingCellComponentsPool;
    private EcsPoolInject<CellsFieldComponent> _cellsFieldComponentsPool;

    private EcsFilterInject<Inc<ShowTowerMenuEvent>> _showTowerMenuEventsFilter;
    private EcsFilterInject<Inc<CellsFieldComponent>> _cellsFieldComponentsFilter;

    public void Init(IEcsSystems systems)
    {
        _sceneData.Value.closeTowerActionsContainerButton.onClick.AddListener(CloseTowerMenuContainer);
    }

    public void Run(IEcsSystems systems)
    {
      
    }

    private void CloseTowerMenuContainer()
    {
        foreach (var cellsFieldEntity in _cellsFieldComponentsFilter.Value)
        {
            ref var cellsFieldCmp = ref _cellsFieldComponentsPool.Value.Get(cellsFieldEntity);
            cellsFieldCmp.currentInteractWithTowerState = CellsFieldComponent.InteractTowerState.none;
            cellsFieldCmp.currentCellView = null;
            cellsFieldCmp.currentBuildingView = null;
            cellsFieldCmp.currentMovedBuildingEntity = 0;
            _sceneData.Value.towerButtonsContainer.gameObject.SetActive(false);
        }
    }
}