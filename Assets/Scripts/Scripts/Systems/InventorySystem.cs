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

        _sceneData.Value._carPartInfoUIView.SetupButtonsInfoPanel(BackCarPartInPanelInfo, DestroyMarkedCarPart,
            UpgradeMarkedCarPart);

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
                Vector2 positionOffset = inventoryCmp.IsCarPartsRedactorMode
                    ? _sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform.sizeDelta - inventoryCmp.onCarCellSize
                    : _sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform.sizeDelta - new Vector2(200, 200) /
                    Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1));
                if (positionOffset != Vector2.zero) {
                    var carPartSize = GetCarPartSize(GetCurrentDraggedCellCarPart());
                    positionOffset -= (new Vector2(carPartSize.x - 1, carPartSize.y - 1) * inventoryCmp.onCarCellSize / 9.82f);
                }

                Vector2 targetCellPosition;
                // Debug.Log(inventoryCmp.CurrentCellCarPart._gridPosition);
                if (inventoryCmp.CurrentCellCarPart != null && inventoryCmp.CurrentCellCarPart._gridPosition != -Vector2Int.one &&
                    CanPlacedOnCarCell()) {
                    targetCellPosition = (((Vector2)inventoryCmp.CurrentCellCarPart._imageCarPart.transform.position + positionOffset) * 7 +
                                          (Vector2)Input.mousePosition + positionOffset) / 8;
                } else if (carCmp.CarPartContainerGroupsList.Count != 0 &&
                           CanPlaceCarPatsContainerOnField(GetCurrentDragCarPartContainerGridPosition())) {
                    var worldPosition = GetWorldPositionFromGrid(GetCurrentDragCarPartContainerGridPosition());
                    targetCellPosition = ((worldPosition + positionOffset) * 7 + (Vector2)Input.mousePosition + positionOffset) / 8;
                } else {
                    targetCellPosition = (Vector2)Input.mousePosition + positionOffset;
                }

                _sceneData.Value._inventoryUIView._draggedInventoryCell.transform.position = Vector3.Slerp(
                    _sceneData.Value._inventoryUIView._draggedInventoryCell.transform.position, targetCellPosition, Time.deltaTime * 10f);
                if (Input.GetMouseButtonDown(1) && (GetCurrentDraggedCellCarPart().PartType == CarPartType.Gun ||
                                                    GetCurrentDraggedCellCarPart().PartType == CarPartType.MeleeWeapon))
                    RotateCarPart();
                if (Input.GetMouseButtonUp(0)) {
                    if (inventoryCmp.IsCarPartsRedactorMode) {
                        if (inventoryCmp.CurrentCellCarPart != null && inventoryCmp.CurrentCellCarPart._gridPosition != -Vector2Int.one) {
                            if (_currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRoundType ==
                                RoundType.Transfer) { } else {
                                if (CanPlacedOnCarCell())
                                    PlaceCarPartInCell(ref inventoryCmp /*, ref carPartCellCmp*/);
                                else
                                    CancelCarPartPlace(ref inventoryCmp);
                            }
                        } else
                            CancelCarPartPlace(ref inventoryCmp);
                    } else {
                        if (inventoryCmp.CurrentDraggedCellEntity != -1) {
                            if (inventoryCmp.CurrentCellCarPart != null &&
                                inventoryCmp.CurrentCellCarPart._gridPosition == new Vector2Int(-999, -999)) {
                                SetFirstCarPartContainer();
                                inventoryCmp.CurrentDraggedCellEntity = -1;
                            } else if (CanPlaceCarPatsContainerOnField(GetCurrentDragCarPartContainerGridPosition())) {
                                SetCarPartContainer();
                                inventoryCmp.CurrentDraggedCellEntity = -1;
                            }
                        } else {
                            CancelCarPartPlace(ref inventoryCmp);
                        }
                    }

                    inventoryCmp.CellIsSetup = false;
                }
            }

            inventoryCmp.IsCarPartsRedactorMode = false;
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
    private void StartDragShopCell(ref InventoryComponent inventoryCmp) {
        var inventoryView = _sceneData.Value._inventoryUIView;
        var shopItem = _shopCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CurrentBuyItem;
        int carPartId = shopItem.PartId;
        int inventoryPartIndex =carPartId;
      //  inventoryView._inventoryCellViews[inventoryCmp.CurrentDraggedCellEntity].gameObject.SetActive(false);

        SetDraggedCell(ref inventoryCmp, inventoryPartIndex, inventoryView);
    }
    private void StartDragInventoryCell(ref InventoryComponent inventoryCmp) {
        var inventoryView = _sceneData.Value._inventoryUIView;
        int inventoryCellIndex = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CellId;
        int inventoryPartIndex =
            GetSortedCarPartList()[
                inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length +
                inventoryCellIndex];
        inventoryView._inventoryCellViews[inventoryCellIndex].gameObject.SetActive(false);

        SetDraggedCell(ref inventoryCmp, inventoryPartIndex, inventoryView);
    }

    private void SetDraggedCell(ref InventoryComponent inventoryCmp, int inventoryPartIndex, InventoryUIView inventoryView) {
        var partInfo = _sceneData.Value._carStuffListsConfig.CarPartConfigs[inventoryPartIndex];

        inventoryView._draggedInventoryCell.gameObject.SetActive(true);
        inventoryView._draggedInventoryCell.sprite = partInfo.PartSprite;

        _sceneData.Value._inventoryUIView._draggedInventoryCell.transform.rotation = Quaternion.Euler(0, 0, 0);
        inventoryCmp.CurrentRotateDegrees = 0;
        var carCmp  =_carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        Vector2 carrentCellSize = inventoryCmp.IsCarPartsRedactorMode ? inventoryCmp.onCarCellSize :
            new Vector2(200,200) / Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0),
                carCmp.CarPartContainerGroupsGrid.GetLength(1)) ;
        Vector2 cellSize = GetCarPartSize(partInfo) * carrentCellSize;
        inventoryCmp.CurrentTween = _sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform
            .DOSizeDelta(cellSize, 0.3f).SetEase(Ease.OutBack);
    }

    private Vector2Int GetCarPartSize(CarPartConfig carPartConfig) {
        Vector2Int carPartSize = Vector2Int.one;
        foreach (Vector2Int cell in carPartConfig.PartForm) {
            if (cell.x + 1 > carPartSize.x)
                carPartSize.x = cell.x + 1;
            if (cell.y + 1 > carPartSize.y)
                carPartSize.y = cell.y + 1;
        }
        return carPartSize;
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
    }

    private void SetFirstCarPartContainer() {
        Debug.Log(" SetFirstCarPartContainer");
        var carPartConfig = GetCurrentDraggedCellCarPart();
        if (carPartConfig.PartType == CarPartType.CarStuffContainer || carPartConfig.PartType == CarPartType.WheelsContainer) {
            _sceneData.Value._carPartsContainersContainer.GetComponent<CellView>().enabled = false;
            ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            var partSize = GetCarPartSize(carPartConfig);
            int carPartEntity = _world.Value.NewEntity();
            ref var carPartCmp  = ref _carPartComponentsPool.Value.Add(carPartEntity);
          
            carPartCmp.carPartCellImage = _sceneData.Value.GetCarPartUISprite(_sceneData.Value._carPartsContainersContainer,
                _sceneData.Value._carPartsContainersContainer.position);
            carPartCmp.carPartCellImage.transform.localPosition = new Vector2(_sceneData.Value._carPartsContainersContainer.sizeDelta.x / 2,
               _sceneData.Value._carPartsContainersContainer.sizeDelta.y / 2);
            carPartCmp.carPartCellImage.rectTransform.sizeDelta = _sceneData.Value._carPartsContainersContainer.sizeDelta;
            carPartCmp.carPartCellImage.sprite = carPartConfig.PartSprite;
            carCmp.CarPartContainerGroupsGrid = new int[partSize.x, partSize.y];
            for (int i = 0; i < carCmp.CarPartContainerGroupsGrid.GetLength(0); i++)
            {
                for (int j = 0; j < carCmp.CarPartContainerGroupsGrid.GetLength(1); j++)
                    carCmp.CarPartContainerGroupsGrid[i, j] = carPartEntity;
            }
            SetCarContainersContainerSize();
            carCmp.CarPartContainerGroupsList.Add(carPartEntity,carPartConfig.PartForm.ToList());
            DestroyMarkedCarPart();
        
        }    
        CancelCarPartPlace(ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity));
    }
    
    private void SetCarPartContainer() {
       
    
        var carPartConfig = GetCurrentDraggedCellCarPart();
        if (carPartConfig.PartType == CarPartType.CarStuffContainer || carPartConfig.PartType == CarPartType.WheelsContainer) {    
            int carPartEntity = _world.Value.NewEntity();
            Vector2Int currentGridPosition = GetCurrentDragCarPartContainerGridPosition();
           // Debug.Log(" SetCarPartContainer"+currentGridPosition);
            var newFormCellPositions = GetCellsFromField(currentGridPosition, carPartConfig.PartForm);
            int xOffset = 0;
            int yOffset = 0;
            Vector2Int carpartSize = GetCarPartSize(carPartConfig);
            ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            
            
            if (currentGridPosition.x < 0)
                xOffset =currentGridPosition.x;
            else if (currentGridPosition.x + (carpartSize.x)> carCmp.CarPartContainerGroupsGrid.GetLength(0) )
                xOffset = Mathf.Abs(currentGridPosition.x  + (carpartSize.x)- carCmp.CarPartContainerGroupsGrid.GetLength(0)) ;

            if (currentGridPosition.y < 0)
                yOffset =currentGridPosition.y;
            else if (currentGridPosition.y + (carpartSize.y)> carCmp.CarPartContainerGroupsGrid.GetLength(1) )
                yOffset =Mathf.Abs( currentGridPosition.y + (carpartSize.y) - carCmp.CarPartContainerGroupsGrid.GetLength(1));
            
            
           // Debug.Log(" old Pos grid size " + carCmp.CarPartContainerGroupsGrid.GetLength(0) + "  " +
           //           carCmp.CarPartContainerGroupsGrid.GetLength(1) + " xoff"+xOffset + " yoff"+yOffset);
            
            
            if (xOffset != 0 || yOffset != 0)
                carCmp.CarPartContainerGroupsGrid = new int[carCmp.CarPartContainerGroupsGrid.GetLength(0) + Mathf.Abs(xOffset),
                    carCmp.CarPartContainerGroupsGrid.GetLength(1) + Mathf.Abs(yOffset)];
           
            foreach (var cellGroup in carCmp.CarPartContainerGroupsList) {
                for (int i = 0; i < cellGroup.Value.Count; i++) {
                    Vector2Int newPos = carCmp.CarPartContainerGroupsList[cellGroup.Key][i];
                    if (xOffset < 0)
                        newPos.x -= xOffset;
                    if (yOffset < 0)
                        newPos.y -= yOffset;
                    carCmp.CarPartContainerGroupsList[cellGroup.Key][i] = newPos;
                    carCmp.CarPartContainerGroupsGrid[newPos.x, newPos.y] = cellGroup.Key;
                 //   Debug.Log(" new Pos " + newPos + " key " + cellGroup.Key);
                }
            }
 //Debug.Log(" new Pos grid size " + carCmp.CarPartContainerGroupsGrid.GetLength(0) + "  " +
        //              carCmp.CarPartContainerGroupsGrid.GetLength(1));
            foreach (var newFormCell in newFormCellPositions) {
                var newCell = newFormCell;
                if (xOffset < 0)
                    newCell.x -= xOffset;
                if (yOffset < 0)
                    newCell.y -= yOffset;

                if(!carCmp.CarPartContainerGroupsList.TryAdd(carPartEntity, new List<Vector2Int>{newCell}))
                    carCmp.CarPartContainerGroupsList[carPartEntity].Add(newCell);
                Debug.Log(" new Pos "+newCell + " key "+carPartEntity);
                carCmp.CarPartContainerGroupsGrid[newCell.x, newCell.y] = carPartEntity;
                
            }

             
            
            Debug.Log(" new Pos setted");
            var partSize = GetCarPartSize(carPartConfig);
        
            ref var carPartCmp  = ref _carPartComponentsPool.Value.Add(carPartEntity);
          
            carPartCmp.carPartCellImage = _sceneData.Value.GetCarPartUISprite(_sceneData.Value._carPartsContainersContainer,
                _sceneData.Value._carPartsContainersContainer.position);
            carPartCmp.carPartCellImage.transform.localPosition = new Vector2(_sceneData.Value._carPartsContainersContainer.sizeDelta.x / 2,
                _sceneData.Value._carPartsContainersContainer.sizeDelta.y / 2);
            //new position
            
            carPartCmp.carPartCellImage.rectTransform.sizeDelta = _sceneData.Value._carPartsContainersContainer.sizeDelta;
            carPartCmp.carPartCellImage.sprite = carPartConfig.PartSprite;
          //  carCmp.CarPartContainerGroupsGrid = new int[partSize.x, partSize.y];
            SetCarContainersContainerSize();
            //carCmp.CarPartContainerGroupsList.Add(carPartEntity,carPartConfig.PartForm.ToList());
            DestroyMarkedCarPart();
        
        }    
        CancelCarPartPlace(ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity));
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

    private void UpgradeMarkedCarPart() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        int markedCell = inventoryCmp.CurrentMarkedCellForPanelInfo;
        if (_inventoryCellComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
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
        } else if (_carPartCellComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
            
            ref var cellCmp = ref _carPartCellComponentsPool.Value.Get(markedCell);
            var carPartConfig = _sceneData.Value.GetCarPartConfig(_sceneData.Value._carPartInfoUIView._currentPartInfoId);
            var upgradedPartConfig = _sceneData.Value.GetCarPartConfig(carPartConfig.UpgradedPartId);
        
                ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
                int carPartEntity = carCmp.CarPartGroupsGrid[cellCmp.CellView._gridPosition.x, cellCmp.CellView._gridPosition.y] ;
                if(carPartEntity == 0)return;

                ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartEntity);
                carPartCmp.CarPartId = upgradedPartConfig.Id;
                carPartCmp.carPartCellImage.sprite = upgradedPartConfig.PartSprite;
             
            _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(GetCarPartConfig(upgradedPartConfig.Id).UpgradedPartId != 0);
            _sceneData.Value._carPartInfoUIView._upgradeButton.enabled =
                GetCarPartConfig(upgradedPartConfig.Id).UpgradedPartCost <= playerCurrencyCmp.moneyCount;
            _sceneData.Value._carPartInfoUIView.SetPanelForNeededPart(upgradedPartConfig, true);
            playerCurrencyCmp.moneyCount -= carPartConfig.UpgradedPartCost;
            SetMoneyText();
            UpdateCarDescription();
        }
    }

    private void DestroyMarkedCarPart() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        if (_inventoryCellComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
            var invCellCmp = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo);
            _sceneData.Value._carPartInfoUIView._switchButtonsContainer.gameObject.SetActive(false);
            inventoryCmp.PartsInInventory.Remove(_sceneData.Value._carPartInfoUIView._currentPartInfoId);
            if (inventoryCmp.CurrentInventoryPage != 0 &&
                !_sceneData.Value._inventoryUIView._inventoryCellViews[1].gameObject.activeInHierarchy)
                inventoryCmp.CurrentInventoryPage--;
            SetInventoryCells();
        } else if (_carPartCellComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
            var cellCmp = _carPartCellComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo); 
            ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            int carPartEntity = carCmp.CarPartGroupsGrid[cellCmp.CellView._gridPosition.x, cellCmp.CellView._gridPosition.y] ;
          if(carPartEntity == 0)return;

          ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartEntity);


          foreach (var carPartCellPosition in carCmp.CarPartGroupsList[carPartEntity]) {
              var cellComponent = _carPartCellComponentsPool.Value.Get(inventoryCmp.GridCarPartCellsEntities[carPartCellPosition.x, carPartCellPosition.y] ); 
              carCmp.CarPartGroupsGrid[carPartCellPosition.x, carPartCellPosition.y] = 0;
              cellComponent.CellView._imageCarPart.sprite = _sceneData.Value._buildCellSprite;
              cellComponent.CellView._imageCarPart.transform.rotation = Quaternion.Euler(0, 0, 0);
          }

          _sceneData.Value.DestroyGameobject(carPartCmp.carPartCellImage.transform);
          carCmp.CarPartGroupsList.Remove(carPartEntity);
          
          _carPartComponentsPool.Value.Del(carPartEntity);
           

          //  cellCmp.CarPartsId.Remove(_sceneData.Value._carPartInfoUIView._currentPartInfoId);
        } //add for shop cell

        CloseShowInfoPanel();
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
        Debug.Log("SetCarPartInfoPanel");
        int needCarPartId = 0;
        bool showDestroyButton = true;
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (_inventoryCellComponentsPool.Value.Has(entity)) {
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
        } else if (_carPartCellComponentsPool.Value.Has(entity)) {
            //   
            var gridPosition = _carPartCellComponentsPool.Value.Get(entity).CellView._gridPosition;
            int carPartEntity = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CarPartGroupsGrid[gridPosition.x, gridPosition.y];
            if (carPartEntity == 0) return;
            _sceneData.Value._carPartInfoUIView._switchButtonsContainer.gameObject.SetActive(false);
            var cellCmp = _carPartComponentsPool.Value.Get(carPartEntity);
            if (inventoryCmp.CurrentMarkedCellForPanelInfo != entity) {
                needCarPartId = cellCmp.CarPartId;
            } else {
                CloseShowInfoPanel();
                return;
            }

            _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(GetCarPartConfig(needCarPartId).UpgradedPartId != 0);
            _sceneData.Value._carPartInfoUIView._upgradeButton.enabled =
                GetCarPartConfig(needCarPartId).UpgradedPartCost <= playerCurrencyCmp.moneyCount;
        } else if (_shopCellComponentsPool.Value.Has(entity)) {
            showDestroyButton = false;
            if (inventoryCmp.CurrentMarkedCellForPanelInfo == entity) {
                CloseShowInfoPanel();
                return;
            }

            var shopCellCmp = _shopCellComponentsPool.Value.Get(entity);

            needCarPartId = shopCellCmp.CurrentBuyItem.PartId;
            _sceneData.Value._carPartInfoUIView._switchButtonsContainer.gameObject.SetActive(false);
            _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(false);
        }

        inventoryCmp.CurrentMarkedCellForPanelInfo = entity;

        if (GetCarPartConfig(needCarPartId).PartType == CarPartType.Main)
            showDestroyButton = false;
        _sceneData.Value._carPartInfoUIView.gameObject.SetActive(true);
        _sceneData.Value._carPartInfoUIView.SetPanelForNeededPart(GetCarPartConfig(needCarPartId), showDestroyButton);
    }

    private void CloseShowInfoPanel() {
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
            });
        }

        inventoryCmp.CurrentDraggedCellEntity = -1;
        inventoryCmp.CurrentCellCarPart = null;
    }

    private CarPartConfig GetCarPartConfig(int id) {
        return _sceneData.Value._carStuffListsConfig.CarPartConfigs[id];
    }

    private bool CanPlacedOnCarCell() {
        var carDraggedCell = GetCurrentDraggedCellCarPart()/*.PartType*/;
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
  
    var inventoryCmp  =_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        return CanPlaceCarPartOnField(carDraggedCell.PartForm, inventoryCmp.CurrentCellCarPart._gridPosition, carCmp.CarPartGroupsGrid);
    }

    private bool CanPlaceCarPartOnField(Vector2Int[] form, Vector2Int position, int[,] grid) /*need caar part containers grid*/ {
       var formCells = GetCellsFromField(position, form,grid);
        if(formCells.Count != form.Length) return false;
        foreach (var cell in formCells) {
            
                if (grid[cell.x, cell.y] != 0) return false;
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
            Debug.Log(GetCarPartConfig(cellId).PartName);
            return GetCarPartConfig(cellId);
        }
        
    }
    public Vector2Int GetGridPosition(Vector2 localPosition)
    {
        // Вычитаем смещение сетки
        Vector2 adjustedPosition = localPosition;
        var carCmp  =_carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        // Учитываем расстояние между клетками
        Vector2 effectiveCellSize = new Vector2(200,200) / Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0),
            carCmp.CarPartContainerGroupsGrid.GetLength(1)) ;
        
        // Вычисляем координаты в сетке
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
    private Vector2Int GetCurrentDragCarPartContainerGridPosition() {
        Vector2 localPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _sceneData.Value._carPartsContainersContainer, 
            Input.mousePosition, 
            null, 
            out localPosition
        );
            
        // Определяем позицию в сетке
        Vector2Int gridPos = GetGridPosition(localPosition);
            
        // Проверяем, что позиция внутри сетки
       
          Debug.Log($"Клетка выбрана: {gridPos}");
           return     gridPos;
            // Получаем позицию для размещения объекта
            Vector2 worldPos = GetWorldPositionFromGrid(gridPos);
            // Используем worldPos для позиционирования объекта
        
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
        Vector2Int carPartSize = GetCarPartSize(partInfo);
        CellView currentCarPartCell = inventoryCmp.CurrentCellCarPart;
        float currentDegrees = inventoryCmp.CurrentRotateDegrees;
        Sequence returnSequence = DOTween.Sequence();
       // returnSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform
    //        .DOSizeDelta(inventoryCmp.CurrentCellCarPart._imageCarPart.rectTransform.sizeDelta, 0.2f).SetEase(Ease.InBack));
    Vector2 offset = carPartSize* inventoryCmp.onCarCellSize - inventoryCmp.onCarCellSize;
    if (carPartSize != Vector2Int.one) {
        offset -= (new Vector2(carPartSize.x-1, carPartSize.y-1)*inventoryCmp.onCarCellSize/9.82f);
    }
                     
        returnSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.transform
            .DOMove((Vector2)currentCarPartCell._imageCarPart.gameObject.transform.position+offset, 0.1f).SetEase(Ease.InOutQuad));
        
        bool isWeapon = partInfo.PartType == CarPartType.Gun || partInfo.PartType == CarPartType.MeleeWeapon;
     /*   if (isWeapon) {
            returnSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.transform
                .DORotate(new Vector3(0, 0, inventoryCmp.CurrentRotateDegrees), 0.2f, RotateMode.Fast)
                .SetEase(Ease.OutBack));
        }*/

        Vector2Int curCellPosToPlace = currentCarPartCell._gridPosition;
        returnSequence.OnComplete(() => { SetCarPartInfo(currentCarPartCell, partInfo, carPartEntity, curCellPosToPlace, offset); });

        inventoryCmp.CurrentDraggedCellEntity = -1;
        inventoryCmp.CurrentCellCarPart = null;
    }

    private void SetCarPartInfo(CellView currentCarPartCell, CarPartConfig partInfo, int carPartCmpEntity, Vector2Int gridPosition, Vector2 imageOffset) {
        
        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartCmpEntity);
        _sceneData.Value._inventoryUIView._draggedInventoryCell.gameObject.SetActive(false);
       // currentCarPartCell._imageCarPart.sprite = _sceneData.Value.transparentSprite;
     //   currentCarPartCell._secondImageCarPart.sprite = _sceneData.Value.transparentSprite;
       
        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var needCells = GetCellsFromField(gridPosition, partInfo.PartForm,carCmp.CarPartGroupsGrid);
        var inventoryCmp  =_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var cell in needCells) {
            carCmp.CarPartGroupsGrid[cell.x, cell.y] = carPartCmpEntity;
           _carPartCellComponentsPool.Value.Get(inventoryCmp.GridCarPartCellsEntities[cell.x, cell.y]).CellView._imageCarPart.sprite = _sceneData.Value._transparentPixel; 
        }
        carCmp.CarPartGroupsList.Add(carPartCmpEntity, needCells);
        Vector2Int carPartSize = GetCarPartSize(partInfo);
       // Vector2 halfCellSize = currentCarPartCell._imageCarPart.rectTransform.sizeDelta * new Vector2Int(carPartSize.x-1, carPartSize.y-1) ;
        Vector2 needPos = (Vector2)currentCarPartCell._imageCarPart.transform.position + imageOffset;
        carPartCmp.carPartCellImage = _sceneData.Value.GetCarPartUISprite(_sceneData.Value._inventoryUIView._carStuffContainer, needPos);
        carPartCmp.carPartCellImage.sprite = partInfo.PartSprite;
        carPartCmp.carPartCellImage.transform.eulerAngles = new Vector3(0, 0, inventoryCmp.CurrentRotateDegrees);
        carPartCmp.carPartCellImage.rectTransform.sizeDelta = currentCarPartCell._imageCarPart.rectTransform.sizeDelta *carPartSize ;
    }
    private void IncreaseCarSize() { }
    private bool IsPositionInGrid(Vector2Int gridPosition, int[,] gridSize)
    {
       
        return gridPosition.x >= 0 && gridPosition.x < gridSize.GetLength(0) &&
               gridPosition.y >= 0 && gridPosition.y < gridSize.GetLength(1);
        
    }

    private bool CanPlaceCarPatsContainerOnField(Vector2Int position) {
        var partInfo = GetCurrentDraggedCellCarPart();
        var needCells = GetCellsFromField(position, partInfo.PartForm);
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var cell in needCells) {
            if (IsPositionInGrid(cell, carCmp.CarPartContainerGroupsGrid) && carCmp.CarPartContainerGroupsGrid[cell.x, cell.y] != 0)
                return false;
        }

        return true;
    }
    private List<Vector2Int> GetCellsFromField( Vector2Int position, Vector2Int[] form, int[,] grid = null) {
        List<Vector2Int> returnedCells = new List<Vector2Int>();
        var rotateAngle = grid == null ?0:_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRotateDegrees;
        foreach (var cell in form) {
            var addedCell = cell;
            if(rotateAngle == 90)
                addedCell = new Vector2Int(-cell.y+1, cell.x);
            else if(rotateAngle == 180)
                addedCell = new Vector2Int(-cell.x+1, -cell.y+1);
            else if(rotateAngle == 270)
                addedCell = new Vector2Int(cell.y, -cell.x+1);
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
        Debug.Log("TakeItem");
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
        _sceneData.Value._inventoryUIView._carBackground.sprite = carConfig.CarBackground;
        _sceneData.Value._inventoryUIView._gridLayoutGroup.GetComponent<Image>().sprite = carConfig.CarBackground;
        var gridLayoutGroupRect = _sceneData.Value._inventoryUIView._gridLayoutGroup.GetComponent<RectTransform>();
        gridLayoutGroupRect.sizeDelta = new Vector2(needCellPartSize * carConfig.Width, needCellPartSize * carConfig.Height);
        _sceneData.Value._inventoryUIView._gridLayoutGroup.cellSize = new Vector2(needCellPartSize, needCellPartSize);
        inventoryCmp.GridCarPartCellsEntities = new int[carConfig.Width, carConfig.Height];
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
        }

        foreach (var cancelCell in _sceneData.Value._inventoryUIView._cancelCellPlaceButtons) {
            cancelCell.Construct(-Vector2Int.one, SetCurrentCarCell, -1, null);
        }
        _sceneData.Value._carPartsContainersContainer.GetComponent<CellView>().Construct(-Vector2Int.one*999, SetCurrentCarCell, -1, null);
        inventoryCmp.onCarCellSize = new Vector2(needCellPartSize, needCellPartSize);
        carCmp.CarPartGroupsGrid = new int[carConfig.Width, carConfig.Height];
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
        Debug.Log("SetCurrentCarCell");
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
