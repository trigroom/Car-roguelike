using System.Collections.Generic;
using Client;
using DG.Tweening;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using UnityEngine;
using UnityEngine.UI;

public class InventorySystem : IEcsRunSystem, IEcsInitSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;

    private EcsPoolInject<CarComponent> _carComponentsPool;
    private EcsPoolInject<CarPartCellComponent> _cellComponentsPool;
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

    private EcsFilterInject<Inc<CarComponent>> _carComponentsFilter;
    private EcsFilterInject<Inc<EndRoundEvent>> _endRoundEventsFilter;
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

        _sceneData.Value._carPartInfoUIView.SetupButtonsInfoPanel(BackCarPartInPanelInfo, NextCarPartInPanelInfo, DestroyMarkedCarPart,
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

        foreach (var endRound in _endRoundEventsFilter.Value) {
            UpdateShopItems();
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
            }

            inventoryCmp.CellIsSetup = true;

            if (inventoryCmp.CurrentDraggedCellEntity != -1) {
                _sceneData.Value._inventoryUIView._draggedInventoryCell.transform.position = Input.mousePosition;

                if (Input.GetMouseButtonDown(1) && GetCurrentDraggedCellCarPart().PartType == CarPartType.WeaponContainer)
                    RotateCarPart();
                if (Input.GetMouseButtonUp(0)) {
                    if (inventoryCmp.CurrentCellCarPart != null && inventoryCmp.CurrentCellCarPart._gridPosition != -Vector2Int.one) {
                        ref var carPartCellCmp = ref _cellComponentsPool.Value.Get(inventoryCmp.CurrentCellCarPart._entity);
                        if (CanPlacedOnCarCell(carPartCellCmp))
                            PlaceCarPartInCell(ref inventoryCmp, ref carPartCellCmp);
                        else
                            CancelCarPartPlace(ref inventoryCmp);
                    } else
                        CancelCarPartPlace(ref inventoryCmp);

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

        inventoryCmp.CurrentTween = _sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform
            .DOSizeDelta(new Vector2(100, 100), 0.3f).SetEase(Ease.OutBack);
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

        SetShopCells();
    }

    private void UpdateShopItems() {
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        _sceneData.Value._shopUIView._updateItemsText.text = "update for 5$";
        if (playerCurrencyCmp.moneyCount > 5) {
            playerCurrencyCmp.moneyCount -= 5;
            SetShopCells();
            SetMoneyText();
        }
    }

    private void SetMoneyText() {
        _sceneData.Value._shopUIView._moneyCountText.text =
            _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).moneyCount + "$";
    }

    private CarPartType[] _firstCarPartTypesForShop = new[] {
        CarPartType.WeaponContainer,
        CarPartType.Armor,
        CarPartType.Gun,
        CarPartType.MeleeWeapon
    };
    private void SetShopCells() {
        var shopCmp = _shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        Dictionary<CarPartType, List<ShopItem>> carPartsForTypes = new Dictionary<CarPartType, List<ShopItem>>();
        foreach (var shopItem in shopCmp.CurrentShopItems) {
            if(!carPartsForTypes.TryAdd(_sceneData.Value.GetCarPartConfig(shopItem.PartId).PartType,new List<ShopItem>() {shopItem} ))
                carPartsForTypes[_sceneData.Value.GetCarPartConfig(shopItem.PartId).PartType].Add(shopItem); 
        }

        for (int i = 0; i < _sceneData.Value._shopUIView._buttonsShop.Length; i++) {
            var shopButton = _sceneData.Value._shopUIView._buttonsShop[i];
            var randomItem = i >= _firstCarPartTypesForShop.Length ?shopCmp.CurrentShopItems[Random.Range(0, shopCmp.CurrentShopItems.Count)]:
                    carPartsForTypes[_firstCarPartTypesForShop[i]][Random.Range(0, carPartsForTypes[_firstCarPartTypesForShop[i]].Count)];
            ref var shopCellCmp = ref _shopCellComponentsPool.Value.Get(shopButton._entity);
            shopCellCmp.CurrentBuyItem = randomItem;
            shopButton.SetShopCellInfo(randomItem.Price, GetCarPartConfig(randomItem.PartId).PartSprite);
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
        if (shopCellCmp.CurrentBuyItem.Price > playerCurrencyCmp.moneyCount) return true;
        playerCurrencyCmp.moneyCount -= shopCellCmp.CurrentBuyItem.Price;
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
        } else if (_cellComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
            ref var cellCmp = ref _cellComponentsPool.Value.Get(markedCell);
            var carPartConfig = _sceneData.Value.GetCarPartConfig(_sceneData.Value._carPartInfoUIView._currentPartInfoId);
            var upgradedPartConfig = _sceneData.Value.GetCarPartConfig(carPartConfig.UpgradedPartId);
            bool isSecondPart = _sceneData.Value._carPartInfoUIView._currentPartInfoId == cellCmp.CarPartsId[0];
            if (isSecondPart) {
                cellCmp.CellView._imageCarPart.sprite = upgradedPartConfig.PartSprite;
                cellCmp.CarPartsId[0] = upgradedPartConfig.Id;
            } else {
                cellCmp.CellView._secondImageCarPart.sprite = upgradedPartConfig.PartSprite;
                cellCmp.CarPartsId[1] = upgradedPartConfig.Id;
            }
            _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(GetCarPartConfig(upgradedPartConfig.Id).UpgradedPartId != 0);
            _sceneData.Value._carPartInfoUIView._upgradeButton.enabled =
                GetCarPartConfig(upgradedPartConfig.Id).UpgradedPartCost <= playerCurrencyCmp.moneyCount;
            _sceneData.Value._carPartInfoUIView.SetPanelForNeededPart(upgradedPartConfig, !isSecondPart);
            playerCurrencyCmp.moneyCount -= carPartConfig.UpgradedPartCost;
            SetMoneyText();
            UpdateCarDescription();
            //  SetCarPartInfoPanel(markedCell);
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
        } else if (_cellComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
            var cellCmp = _cellComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo);
            if (cellCmp.CarPartsId.Count > 1)
                cellCmp.CellView._secondImageCarPart.sprite = _sceneData.Value._transparentPixel;
            else {
                cellCmp.CellView._imageCarPart.sprite = _sceneData.Value._buildCellSprite;
                cellCmp.CellView._imageCarPart.transform.rotation = Quaternion.Euler(0, 0, 0);
            }

            cellCmp.CarPartsId.Remove(_sceneData.Value._carPartInfoUIView._currentPartInfoId);
        } //add for shop cell

        CloseShowInfoPanel();
    }

    private void NextCarPartInPanelInfo() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        var cellCmp = _cellComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo);
        bool showDestroyButton = true;
        int needCarPartId;

        if (_sceneData.Value._carPartInfoUIView._currentPartInfoId == cellCmp.CarPartsId[0]) {
            needCarPartId = cellCmp.CarPartsId[1];
        } else {
            if (cellCmp.CarPartsId.Count > 1)
                showDestroyButton = false;
            needCarPartId = cellCmp.CarPartsId[0];
        }

        var carPartConfig = GetCarPartConfig(needCarPartId);
        if (carPartConfig.PartType == CarPartType.Main)
            showDestroyButton = false;

        _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(carPartConfig.UpgradedPartCost != 0);
        _sceneData.Value._carPartInfoUIView.SetPanelForNeededPart(carPartConfig, showDestroyButton);
    }

    private void BackCarPartInPanelInfo() {
        NextCarPartInPanelInfo();
    }

    private void UpdateCarDescription() {
        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId) ;
        float defaultMaxCarSpeed = carConfig.DefaultMaxSpeed;
        float carRotateSpeed = carConfig.RotateSpeed;
        float carWeight = carConfig.CarWeight;
        float maxFuel = carConfig.DefaultMaxFuel;
        float speedBoost = carConfig.BoostSpeedMultiplayer;
        float speedBoostFuelConsumption = carConfig.AccelerationFuelСonsumption;
        float defaultFuelConsumption = carConfig.FuelСonsumption;
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var carPart in inventoryCmp.GridCarPartsEntities) {
           var carPartCellCmp = _cellComponentsPool.Value.Get(carPart);
           if (carPartCellCmp.CarPartsId.Count > 0) {
               foreach (var carPartId in carPartCellCmp.CarPartsId) {
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
        } else if (_cellComponentsPool.Value.Has(entity)) {
            var cellCmp = _cellComponentsPool.Value.Get(entity);
            if (cellCmp.CarPartsId.Count == 0) return;

            _sceneData.Value._carPartInfoUIView._switchButtonsContainer.gameObject.SetActive(cellCmp.CarPartsId.Count > 1);

            if (inventoryCmp.CurrentMarkedCellForPanelInfo == entity && cellCmp.CarPartsId.Count > 1 &&
                _sceneData.Value._carPartInfoUIView._currentPartInfoId == cellCmp.CarPartsId[0]) {
                needCarPartId = cellCmp.CarPartsId[1];
            } else if (inventoryCmp.CurrentMarkedCellForPanelInfo == entity && (cellCmp.CarPartsId.Count == 1 ||
                                                                                (cellCmp.CarPartsId.Count > 1 &&
                                                                                 _sceneData.Value._carPartInfoUIView._currentPartInfoId ==
                                                                                 cellCmp.CarPartsId[1]))) {
                CloseShowInfoPanel();
                return;
            } else if (inventoryCmp.CurrentMarkedCellForPanelInfo != entity) {
                if (cellCmp.CarPartsId.Count > 1)
                    showDestroyButton = false;
                needCarPartId = cellCmp.CarPartsId[0];
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

    private bool CanPlacedOnCarCell(CarPartCellComponent carPartCellComponent) {
        var carPartType = GetCurrentDraggedCellCarPart().PartType;
        bool canPlace =
       (carPartCellComponent.CarPartsId.Count == 0 && carPartType != CarPartType.Gun && carPartType != CarPartType.MeleeWeapon) ||
               (carPartCellComponent.CarPartsId.Count != 0 &&
                GetCarPartConfig(carPartCellComponent.CarPartsId[0]).PartType == CarPartType.WeaponContainer &&
                (carPartType == CarPartType.Gun || carPartType == CarPartType.MeleeWeapon) && carPartCellComponent.CarPartsId.Count == 1);
            
        return canPlace;
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

    private void PlaceCarPartInCell(ref InventoryComponent inventoryCmp, ref CarPartCellComponent carPartCarPartCellCmp) {
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
        
carPartCarPartCellCmp.CarPartsId.Add(partIndex);
        var partInfo = GetCarPartConfig(partIndex);
        //var partInfo = GetCarPartConfig(partIndex);
        UpdateCarDescription();
        CellView currentCarPartCell = inventoryCmp.CurrentCellCarPart;
        float currentDegrees = inventoryCmp.CurrentRotateDegrees;
        Sequence returnSequence = DOTween.Sequence();
        returnSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform
            .DOSizeDelta(inventoryCmp.CurrentCellCarPart._imageCarPart.rectTransform.sizeDelta, 0.2f).SetEase(Ease.InBack));
        returnSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.transform
            .DOMove(inventoryCmp.CurrentCellCarPart._imageCarPart.gameObject.transform.position, 0.1f).SetEase(Ease.InOutQuad));
        bool isWeapon = partInfo.PartType == CarPartType.Gun || partInfo.PartType == CarPartType.MeleeWeapon;
        if (isWeapon) {
            returnSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.transform
                .DORotate(new Vector3(0, 0, currentCarPartCell._imageCarPart.transform.eulerAngles.z), 0.2f, RotateMode.Fast)
                .SetEase(Ease.OutBack));
        }

        returnSequence.OnComplete(() => {
            _sceneData.Value._inventoryUIView._draggedInventoryCell.gameObject.SetActive(false);
            if (!isWeapon) {
                currentCarPartCell._imageCarPart.sprite = partInfo.PartSprite;
                currentCarPartCell._imageCarPart.transform.rotation = Quaternion.Euler(0, 0, currentDegrees);
            } else
                currentCarPartCell._secondImageCarPart.sprite = partInfo.PartSprite;
        });

        inventoryCmp.CurrentDraggedCellEntity = -1;
        inventoryCmp.CurrentCellCarPart = null;
    }

    private void IncreaseCarSize() { }

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
        var needCellPartSize = 400 / Mathf.Max(carConfig.Width, carConfig.Height);
        _sceneData.Value._inventoryUIView._carBackground.sprite = carConfig.CarBackground;
        _sceneData.Value._inventoryUIView._gridLayoutGroup.GetComponent<Image>().sprite = carConfig.CarBackground;
        var gridLayoutGroupRect = _sceneData.Value._inventoryUIView._gridLayoutGroup.GetComponent<RectTransform>();
        gridLayoutGroupRect.sizeDelta = new Vector2(needCellPartSize * carConfig.Width, needCellPartSize * carConfig.Height);
        _sceneData.Value._inventoryUIView._gridLayoutGroup.cellSize = new Vector2(needCellPartSize, needCellPartSize);
        inventoryCmp.GridCarPartsEntities = new int[carConfig.Height, carConfig.Width];
        for (int i = 0; i < carConfig.Height; i++) {
            for (int j = 0; j < carConfig.Width; j++) {
                var inventoryCell = _sceneData.Value.InstantiateCell(_sceneData.Value._inventoryUIView._cellView,
                    _sceneData.Value._inventoryUIView._gridLayoutGroup.transform);
                int cellEntity = _world.Value.NewEntity();
                inventoryCmp.GridCarPartsEntities[i, j] = cellEntity;

                ref var cellCmp = ref _cellComponentsPool.Value.Add(cellEntity);
                cellCmp.MainEntity = _sceneData.Value.PlayerEntity;
                cellCmp.CellView = inventoryCell;
                cellCmp.CarPartsId = new List<int>();
                _carPartInGameUIComponentsPool.Value.Add(cellEntity);
                inventoryCell.Construct(new Vector2Int(j, i), SetCurrentCarCell, cellEntity, SetCarPartInfoPanel);
            }
        }

        foreach (var cancelCell in _sceneData.Value._inventoryUIView._cancelCellPlaceButtons) {
            cancelCell.Construct(-Vector2Int.one, SetCurrentCarCell, -1, null);
        }

        AddMainCarParts(_sceneData.Value.GetCarConfig(_sceneData.Value._defaultCarConfigId)); //for test
    }

    private void AddMainCarParts(CarConfig carConfig) {
        AddInventoryItem(carConfig.CarEngine.Id);
        AddInventoryItem(carConfig.DriverCabine.Id);
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
