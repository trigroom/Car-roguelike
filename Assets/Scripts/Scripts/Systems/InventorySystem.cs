using System;
using System.Collections.Generic;
using System.Linq;
using Client;
using DG.Tweening;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class InventorySystem : IEcsRunSystem, IEcsInitSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;

    private EcsPoolInject<CarComponent> _carComponentsPool;
    private EcsPoolInject<CarPartCellComponent> _carPartCellComponentsPool;
    private EcsPoolInject<MovementComponent> _movementComponentsPool;
    private EcsPoolInject<ShopCellComponent> _shopCellComponentsPool;
    private EcsPoolInject<ShopComponent> _shopComponentsPool;
    private EcsPoolInject<FuelComponent> _fuelComponentsPool;
    private EcsPoolInject<PlayerCurrencyComponent> _playerCurrencyComponentsPool;
    private EcsPoolInject<PlayerDeathEvent> _playerDeathEventsPool;
    private EcsPoolInject<InventoryComponent> _inventoryComponentsPool;
    private EcsPoolInject<InventoryCellComponent> _inventoryCellComponentsPool;
    private EcsPoolInject<CarPartInGameUIComponent> _carPartInGameUIComponentsPool;
    private EcsPoolInject<AddInventoryItemEvent> _addInventoryItemEventsPool;
    private EcsPoolInject<UpdateShopToSaleRoundEvent> _updateShopToSaleRoundEventsPool;
    private EcsPoolInject<CurrentGameComponent> _currentGameComponentsPool;
    private EcsPoolInject<CarPartComponent> _carPartComponentsPool;
 //   private EcsPoolInject<EndRoundEvent> _endRoundEventsPool;

    private EcsFilterInject<Inc<UpdateShopToSaleRoundEvent>> _updateShopToSaleRoundEventsFilter;
    private EcsFilterInject<Inc<CarComponent>> _carComponentsFilter;
    private EcsFilterInject<Inc<StartRoundEvent>> _startRoundEventsFilter;
    private EcsFilterInject<Inc<CarPartComponent>> _carPartComponentsFilter;
    private EcsFilterInject<Inc<AddInventoryItemEvent>> _addInventoryItemEventsFilter;

    public void Init(IEcsSystems systems) {
        //test

        int playerEntity = _world.Value.NewEntity();
        _sceneData.Value.PlayerEntity = playerEntity;

        _playerCurrencyComponentsPool.Value.Add(playerEntity).moneyCount = 100;

        ref var carCmp = ref _carComponentsPool.Value.Add(playerEntity);
        carCmp.CarId = _sceneData.Value._defaultCarConfigId;
        
        var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Add(playerEntity);
        inventoryCmp.CurrentInventoryPage = 0;
        inventoryCmp.PartsInInventory = new List<int>();
        inventoryCmp.CurrentDraggedCellEntity = -1;
        inventoryCmp.CurrentCellCarPart = null;
//test
        ref var movementComponent = ref _movementComponentsPool.Value.Add(playerEntity);
        //movementComponent.MaxMoveSpeed = carConfig.DefaultMaxSpeed;
        movementComponent.RotateSpeed = carConfig.RotateSpeed;
        movementComponent.Acceleration = carConfig.Acceleration;
        movementComponent.Deceleration = carConfig.Deceleration;
        SortCarParts(CarPartType.None);

        SetCarGrid();

        SetSortButtons();
        _sceneData.Value._carPartInfoUIView.SetupButtonsInfoPanel(BackCarPartInPanelInfo, DestroyMarkedCarPart, UpgradeMarkedCarPart,MoveToInventoryCell);
        _sceneData.Value._inventoryUIView._changeToRedactModeButton.onClick.AddListener(SetCarPartRedactorMode);
        SetupInventoryCells();

        StartupShopCells();

        UpdateCarDescription();
        /*  foreach (var carPartId in _sceneData.Value._gameProcessConfig.GameRounds[0].CarParts) {
              var gun = _sceneData.Value.GetGunFromCarPart(carPartId);
              if (gun != null) {
                  Debug.Log(gun.BulletsCount);
              }
          }*/
    }

    public void Run(IEcsSystems systems) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        foreach (var startRound in _startRoundEventsFilter.Value) {
            SetShopCells(false);
            //     _endRoundEventsPool.Value.Del(endRound);
        }

        foreach (var updateSaleShop in _updateShopToSaleRoundEventsFilter.Value) {
            SetShopCells(true);
            _updateShopToSaleRoundEventsPool.Value.Del(updateSaleShop);
        }

        if (Input.GetMouseButtonDown(0))
            inventoryCmp.StartMouseInputPosition = Input.mousePosition;
        if (Input.GetMouseButton(0))
            inventoryCmp.PressedButtonTime += Time.deltaTime;

        if ((inventoryCmp.PressedButtonTime > 0.5f || Vector3.Distance(inventoryCmp.StartMouseInputPosition, Input.mousePosition) > 10f) &&
            inventoryCmp.IsTryDraggedCell) {
            if (!inventoryCmp.CellIsSetup) {
                if (_inventoryCellComponentsPool.Value.Has(inventoryCmp.CurrentDraggedCellEntity))
                    StartDragInventoryCell(ref inventoryCmp);
                else {
                    StartDragShopCell(ref inventoryCmp);
                }

                _sceneData.Value._inventoryUIView._draggedInventoryCell.transform.position = (Vector2)Input.mousePosition +
                    _sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform.sizeDelta - inventoryCmp.onCarCellSize;
            }

            inventoryCmp.CellIsSetup = true;

            if (inventoryCmp.CurrentDraggedCellEntity != -1) {
                var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
               
            var currentCellSize =new Vector2(200, 200) / Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0),
                    carCmp.CarPartContainerGroupsGrid.GetLength(1));
            var carPartsContainer = _sceneData.Value._carPartsContainersContainer;
            _sceneData.Value._subGridTransform.transform.position = carPartsContainer.position;
            _sceneData.Value._gridMaskRect.position = _sceneData.Value._inventoryUIView._draggedInventoryCell.transform.position;
            Vector2 positionOffset = Vector2.zero;

            var carPartSize = GridUtils.GetCarPartSize(GetCurrentDraggedCellCarPart());
            var startOffset = -inventoryCmp.StartOffsetFromCell;
            if (inventoryCmp.CurrentRotateDegrees == 90) {
                  startOffset = new Vector2(-startOffset.y, startOffset.x);
                  carPartSize = new Vector2Int(carPartSize.y, carPartSize.x);
            }
                
              
            else if (inventoryCmp.CurrentRotateDegrees == 180) {
                  startOffset = new Vector2(-startOffset.x, -startOffset.y);
                  carPartSize = new Vector2Int(carPartSize.x, carPartSize.y);
            }
              
            else if (inventoryCmp.CurrentRotateDegrees == 270) {
                 startOffset = new Vector2(startOffset.y, -startOffset.x);
                 carPartSize = new Vector2Int(carPartSize.y, carPartSize.x);
            }
               
            positionOffset = new Vector2(carPartSize.x, carPartSize.y) / 2 * currentCellSize * startOffset *
                             _sceneData.Value.mainCanvas.localScale.x;
            var edgePositionOffset = new Vector2(carPartSize.x - 1, carPartSize.y - 1) / 2 * currentCellSize *
                                     _sceneData.Value.mainCanvas.localScale.x;
              
             
              
              var finalEdgeInputPosition =(Vector2)Input.mousePosition + positionOffset - edgePositionOffset;
          

            if (inventoryCmp.LastCheckedGridCellPosition != GetCurrentDragCarPartContainerGridPosition(finalEdgeInputPosition)) {
                var positionOnGrid = GetCurrentDragCarPartContainerGridPosition(finalEdgeInputPosition);
                inventoryCmp.LastCheckedGridCellPosition = positionOnGrid;
                inventoryCmp.IsSnappingToGrid = inventoryCmp.IsCarPartsRedactorMode
                    ? CanPlacedOnCarCell()
                    : CanPlaceCarPartsContainerOnField(positionOnGrid);
            }

            Vector2 targetCellPosition;
            if (carCmp.CarPartContainerGroupsList.Count != 0 && inventoryCmp.IsSnappingToGrid) {
                if (_sceneData.Value._subGridTransform.GetCurrentState() != inventoryCmp.IsSnappingToGrid)
                    _sceneData.Value._subGridTransform.SetSubGridState(inventoryCmp.IsSnappingToGrid || carCmp.CarPartContainerGroupsList.Count == 0);
                var worldPosition = GetWorldPositionFromGrid(GetCurrentDragCarPartContainerGridPosition(finalEdgeInputPosition));
                targetCellPosition = ((worldPosition + edgePositionOffset) * 5 + (Vector2)Input.mousePosition + positionOffset) / 6;
                
                targetCellPosition = (worldPosition + edgePositionOffset);
            } else {
                targetCellPosition = (Vector2)Input.mousePosition + positionOffset;
                if (_sceneData.Value._subGridTransform.GetCurrentState() != inventoryCmp.IsSnappingToGrid)
                    _sceneData.Value._subGridTransform.SetSubGridState(inventoryCmp.IsSnappingToGrid);
            }
                _sceneData.Value._inventoryUIView._draggedInventoryCell.transform.position = Vector3.Slerp(
                    _sceneData.Value._inventoryUIView._draggedInventoryCell.transform.position, targetCellPosition, Time.deltaTime * 10f);
                if (Input.GetMouseButtonDown(1) && (GetCurrentDraggedCellCarPart().PartType == CarPartType.Gun ||
                                                    GetCurrentDraggedCellCarPart().PartType == CarPartType.MeleeWeapon))
                    RotateCarPart();
                if (Input.GetMouseButtonUp(0)) {
                    if (inventoryCmp.IsCarPartsRedactorMode) {
                        if (_currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRoundType ==
                            RoundType.Transfer) { } else {
                            if (CanPlacedOnCarCell())
                                PlaceCarPartInCell(ref inventoryCmp /*, ref carPartCellCmp*/);
                            else
                                CancelCarPartPlace(ref inventoryCmp);
                        }
                    } else {
                        if (inventoryCmp.CurrentDraggedCellEntity != -1) {
                            if (inventoryCmp.CurrentCellCarPart != null &&
                                inventoryCmp.CurrentCellCarPart._gridPosition == new Vector2Int(-999, -999)) {
                                SetFirstCarPartContainer();
                                inventoryCmp.CurrentDraggedCellEntity = -1;
                            } else if (CanPlaceCarPartsContainerOnField(GetCurrentDragCarPartContainerGridPosition(finalEdgeInputPosition))) {
                                SetCarPartContainer(finalEdgeInputPosition);
                                inventoryCmp.CurrentDraggedCellEntity = -1;
                            } else {
                                CancelCarPartPlace(ref inventoryCmp);
                            }
                        } else {
                            CancelCarPartPlace(ref inventoryCmp);
                        }
                    }

                    inventoryCmp.CellIsSetup = false;
                }
            }
        }

        if (Input.GetMouseButtonUp(0)) {
            inventoryCmp.PressedButtonTime = 0;
            inventoryCmp.IsTryDraggedCell = false;
        }

        foreach (var addItem in _addInventoryItemEventsFilter.Value) {
          AddInventoryItem(_addInventoryItemEventsPool.Value.Get(addItem).CarPartId);
          _addInventoryItemEventsPool.Value.Del(addItem);
        }
    }

    private void SetCarPartRedactorMode() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (!inventoryCmp.IsCarPartsRedactorMode && !CanUseThisCarPartContainers()) 
            return;
        //messege about this
       
       inventoryCmp.IsCarPartsRedactorMode =  !inventoryCmp.IsCarPartsRedactorMode;

       if (inventoryCmp.IsCarPartsRedactorMode) {
           _sceneData.Value._inventoryUIView._changeToRedactModeImage.sprite =
               _sceneData.Value._inventoryUIView._changeToRedactCarPartsModeSprite;
           _sceneData.Value._inventoryUIView._changeToRedactModeText.text = "Redact up level";
           _sceneData.Value._inventoryUIView._carPartsUpLevelContainer.alpha = 1;
       } else {
           _sceneData.Value._inventoryUIView._changeToRedactModeImage.sprite =
               _sceneData.Value._inventoryUIView._changeToRedactCarContainersModeSprite;
           _sceneData.Value._inventoryUIView._changeToRedactModeText.text = "Redact down level";
           _sceneData.Value._inventoryUIView._carPartsUpLevelContainer.alpha = 0.25f;
       }
           
    }

    private void StartDragShopCell(ref InventoryComponent inventoryCmp) {
        var inventoryView = _sceneData.Value._inventoryUIView;
        var shopItem = _shopCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CurrentBuyItem;
        int carPartId = shopItem.PartId;
        int inventoryPartIndex = carPartId;
        //  inventoryView._inventoryCellViews[inventoryCmp.CurrentDraggedCellEntity].gameObject.SetActive(false);
        var partInfo = _sceneData.Value._carStuffListsConfig.CarPartConfigs[inventoryPartIndex];
        if ((partInfo.PartType == CarPartType.CarStuffContainer || partInfo.PartType == CarPartType.WheelsContainer) ==
            inventoryCmp.IsCarPartsRedactorMode) return;

        inventoryCmp.StartOffsetFromCell = GridUtils.GetNormalizedMouseOffset(_sceneData.Value._shopUIView
            ._buttonsShop[_shopCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).ShopButtonIndex].ItemSprite.rectTransform);
        SetDraggedCell(ref inventoryCmp, inventoryPartIndex, inventoryView);
    }

    private void StartDragInventoryCell(ref InventoryComponent inventoryCmp) {
        var inventoryView = _sceneData.Value._inventoryUIView;
        int inventoryCellIndex = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CellId;
        int inventoryPartIndex =
            GetSortedCarPartList()[
                inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length + inventoryCellIndex];
        var partInfo = _sceneData.Value._carStuffListsConfig.CarPartConfigs[inventoryPartIndex];
        if ((partInfo.PartType == CarPartType.CarStuffContainer || partInfo.PartType == CarPartType.WheelsContainer) ==
            inventoryCmp.IsCarPartsRedactorMode) return;
        inventoryCmp.StartOffsetFromCell =
            GridUtils.GetNormalizedMouseOffset(inventoryView._inventoryCellViews[inventoryCellIndex]._inventoryCellImage.rectTransform);
        SetDraggedCell(ref inventoryCmp, inventoryPartIndex, inventoryView);
 
        inventoryView._inventoryCellViews[inventoryCellIndex].gameObject.SetActive(false);
        
    }

    private void MoveToInventoryCell() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var carPartConfig = GetCarPartConfig(_carPartComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo).CarPartId);
        if (carPartConfig.PartType == CarPartType.WheelsContainer || carPartConfig.PartType == CarPartType.CarStuffContainer)
            MoveCarPartContainerToInventory(inventoryCmp.CurrentMarkedCellForPanelInfo);
        else
            MoveToInventoryCellNeededCarPart(inventoryCmp.CurrentMarkedCellForPanelInfo);
    }

    private void MoveToInventoryCellNeededCarPart(int currentPartEntity) {    
        int currentPartId = _carPartComponentsPool.Value.Get(currentPartEntity).CarPartId;
        
        DestroyNeededCarPart(currentPartEntity);
         AddInventoryItem(currentPartId);
    }

    private void SetDraggedCell(ref InventoryComponent inventoryCmp, int inventoryPartIndex, InventoryUIView inventoryView) {
        var partInfo = _sceneData.Value._carStuffListsConfig.CarPartConfigs[inventoryPartIndex];
        inventoryView._draggedInventoryCell.gameObject.SetActive(true);
        inventoryView._draggedInventoryCell.sprite = partInfo.PartSprite;

        _sceneData.Value._inventoryUIView._draggedInventoryCell.transform.rotation = Quaternion.Euler(0, 0, 0);
        inventoryCmp.CurrentRotateDegrees = 0;
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        Vector2 currentCellSize = /*inventoryCmp.IsCarPartsRedactorMode ? inventoryCmp.onCarCellSize :*/
            new Vector2(200, 200) / Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1));
        Vector2 cellSize = GridUtils.GetCarPartSize(partInfo) * currentCellSize;
        _sceneData.Value._subGridTransform.gameObject.SetActive(true);
        _sceneData.Value._gridMaskRect.gameObject.SetActive(true);
        _sceneData.Value._subGridTransform.SetSubGridSize(Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0),
            carCmp.CarPartContainerGroupsGrid.GetLength(1)) * 8);

        _sceneData.Value._gridMaskRect.sizeDelta = cellSize + currentCellSize * 1.8f;
        inventoryCmp.CurrentTween = _sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform.DOSizeDelta(cellSize, 0.3f)
            .SetEase(Ease.OutBack);
    }

    private void RotateCarPart() {
        ref var inventoryComponent = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (inventoryComponent.CurrentTween != null && inventoryComponent.CurrentTween.IsActive()) {
            inventoryComponent.CurrentTween.Kill();
        }

        inventoryComponent.CurrentRotateDegrees += 90;
        if (inventoryComponent.CurrentRotateDegrees >= 360)
            inventoryComponent.CurrentRotateDegrees = 0;

        var cellView = _sceneData.Value._inventoryUIView._draggedInventoryCell;
        
        _sceneData.Value._gridMaskRect.sizeDelta = new Vector2(_sceneData.Value._gridMaskRect.sizeDelta.y, _sceneData.Value._gridMaskRect.sizeDelta.x);
        // Создаем новую анимацию от текущего положения к целевому
        inventoryComponent.CurrentTween = cellView.transform
            .DORotate(new Vector3(0, 0, inventoryComponent.CurrentRotateDegrees), 0.3f, RotateMode.Fast).SetEase(Ease.OutBack);
    }

    private void StartupShopCells() {
        ref var shopCmp = ref _shopComponentsPool.Value.Add(_sceneData.Value.PlayerEntity);
        shopCmp.AllUnlockedShopItems = new List<ShopItem>();
        shopCmp.CurrentShopItems = new List<ShopItem>();

        foreach (var shopItem in _sceneData.Value._carStuffListsConfig.ShopItems) {
            shopCmp.AllUnlockedShopItems.Add(shopItem);
            if (GetCarPartConfig(shopItem.PartId).RartRare == CarPartRare.Low)
                shopCmp.CurrentShopItems.Add(shopItem);
        }

        _sceneData.Value._shopUIView._buttonUpdateShop.onClick.AddListener(UpdateShopItems);
        for (int i = 0; i < _sceneData.Value._shopUIView._buttonsShop.Length; i++) {
            var shopCell = _sceneData.Value._shopUIView._buttonsShop[i];
            int shopCellEntity = _world.Value.NewEntity();
            ref var shopCellCmp = ref _shopCellComponentsPool.Value.Add(shopCellEntity);
            shopCellCmp.ShopButtonIndex = i;
            shopCell.Construct(shopCellEntity, BuyShopItem, SetCarPartInfoPanel, TakeItemFromInventory);
        }

        SetShopCells(false);
    }

    private void UpdateShopItems() {
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        _sceneData.Value._shopUIView._updateItemsText.text = "update for 5$";
        if (playerCurrencyCmp.moneyCount >= 5) {
            playerCurrencyCmp.moneyCount -= 5;
            SetShopCells(false);
            SetMoneyText();
            
        }
    }

    private void SetCarContainersContainerSize() {
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
       
        float containerSizeX = carCmp.CarPartContainerGroupsGrid.GetLength(0) >carCmp.CarPartContainerGroupsGrid.GetLength(1)
            ? 200f
            : 200f * ((float)carCmp.CarPartContainerGroupsGrid.GetLength(0)/ (carCmp.CarPartContainerGroupsGrid.GetLength(1)));
        float containerSizeY = carCmp.CarPartContainerGroupsGrid.GetLength(0) < carCmp.CarPartContainerGroupsGrid.GetLength(1)
            ? 200
            : 200f * ((float)carCmp.CarPartContainerGroupsGrid.GetLength(1) / (carCmp.CarPartContainerGroupsGrid.GetLength(0)));
        _sceneData.Value._carPartsContainersContainer.sizeDelta = new Vector2(containerSizeX, containerSizeY); 
        _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).onCarCellSize = new Vector2(containerSizeX/carCmp.CarPartContainerGroupsGrid.GetLength(0),containerSizeX/carCmp.CarPartContainerGroupsGrid.GetLength(0));
    }

    private void SetFirstCarPartContainer() {
        Debug.Log(" SetFirstCarPartContainer");
        var carPartConfig = GetCurrentDraggedCellCarPart();
       
        
        if (carPartConfig.PartType == CarPartType.CarStuffContainer || carPartConfig.PartType == CarPartType.WheelsContainer) { 
            RemoveCurrentCarPart();
            _sceneData.Value._carPartsContainersContainer.GetComponent<CellView>()._gridPosition = -Vector2Int.one;
            ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            var partSize = GridUtils.GetCarPartSize(carPartConfig);
            int carPartEntity = _world.Value.NewEntity();
            ref var carPartCmp  = ref _carPartComponentsPool.Value.Add(carPartEntity);
          
            carPartCmp.carPartCellImage = _sceneData.Value.GetCarPartUISprite(_sceneData.Value._carPartsContainersContainer,
                _sceneData.Value._carPartsContainersContainer.position);
            carPartCmp.carPartCellImage.transform.localPosition = new Vector2(_sceneData.Value._carPartsContainersContainer.sizeDelta.x / 2,
               _sceneData.Value._carPartsContainersContainer.sizeDelta.y / 2);
            carPartCmp.carPartCellImage.rectTransform.sizeDelta = _sceneData.Value._carPartsContainersContainer.sizeDelta;
            carPartCmp.carPartCellImage.sprite = carPartConfig.PartSprite;
            carCmp.CarPartContainerGroupsGrid = new int[partSize.x, partSize.y];
            carCmp.CarPartGroupsGrid = new int[partSize.x, partSize.y];
            for (int i = 0; i < carCmp.CarPartContainerGroupsGrid.GetLength(0); i++)
            {
                for (int j = 0; j < carCmp.CarPartContainerGroupsGrid.GetLength(1); j++)
                    carCmp.CarPartContainerGroupsGrid[i, j] = carPartEntity;
            }
            SetCarContainersContainerSize();
            carCmp.CarPartContainerGroupsList.Add(carPartEntity,carPartConfig.PartForm.ToList());
            carPartCmp.CarPartId = carPartConfig.Id;
            
        } else {
               CancelCarPartPlace(ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity));
        }
        _sceneData.Value._subGridTransform.gameObject.SetActive(false);
        _sceneData.Value._gridMaskRect.gameObject.SetActive(false);
        _sceneData.Value._inventoryUIView._draggedInventoryCell.gameObject.SetActive(false);
    }

    private void RemoveCurrentCarPart() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        bool isBuyCell = _shopCellComponentsPool.Value.Has(inventoryCmp.CurrentDraggedCellEntity);
        int partIndex;
        if (!isBuyCell) {
            int cellId = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CellId;

            if (inventoryCmp.CurrentMarkedCellForPanelInfo == _sceneData.Value._inventoryUIView._inventoryCellViews[cellId]._entity)
                CloseShowInfoPanel();

            int partsInInventoryIndex = inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length +
                                        cellId;
            partIndex = GetSortedCarPartList()[partsInInventoryIndex];
            inventoryCmp.PartsInInventory.Remove(partIndex);

            if (inventoryCmp.CurrentInventoryPage != 0 &&
                !_sceneData.Value._inventoryUIView._inventoryCellViews[1].gameObject.activeInHierarchy)
                inventoryCmp.CurrentInventoryPage--;

            SetInventoryCells();
        } else {
            var shopCellCmp = _shopCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity);
            if (RemoveMoneyForBuy(shopCellCmp)) {
                CancelCarPartPlace(ref inventoryCmp);
                return;
            }

            partIndex = shopCellCmp.CurrentBuyItem.PartId;
        }
    }
    private void SetCarPartContainer(Vector2 edgePosition) { 
        var carPartConfig = GetCurrentDraggedCellCarPart();

        if (carPartConfig.PartType == CarPartType.CarStuffContainer || carPartConfig.PartType == CarPartType.WheelsContainer) {  
            RemoveCurrentCarPart();   
            int carPartEntity = _world.Value.NewEntity();
            Vector2Int currentGridPosition = GetCurrentDragCarPartContainerGridPosition(edgePosition);
           // Debug.Log(" SetCarPartContainer"+currentGridPosition);
            var newFormCellPositions = GetCellsFromField(currentGridPosition, carPartConfig);
            int xOffsetUp = 0;
            int xOffsetDown = 0;
            int yOffsetUp = 0;
            int yOffsetDown = 0;
            Vector2Int carPartSize = GridUtils.GetCarPartSize(carPartConfig);
            ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            Vector2Int carPartContainerGridSize = new (carCmp.CarPartContainerGroupsGrid.GetLength(0),
                carCmp.CarPartContainerGroupsGrid.GetLength(1));
            
            if (currentGridPosition.x < 0)
                xOffsetDown =currentGridPosition.x;
            if (currentGridPosition.x + (carPartSize.x)> carPartContainerGridSize.x )
                xOffsetUp = Mathf.Abs(currentGridPosition.x  + (carPartSize.x)- carPartContainerGridSize.x) ;

            if (currentGridPosition.y < 0)
                yOffsetDown =currentGridPosition.y;
            if (currentGridPosition.y + (carPartSize.y)> carPartContainerGridSize.y )
                yOffsetUp =Mathf.Abs( currentGridPosition.y + (carPartSize.y) - carPartContainerGridSize.y);
            
            if (xOffsetUp != 0 || yOffsetUp != 0 || xOffsetDown != 0 || yOffsetDown != 0 )
                carCmp.CarPartContainerGroupsGrid = new int[carPartContainerGridSize.x + Mathf.Abs(xOffsetUp) + Mathf.Abs(xOffsetDown),
                    carPartContainerGridSize.y + Mathf.Abs(yOffsetUp)+ Mathf.Abs(yOffsetDown)];

            carPartContainerGridSize = new(carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1));
            carCmp.CarPartGroupsGrid = new int[carPartContainerGridSize.x, carPartContainerGridSize.y];
            if (xOffsetDown < 0)
                currentGridPosition.x -= xOffsetDown;
            if (yOffsetDown < 0)
                currentGridPosition.y -= yOffsetDown;

          //  float gridCellSize = 200 / MathF.Max(carPartContainerGridSize.x, carPartContainerGridSize.y);
     //       gridCellNewSize = new (gridCellSize, gridCellSize);
            
            SetCarContainersContainerSize();
            Vector2 gridCellNewSize = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).onCarCellSize;
            SetNewGridsFromLists(xOffsetDown, yOffsetDown, gridCellNewSize);

            foreach (var newFormCell in newFormCellPositions) {
                var newCell = newFormCell;
                if (xOffsetDown < 0)
                    newCell.x -= xOffsetDown;
                if (yOffsetDown < 0)
                    newCell.y -= yOffsetDown;

                if(!carCmp.CarPartContainerGroupsList.TryAdd(carPartEntity, new List<Vector2Int>{newCell}))
                    carCmp.CarPartContainerGroupsList[carPartEntity].Add(newCell);
                carCmp.CarPartContainerGroupsGrid[newCell.x, newCell.y] = carPartEntity;
                
            }
            
            _carPartComponentsPool.Value.Add(carPartEntity);
            Vector2 offset = (carPartSize - Vector2Int.one) * gridCellNewSize;
            if (carPartSize != Vector2Int.one)
                offset = new Vector2(carPartSize.x - 1, carPartSize.y - 1)/2 * gridCellNewSize * _sceneData.Value.mainCanvas.localScale.x;
            Vector3 finalPosition = GetWorldPositionFromGrid(currentGridPosition) + offset;
            Sequence carPartSequence = DOTween.Sequence();
            carPartSequence.Append(_sceneData.Value._inventoryUIView._draggedInventoryCell.transform.DOMove(finalPosition, 0.2f)
                .SetEase(Ease.InOutQuad));
            carPartSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform
                .DOSizeDelta(gridCellNewSize * carPartSize, 0.2f).SetEase(Ease.InOutQuad));
            carPartSequence.OnComplete(() => {
                CreateCarPartContainerOnComplete(carPartEntity, finalPosition, carPartConfig, gridCellNewSize, carPartSize);
            });
        
          //  carPartSequence.Play();
        } else {
            CancelCarPartPlace(ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity));
        }

        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        Debug.Log(inventoryCmp.CurrentMarkedCellForPanelInfo + "inventoryCmp.CurrentMarkedCellForPanelInfo");
        if (inventoryCmp.CurrentMarkedCellForPanelInfo != 0)
            SetCarPartInfoPanel(inventoryCmp.CurrentMarkedCellForPanelInfo);
        _sceneData.Value._subGridTransform.gameObject.SetActive(false);
        _sceneData.Value._gridMaskRect.gameObject.SetActive(false);
    }

    private void SetNewGridsFromLists(int xOffsetDown, int yOffsetDown, Vector2 gridCellNewSize) {
        Debug.Log(" offset move"+xOffsetDown+" "+yOffsetDown);
        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var cellGroup in carCmp.CarPartContainerGroupsList) {
            for (int i = 0; i < cellGroup.Value.Count; i++) {
                Vector2Int newPos = carCmp.CarPartContainerGroupsList[cellGroup.Key][i];
                if (xOffsetDown != 0)
                    newPos.x -= xOffsetDown;
                if (yOffsetDown != 0)
                    newPos.y -= yOffsetDown;
         //       Debug.Log(newPos + " new pos"+ carCmp.CarPartContainerGroupsList[cellGroup.Key][i]);
                carCmp.CarPartContainerGroupsList[cellGroup.Key][i] = newPos;
                carCmp.CarPartContainerGroupsGrid[newPos.x, newPos.y] = cellGroup.Key;
            }

            ref var carPartOldCmp = ref _carPartComponentsPool.Value.Get(cellGroup.Key);
            var oldCarPartConfig = GetCarPartConfig(carPartOldCmp.CarPartId);
            var oldCarPartSize = GridUtils.GetCarPartSize(oldCarPartConfig);
            Vector2 oldFiguresOffset = (oldCarPartSize - Vector2Int.one) * gridCellNewSize;
            if (oldCarPartSize != Vector2Int.one)
            oldFiguresOffset = new Vector2(oldCarPartSize.x - 1, oldCarPartSize.y - 1)/2 * gridCellNewSize * _sceneData.Value.mainCanvas.localScale.x;
            Sequence returnSequence = DOTween.Sequence();
            returnSequence.Append(carPartOldCmp.carPartCellImage.rectTransform.DOSizeDelta(gridCellNewSize * oldCarPartSize, 0.2f)
                .SetEase(Ease.InOutQuad));
            returnSequence.Join(carPartOldCmp.carPartCellImage.transform
                .DOMove(GetWorldPositionFromGrid(GridUtils.GetEdgePointFromCarPart(carCmp.CarPartContainerGroupsList[cellGroup.Key])) + oldFiguresOffset,
                    0.2f).SetEase(Ease.InOutQuad));
        }

        foreach (var carParts in carCmp.CarPartGroupsList) {
            for (int i = 0; i < carParts.Value.Count; i++) {
                Vector2Int newPos = carCmp.CarPartGroupsList[carParts.Key][i];
                if (xOffsetDown != 0)
                    newPos.x -= xOffsetDown;
                if (yOffsetDown != 0)
                    newPos.y -= yOffsetDown;
                carCmp.CarPartGroupsList[carParts.Key][i] = newPos;
                carCmp.CarPartGroupsGrid[newPos.x, newPos.y] = carParts.Key;
            }

            ref var carPartOldCmp = ref _carPartComponentsPool.Value.Get(carParts.Key);
            var oldCarPartConfig = GetCarPartConfig(carPartOldCmp.CarPartId);
            var oldCarPartSize = GridUtils.GetCarPartSize(oldCarPartConfig);
            Vector2 oldFiguresOffset = (oldCarPartSize - Vector2Int.one) * gridCellNewSize;
            if (oldCarPartSize != Vector2Int.one)
            oldFiguresOffset = new Vector2(oldCarPartSize.x - 1, oldCarPartSize.y - 1)/2 * gridCellNewSize * _sceneData.Value.mainCanvas.localScale.x;
            Sequence returnSequence = DOTween.Sequence();
            returnSequence.Append(carPartOldCmp.carPartCellImage.rectTransform.DOSizeDelta(gridCellNewSize * oldCarPartSize, 0.2f)
                .SetEase(Ease.InOutQuad));
            returnSequence.Join(carPartOldCmp.carPartCellImage.transform
                .DOMove(GetWorldPositionFromGrid(GridUtils.GetEdgePointFromCarPart(carCmp.CarPartGroupsList[carParts.Key])) + oldFiguresOffset, 0.2f)
                .SetEase(Ease.InOutQuad));
        }
    }

    private void CreateCarPartContainerOnComplete(int carPartEntity, Vector3 finalPosition, CarPartConfig carPartConfig,
        Vector2 gridCellNewSize, Vector2Int carPartSize) {
        _sceneData.Value._inventoryUIView._draggedInventoryCell.gameObject.SetActive(false);
        _sceneData.Value._gridMaskRect.gameObject.SetActive(false);
        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartEntity);
        carPartCmp.carPartCellImage = _sceneData.Value.GetCarPartUISprite(_sceneData.Value._carPartsContainersContainer,
            _sceneData.Value._carPartsContainersContainer.position);
        carPartCmp.carPartCellImage.transform.localPosition = new Vector2(_sceneData.Value._carPartsContainersContainer.sizeDelta.x / 2,
            _sceneData.Value._carPartsContainersContainer.sizeDelta.y / 2);
        carPartCmp.carPartCellImage.transform.position = finalPosition;
        carPartCmp.CarPartId = carPartConfig.Id;
        carPartCmp.carPartCellImage.rectTransform.sizeDelta = gridCellNewSize * carPartSize;
        carPartCmp.carPartCellImage.sprite = carPartConfig.PartSprite;
    }

    
    private void SetMoneyText() {
        _sceneData.Value._shopUIView._moneyCountText.text =
            _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).moneyCount + "$";
    }

    private CarPartType[] _firstCarPartTypesForShop = new[] {
        CarPartType.Armor,
        CarPartType.Gun,
        CarPartType.MeleeWeapon
    };

    private void SetShopCells(bool isShopSaleRound) {
        var shopCmp = _shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        Dictionary<CarPartType, List<ShopItem>> carPartsForTypes = new Dictionary<CarPartType, List<ShopItem>>();
        foreach (var shopItem in shopCmp.CurrentShopItems) {
            if (!carPartsForTypes.TryAdd(_sceneData.Value.GetCarPartConfig(shopItem.PartId).PartType, new List<ShopItem>() { shopItem }))
                carPartsForTypes[_sceneData.Value.GetCarPartConfig(shopItem.PartId).PartType].Add(shopItem);
        }

        int saleIndex = Random.Range(0, _sceneData.Value._shopUIView._buttonsShop.Length);
        for (int i = 0; i < _sceneData.Value._shopUIView._buttonsShop.Length; i++) {
            var shopButton = _sceneData.Value._shopUIView._buttonsShop[i];
            var randomItem = i >= _firstCarPartTypesForShop.Length
                ? shopCmp.CurrentShopItems[Random.Range(0, shopCmp.CurrentShopItems.Count)]
                : carPartsForTypes[_firstCarPartTypesForShop[i]][Random.Range(0, carPartsForTypes[_firstCarPartTypesForShop[i]].Count)];
            ref var shopCellCmp = ref _shopCellComponentsPool.Value.Get(shopButton._entity);
            int price = randomItem.Price;
            if (i == saleIndex || isShopSaleRound) {
                float saleSize = isShopSaleRound ? Random.Range(2, 15) * 5 : Random.Range(1, 7) * 5;
                price = Mathf.Max(1, Mathf.FloorToInt(price * (1f - (saleSize/ 100f))));
                shopButton.SetShopSaleInfo((int)saleSize);
            } else {
                shopButton.SetShopSaleInfo(0);
            }
 
            shopCellCmp.CurrentBuyItem = randomItem;
            shopButton.SetShopCellInfo(price, GetCarPartConfig(randomItem.PartId).PartSprite);
            shopCellCmp.Price = price;
        }
    }

    private void BuyShopItem(int entity) {
        var shopCellCmp = _shopCellComponentsPool.Value.Get(entity);
        if (RemoveMoneyForBuy(shopCellCmp)) 
            return;

        AddInventoryItem(shopCellCmp.CurrentBuyItem.PartId);
    }

    private bool RemoveMoneyForBuy(ShopCellComponent shopCellCmp) {
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (shopCellCmp.Price > playerCurrencyCmp.moneyCount) return true;
        playerCurrencyCmp.moneyCount -= shopCellCmp.Price;
        _sceneData.Value._shopUIView._buttonsShop[shopCellCmp.ShopButtonIndex].SoldItemChangeButton();
        SetMoneyText();
        return false;
    }

    private int GetCarPartEntityFromField() {
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var gridPosition = GetCurrentDragCarPartContainerGridPosition(Input.mousePosition);
        if ((gridPosition.y < 0 && gridPosition.y >= carCmp.CarPartContainerGroupsGrid.GetLength(1) && gridPosition.x < 0 &&
             gridPosition.x >= carCmp.CarPartContainerGroupsGrid.GetLength(0))) return 0;
        int carPartEntity = 0;
        if (carCmp.CarPartGroupsGrid != null && carCmp.CarPartGroupsGrid[gridPosition.x, gridPosition.y] != 0)
            carPartEntity = carCmp.CarPartGroupsGrid[gridPosition.x, gridPosition.y];
        else if (carCmp.CarPartContainerGroupsGrid[gridPosition.x, gridPosition.y] != 0)
            carPartEntity = carCmp.CarPartContainerGroupsGrid[gridPosition.x, gridPosition.y];
        return carPartEntity;
    }

    private void UpgradeMarkedCarPart() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        // made -999 entity when tap on field grid
        Debug.Log("Upgrade"+inventoryCmp.CurrentMarkedCellForPanelInfo);
        if ( _carPartComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
            var carPartConfig = _sceneData.Value.GetCarPartConfig(_sceneData.Value._carPartInfoUIView._currentPartInfoId);
            var upgradedPartConfig = _sceneData.Value.GetCarPartConfig(carPartConfig.UpgradedPartId);

            int carPartEntity = inventoryCmp.CurrentMarkedCellForPanelInfo;
            if (carPartEntity == 0) return;

            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartEntity);
            carPartCmp.CarPartId = upgradedPartConfig.Id;
            carPartCmp.carPartCellImage.sprite = upgradedPartConfig.PartSprite;

            _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(GetCarPartConfig(upgradedPartConfig.Id).UpgradedPartId !=
                                                                                    0);
            _sceneData.Value._carPartInfoUIView._upgradeButton.enabled =
                GetCarPartConfig(upgradedPartConfig.Id).UpgradedPartCost <= playerCurrencyCmp.moneyCount;
            _sceneData.Value._carPartInfoUIView.SetPanelForNeededPart(upgradedPartConfig, true, true);
            playerCurrencyCmp.moneyCount -= carPartConfig.UpgradedPartCost;
            SetMoneyText();
            UpdateCarDescription();
        } else if (_inventoryCellComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
            var invCellCmp = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo);

            int needCarPartId =
                GetSortedCarPartList()[
                    inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length + invCellCmp.CellId];
            var carPartConfig = _sceneData.Value.GetCarPartConfig(needCarPartId);
            playerCurrencyCmp.moneyCount -= carPartConfig.UpgradedPartCost;
            SetMoneyText();
            var upgradedCarPartConfig = _sceneData.Value.GetCarPartConfig(carPartConfig.UpgradedPartId);

            inventoryCmp.PartsInInventory[inventoryCmp.PartsInInventory.IndexOf(needCarPartId)] = upgradedCarPartConfig.Id;
            SetInventoryCells();
            CloseShowInfoPanel();
        }  
    }

    private void DestroyMarkedCarPart() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        DestroyNeededCarPart(inventoryCmp.CurrentMarkedCellForPanelInfo);
    }

    private void DestroyNeededCarPart(int carPartEntity) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        if (_inventoryCellComponentsPool.Value.Has(carPartEntity)) {
            _sceneData.Value._carPartInfoUIView._switchButtonsContainer.gameObject.SetActive(false);
            inventoryCmp.PartsInInventory.Remove(_sceneData.Value._carPartInfoUIView._currentPartInfoId);
            if (inventoryCmp.CurrentInventoryPage != 0 &&
                !_sceneData.Value._inventoryUIView._inventoryCellViews[1].gameObject.activeInHierarchy)
                inventoryCmp.CurrentInventoryPage--;
            SetInventoryCells();
        } else if (_carPartComponentsPool.Value.Has(carPartEntity)) {
            ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            if (carPartEntity == 0) return;

            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartEntity);
            var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartCmp.CarPartId);
            if (carPartConfig.PartType == CarPartType.WheelsContainer || carPartConfig.PartType == CarPartType.CarStuffContainer) {
                foreach (var carPartCellPosition in carCmp.CarPartContainerGroupsList[carPartEntity]) {
                    carCmp.CarPartContainerGroupsGrid[carPartCellPosition.x, carPartCellPosition.y] = 0;
                }
                carCmp.CarPartContainerGroupsList.Remove(carPartEntity);
            } else {
                foreach (var carPartCellPosition in carCmp.CarPartGroupsList[carPartEntity]) {
                    carCmp.CarPartGroupsGrid[carPartCellPosition.x, carPartCellPosition.y] = 0;
                } 
                carCmp.CarPartGroupsList.Remove(carPartEntity);
            }

