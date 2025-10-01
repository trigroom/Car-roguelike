using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

public class CellsSystem : IEcsRunSystem, IEcsInitSystem
{
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;

    private EcsPoolInject<BuildingCellComponent> _buildingCellComponentsPool;
    private EcsPoolInject<CellsFieldComponent> _cellsFieldComponentsPool;
    private EcsPoolInject<CarPartCellComponent> _cellComponentsPool;
    private EcsPoolInject<ChangeCurrentBuildingShowEvent> _changeCurrentBuildingShowEventsPool;

    private EcsFilterInject<Inc<ChangeCurrentBuildingShowEvent>> _changeCurrentBuildingShowEventsFilter;
    private EcsFilterInject<Inc<TryBuildBuildingEvent>> _tryBuildBuildingEventsFilter;
    private EcsFilterInject<Inc<CellsFieldComponent>> _cellsFieldComponentsFilter;
    public void Init(IEcsSystems systems)
    {
        _sceneData.Value.buyButtons[0].onClick.AddListener(GetTowerToBuy);
        _sceneData.Value.moveTowerButton.onClick.AddListener(StartMoveExistedTower);
        _sceneData.Value.destroyTowerButton.onClick.AddListener(DestroyTower);

        int xSize = 8;
        int zSize = 8;

        _cellsFieldComponentsPool.Value.Add(_world.Value.NewEntity()).occupedCells = new int[xSize, zSize];
        BuildCellsField(xSize, zSize);
        _sceneData.Value.cellsContainer.position = new Vector3((float)(-xSize + 1) / 2, _sceneData.Value.cellsContainer.position.y, -zSize / 2);
    }

    public void Run(IEcsSystems systems)
    {
        foreach (var showBuildingEvent in _changeCurrentBuildingShowEventsFilter.Value)
        {
            foreach (var cellsFieldEntity in _cellsFieldComponentsFilter.Value)
            {
                ref var cellsFieldCmp = ref _cellsFieldComponentsPool.Value.Get(cellsFieldEntity);

                if (cellsFieldCmp.currentBuildingView == null || cellsFieldCmp.currentInteractWithTowerState == CellsFieldComponent.InteractTowerState.showMenu) continue;
                bool canPlace = true;
                ref var cellCmp = ref _changeCurrentBuildingShowEventsPool.Value.Get(showBuildingEvent);
                ref var buildingCmp = ref _buildingCellComponentsPool.Value.Get(cellsFieldCmp.currentMovedBuildingEntity);

                foreach (var occupedCell in buildingCmp.buildingInfo.occupedCells)
                {
                    var originCellPosition = new Vector2(cellCmp.cellView.transform.localPosition.x + occupedCell.x, cellCmp.cellView.transform.localPosition.z + occupedCell.y);
                    if ((originCellPosition.x < 0 || originCellPosition.x + 1 > cellsFieldCmp.occupedCells.GetLength(0) ||
                        originCellPosition.y < 0 || originCellPosition.y + 1 > cellsFieldCmp.occupedCells.GetLength(1)) ||
                        cellsFieldCmp.occupedCells[(int)originCellPosition.x, (int)originCellPosition.y] != 0)
                    {
                        canPlace = false;
                        break;
                    }
                }
                if (!canPlace) continue;
                cellsFieldCmp.currentBuildingView.transform.position = cellCmp.cellView.transform.position;
                cellsFieldCmp.currentCellView = cellCmp.cellView;
            }
            _changeCurrentBuildingShowEventsPool.Value.Del(showBuildingEvent);
        }

        foreach (var tryBuild in _tryBuildBuildingEventsFilter.Value)
        {
            foreach (var cellsFieldEntity in _cellsFieldComponentsFilter.Value)
            {
                ref var cellsFieldCmp = ref _cellsFieldComponentsPool.Value.Get(cellsFieldEntity);
                if (cellsFieldCmp.currentInteractWithTowerState == CellsFieldComponent.InteractTowerState.none)
                    TryShowTowerMenu(tryBuild);
                else if (cellsFieldCmp.currentInteractWithTowerState != CellsFieldComponent.InteractTowerState.showMenu)
                    TryPlaceTower();
            }
        }
    }