//destroyMarkedCarPartContainer
            _sceneData.Value.DestroyGameobject(carPartCmp.carPartCellImage.transform);
       
            _carPartComponentsPool.Value.Del(carPartEntity);
        }

        CloseShowInfoPanel();
    }

    private void MoveCarPartContainerToInventory(int entity) {
        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var carPartCellPosition in carCmp.CarPartContainerGroupsList[entity]) {
            carCmp.CarPartContainerGroupsGrid[carPartCellPosition.x, carPartCellPosition.y] = 0;
            if (carCmp.CarPartGroupsGrid[carPartCellPosition.x, carPartCellPosition.y] != 0)
                MoveToInventoryCellNeededCarPart(carCmp.CarPartGroupsGrid[carPartCellPosition.x, carPartCellPosition.y]);
        }

        MoveToInventoryCellNeededCarPart(entity);
        //
        //move all grid to new positions if need
Debug.Log(carCmp.CarPartContainerGroupsList.Count + "carCmp.CarPartContainerGroupsList.Count");
        if (carCmp.CarPartContainerGroupsList.Count == 0) {
            carCmp.CarPartContainerGroupsGrid = new int[3, 3];
            _sceneData.Value._carPartsContainersContainer.gameObject.GetComponent<CellView>()._gridPosition = -Vector2Int.one * 999;
            SetCarContainersContainerSize();
            return;
        }

        (int xOffsetUp, int yOffsetUp, int xOffsetDown, int yOffsetDown) = GetNonZeroOffsets(carCmp.CarPartContainerGroupsGrid);
Debug.Log(xOffsetUp + " xOffsetUp" + yOffsetUp + " yOffsetUp"+ xOffsetDown + "xOffsetDown"+yOffsetDown + "yOffsetDown" );
        if (xOffsetUp != 0 || yOffsetUp != 0 || xOffsetDown != 0 || yOffsetDown != 0)
            carCmp.CarPartContainerGroupsGrid =
                new int[carCmp.CarPartContainerGroupsGrid.GetLength(0) - Mathf.Abs(yOffsetUp) - Mathf.Abs(yOffsetDown),
                    carCmp.CarPartContainerGroupsGrid.GetLength(1) - Mathf.Abs(xOffsetUp) - Mathf.Abs(xOffsetDown)];

        //  carPartContainerGridSize = new(carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1));
        carCmp.CarPartGroupsGrid = new int[carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1)];
        SetCarContainersContainerSize();
        Vector2 gridCellNewSize = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).onCarCellSize;
        SetNewGridsFromLists(yOffsetUp, xOffsetUp, gridCellNewSize);
        CloseShowInfoPanel();
    }

    public (int upOffsetX, int upOffsetY, int downOffsetX, int downOffsetY) GetNonZeroOffsets(int[,] grid)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        for (int i = 0; i < grid.GetLength(0); i++) {
            string debug = "";
            for (int j = 0; j < grid.GetLength(1); j++) {
                debug += grid[i, j] + " ";
            }
            Debug.Log(debug);
        }
        
        
        return (
            CountZeroColumnsFromLeft(grid, rows, cols),
            CountZeroRowsFromTop(grid, rows, cols),
            CountZeroColumnsFromRight(grid, rows, cols),
            CountZeroRowsFromBottom(grid, rows, cols)
        );
    }

    private int CountZeroRowsFromTop(int[,] grid, int rows, int cols)
    {
        for (int i = 0; i < rows; i++)
        {
            if (!IsRowAllZero(grid, i, cols))
                return i;
        }
        return rows;
    }

    private int CountZeroRowsFromBottom(int[,] grid, int rows, int cols)
    {
        for (int i = rows - 1; i >= 0; i--)
        {
            if (!IsRowAllZero(grid, i, cols))
                return rows - 1 - i;
        }
        return rows;
    }

    private int CountZeroColumnsFromLeft(int[,] grid, int rows, int cols)
    {
        for (int j = 0; j < cols; j++)
        {
            if (!IsColumnAllZero(grid, j, rows))
                return j;
        }
        return cols;
    }

    private int CountZeroColumnsFromRight(int[,] grid, int rows, int cols)
    {
        for (int j = cols - 1; j >= 0; j--)
        {
            if (!IsColumnAllZero(grid, j, rows))
                return cols - 1 - j;
        }
        return cols;
    }

    private bool IsRowAllZero(int[,] grid, int rowIndex, int cols)
    {
        for (int j = 0; j < cols; j++)
        {
            if (grid[rowIndex, j] != 0)
                return false;
        }
        return true;
    }

    private bool IsColumnAllZero(int[,] grid, int colIndex, int rows)
    {
        for (int i = 0; i < rows; i++)
        {
            if (grid[i, colIndex] != 0)
                return false;
        }
        return true;
    }
    private void BackCarPartInPanelInfo() {
       // NextCarPartInPanelInfo();
    }

    private void UpdateCarDescription() {
        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);
        float defaultMaxCarSpeed = carConfig.DefaultMaxSpeed;
        float carRotateSpeed = carConfig.RotateSpeed;
        float carWeight = carConfig.CarWeight;
        float maxFuel = carConfig.DefaultMaxFuel;
        float speedBoost = carConfig.BoostSpeedMultiplayer;
        float speedBoostFuelConsumption = carConfig.AccelerationFuelСonsumption;
        float defaultFuelConsumption = carConfig.FuelСonsumption;
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var carPart in _carPartComponentsFilter.Value) {
            var carPartCellCmp = _carPartComponentsPool.Value.Get(carPart);
            int carPartId = carPartCellCmp.CarPartId;
            var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartId);
            carWeight += carPartConfig.Weight;
            if (carPartConfig.PartType == CarPartType.Support) {
                var supportPartConfig = _sceneData.Value.GetSupportModuleFromCarPart(carPartId);
                if (supportPartConfig.ModuleType == SupportModuleType.Fuel)
                    maxFuel += supportPartConfig.FirstCount;
                else if (supportPartConfig.ModuleType == SupportModuleType.Turbine) {
                    defaultMaxCarSpeed += supportPartConfig.SecondCount;
                    defaultFuelConsumption += supportPartConfig.FirstCount;
                    speedBoostFuelConsumption += supportPartConfig.FirstCount;
                }
            }
        }

        carCmp.CurrentWeight = carWeight;
        float weightFactor = carWeight / carConfig.MaxWeight;
        defaultMaxCarSpeed *= 1-weightFactor;
        defaultMaxCarSpeed = Mathf.Max(defaultMaxCarSpeed, 0);
        speedBoost += defaultMaxCarSpeed;

        _sceneData.Value._carPartInfoUIView.CarDescriptionText.text =
            $"{carConfig.CarName}\n Max move speed: {defaultMaxCarSpeed:F1} m/s \n Boosted speed: {speedBoost:F1} m/s ({speedBoostFuelConsumption} fuel consumption)" +
            $" \n Default fuel consumption {defaultFuelConsumption} \n Rotate speed: {carRotateSpeed} m/s " +
            $"\n Weight: {carWeight:F1}/{carConfig.MaxWeight} kg \n Fuel: {maxFuel}";
        _movementComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).MaxMoveSpeed = defaultMaxCarSpeed;
    }

    private void SetCarPartInfoPanel(int entity) {
        Debug.Log("SetCarPartInfoPanel" + entity);
        int needCarPartId = 0;
        bool showDestroyButton = false;
        bool showMoveButton = false;
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (entity == -999 || _carPartComponentsPool.Value.Has(entity)) {
            
            int carPartEntity =entity == -999 ? GetCarPartEntityFromField(): inventoryCmp.CurrentMarkedCellForPanelInfo;
            if (carPartEntity == inventoryCmp.CurrentMarkedCellForPanelInfo ) {
                CloseShowInfoPanel();
                return;
            }

            _sceneData.Value._carPartInfoUIView._switchButtonsContainer.gameObject.SetActive(false);
            var cellCmp = _carPartComponentsPool.Value.Get(carPartEntity);
            if (inventoryCmp.CurrentMarkedCellForPanelInfo != entity) {
                needCarPartId = cellCmp.CarPartId;
            } else {
                CloseShowInfoPanel();
                return;
            }

            var currentCarPartConfig = GetCarPartConfig(needCarPartId);
            bool isContainer = currentCarPartConfig.PartType == CarPartType.WheelsContainer ||
                               currentCarPartConfig.PartType == CarPartType.CarStuffContainer;
         if (currentCarPartConfig.PartType != CarPartType.Main && !isContainer)
             showDestroyButton = true;
         showMoveButton = !isContainer || (isContainer &&
                                           AreNonZeroAndNonDeletedConnected(
                                               _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CarPartContainerGroupsGrid,
                                               carPartEntity));
            _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(GetCarPartConfig(needCarPartId).UpgradedPartId != 0);
            _sceneData.Value._carPartInfoUIView._upgradeButton.enabled =
                GetCarPartConfig(needCarPartId).UpgradedPartCost <= playerCurrencyCmp.moneyCount;
            inventoryCmp.CurrentMarkedCellForPanelInfo = carPartEntity;
        } else if (_inventoryCellComponentsPool.Value.Has(entity)) {
            if (inventoryCmp.CurrentMarkedCellForPanelInfo == entity) {
                CloseShowInfoPanel();
                return;
            }

            var invCellCmp = _inventoryCellComponentsPool.Value.Get(entity);

            needCarPartId =
                GetSortedCarPartList()[
                    inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length + invCellCmp.CellId];
            _sceneData.Value._carPartInfoUIView._switchButtonsContainer.gameObject.SetActive(false);
            _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(GetCarPartConfig(needCarPartId).UpgradedPartId != 0);
            _sceneData.Value._carPartInfoUIView._upgradeButton.enabled =
                GetCarPartConfig(needCarPartId).UpgradedPartCost <= playerCurrencyCmp.moneyCount;
            inventoryCmp.CurrentMarkedCellForPanelInfo = entity;
        } else if (_shopCellComponentsPool.Value.Has(entity)) {
            if (inventoryCmp.CurrentMarkedCellForPanelInfo == entity) {
                CloseShowInfoPanel();
                return;
            }

            var shopCellCmp = _shopCellComponentsPool.Value.Get(entity);

            needCarPartId = shopCellCmp.CurrentBuyItem.PartId;
            _sceneData.Value._carPartInfoUIView._switchButtonsContainer.gameObject.SetActive(false);
            _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(false);
            inventoryCmp.CurrentMarkedCellForPanelInfo = entity;
        }

        var carPartConfig = GetCarPartConfig(needCarPartId);
        if (carPartConfig.PartType == CarPartType.Main || carPartConfig.PartType == CarPartType.WheelsContainer ||
            carPartConfig.PartType == CarPartType.CarStuffContainer)
        _sceneData.Value._carPartInfoUIView.gameObject.SetActive(true);
        _sceneData.Value._carPartInfoUIView.SetPanelForNeededPart(GetCarPartConfig(needCarPartId), showDestroyButton, showMoveButton);
    }

    private void CloseShowInfoPanel() {
        Debug.Log("close info panel");
        _sceneData.Value._carPartInfoUIView.gameObject.SetActive(false);
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.CurrentMarkedCellForPanelInfo = 0;
    }

    private void CancelCarPartPlace(ref InventoryComponent inventoryCmp) {
        bool isInventoryCell = _inventoryCellComponentsPool.Value.Has(inventoryCmp.CurrentDraggedCellEntity);
        if (isInventoryCell) {
            int curDraggedInventoryCell = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CellId;
            Sequence returnSequence = DOTween.Sequence();
            returnSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform.DOSizeDelta(new Vector2(80, 80), 0.3f)
                .SetEase(Ease.InBack));
            returnSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.transform
                .DOMove(_sceneData.Value._inventoryUIView._inventoryCellViews[curDraggedInventoryCell].gameObject.transform.position, 0.3f)
                .SetEase(Ease.InOutQuad));
            returnSequence.OnComplete(() => {
                _sceneData.Value._inventoryUIView._draggedInventoryCell.gameObject.SetActive(false);
                _sceneData.Value._gridMaskRect.gameObject.SetActive(false);
                _sceneData.Value._inventoryUIView._inventoryCellViews[curDraggedInventoryCell].gameObject.SetActive(true);
            });
        } else {
            int curDraggedShopCell = _shopCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).ShopButtonIndex;
            Sequence returnSequence = DOTween.Sequence();
            returnSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform.DOSizeDelta(new Vector2(80, 80), 0.3f)
                .SetEase(Ease.InBack));
            returnSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.transform
                .DOMove(_sceneData.Value._shopUIView._buttonsShop[curDraggedShopCell].gameObject.transform.position, 0.3f)
                .SetEase(Ease.InOutQuad));
            returnSequence.OnComplete(() => {
                _sceneData.Value._inventoryUIView._draggedInventoryCell.gameObject.SetActive(false);
                _sceneData.Value._gridMaskRect.gameObject.SetActive(false);
            });
        }
        _sceneData.Value._subGridTransform.gameObject.SetActive(false);
        inventoryCmp.CurrentDraggedCellEntity = -1;
        inventoryCmp.CurrentCellCarPart = null;
    }

    private CarPartConfig GetCarPartConfig(int id) {
        return _sceneData.Value._carStuffListsConfig.CarPartConfigs[id];
    }

    private bool CanPlacedOnCarCell() {
        var carDraggedCell = GetCurrentDraggedCellCarPart()/*.PartType*/;
        if (carDraggedCell.PartType == CarPartType.CarStuffContainer || carDraggedCell.PartType == CarPartType.WheelsContainer) return false;
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        return CanPlaceCarPartOnField(carDraggedCell, /*inventoryCmp.CurrentCellCarPart._gridPosition*/
            inventoryCmp.LastCheckedGridCellPosition, carCmp.CarPartGroupsGrid, carCmp.CarPartContainerGroupsGrid);
    }

    private bool CanPlaceCarPartOnField(CarPartConfig form, Vector2Int position, int[,] grid,int[,] gridContainers) /*need caar part containers grid*/ {
       var formCells = GetCellsFromField(position, form,grid);
        if(formCells.Count != form.PartForm.Length) return false;
        foreach (var cell in formCells) {
            
                if (grid[cell.x, cell.y] != 0 || gridContainers[cell.x, cell.y] == 0) return false;
        }

        return true;
    }

    private CarPartConfig GetCurrentDraggedCellCarPart() {
        int playerEntity = _sceneData.Value.PlayerEntity;

        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(playerEntity);
        if (_inventoryCellComponentsPool.Value.Has(inventoryCmp.CurrentDraggedCellEntity)) {
            int cellId = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CellId;
            int partsInInventoryIndex = inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length +
                                        cellId;
            int inventoryPartIndex = GetSortedCarPartList()[partsInInventoryIndex];
            return GetCarPartConfig(inventoryPartIndex);
        } else {
            int cellId = _shopCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CurrentBuyItem.PartId;
            return GetCarPartConfig(cellId);
        }
        
    }
    public Vector2Int GetGridPosition(Vector2 localPosition)
    {
        Vector2 adjustedPosition = localPosition;
        var carCmp  =_carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
     
        Vector2 effectiveCellSize = new Vector2(200,200) / Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0),
            carCmp.CarPartContainerGroupsGrid.GetLength(1)) ;
        
        int gridX = Mathf.FloorToInt((adjustedPosition.x) / effectiveCellSize.x  );
        int gridY = Mathf.FloorToInt((adjustedPosition.y) / effectiveCellSize.y);
        
        return new Vector2Int(gridX, gridY);
    }
    
    public Vector2 GetWorldPositionFromGrid(Vector2Int gridPosition)
    {
        var carCmp  =_carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        Vector2 effectiveCellSize = new Vector2(200,200) / Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0),
            carCmp.CarPartContainerGroupsGrid.GetLength(1)) ;
       
        float posX = (gridPosition.x * effectiveCellSize.x) + (effectiveCellSize.x / 2);
        float posY = (gridPosition.y * effectiveCellSize.y) + (effectiveCellSize.y / 2);
        Vector2 localPosition = new (posX, posY);
        Vector3 worldPosition =  _sceneData.Value._carPartsContainersContainer.TransformPoint(localPosition);
        
        return worldPosition;
    }

    private Vector2Int GetCurrentDragCarPartContainerGridPosition(Vector2 needPos) {
        Vector2 localPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_sceneData.Value._carPartsContainersContainer,needPos, null,
            out localPosition);

        Vector2Int gridPos = GetGridPosition(localPosition);

        return gridPos;
    }

    private void PlaceCarPartInCell(ref InventoryComponent inventoryCmp) {
        bool isBuyCell = _shopCellComponentsPool.Value.Has(inventoryCmp.CurrentDraggedCellEntity);
        int partIndex;
        if (!isBuyCell) {
            int cellId = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CellId;

            if (inventoryCmp.CurrentMarkedCellForPanelInfo == _sceneData.Value._inventoryUIView._inventoryCellViews[cellId]._entity)
                CloseShowInfoPanel();

            int partsInInventoryIndex = inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length +
                                        cellId;
            partIndex = GetSortedCarPartList()[partsInInventoryIndex];
            inventoryCmp.PartsInInventory.Remove(partIndex);

            if (inventoryCmp.CurrentInventoryPage != 0 &&
                !_sceneData.Value._inventoryUIView._inventoryCellViews[1].gameObject.activeInHierarchy)
                inventoryCmp.CurrentInventoryPage--;

            SetInventoryCells();
        } else {
            var shopCellCmp = _shopCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity);
            if (RemoveMoneyForBuy(shopCellCmp)) {
                CancelCarPartPlace(ref inventoryCmp);
                return;
            }

            partIndex = shopCellCmp.CurrentBuyItem.PartId;
        }

        int carPartEntity = _world.Value.NewEntity();
        ref var carPartCarPartCmp = ref _carPartComponentsPool.Value.Add(carPartEntity);
        carPartCarPartCmp.CarPartId = partIndex;
        var partInfo = GetCarPartConfig(partIndex);
        //var partInfo = GetCarPartConfig(partIndex);
        UpdateCarDescription();
        Vector2Int carPartSize = GridUtils.GetCarPartSize(partInfo);
        Sequence returnSequence = DOTween.Sequence();
        
        if (inventoryCmp.CurrentRotateDegrees == 90 || inventoryCmp.CurrentRotateDegrees == 270) 
            carPartSize = new Vector2Int(carPartSize.y, carPartSize.x);
        
        Vector2 offset = carPartSize * inventoryCmp.onCarCellSize - inventoryCmp.onCarCellSize;
        if (carPartSize != Vector2Int.one) {
            offset = new Vector2(carPartSize.x - 1, carPartSize.y - 1) / 2 * inventoryCmp.onCarCellSize *
                     _sceneData.Value.mainCanvas.localScale.x;
        }

        var gridPos = inventoryCmp.LastCheckedGridCellPosition;
        var worldPos = GetWorldPositionFromGrid(gridPos);
        returnSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.transform.DOMove(worldPos + offset, 0.1f)
            .SetEase(Ease.InOutQuad));

        returnSequence.OnComplete(() => { SetCarPartInfo(worldPos, partInfo, carPartEntity, gridPos, offset); });

        inventoryCmp.CurrentDraggedCellEntity = -1;
        inventoryCmp.CurrentCellCarPart = null;
    }

    private void SetCarPartInfo(Vector2 worldPosition,CarPartConfig partInfo, int carPartCmpEntity, Vector2Int gridPosition, Vector2 imageOffset) {
        Debug.Log("CellIsPlaced2 in that position");
        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartCmpEntity);
        _sceneData.Value._inventoryUIView._draggedInventoryCell.gameObject.SetActive(false);
        _sceneData.Value._gridMaskRect.gameObject.SetActive(false);
        // currentCarPartCell._imageCarPart.sprite = _sceneData.Value.transparentSprite;
        //   currentCarPartCell._secondImageCarPart.sprite = _sceneData.Value.transparentSprite;

        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var needCells = GetCellsFromField(gridPosition, partInfo, carCmp.CarPartGroupsGrid);
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var cell in needCells) {
            Debug.Log("CellIsPlaced:" + cell);
            carCmp.CarPartGroupsGrid[cell.x, cell.y] = carPartCmpEntity;
           // _carPartCellComponentsPool.Value.Get(inventoryCmp.GridCarPartCellsEntities[cell.x, cell.y]).CellView._imageCarPart.sprite =
           //     _sceneData.Value._transparentPixel;
        }

        carCmp.CarPartGroupsList.Add(carPartCmpEntity, needCells);
        Vector2Int carPartSize = GridUtils.GetCarPartSize(partInfo);
        // Vector2 halfCellSize = currentCarPartCell._imageCarPart.rectTransform.sizeDelta * new Vector2Int(carPartSize.x-1, carPartSize.y-1) ;
        Vector2 needPos = worldPosition + imageOffset;
        carPartCmp.carPartCellImage = _sceneData.Value.GetCarPartUISprite(_sceneData.Value._inventoryUIView._carPartsUpLevelContainer.transform, needPos);
        carPartCmp.carPartCellImage.sprite = partInfo.PartSprite;
        carPartCmp.carPartCellImage.transform.eulerAngles = new Vector3(0, 0, inventoryCmp.CurrentRotateDegrees);
        carPartCmp.carPartCellImage.rectTransform.sizeDelta = inventoryCmp.onCarCellSize * carPartSize;
    }
   
    public bool AreNonZeroAndNonDeletedConnected(int[,] grid, int deletedInt)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
    
        // Находим первую ячейку, которая не 0 и не deletedInt
        int startRow = -1, startCol = -1;
        bool foundStart = false;
    
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (grid[i, j] != 0 && grid[i, j] != deletedInt)
                {
                    startRow = i;
                    startCol = j;
                    foundStart = true;
                    break;
                }
            }
            if (foundStart) break;
        }
    
        // Если не нашли ни одной подходящей ячейки, считаем что все "связаны"
        if (!foundStart) return true;
    
        // Обход в ширину (BFS) для поиска всех связанных ячеек
        bool[,] visited = new bool[rows, cols];
        Queue<(int, int)> queue = new Queue<(int, int)>();
    
        queue.Enqueue((startRow, startCol));
        visited[startRow, startCol] = true;
    
        // Направления: вверх, вправо, вниз, влево
        int[] dr = {-1, 0, 1, 0};
        int[] dc = {0, 1, 0, -1};
    
        while (queue.Count > 0)
        {
            var (r, c) = queue.Dequeue();
        
            for (int i = 0; i < 4; i++)
            {
                int nr = r + dr[i];
                int nc = c + dc[i];
            
                if (nr >= 0 && nr < rows && nc >= 0 && nc < cols && 
                    !visited[nr, nc] && 
                    grid[nr, nc] != 0 && grid[nr, nc] != deletedInt)
                {
                    visited[nr, nc] = true;
                    queue.Enqueue((nr, nc));
                }
            }
        }
    
        // Проверяем, все ли подходящие ячейки были посещены
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (grid[i, j] != 0 && grid[i, j] != deletedInt && !visited[i, j])
                {
                    return false; // Нашли непосещенную подходящую ячейку
                }
            }
        }
    
        return true;
    }

    private bool CanUseThisCarPartContainers() {
        int wheelsCount = 0;
        Vector2Int lastWheelMinMaxY = Vector2Int.zero;
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartContainer.Key);
            var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartCmp.CarPartId);
            if (carPartConfig.PartType == CarPartType.WheelsContainer) {
                wheelsCount++;
                Vector2Int edgePoint = GridUtils.GetEdgePointFromCarPart(carPartContainer.Value);
                Vector2Int partSize = GridUtils.GetCarPartSize(carPartConfig);
                if (wheelsCount == 1) {
                    lastWheelMinMaxY = new Vector2Int(edgePoint.y, edgePoint.y + partSize.y - 1);
                } else if( lastWheelMinMaxY.x > (edgePoint.y + partSize.y - 1) || lastWheelMinMaxY.y < edgePoint.y) {
                    return true;
                }
            }
        }

        return false;
    }
    private bool CanPlaceCarPartsContainerOnField(Vector2Int position) {
        var partInfo = GetCurrentDraggedCellCarPart();
        var needCells = GetCellsFromField(position, partInfo);
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var cell in needCells) {
            if (GridUtils.IsPositionInGrid(cell, carCmp.CarPartContainerGroupsGrid) && carCmp.CarPartContainerGroupsGrid[cell.x, cell.y] != 0)
                return false;
        }

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        bool hasCarPartContainersAround = false;
        foreach (var cell in needCells) {
            // if (hasCarPartContainersAround) break;
            foreach (var needDir in directions) {
                //   if (hasCarPartContainersAround) break;
                var checkedCell = cell + needDir;
                if (GridUtils.IsPositionInGrid(checkedCell, carCmp.CarPartContainerGroupsGrid) &&
                    carCmp.CarPartContainerGroupsGrid[checkedCell.x, checkedCell.y] != 0) {
                    hasCarPartContainersAround = true;
                    if (partInfo.PartType == CarPartType.WheelsContainer &&
                        _sceneData.Value.GetWheelContainerFromCarPart(partInfo.Id).WheelsPositions.Length > 1) {
                        if (cell.y == checkedCell.y) return false;
                    } else {
                        var wheelConfig = _sceneData.Value.GetWheelContainerFromCarPart(_carPartComponentsPool.Value
                            .Get(carCmp.CarPartContainerGroupsGrid[checkedCell.x, checkedCell.y]).CarPartId);
                        if (!wheelConfig) continue;
                        if (wheelConfig.WheelsPositions.Length != 1 && cell.y == checkedCell.y) return false;
                    }
                }
            }
        }

        return hasCarPartContainersAround;
    }

    private List<Vector2Int> GetCellsFromField( Vector2Int position, CarPartConfig carPartConfig, int[,] grid = null) {
        List<Vector2Int> returnedCells = new List<Vector2Int>();
        var carPartSize = GridUtils.GetCarPartSize(carPartConfig);
        var form = carPartConfig.PartForm;
        var rotateAngle = grid == null ?0:_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRotateDegrees;
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
    private void AddInventoryItem(int partId) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.PartsInInventory.Add(partId);
        SetInventoryCells();
    }

    private void SetInventoryCell(int partId, int cellNumber) {
        var inventoryCell = _sceneData.Value._inventoryUIView._inventoryCellViews[cellNumber];
        inventoryCell.gameObject.SetActive(true);

        inventoryCell.SetSprite(_sceneData.Value._carStuffListsConfig.CarPartConfigs[partId].PartSprite);
    }

    private void SetupInventoryCells() {
        int i = 0;
        foreach (var inventoryCell in _sceneData.Value._inventoryUIView._inventoryCellViews) {
            int entity = _world.Value.NewEntity();
            _inventoryCellComponentsPool.Value.Add(entity).CellId = i;
            int cellId = i;
            inventoryCell.Construct( /*_world.Value,*/ entity, TakeItemFromInventory, cellId, SetCarPartInfoPanel);
            i++;
        }
    }

    private void TakeItemFromInventory(int cellEntity) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.IsTryDraggedCell = true;
        inventoryCmp.CurrentCellCarPart = null;
        inventoryCmp.CurrentDraggedCellEntity = cellEntity;
    }

    private void SetSortButtons() {
        for (int i = 0; i < _sceneData.Value._inventoryUIView._sortButtons.Length; i++) {
            CarPartType sortType = (CarPartType)i;
            _sceneData.Value._inventoryUIView._sortButtons[i].onClick.AddListener(() => SortCarParts(sortType));
        }

        _sceneData.Value._inventoryUIView._nextInventoryCellsButton.onClick.AddListener(() => NextInventoryPage());
        _sceneData.Value._inventoryUIView._backInventoryCellsButton.onClick.AddListener(() => LastInventoryPage());
    }

    private void SortCarParts(CarPartType carPartType) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.SortedCarPartType = carPartType;
        inventoryCmp.CurrentInventoryPage = 0;

        SetInventoryCells();
    }

    private List<int> GetSortedCarPartList() {
        var SortedPartsInInventory = new List<int>();
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        if (inventoryCmp.SortedCarPartType == CarPartType.None)
            SortedPartsInInventory = inventoryCmp.PartsInInventory;
        else {
            foreach (var partId in inventoryCmp.PartsInInventory) {
                if (GetCarPartConfig(partId).PartType == inventoryCmp.SortedCarPartType)
                    SortedPartsInInventory.Add(partId);
            }
        }

        return SortedPartsInInventory;
    }

    private void NextInventoryPage() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.CurrentInventoryPage++;
        if (inventoryCmp.CurrentInventoryPage > Mathf.FloorToInt((float)GetSortedCarPartList().Count /
                                                                 _sceneData.Value._inventoryUIView._inventoryCellViews.Length))
            inventoryCmp.CurrentInventoryPage = 0;

        SetInventoryCells();
    }

    private void LastInventoryPage() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.CurrentInventoryPage--;
        if (inventoryCmp.CurrentInventoryPage < 0)
            inventoryCmp.CurrentInventoryPage = Mathf.FloorToInt((float)GetSortedCarPartList().Count /
                                                                 _sceneData.Value._inventoryUIView._inventoryCellViews.Length);

        SetInventoryCells();
    }

    private void SetCarGrid() {
        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);
        var needCellPartSize = 400f / Mathf.Max(carConfig.Width, carConfig.Height);