    private void TryShowTowerMenu(int cellEntity)
    {
        foreach (var cellsFieldEntity in _cellsFieldComponentsFilter.Value)
        {
            ref var cellsFieldCmp = ref _cellsFieldComponentsPool.Value.Get(cellsFieldEntity);
            ref var cellCmp = ref _cellComponentsPool.Value.Get(cellEntity);
           // int buildingEntity = cellsFieldCmp.occupedCells[(int)cellCmp.cellView.transform.localPosition.x, (int)cellCmp.cellView.transform.localPosition.z];
          //  if (buildingEntity == 0) continue;

          //  ref var buildingCmp = ref _buildingCellComponentsPool.Value.Get(buildingEntity);

            //if (cellsFieldCmp.currentInteractWithTowerState == CellsFieldComponent.InteractTowerState.buildingNewTower || cellsFieldCmp.currentInteractWithTowerState == CellsFieldComponent.InteractTowerState.moving
          //      || (cellsFieldCmp.currentBuildingView != null && cellsFieldCmp.currentBuildingView == buildingCmp.currentBuilding)) continue;

            _sceneData.Value.towerButtonsContainer.anchoredPosition = _sceneData.Value.GetCurrentMousePositionOnUI();
            _sceneData.Value.towerButtonsContainer.gameObject.SetActive(true);

            cellsFieldCmp.currentInteractWithTowerState = CellsFieldComponent.InteractTowerState.showMenu;

          //  cellsFieldCmp.currentBuildingView = buildingCmp.currentBuilding;
           // cellsFieldCmp.currentCellView = cellCmp.cellView;
        }
    }
    private void TryPlaceTower()
    {
        foreach (var cellsFieldEntity in _cellsFieldComponentsFilter.Value)
        {
            ref var cellsFieldCmp = ref _cellsFieldComponentsPool.Value.Get(cellsFieldEntity);

            if (cellsFieldCmp.currentBuildingView == null || cellsFieldCmp.currentCellView == null) continue;

          //  ref var cellCmp = ref _cellComponentsPool.Value.Get(cellsFieldCmp.currentCellView._entity);

            /* if (cellsFieldCmp.currentInteractWithTowerState == CellsFieldComponent.InteractTowerState.buildingNewTower)
             {
                 int towerEntity = _world.Value.NewEntity();
                 _buildingCellComponentsPool.Value.Add(towerEntity).currentBuilding = cellsFieldCmp.currentBuildingView;
                 cellsFieldCmp.currentMovedBuildingEntity = towerEntity;
                 //cellsFieldCmp.currentBuildingView.Construct(_world.Value, towerEntity);
             }*/

            ref var buildingCmp = ref _buildingCellComponentsPool.Value.Get(cellsFieldCmp.currentMovedBuildingEntity);
            buildingCmp.occupedCells = new Vector2[buildingCmp.buildingInfo.occupedCells.Length];

            for (int i = 0; i < buildingCmp.buildingInfo.occupedCells.Length; i++)
            {
                var occupedCell = buildingCmp.buildingInfo.occupedCells[i];
                var originCellPosition = new Vector2(cellsFieldCmp.currentCellView.transform.localPosition.x + occupedCell.x, cellsFieldCmp.currentCellView.transform.localPosition.z + occupedCell.y);
                cellsFieldCmp.occupedCells[(int)originCellPosition.x, (int)originCellPosition.y] = cellsFieldCmp.currentMovedBuildingEntity;

                //cellsFieldCmp.currentBuildingView.buildingCollider.enabled = true;
                buildingCmp.occupedCells[i] = originCellPosition;
            }

            cellsFieldCmp.currentInteractWithTowerState = CellsFieldComponent.InteractTowerState.none;
            cellsFieldCmp.currentCellView = null;
            cellsFieldCmp.currentBuildingView = null;
        }
    }
    private void GetTowerToBuy()
    {
        foreach (var cellsFieldEntity in _cellsFieldComponentsFilter.Value)
        {
            //check money to buy

            ref var cellsFieldCmp = ref _cellsFieldComponentsPool.Value.Get(cellsFieldEntity);
            if (cellsFieldCmp.currentInteractWithTowerState == CellsFieldComponent.InteractTowerState.buildingNewTower)
                DestroyTower();

            else if (cellsFieldCmp.currentInteractWithTowerState == CellsFieldComponent.InteractTowerState.moving)
                TryPlaceTower();

            else if (cellsFieldCmp.currentInteractWithTowerState == CellsFieldComponent.InteractTowerState.showMenu)
                _sceneData.Value.towerButtonsContainer.gameObject.SetActive(false);

            cellsFieldCmp.currentBuildingView = _sceneData.Value.InstantiateBuilding(_sceneData.Value.buildingInfo.buildingView);

            int towerEntity = _world.Value.NewEntity();
            ref var buildingCmp = ref _buildingCellComponentsPool.Value.Add(towerEntity);

            buildingCmp.buildingInfo = _sceneData.Value.buildingInfo;
            buildingCmp.currentBuilding = cellsFieldCmp.currentBuildingView;

            cellsFieldCmp.currentMovedBuildingEntity = towerEntity;

            cellsFieldCmp.currentInteractWithTowerState = CellsFieldComponent.InteractTowerState.buildingNewTower;
            //cellsFieldCmp.currentBuildingView.buildingCollider.enabled = false;
        }
    }