//        _sceneData.Value._inventoryUIView._gridLayoutGroup.GetComponent<Image>().sprite = carConfig.CarBackground;
      //  var gridLayoutGroupRect = _sceneData.Value._inventoryUIView._gridLayoutGroup.GetComponent<RectTransform>();
       // gridLayoutGroupRect.sizeDelta = new Vector2(needCellPartSize * carConfig.Width, needCellPartSize * carConfig.Height);
//        _sceneData.Value._inventoryUIView._gridLayoutGroup.cellSize = new Vector2(needCellPartSize, needCellPartSize);
     /*   inventoryCmp.GridCarPartCellsEntities = new int[carConfig.Width, carConfig.Height];
        for (int i = carConfig.Height-1; i >= 0; i--) {
            for (int j = 0; j < carConfig.Width; j++) {
                var inventoryCell = _sceneData.Value.InstantiateCell(_sceneData.Value._inventoryUIView._cellView,
                    _sceneData.Value._inventoryUIView._gridLayoutGroup.transform);
                int cellEntity = _world.Value.NewEntity();
                inventoryCmp.GridCarPartCellsEntities[j, i] = cellEntity;

                ref var cellCmp = ref _carPartCellComponentsPool.Value.Add(cellEntity);
               // cellCmp.MainEntity = _sceneData.Value.PlayerEntity;
                cellCmp.CellView = inventoryCell;
             //   cellCmp.CarPartId = -1;
                _carPartInGameUIComponentsPool.Value.Add(cellEntity);
                inventoryCell.Construct(new Vector2Int(j, i), SetCurrentCarCell, cellEntity, SetCarPartInfoPanel);
            }
        }*/

        _sceneData.Value._carPartsContainersContainer.GetComponent<CellView>().Construct(-Vector2Int.one*999, SetCurrentCarCell, -999, SetCarPartInfoPanel);
        inventoryCmp.onCarCellSize = new Vector2(needCellPartSize, needCellPartSize);
        carCmp.CarPartGroupsGrid = new int[0, 0];
        carCmp.CarPartGroupsList = new Dictionary<int, List<Vector2Int>>();
        carCmp.CarPartContainerGroupsGrid= new int[3,3];
        carCmp.CarPartContainerGroupsList = new Dictionary<int, List<Vector2Int>>();
        AddMainCarParts(_sceneData.Value.GetCarConfig(_sceneData.Value._defaultCarConfigId)); //for test
    }

    private void AddMainCarParts(CarConfig carConfig) {
        foreach (var carPartId in carConfig.StartCarParts) {
            AddInventoryItem(carPartId);
        }
    }

    private void SetCurrentCarCell(CellView _cellPosition) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.CurrentCellCarPart = _cellPosition;
    }

    private void SetInventoryCells() {
        //check arrow buttons
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (inventoryCmp.SortedCarPartType != CarPartType.None)
            _sceneData.Value._inventoryUIView._pageNumberText.text = inventoryCmp.SortedCarPartType + " ";
        else
            _sceneData.Value._inventoryUIView._pageNumberText.text = "";
        if ((float)GetSortedCarPartList().Count / _sceneData.Value._inventoryUIView._inventoryCellViews.Length > 1.1f) {
            _sceneData.Value._inventoryUIView._inventoryArrowsContainer.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._pageNumberText.text += inventoryCmp.CurrentInventoryPage + "/" +
                                                                      (Mathf.FloorToInt((float)GetSortedCarPartList().Count /
                                                                                        _sceneData.Value._inventoryUIView._inventoryCellViews
                                                                                            .Length));
        } else {
            _sceneData.Value._inventoryUIView._inventoryArrowsContainer.gameObject.SetActive(false);
        }

        for (int i = 0; i < _sceneData.Value._inventoryUIView._inventoryCellViews.Length; i++) {
            int inventoryPartIndex = inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length + i;

            if (inventoryPartIndex < GetSortedCarPartList().Count) {
                SetInventoryCell(GetSortedCarPartList()[inventoryPartIndex], i);
            } else
                _sceneData.Value._inventoryUIView._inventoryCellViews[i].gameObject.SetActive(false);
        }
    }
}