    private void BuildCellsField(int xSize, int ySize)
    {
        for (int i = 0; i < xSize; i++)
        {
            for (int j = 0; j < ySize; j++)
            {
                var newCell = GetNewCell();
                int cellEntity = _world.Value.NewEntity();
              //  newCell.Construct(_world.Value, cellEntity);
          //      _cellComponentsPool.Value.Add(cellEntity).cellView = newCell;
                newCell.transform.localPosition = new Vector3(i, 1.5f, j);
            }
        }
    }

    public void StartMoveExistedTower()
    {
        foreach (var cellsFieldEntity in _cellsFieldComponentsFilter.Value)
        {
            ref var cellsFieldCmp = ref _cellsFieldComponentsPool.Value.Get(cellsFieldEntity);
            Vector3 curTower = cellsFieldCmp.currentCellView.transform.localPosition;
            int buildingEntity = cellsFieldCmp.occupedCells[(int)curTower.x, (int)curTower.z];

            cellsFieldCmp.currentMovedBuildingEntity = buildingEntity;
            ref var buildingCmp = ref _buildingCellComponentsPool.Value.Get(buildingEntity);
            foreach (var curOccupedCell in buildingCmp.occupedCells)
                cellsFieldCmp.occupedCells[(int)curOccupedCell.x, (int)curOccupedCell.y] = 0;

            buildingCmp.occupedCells = null;
            //buildingCmp.currentBuilding.buildingCollider.enabled = false;
            cellsFieldCmp.currentInteractWithTowerState = CellsFieldComponent.InteractTowerState.moving;
            _sceneData.Value.towerButtonsContainer.gameObject.SetActive(false);
        }
    }


    public void DestroyTower()
    {
        foreach (var cellsFieldEntity in _cellsFieldComponentsFilter.Value)
        {
            ref var cellsFieldCmp = ref _cellsFieldComponentsPool.Value.Get(cellsFieldEntity);
            Vector3 curTower = cellsFieldCmp.currentCellView.transform.localPosition;
            int buildingEntity = cellsFieldCmp.occupedCells[(int)curTower.x, (int)curTower.z];

            ref var buildingCmp = ref _buildingCellComponentsPool.Value.Get(buildingEntity);

            foreach (var curOccupedCell in buildingCmp.occupedCells)
                cellsFieldCmp.occupedCells[(int)curOccupedCell.x, (int)curOccupedCell.y] = 0;

            _buildingCellComponentsPool.Value.Del(buildingEntity);


            _sceneData.Value.DestroyGameobject(cellsFieldCmp.currentBuildingView.transform);
            cellsFieldCmp.currentBuildingView = null;

            cellsFieldCmp.currentInteractWithTowerState = CellsFieldComponent.InteractTowerState.none;
            _sceneData.Value.towerButtonsContainer.gameObject.SetActive(false);
        }
    }

    private CellView GetNewCell()
    {
        return _sceneData.Value.InstantiateCell(_sceneData.Value.emptyCell, _sceneData.Value.cellsContainer);
    }
}
