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
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class InventorySystem : IEcsRunSystem, IEcsInitSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;
    private EcsCustomInject<BetweenScenesService> _betweenScenesService;

    private EcsPoolInject<CarComponent> _carComponentsPool;
    private EcsPoolInject<CarPartCellComponent> _carPartCellComponentsPool;
    private EcsPoolInject<MovementComponent> _movementComponentsPool;
    private EcsPoolInject<ShopCellComponent> _shopCellComponentsPool;
    private EcsPoolInject<ShopCellCarComponent> _shopCellCarComponentsPool;
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
    private EcsPoolInject<GunComponent> _gunComponentsPool;
    private EcsPoolInject<AddTeammateEvent> _addTeammateEventsPool;
    private EcsPoolInject<MeleeWeaponComponent> _meleeWeaponComponentsPool;
    private EcsPoolInject<EnemyAIComponent> _enemyAIComponentsPool;
    private EcsPoolInject<EndRoundEvent> _endRoundEventsPool;
    private EcsPoolInject<UpdateCarsStatsInfoEvent> _updateCarsStatsInfoEventsPool;
    private EcsPoolInject<CarPartInfoComponent> _carPartInfoComponentsPool;
    private EcsPoolInject<ModuleInventoryItem> _moduleInventoryItemsPool;

    private EcsFilterInject<Inc<UpdateShopToSaleRoundEvent>> _updateShopToSaleRoundEventsFilter;
    private EcsFilterInject<Inc<CarComponent>> _carComponentsFilter;
    private EcsFilterInject<Inc<StartRoundEvent>> _startRoundEventsFilter;
    private EcsFilterInject<Inc<UpdateCarsStatsInfoEvent>> _updateCarsStatsInfoEventsFilter;
    private EcsFilterInject<Inc<EndRoundEvent>> _endRoundEventsFilter;
    private EcsFilterInject<Inc<CarPartComponent>> _carPartComponentsFilter;
    private EcsFilterInject<Inc<InventoryCellComponent>> _inventoryCellComponentsFilter;
    private EcsFilterInject<Inc<AddInventoryItemEvent>> _addInventoryItemEventsFilter;
    private EcsFilterInject<Inc<ModuleInventoryItem>> _moduleInventoryItemsFilter;

    public void Init(IEcsSystems systems) {
        //test
        
        int playerEntity =_sceneData.Value.PlayerEntity;

        _playerCurrencyComponentsPool.Value.Add(playerEntity).MoneyCount = 0;
       

        ref var carCmp = ref _carComponentsPool.Value.Add(playerEntity);
        carCmp.CarId = _sceneData.Value._defaultCarConfigId;
        
        var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Add(playerEntity);
        inventoryCmp.CurrentInventoryPage = 0;
        inventoryCmp.CurrentDraggedCellEntity = -1;
        inventoryCmp.CurrentCellCarPart = null;
        inventoryCmp.CurrentGivenItems = new List<int>();
        _fuelComponentsPool.Value.Add(playerEntity);
       
        ref var movementComponent = ref _movementComponentsPool.Value.Add(playerEntity);
        //movementComponent.MaxMoveSpeed = carConfig.DefaultMaxSpeed;
        movementComponent.RotateSpeed = carConfig.RotateSpeed;
        movementComponent.Acceleration = carConfig.Acceleration;
        movementComponent.Deceleration = carConfig.Deceleration;
        SetCarUpgradeButtonInfo();
        
        SortCarParts(CarPartType.None, false);
        SortCarParts(CarPartType.CarStuffContainer, false);
        SortCarParts(CarPartType.WheelsContainer, false);

        SetCarGrid();

        SetSortButtons();
        _sceneData.Value._carPartInfoUIView.SetupButtonsInfoPanel(BackCarPartInPanelInfo, SellMarkedCarPart, UpgradeMarkedCarPartLevel,
            UpdateButtonEnter, UpdateButtonExit, MoveToInventoryCell);
        _sceneData.Value._inventoryUIView._changeToRedactModeButton.onClick.AddListener(SetCarPartRedactorMode);
        SetupInventoryCells();

        SetupShopCells();

        UpdateCarDescription(playerEntity);
        
         SetMoneyCount(_betweenScenesService.Value.GetMapDifficultyConfig().StartMoney);
        
         foreach (var endGameButton in _sceneData.Value._endGameScreenView.closeEndGameMenuButtons) {
             endGameButton.onClick.AddListener(ActivateGiveCarPartsWindow);
         }
        
         _sceneData.Value._givingCarPartsUIView._getCarPartButton.onClick.AddListener(GetCarPartAndActiveNext);
         _sceneData.Value._givingCarPartsUIView._destroyCarPartButton .onClick.AddListener(SellCarPartAndActiveNext);
        _sceneData.Value._enemiesCarBuilderView.CreateEnemyConfigButton.onClick.AddListener(SaveEnemyCarConfig);
        _sceneData.Value._inventoryUIView._upgradeCarButton.onClick.AddListener(IncriseCarLevel);
        _sceneData.Value._inventoryUIView._healCarButton.onClick.AddListener(HealAllCarPart);

        for (int i = 0; i < _sceneData.Value._inventoryUIView._selectCarButtons.Length; i++) {
            int needIndex = i;
            var needButtonStuff = _sceneData.Value._inventoryUIView._selectCarButtons[i];
            needButtonStuff.selectCarButton.onClick.AddListener(() => ClearAndSetNewCarUI(needIndex));
            if (i != 0)
                needButtonStuff.sellCarButton.onClick.AddListener(() => SellCarButtonAction(needIndex));
            var carNameText = needButtonStuff.carNameText;
            if (i == 0)
                carNameText.text = GetLocalization("Main car");
            else
                carNameText.text = $"{GetLocalization("Car")} {i}";
        }

        //AddNewTeammateCarToPlayer(playerEntity);
        _sceneData.Value._carPartInfoUIView._closeInfoPanelButton.onClick.AddListener(CloseShowInfoPanel);
        _sceneData.Value._carPartInfoUIView._healCarPartButton.onClick.AddListener(HealMarkedCarPart);
        inventoryCmp.PlayerTeammateCarsEntity = new List<int>();
        inventoryCmp.PlayerTeammateCarsEntity.Add(playerEntity);
        inventoryCmp.CurrentRedactedCarEntity = playerEntity;
        inventoryCmp.CurrentSelectedModuleCellId = -1;
        AddRareToShopItems();
        _sceneData.Value._staticTextsView.SetTextLanguage();
        SetupLocalizationButtons();

        for (int i = 0; i < _sceneData.Value._carPartInfoUIView._moduleViews.Length; i++) {
            int index = i;
            var moduleStuff = _sceneData.Value._carPartInfoUIView._moduleViews[i];
            
            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => { SelectModuleCell(index); });

            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { SetModuleWindowInfo(false, index);});
            
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => { SetModuleWindowInfo(true, index);});
            
            moduleStuff.ModuleButton.triggers.Add(clickEntry);
            moduleStuff.ModuleButton.triggers.Add(enterEntry);
            moduleStuff.ModuleButton.triggers.Add(exitEntry);
            /*(data) => { ChangeRoundDescription(randomRounds[newIndex], (PointerEventData)data); }*/
        }

        //test
        SetUpAdminButtons();
        AddModuleInventoryItem(0);
        AddModuleInventoryItem(1);
    }

    public void Run(IEcsSystems systems) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        foreach (var startRound in _startRoundEventsFilter.Value) {
            SetShopCells(false);
            SetCarShopCells();
            AddRareToShopItems();
            UpdateCarDescription(inventoryCmp.CurrentRedactedCarEntity);
            foreach (var carEntity in inventoryCmp.PlayerTeammateCarsEntity) {
                if (inventoryCmp.CurrentRedactedCarEntity != carEntity)
                    UpdateCarStats(carEntity);
            }

            ActivateGiveCarPartsWindow();
            _sceneData.Value._shopUIView._updateItemsText.text = $"{GetLocalization("Update for")} {GetShopUpdateCost(0)}$";
            _shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentUpdateCountInRound = 0;
            //     _endRoundEventsPool.Value.Del(endRound);
        }

        foreach (var updateCarPartsStatsEntity in _updateCarsStatsInfoEventsFilter.Value) {
            _updateCarsStatsInfoEventsPool.Value.Del(updateCarPartsStatsEntity);
            UpdateCarDescription(inventoryCmp.CurrentRedactedCarEntity);
            foreach (var carEntity in inventoryCmp.PlayerTeammateCarsEntity) {
                if (inventoryCmp.CurrentRedactedCarEntity != carEntity)
                    UpdateCarStats(carEntity);
            }
        }
        foreach (var endRound in _endRoundEventsFilter.Value) {
            SetHealthToMinimumLevel();
            UpdateHealAllCarPartButton();
            _sceneData.Value._playerUIView._fuelLampLightImage.DOKill();
            Debug.Log("End round Inv System");
            CloseShowInfoPanel();
            //_endRoundEventsPool.Value.Del(endRound);
        }

        foreach (var updateSaleShop in _updateShopToSaleRoundEventsFilter.Value) {
            var shopCmp = _shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            if (shopCmp.isModulesShop) {
                if (shopCmp.IsCarShopMode)
                    ChangeShopMode();
                _sceneData.Value._shopUIView._shopModeButton.gameObject.SetActive(false);
            }

            SetShopCells(!shopCmp.isModulesShop);
            //
            _updateShopToSaleRoundEventsPool.Value.Del(updateSaleShop);
        }

        if (_addTeammateEventsPool.Value.Has(_sceneData.Value.PlayerEntity)) {
            _addTeammateEventsPool.Value.Del(_sceneData.Value.PlayerEntity);
            var gameCmp = _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            var currentRoundCarStuff = _betweenScenesService.Value.GetMapConfig().GameProcessConfig.GameRounds[gameCmp.CurrentRound];

            int i = GridUtils.GetRandomIndexFromChancesList(currentRoundCarStuff.EnemyCarChances);
            var carsList = _betweenScenesService.Value.GetMapConfig().EnemyMapConfigs[i].CarMapsList;
            var enemyCar = carsList[Random.Range(0, carsList.Count)];

            AddNewTeammateCarToPlayer(enemyCar);
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
                    _sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform.sizeDelta - inventoryCmp.OnCarCellSize;
               
            }

            inventoryCmp.CellIsSetup = true;

            if (inventoryCmp.CurrentDraggedCellEntity != -1) {
                var carCmp = _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity);

                var currentCellSize = new Vector2(200, 200) / Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0),
                    carCmp.CarPartContainerGroupsGrid.GetLength(1)) * _sceneData.Value._inventoryUIView._carPartsContainer.localScale.x;
                var carPartsContainer = _sceneData.Value._inventoryUIView._carPartsDownContainer;

                _sceneData.Value._gridMaskRect.position = _sceneData.Value._inventoryUIView._draggedInventoryCell.transform.position;
                var subGridNewPosition = carPartsContainer.position;
                var carSizeX = (carCmp.CarPartContainerGroupsGrid.GetLength(0));
                var carSizeY = (carCmp.CarPartContainerGroupsGrid.GetLength(1));
                var carPartSize = GridUtils.GetCarPartSize(GetCurrentDraggedCellCarPart());
                if (inventoryCmp.IsCarPartsRedactorMode && (carSizeX % 2 == 1 && carSizeY % 2 == 1 ||
                    ((carSizeX % 2 == 1 && carSizeY % 2 == 0) && carSizeX > carSizeY) ||
                    (carSizeX % 2 == 0 && carSizeY % 2 == 1) && carSizeX < carSizeY)) {
                    subGridNewPosition += (Vector3)currentCellSize * _sceneData.Value.mainCanvas.localScale.x / 2;
                }

                _sceneData.Value._subGridTransform.transform.position = subGridNewPosition;

                var startOffset = -inventoryCmp.StartOffsetFromCell;
                if (inventoryCmp.CurrentRotateDegrees == 90) {
                    startOffset = new Vector2(-startOffset.y, startOffset.x);
                    carPartSize = new Vector2Int(carPartSize.y, carPartSize.x);
                } else if (inventoryCmp.CurrentRotateDegrees == 180) {
                    startOffset = new Vector2(-startOffset.x, -startOffset.y);
                    carPartSize = new Vector2Int(carPartSize.x, carPartSize.y);
                } else if (inventoryCmp.CurrentRotateDegrees == 270) {
                    startOffset = new Vector2(startOffset.y, -startOffset.x);
                    carPartSize = new Vector2Int(carPartSize.y, carPartSize.x);
                }

                Vector2 positionOffset = Vector2.zero;
                positionOffset = new Vector2(carPartSize.x, carPartSize.y) / 2 * currentCellSize * startOffset *
                                 _sceneData.Value.mainCanvas.localScale.x;
                var edgePositionOffset = new Vector2(carPartSize.x - 1, carPartSize.y - 1) / 2 * currentCellSize *
                                         _sceneData.Value.mainCanvas.localScale.x;

                var finalEdgeInputPosition = (Vector2)Input.mousePosition + positionOffset - edgePositionOffset;

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
                        _sceneData.Value._subGridTransform.SetSubGridState(inventoryCmp.IsSnappingToGrid ||
                                                                           carCmp.CarPartContainerGroupsList.Count == 0);
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
                if (Input.GetMouseButtonDown(1) && !GridUtils.IsContainerType(GetCurrentDraggedCellCarPart()))
                    RotateCarPart();
                if (Input.GetMouseButtonUp(0)) {
                    _sceneData.Value._inventoryUIView._rotateCarPartIconContainer.gameObject.SetActive(false);
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

                if (_sceneData.Value._inventoryUIView._rotateCarPartIconContainer.gameObject.activeInHierarchy)
                    _sceneData.Value._inventoryUIView.SetUIBottomRight();
            }
        }

        if (Input.GetMouseButtonUp(0)) {
            inventoryCmp.PressedButtonTime = 0;
            inventoryCmp.IsTryDraggedCell = false;
        }

    }
    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
    string[] languages = { "en", "ru", "de", "es", "fr" };
    private void SetupLocalizationButtons() {
        for (int i = 0; i < _sceneData.Value._settingsView._selectLocalizationButtons.Length; i++) {
            int langIndex = i;
            _sceneData.Value._settingsView._selectLocalizationButtons[langIndex].onClick
                .AddListener(()=> {
                    ChangeLocalization(langIndex);
                });
        }
    }

    private void ChangeLocalization(int langIndex) {
        _betweenScenesService.Value.CsvLocalization.ChangeLanguage(languages[langIndex]);
        _sceneData.Value._staticTextsView.SetTextLanguage();
    }
    private void UpdateButtonEnter() {
        int currentMarkedEntity = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentMarkedCellForPanelInfo;
        int[] moduleIds = _carPartInfoComponentsPool.Value.Has(currentMarkedEntity)
            ? _carPartInfoComponentsPool.Value.Get(currentMarkedEntity).ModuleIds
            : null;
        _sceneData.Value._carPartInfoUIView.SetDescriptionInfo(true, moduleIds);
    }
    private void UpdateButtonExit() {
        int currentMarkedEntity = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentMarkedCellForPanelInfo;
        int[] moduleIds = _carPartInfoComponentsPool.Value.Has(currentMarkedEntity)
            ? _carPartInfoComponentsPool.Value.Get(currentMarkedEntity).ModuleIds
            : null;
        _sceneData.Value._carPartInfoUIView.SetDescriptionInfo(false,moduleIds);
    }

    private void SetCarPartRedactorMode() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (!inventoryCmp.IsCarPartsRedactorMode && !CanUseThisCarPartContainers()) {
            if (!inventoryCmp.SortedCarPartType.Contains(CarPartType.WheelsContainer))
                SortCarParts(CarPartType.WheelsContainer);

            if (!inventoryCmp.SortedCarPartType.Contains(CarPartType.CarStuffContainer))
                SortCarParts(CarPartType.CarStuffContainer);
            return;
        }

        //messege about this

        inventoryCmp.IsCarPartsRedactorMode = !inventoryCmp.IsCarPartsRedactorMode;
        var containerSize = _sceneData.Value._inventoryUIView._carPartsDownContainer.sizeDelta;
        if (inventoryCmp.IsCarPartsRedactorMode) {
            _sceneData.Value._inventoryUIView._changeToRedactModeImage.sprite =
                _sceneData.Value._inventoryUIView._changeToRedactCarPartsModeSprite;
            _sceneData.Value._inventoryUIView._changeToRedactModeText.text = GetLocalization("Redact up level");
            _sceneData.Value._inventoryUIView._carPartsUpLevelContainer.alpha = 1;
            inventoryCmp.CurrentCarPartContainersTween.Kill();
            inventoryCmp.CurrentCarPartContainersTween = DOTween.Sequence();

            inventoryCmp.CurrentCarPartContainersTween.Append(
                _sceneData.Value._inventoryUIView._carPartsContainer.DOScale(Vector3.one * 2, 0.3f));
            inventoryCmp.CurrentCarPartContainersTween.Join(
                _sceneData.Value._inventoryUIView._carPartsContainer.DOLocalMove(
                    new Vector2(200 - containerSize.x - 100, 200 - containerSize.y + 38), 0.3f));

            // _sceneData.Value._inventoryUIView._carPartsContainer.transform.localPosition =
            //   new Vector2((200-containerSizeX)  - 100, (200-containerSizeY) + 38);
            SortCarParts(CarPartType.All);
        } else {
            _sceneData.Value._inventoryUIView._changeToRedactModeImage.sprite =
                _sceneData.Value._inventoryUIView._changeToRedactCarContainersModeSprite;
            _sceneData.Value._inventoryUIView._changeToRedactModeText.text = GetLocalization("Redact down level");

            inventoryCmp.CurrentCarPartContainersTween.Kill();
            inventoryCmp.CurrentCarPartContainersTween = DOTween.Sequence();

            inventoryCmp.CurrentCarPartContainersTween.Append(_sceneData.Value._inventoryUIView._carPartsContainer.DOScale(Vector3.one, 0.3f));
            inventoryCmp.CurrentCarPartContainersTween.Join(
                _sceneData.Value._inventoryUIView._carPartsContainer.DOLocalMove(
                    new Vector2((200 - containerSize.x) / 2 - 100, (200 - containerSize.y) / 2 + 38), 0.3f));

            _sceneData.Value._inventoryUIView._carPartsUpLevelContainer.alpha = 0.25f;

            SortCarParts(CarPartType.None);
        }

        SortCarParts(CarPartType.CarStuffContainer, false);
        SortCarParts(CarPartType.WheelsContainer, false);
    }

    private void StartDragShopCell(ref InventoryComponent inventoryCmp) {
        var inventoryView = _sceneData.Value._inventoryUIView;
        var shopItem = _shopCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CurrentBuyItemId;
        int carPartId = shopItem;
        int inventoryPartIndex = carPartId;
        //  inventoryView._inventoryCellViews[inventoryCmp.CurrentDraggedCellEntity].gameObject.SetActive(false);
        var partInfo = _sceneData.Value._carStuffListsConfig.CarPartConfigs[inventoryPartIndex];
        if ((partInfo.PartType == CarPartType.CarStuffContainer || partInfo.PartType == CarPartType.WheelsContainer) ==
            inventoryCmp.IsCarPartsRedactorMode) {
            AnimateChangeModeButton();
            return;
        }

        inventoryCmp.StartOffsetFromCell = GridUtils.GetNormalizedMouseOffset(_sceneData.Value._shopUIView
            ._buttonsShop[_shopCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).ShopButtonIndex].ItemSprite.rectTransform);
        SetDraggedCell(ref inventoryCmp, inventoryPartIndex, inventoryView);
        _sceneData.Value._inventoryUIView._rotateCarPartIconContainer.gameObject.SetActive(!GridUtils.IsContainerType(partInfo));
    }

    private void StartDragInventoryCell(ref InventoryComponent inventoryCmp) {
        var inventoryView = _sceneData.Value._inventoryUIView;
       
         
      // int cellEntity =
    //        GetSortedCarPartList()[
        //        inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length + inventoryCellIndex];
       var inventoryCellCmp = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity); 
       var inventoryCellInfoCmp = _carPartInfoComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity); 
       int inventoryCellIndex = inventoryCellCmp.CellId;
        var partInfo = _sceneData.Value._carStuffListsConfig.CarPartConfigs[inventoryCellInfoCmp.CarPartId];
        if ((partInfo.PartType == CarPartType.CarStuffContainer || partInfo.PartType == CarPartType.WheelsContainer) ==
            inventoryCmp.IsCarPartsRedactorMode) {
            AnimateChangeModeButton();
            return;
        }
        inventoryCmp.StartOffsetFromCell =
            GridUtils.GetNormalizedMouseOffset(inventoryView._inventoryCellViews[inventoryCellIndex]._inventoryCellImage.rectTransform);
        SetDraggedCell(ref inventoryCmp, inventoryCellInfoCmp.CarPartId, inventoryView);
        _sceneData.Value._inventoryUIView._rotateCarPartIconContainer.gameObject.SetActive(!GridUtils.IsContainerType(partInfo));
 
        inventoryView._inventoryCellViews[inventoryCellIndex].gameObject.SetActive(false);
    }

    private void AnimateChangeModeButton() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.CurrentTweenChangeModeButton = DOTween.Sequence();
        inventoryCmp.CurrentTweenChangeModeButton.Append(_sceneData.Value._inventoryUIView._changeToRedactModeImage.transform.DOScale(1.3f, 0.3f)
            .SetEase(Ease.InOutSine));
        inventoryCmp.CurrentTweenChangeModeButton.Append(_sceneData.Value._inventoryUIView._changeToRedactModeImage.transform.DOScale(1, 0.3f)
            .SetEase(Ease.InOutSine));
    }
    private void MoveToInventoryCell() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var carPartConfig = GetCarPartConfig(_carPartInfoComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo).CarPartId);
        if (GridUtils.IsContainerType(carPartConfig))
            MoveCarPartContainerToInventory(inventoryCmp.CurrentMarkedCellForPanelInfo);
        else
            MoveToInventoryCellNeededCarPart(inventoryCmp.CurrentMarkedCellForPanelInfo);

        UpdateCarDescription(inventoryCmp.CurrentRedactedCarEntity);
    }

    private void MoveToInventoryCellNeededCarPart(int currentPartEntity) {    
        var currentPartCmp = _carPartInfoComponentsPool.Value.Get(currentPartEntity);
        
        SellNeededCarPart(currentPartEntity, false);
         AddInventoryItem(currentPartCmp.CarPartId,currentPartCmp.PartLevel , currentPartCmp.CurrentHealth,currentPartCmp.ModuleIds);
    }

    private void SetDraggedCell(ref InventoryComponent inventoryCmp, int inventoryPartIndex, InventoryUIView inventoryView) {
        var partInfo = _sceneData.Value._carStuffListsConfig.CarPartConfigs[inventoryPartIndex];
        inventoryView._draggedInventoryCell.gameObject.SetActive(true);
        inventoryView._draggedInventoryCell.sprite = partInfo.PartSprite;

        _sceneData.Value._inventoryUIView._draggedInventoryCell.transform.rotation = Quaternion.Euler(0, 0, 0);
        inventoryCmp.CurrentRotateDegrees = 0;
        var partSize = GridUtils.GetCarPartSize(partInfo);
        var carCmp = _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity);
        Vector2 currentCellSize = /*inventoryCmp.IsCarPartsRedactorMode ? inventoryCmp.onCarCellSize :*/
            new Vector2(200, 200) / Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1));
        currentCellSize *= _sceneData.Value._inventoryUIView._carPartsContainer.transform.localScale;
        Vector2 cellSize = partSize * currentCellSize;

        _sceneData.Value._subGridTransform.gameObject.SetActive(true);
        _sceneData.Value._gridMaskRect.gameObject.SetActive(true);
        _sceneData.Value._subGridTransform.SetSubGridSize(Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0),
            carCmp.CarPartContainerGroupsGrid.GetLength(1)) * 8/ _sceneData.Value._inventoryUIView._carPartsContainer.transform.localScale.x);
        inventoryCmp.CurrentTween = DOTween.Sequence();

        inventoryCmp.CurrentTween.Append(_sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform.DOSizeDelta(cellSize, 0.3f)
            .SetEase(Ease.OutBack));
        if (partInfo.PartType == CarPartType.Gun || partInfo.PartType == CarPartType.MeleeWeapon) {
            (Sprite upSprite, Vector2 size, Vector2 position) =
                GetUpperPartSizesForUI(_sceneData.Value._inventoryUIView._draggedInventoryCell.rectTransform.sizeDelta, partInfo);

            var upDragCell = _sceneData.Value._inventoryUIView._draggedInventoryCellUp;
            upDragCell.sprite = upSprite;
            upDragCell.transform.localPosition = position;
            upDragCell.rectTransform.sizeDelta = size;
            if (partInfo.PartType == CarPartType.Gun) {
                var gunConfig = _sceneData.Value.GetGunFromCarPart(partInfo.Id);
                if (gunConfig.UpPartSprite != null) {
                    var needPosition = cellSize / Math.Max(partSize.x, partSize.y) * gunConfig.UpPartPosition;

                    inventoryCmp.CurrentTween.Join(upDragCell.rectTransform.DOSizeDelta(currentCellSize / GridUtils.InWorldCellSize * gunConfig.UpPartSprite.rect.size, 0.3f).SetEase(Ease.OutBack));
                    inventoryCmp.CurrentTween.Join(upDragCell.transform.DOLocalMove(needPosition, 0.3f).SetEase(Ease.OutBack));
                }
            } else {
                var meleeConfig = _sceneData.Value.GetMeleeWeaponFromCarPart(partInfo.Id);
                if (meleeConfig.UpSprite != null) {
                    var needPosition = cellSize / Math.Max(partSize.x, partSize.y) * meleeConfig.UpSpritePosition;

                    inventoryCmp.CurrentTween.Join(upDragCell.rectTransform
                        .DOSizeDelta(currentCellSize / GridUtils.InWorldCellSize * meleeConfig.UpSprite.rect.size, 0.3f).SetEase(Ease.OutBack));
                    inventoryCmp.CurrentTween.Join(upDragCell.transform.DOLocalMove(needPosition, 0.3f).SetEase(Ease.OutBack));
                }
            }
        } else
            _sceneData.Value._inventoryUIView._draggedInventoryCellUp.sprite = _sceneData.Value._transparentPixel;

        _sceneData.Value._gridMaskRect.sizeDelta = cellSize + currentCellSize * 1.8f;
    }

    private void SaveEnemyCarConfig() {
       var carCmp  =_carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity );
       if(!CanUseThisCarPartContainers())return;
       
       List<CarPartTypeRow> upLevelGrid = new List<CarPartTypeRow>();
       List<CarPartTypeRow> downLevelGrid = new List<CarPartTypeRow>();

       foreach (var partsContainer in carCmp.CarPartContainerGroupsList) {
           var edgePosition = GridUtils.GetEdgePointFromCarPart(partsContainer.Value);
           var carPartCmp = _carPartInfoComponentsPool.Value.Get(partsContainer.Key);
           downLevelGrid.Add(new CarPartTypeRow(edgePosition, carPartCmp.CarPartId,0,carPartCmp.PartLevel));
       }

       foreach (var part in carCmp.CarPartGroupsList) {
           var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(part.Key);
           var carPartCmp = _carPartComponentsPool.Value.Get(part.Key);
           var edgePosition = GridUtils.GetEdgePointFromCarPart(part.Value);
           upLevelGrid.Add(new CarPartTypeRow(edgePosition, carPartInfoCmp.CarPartId, (int)carPartCmp.CarPartCellImage.transform.localEulerAngles.z,
               carPartInfoCmp.PartLevel));
       }

       _sceneData.Value._enemiesCarBuilderView.SaveEnemyCarConfig(carCmp.CarId,carCmp.CarLevel, downLevelGrid, upLevelGrid,
           new Vector2Int(carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1)));
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
        inventoryComponent.CurrentTween = DOTween.Sequence();
        inventoryComponent.CurrentTween.Append(cellView.transform
                                                           .DORotate(new Vector3(0, 0, inventoryComponent.CurrentRotateDegrees), 0.3f, RotateMode.Fast).SetEase(Ease.OutBack));
        inventoryComponent.LastCheckedGridCellPosition = -Vector2Int.one;
    }

    private void AddRareToShopItems() {
        ref var shopCmp = ref _shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        shopCmp.CurrentShopItemsChances = _sceneData.Value._carStuffListsConfig.RoundShopChances
            [_currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRound].Chances;
    }
    private void SetupShopCells() {
        _sceneData.Value._shopUIView._shopModeText.text = GetLocalization("Car parts shop");
        
        ref var shopCmp = ref _shopComponentsPool.Value.Add(_sceneData.Value.PlayerEntity);
        shopCmp.ListShopItemsForRares = new List<List<int>>(){new List<int>(),new List<int>(),new List<int>(),new List<int>()};
        shopCmp.CurrentShopItemsChances = new List<float>(4);
        shopCmp.ShopModulesIds = new List<int>();
        foreach (var shopItem in _sceneData.Value._carStuffListsConfig.ShopItems) {
            
            if(!DataPersistenceManagerSystem._gameData.CarPartsUnlocked[shopItem])continue;
            
            var partRare = (int)GetCarPartConfig(shopItem).RartRare;
           
            shopCmp.ListShopItemsForRares[partRare].Add(shopItem);
        }

        foreach (var moduleItem in _sceneData.Value._carStuffListsConfig.ModuleConfigs) {
            if (!DataPersistenceManagerSystem._gameData.ModulesUnlocked[moduleItem.ModuleId]) continue;

            shopCmp.ShopModulesIds.Add(moduleItem.ModuleId);
        }

        _sceneData.Value._shopUIView._buttonUpdateShop.onClick.AddListener(UpdateShopItems);
        for (int i = 0; i < _sceneData.Value._shopUIView._buttonsShop.Length; i++) {
            var shopCell = _sceneData.Value._shopUIView._buttonsShop[i];
            int shopCellEntity = _world.Value.NewEntity();
            ref var shopCellCmp = ref _shopCellComponentsPool.Value.Add(shopCellEntity);
            shopCellCmp.ShopButtonIndex = i;
            shopCell.Construct(shopCellEntity, BuyShopItem, SetCarPartInfoPanel, TakeItemFromInventory,LockShopItemToggleAction);
        }
        
        for (int i = 0; i < _sceneData.Value._shopUIView._buttonsCarShop.Length; i++) {
            var shopCell = _sceneData.Value._shopUIView._buttonsCarShop[i];
            int shopCellEntity = _world.Value.NewEntity();
            ref var shopCellCarCmp = ref _shopCellCarComponentsPool.Value.Add(shopCellEntity);
            shopCellCarCmp.ShopCarButtonIndex = i;
            shopCellCarCmp.CarPartsImages = new List<Image>();
            shopCell.Construct(shopCellEntity, BuyShopCar, SetCarPartInfoPanel, TakeItemFromInventory,LockShopItemToggleAction);
        }

        _sceneData.Value._shopUIView._updateItemsText.text = $"{GetLocalization("Update for")} {GetShopUpdateCost(0)}$";
        SetShopCells(false);
        SetCarShopCells();
        
       _sceneData.Value._shopUIView._shopModeButton.onClick.AddListener(ChangeShopMode); 
    }

    private void LockShopItemToggleAction(bool locked, Image toggleImage) {
        toggleImage.sprite = locked? _sceneData.Value._shopUIView._lockSprite:_sceneData.Value._shopUIView._unlockSprite;
    }
    private float GetShopUpdateCost(int curUpdateCount) {
        return curUpdateCount + _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRound;
    }

    private void ChangeShopMode() {
        ref var shopCmp = ref _shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        shopCmp.IsCarShopMode = !shopCmp.IsCarShopMode;
        
        _sceneData.Value._shopUIView._shopModeText.text = shopCmp.IsCarShopMode ? GetLocalization("Car shop") : GetLocalization("Car parts shop");
        _sceneData.Value._shopUIView._shopCarContainer.gameObject.SetActive(shopCmp.IsCarShopMode);
        _sceneData.Value._shopUIView._shopCarPartsContainer.gameObject.SetActive(!shopCmp.IsCarShopMode);
    }
    private void UpdateShopItems() {
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);   
        ref var shopCmp = ref _shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        
        float updateCost = GetShopUpdateCost(shopCmp.CurrentUpdateCountInRound);
     
        if (playerCurrencyCmp.MoneyCount >= updateCost) {
            
            _sceneData.Value._shopUIView._buttonUpdateShop.transform.localEulerAngles = Vector3.zero;
            shopCmp.CurrentUpdateCountInRound++;
            // Анимация вращения на 360 градусов
            _sceneData.Value._shopUIView._buttonUpdateShop.transform.DORotate(
                new Vector3(0, 0, -360f), // Отрицательное значение для вращения по часовой стрелке
                0.7f, RotateMode.LocalAxisAdd).SetEase(Ease.OutBack);
            
            SetShopCells(false);
            SetCarShopCells();
            SetMoneyCount(-updateCost);
            
        } else {
            _sceneData.Value._shopUIView.NotEnoughMoneyAnimation();
        }
        _sceneData.Value._shopUIView._updateItemsText.text = $"{GetLocalization("Update for")} {GetShopUpdateCost(shopCmp.CurrentUpdateCountInRound)}$";
    }

    private void SetCarContainersContainerSize() {
        var carCmp = _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity);

        float containerSizeX = carCmp.CarPartContainerGroupsGrid.GetLength(0) > carCmp.CarPartContainerGroupsGrid.GetLength(1)
            ? 200f
            : 200f * ((float)carCmp.CarPartContainerGroupsGrid.GetLength(0) / (carCmp.CarPartContainerGroupsGrid.GetLength(1)));
        float containerSizeY = carCmp.CarPartContainerGroupsGrid.GetLength(0) < carCmp.CarPartContainerGroupsGrid.GetLength(1)
            ? 200
            : 200f * ((float)carCmp.CarPartContainerGroupsGrid.GetLength(1) / (carCmp.CarPartContainerGroupsGrid.GetLength(0)));
        _sceneData.Value._inventoryUIView._carPartsDownContainer.sizeDelta = new Vector2(containerSizeX, containerSizeY);
        _sceneData.Value._inventoryUIView._carPartsContainer.transform.localPosition = new Vector2(
            (200 - containerSizeX) * _sceneData.Value._inventoryUIView._carPartsContainer.transform.localScale.x / 2 - 100,
            (200 - containerSizeY) * _sceneData.Value._inventoryUIView._carPartsContainer.transform.localScale.x / 2 + 38);

        Debug.Log(containerSizeX + " x " + containerSizeY + " y");
        _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).OnCarCellSize = new Vector2(
            containerSizeX / carCmp.CarPartContainerGroupsGrid.GetLength(0), containerSizeX / carCmp.CarPartContainerGroupsGrid.GetLength(0));
    }

    private void SetFirstCarPartContainer() {
        (var carPartConfig,int level, float health, int[] moduleIds) = GetCurrentDraggedCellCarPartWithLevel();
       
        if (carPartConfig.PartType == CarPartType.CarStuffContainer || carPartConfig.PartType == CarPartType.WheelsContainer) { 
            RemoveCurrentCarPart();
            _sceneData.Value._inventoryUIView._carPartsDownContainer.GetComponent<CellView>()._gridPosition = -Vector2Int.one;
            ref var carCmp = ref _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity );
            var partSize = GridUtils.GetCarPartSize(carPartConfig);
            int carPartEntity = _world.Value.NewEntity();
            ref var carPartCmp  = ref _carPartComponentsPool.Value.Add(carPartEntity);
            ref var carPartInfoCmp  = ref _carPartInfoComponentsPool.Value.Add(carPartEntity);
           /* if (carPartConfig is WheelsContainerConfig wheelConfig) {
                carCmp.CurrentMaxWeight += wheelConfig.MaxWeight;
                Debug.Log(carCmp.CurrentMaxWeight);
            }*/
           carPartInfoCmp.ModuleIds = new int[moduleIds.Length];
           Array.Copy(moduleIds,carPartInfoCmp.ModuleIds,moduleIds.Length);
           
           carPartInfoCmp.PartLevel = level;
           carPartCmp.MaxHealth = carPartConfig.MaxHealth*GridUtils.GetHealthLevelMultiplier(carPartInfoCmp.PartLevel);
           carPartInfoCmp.CurrentHealth = health;
            carPartCmp.CarPartCellImage = _sceneData.Value.GetCarPartUIImage(_sceneData.Value._inventoryUIView._carPartsDownContainer,
                _sceneData.Value._inventoryUIView._carPartsDownContainer.position);
            carPartCmp.CarPartCellImage.transform.localPosition = new Vector2(_sceneData.Value._inventoryUIView._carPartsDownContainer.sizeDelta.x / 2,
               _sceneData.Value._inventoryUIView._carPartsDownContainer.sizeDelta.y / 2);
            carPartCmp.CarPartCellImage.rectTransform.sizeDelta = _sceneData.Value._inventoryUIView._carPartsDownContainer.sizeDelta;
            carPartCmp.CarPartCellImage.sprite = carPartConfig.PartSprite;
            carCmp.CarPartContainerGroupsGrid = new int[partSize.x, partSize.y];
            carCmp.CarPartGroupsGrid = new int[partSize.x, partSize.y];
            for (int i = 0; i < carCmp.CarPartContainerGroupsGrid.GetLength(0); i++)
            {
                for (int j = 0; j < carCmp.CarPartContainerGroupsGrid.GetLength(1); j++)
                    carCmp.CarPartContainerGroupsGrid[i, j] = carPartEntity;
            }
            SetCarContainersContainerSize();
            carCmp.CarPartContainerGroupsList.Add(carPartEntity,carPartConfig.PartForm.ToList());
            carPartInfoCmp.CarPartId = carPartConfig.Id;
            
            UpdateCarDescription(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity);
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
      //  int partIndex;
        if (!isBuyCell) {
            int cellId = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CellId;

            if (inventoryCmp.CurrentMarkedCellForPanelInfo == _sceneData.Value._inventoryUIView._inventoryCellViews[cellId]._entity)
                CloseShowInfoPanel();

            int partsInInventoryIndex = inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length +
                                        cellId;
           
            int cellEntity = GetSortedCarPartList()[partsInInventoryIndex];
           _inventoryCellComponentsPool.Value.Del(cellEntity);
           _carPartInfoComponentsPool.Value.Del(cellEntity);
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

         //   partIndex = shopCellCmp.CurrentBuyItemId;
        }
    }
    private void SetCarPartContainer(Vector2 edgePosition) { 
        (var carPartConfig, int level, float health, int[] moduleIds) =GetCurrentDraggedCellCarPartWithLevel();

        if (carPartConfig.PartType == CarPartType.CarStuffContainer || carPartConfig.PartType == CarPartType.WheelsContainer) {  
            RemoveCurrentCarPart();   
            int carPartEntity = _world.Value.NewEntity();
            Vector2Int currentGridPosition = GetCurrentDragCarPartContainerGridPosition(edgePosition);
           
            var newFormCellPositions = GridUtils.GetCellsFromField(currentGridPosition, carPartConfig,0);
            int xOffsetUp = 0;
            int xOffsetDown = 0;
            int yOffsetUp = 0;
            int yOffsetDown = 0;
            Vector2Int carPartSize = GridUtils.GetCarPartSize(carPartConfig);
            ref var carCmp = ref _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity );
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
            if (carPartConfig is WheelsContainerConfig wheelConfig) {
                carCmp.CurrentMaxWeight += wheelConfig.MaxWeight* GridUtils.GetMaxWeightLevelMultiplier(level);
                Debug.Log(carCmp.CurrentMaxWeight);
            }
            carPartContainerGridSize = new(carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1));
            carCmp.CarPartGroupsGrid = new int[carPartContainerGridSize.x, carPartContainerGridSize.y];
            if (xOffsetDown < 0)
                currentGridPosition.x -= xOffsetDown;
            if (yOffsetDown < 0)
                currentGridPosition.y -= yOffsetDown;

            
            SetCarContainersContainerSize();
            Vector2 gridCellNewSize = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).OnCarCellSize;
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

            ref var carPartCmp = ref _carPartComponentsPool.Value.Add(carPartEntity);
            ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Add(carPartEntity);
            carPartInfoCmp.ModuleIds = new int[moduleIds.Length];
            Array.Copy(moduleIds,carPartInfoCmp.ModuleIds,moduleIds.Length);
            carPartInfoCmp.CurrentHealth = health;
           carPartCmp.MaxHealth = carPartConfig.MaxHealth*GridUtils.GetHealthLevelMultiplier(carPartInfoCmp.PartLevel);
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
                CreateCarPartContainerOnComplete(carPartEntity, finalPosition, carPartConfig, gridCellNewSize, carPartSize,level);
            });
          //  carPartSequence.Play();
        } else {
            CancelCarPartPlace(ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity));
        }

        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (inventoryCmp.CurrentMarkedCellForPanelInfo != 0)
            SetCarPartInfoPanel(inventoryCmp.CurrentMarkedCellForPanelInfo);
        _sceneData.Value._subGridTransform.gameObject.SetActive(false);
        _sceneData.Value._gridMaskRect.gameObject.SetActive(false);
        
    }

    private void SetNewGridsFromLists(int xOffsetDown, int yOffsetDown, Vector2 gridCellNewSize) {
        ref var carCmp =
            ref _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity);
        foreach (var cellGroup in carCmp.CarPartContainerGroupsList) {
            for (int i = 0; i < cellGroup.Value.Count; i++) {
                Vector2Int newPos = carCmp.CarPartContainerGroupsList[cellGroup.Key][i];
                if (xOffsetDown != 0)
                    newPos.x -= xOffsetDown;
                if (yOffsetDown != 0)
                    newPos.y -= yOffsetDown;
                carCmp.CarPartContainerGroupsList[cellGroup.Key][i] = newPos;
                carCmp.CarPartContainerGroupsGrid[newPos.x, newPos.y] = cellGroup.Key;
            }

            ref var carPartOldCmp = ref _carPartComponentsPool.Value.Get(cellGroup.Key);
            ref var carPartInfoOldCmp = ref _carPartInfoComponentsPool.Value.Get(cellGroup.Key);
            var oldCarPartConfig = GetCarPartConfig(carPartInfoOldCmp.CarPartId);
            var oldCarPartSize = GridUtils.GetCarPartSize(oldCarPartConfig);
            Vector2 oldFiguresOffset = (oldCarPartSize - Vector2Int.one) * gridCellNewSize *
                                       _sceneData.Value._inventoryUIView._carPartsContainer.localScale.x;
            if (oldCarPartSize != Vector2Int.one)
                oldFiguresOffset = new Vector2(oldCarPartSize.x - 1, oldCarPartSize.y - 1) / 2 * gridCellNewSize *
                                   _sceneData.Value.mainCanvas.localScale.x * _sceneData.Value._inventoryUIView._carPartsContainer.localScale.x;
            Sequence returnSequence = DOTween.Sequence();
            returnSequence.Append(carPartOldCmp.CarPartCellImage.rectTransform.DOSizeDelta(gridCellNewSize * oldCarPartSize, 0.2f)
                .SetEase(Ease.InOutQuad));
            returnSequence.Join(carPartOldCmp.CarPartCellImage.transform
                .DOMove(
                    GetWorldPositionFromGrid(GridUtils.GetEdgePointFromCarPart(carCmp.CarPartContainerGroupsList[cellGroup.Key])) +
                    oldFiguresOffset, 0.2f).SetEase(Ease.InOutQuad));
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
            ref var carPartInfoOldCmp = ref _carPartInfoComponentsPool.Value.Get(carParts.Key);
            ref var carPartOldCmp = ref _carPartComponentsPool.Value.Get(carParts.Key);
            var oldCarPartConfig = GetCarPartConfig(carPartInfoOldCmp.CarPartId);
            var oldCarPartSize = GridUtils.GetCarPartSize(oldCarPartConfig);
            Sequence returnSequence = DOTween.Sequence();
            returnSequence.Append(carPartOldCmp.CarPartCellImage.rectTransform.DOSizeDelta(gridCellNewSize * oldCarPartSize, 0.2f)
                .SetEase(Ease.InOutQuad));
            if (carPartOldCmp.RotateDegrees == 90 || carPartOldCmp.RotateDegrees == 270)
                oldCarPartSize = new Vector2Int(oldCarPartSize.y, oldCarPartSize.x);
            Vector2 oldFiguresOffset = (oldCarPartSize - Vector2Int.one) * gridCellNewSize *
                                       _sceneData.Value._inventoryUIView._carPartsContainer.localScale.x;
            if (oldCarPartSize != Vector2Int.one)
                oldFiguresOffset = new Vector2(oldCarPartSize.x - 1, oldCarPartSize.y - 1) / 2 * gridCellNewSize *
                                   _sceneData.Value.mainCanvas.localScale.x * _sceneData.Value._inventoryUIView._carPartsContainer.localScale.x;

            returnSequence.Join(carPartOldCmp.CarPartCellImage.transform
                .DOMove(GetWorldPositionFromGrid(GridUtils.GetEdgePointFromCarPart(carCmp.CarPartGroupsList[carParts.Key])) + oldFiguresOffset,
                    0.2f).SetEase(Ease.InOutQuad));
            if (GridUtils.IsWeaponType(oldCarPartConfig)) {
                var currentImage = oldCarPartConfig.PartType == CarPartType.Gun
                    ? _gunComponentsPool.Value.Get(carParts.Key).GunSpriteImage
                    : _meleeWeaponComponentsPool.Value.Get(carParts.Key).MeleeSpriteImage;
                if (currentImage == null) continue;
                (Sprite upSprite, Vector2 upSize, Vector2 upPosition) =
                    GetUpperPartSizesForUI(gridCellNewSize * oldCarPartSize, oldCarPartConfig);

                returnSequence.Join(currentImage.rectTransform.DOSizeDelta(upSize, 0.2f).SetEase(Ease.InOutQuad));
                returnSequence.Join(currentImage.transform.DOLocalMove(upPosition, 0.2f).SetEase(Ease.InOutQuad));
            }
        }
    }

    private void CreateCarPartContainerOnComplete(int carPartEntity, Vector3 finalPosition, CarPartConfig carPartConfig,
        Vector2 gridCellNewSize, Vector2Int carPartSize, int level) {
        _sceneData.Value._inventoryUIView._draggedInventoryCell.gameObject.SetActive(false);
        _sceneData.Value._gridMaskRect.gameObject.SetActive(false);
        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartEntity);
        ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(carPartEntity);
        var container = _sceneData.Value._inventoryUIView._carPartsDownContainer;
        carPartCmp.CarPartCellImage = _sceneData.Value.GetCarPartUIImage(container, container.position);
        carPartCmp.CarPartCellImage.transform.localPosition = new Vector2(container.sizeDelta.x / 2, container.sizeDelta.y / 2);
        carPartCmp.CarPartCellImage.transform.position = finalPosition;
        carPartInfoCmp.CarPartId = carPartConfig.Id;
        carPartInfoCmp.PartLevel = level;
        carPartCmp.CarPartCellImage.rectTransform.sizeDelta = gridCellNewSize * carPartSize;
        carPartCmp.CarPartCellImage.sprite = carPartConfig.PartSprite;
        UpdateCarDescription(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity);
        
        if(GetSortedCarPartList().Count == 0 && CanUseThisCarPartContainers())
            SetCarPartRedactorMode();
    }

    private CarPartType[] _firstCarPartTypesForShop = new[] {
        CarPartType.Armor,
        CarPartType.Gun,
        CarPartType.MeleeWeapon
    };

    private void SetShopCells(bool isShopSaleRound) {
        var shopCmp = _shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        bool isModulesMode = shopCmp.isModulesShop;
        List<int> shopItems = new();
        for (int i = 0; i < _sceneData.Value._shopUIView._buttonsShop.Length; i++) {
            int randomItem;
            if (isModulesMode) {
                randomItem = shopCmp.ShopModulesIds[Random.Range(0, shopCmp.ShopModulesIds.Count)];
            } else {
                int rareIndex = GridUtils.GetRandomIndexFromChancesList(shopCmp.CurrentShopItemsChances);

                if (_firstCarPartTypesForShop.Length > i) {
                    List<int> currentItems = new List<int>();
                    foreach (var shopItem in shopCmp.ListShopItemsForRares[rareIndex]) {
                        if (GetCarPartConfig(shopItem).PartType == _firstCarPartTypesForShop[i])
                            currentItems.Add(shopItem);
                    }

                    if (currentItems.Count > 0)
                        randomItem = currentItems[Random.Range(0, currentItems.Count)];
                    else
                        randomItem = shopCmp.ListShopItemsForRares[rareIndex][Random.Range(0, shopCmp.ListShopItemsForRares[rareIndex].Count)];
                } else
                    randomItem = shopCmp.ListShopItemsForRares[rareIndex][Random.Range(0, shopCmp.ListShopItemsForRares[rareIndex].Count)];
            }

            shopItems.Add(randomItem);
        }

        int saleIndex = Random.Range(0, _sceneData.Value._shopUIView._buttonsShop.Length);
        for (int i = 0; i < _sceneData.Value._shopUIView._buttonsShop.Length; i++) {
            var shopButton = _sceneData.Value._shopUIView._buttonsShop[i];
            if (shopButton.LockToggle.isOn) continue;
            int randomItemIndex = Random.Range(0, shopItems.Count);
            var randomItem = shopItems[randomItemIndex];
            shopItems.RemoveAt(randomItemIndex);

            ref var shopCellCmp = ref _shopCellComponentsPool.Value.Get(shopButton.Entity);
            var carPartConfig = isModulesMode ? null : GetCarPartConfig(randomItem);
            var moduleConfig = isModulesMode ? _betweenScenesService.Value._carStuffListsConfig.ModuleConfigs[randomItem] : null;
            int price = isModulesMode ? (int)moduleConfig.Cost : (int)carPartConfig.MoneyCost; //maybe mult cost for smth round pow 
            if (i == saleIndex || isShopSaleRound) {
                float saleSize = isShopSaleRound ? Random.Range(2, 15) * 5 : Random.Range(1, 7) * 5;
                price = Mathf.Max(1, Mathf.FloorToInt(price * (1f - saleSize / 100f)));
                shopButton.SetShopSaleInfo((int)saleSize);
            } else
                shopButton.SetShopSaleInfo(0);

            shopCellCmp.CurrentBuyItemId = randomItem;
            var downSprite = !isModulesMode ? carPartConfig.PartSprite : moduleConfig.ModuleIcon;
            if (!isModulesMode && (carPartConfig.PartType == CarPartType.Gun || carPartConfig.PartType == CarPartType.MeleeWeapon)) {
                (Sprite upSprite, Vector2 size, Vector2 position) =
                    GetUpperPartSizesForUI(shopButton.ItemSprite.rectTransform.sizeDelta, carPartConfig);
                shopButton.SetShopCellInfo(price, downSprite, upSprite, position, size);
            } else
                shopButton.SetShopCellInfo(price, downSprite, _sceneData.Value._transparentPixel, Vector2.zero, Vector2.zero);

            shopCellCmp.Price = price;
        }
    }
    
     private void SetCarShopCells() {
         var shopCmp = _shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

         var currentMapConfig = _betweenScenesService.Value.GetMapConfig().GameProcessConfig;
         var roundEnemyStuff =
             currentMapConfig.GameRounds[Mathf.Max(_currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRound - 2, 0)];
         var carsList = _betweenScenesService.Value.GetMapConfig().EnemyMapConfigs;
         int saleIndex = Random.Range(0, _sceneData.Value._shopUIView._buttonsCarShop.Length);
        for (int i = 0; i < _sceneData.Value._shopUIView._buttonsCarShop.Length; i++) {
            if (_sceneData.Value._shopUIView._buttonsCarShop[i].LockToggle.isOn) continue;
            var carMapsList = carsList[GridUtils.GetRandomIndexFromChancesList(roundEnemyStuff.EnemyCarChances)].CarMapsList;
            var carMap = carMapsList[Random.Range(0, carMapsList.Count)];
            
            var shopButton = _sceneData.Value._shopUIView._buttonsCarShop[i];

            ref var shopCellCmp = ref _shopCellCarComponentsPool.Value.Get(shopButton.Entity);
            shopCellCmp.CarMap = carMap;

            foreach (var carPartImage in shopCellCmp.CarPartsImages) 
                _sceneData.Value.ReleaseCarPartUIImage(carPartImage);
            
            shopCellCmp.CarPartsImages.Clear();
            int price = GetCarCost(carMap); 
            if (i == saleIndex) {
                float saleSize = Random.Range(1, 7) * 5;
                price = Mathf.Max(1, Mathf.FloorToInt(price * (1f - saleSize / 100f)));
                shopButton.SetShopSaleInfo((int)saleSize);
            } else 
                shopButton.SetShopSaleInfo(0);

            
            shopButton.SetPriceText(price);

            shopCellCmp.Price = price;
            SetShopCarContainersContainerSize(shopButton.UpperItemSprite, shopCellCmp.CarMap.GridSize);
            SetCarUIForShopButton(shopButton.Entity);
        }
    }
     
    private void SetShopCarContainersContainerSize(Image container, Vector2Int gridSize) {
        float containerSizeX = gridSize.x > gridSize.y
            ? 200f
            : 200f * ((float)gridSize.x / gridSize.y);
        float containerSizeY = gridSize.x < gridSize.y
            ? 200f
            : 200f * ((float)gridSize.y / gridSize.x);
        container.rectTransform.sizeDelta = new Vector2(containerSizeX, containerSizeY);
        container.transform.localPosition = new Vector2(
            (200 - containerSizeX)  / 2 - 101.1f,
            (200 - containerSizeY)  / 2 -101.1f);
    }

    private void SetCarUIForShopButton(int cellEntity) {
        ref var shopCellCmp = ref _shopCellCarComponentsPool.Value.Get(cellEntity);

        var shopButton = _sceneData.Value._shopUIView._buttonsCarShop[shopCellCmp.ShopCarButtonIndex];
        float containerSize = Math.Max(shopButton.UpperItemSprite.rectTransform.sizeDelta.x,
            shopButton.UpperItemSprite.rectTransform.sizeDelta.y);
        var cellSize = Vector2.one * containerSize / Math.Max(shopCellCmp.CarMap.GridSize.x, shopCellCmp.CarMap.GridSize.y);
        foreach (var carPartInfo in shopCellCmp.CarMap.CarPartDownLevelGrid) {
            SetCarPartDownImageTransform(carPartInfo.gridPosition, cellEntity, cellSize, shopButton.UpperItemSprite.transform,
                carPartInfo.rotate, carPartInfo.partId);
        }

        foreach (var carPartInfo in shopCellCmp.CarMap.CarPartUpLevelGrid) {
            SetCarPartDownImageTransform(carPartInfo.gridPosition, cellEntity, cellSize, shopButton.UpperItemSprite.transform,
                carPartInfo.rotate, carPartInfo.partId);
        }
    }

    private int GetCarCost(EnemyCarPartsMaps carMap) {
        float carCost = 0;
        var carConfig = _sceneData.Value.GetCarConfig(carMap.CarId);
        carCost = Mathf.FloorToInt(carConfig.CarUpgradeCost * 0.3f * carMap.CarLevel * (carMap.CarLevel + 1) / 2);
        foreach (var carPartInfo in carMap.CarPartDownLevelGrid) {
            var carPartConfig = GetCarPartConfig(carPartInfo.partId);
            carCost += Mathf.FloorToInt(carPartConfig.MoneyCost +
                                        carPartConfig.UpgradedPartCost * 0.3f * carPartInfo.level * (carPartInfo.level + 1) / 2);
        }
        foreach (var carPartInfo in carMap.CarPartUpLevelGrid) {
            var carPartConfig = GetCarPartConfig(carPartInfo.partId);
            carCost += Mathf.FloorToInt(carPartConfig.MoneyCost +
                                        carPartConfig.UpgradedPartCost * 0.3f * carPartInfo.level * (carPartInfo.level + 1) / 2);
        }

        return (int)carCost;
    }

    private void SellCarButtonAction(int indexButton) {
        SellCar(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).PlayerTeammateCarsEntity[indexButton]);
    }

    private async void SellCar(int carEntity) {
        
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (inventoryCmp.CurrentRedactedCarEntity == carEntity) {
            if (!CanUseThisCarPartContainers()) {
                _sceneData.Value.PlayWarningAnimation(GetLocalization("The car is unstable and needs wheels") + "!", false);
                return;
            } else if (!MainPartIsPlaced()) {
                _sceneData.Value.PlayWarningAnimation(GetLocalization("Place the engine and driver's cabin on the car") + "!", false);
                return;
            }
        }

        if (inventoryCmp.SelectCarButtonsNotInteractable) return;
        if (inventoryCmp.CurrentRedactedCarEntity == carEntity)
            ClearAndSetNewCarUI(inventoryCmp.PlayerTeammateCarsEntity[0]);

        _sceneData.Value._inventoryUIView._selectCarButtons[inventoryCmp.PlayerTeammateCarsEntity.Count - 1].gameObject.SetActive(false);
        if (inventoryCmp.PlayerTeammateCarsEntity.Count == 2)
            _sceneData.Value._inventoryUIView._selectCarButtons[0].gameObject.SetActive(false);
        SetMoneyCount(GetCarCost(carEntity));
        _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).SelectCarButtonsNotInteractable = true;
        await Task.Delay(500);
        var carCmp = _carComponentsPool.Value.Get(carEntity);
        foreach (var carPartInfo in carCmp.CarPartContainerGroupsList) {
            _world.Value.DelEntity(carPartInfo.Key);
        }

        foreach (var carPartInfo in carCmp.CarPartGroupsList) {
            _world.Value.DelEntity(carPartInfo.Key);
        }

        _world.Value.DelEntity(carEntity);

        DeleteCar(carEntity);
    }

    private void DeleteCar(int carEntity) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.SelectCarButtonsNotInteractable = false;
        inventoryCmp.PlayerTeammateCarsEntity.Remove(carEntity);

        for (int i = 0; i < _sceneData.Value._inventoryUIView._selectCarButtons.Length; i++) {
            if (i < inventoryCmp.PlayerTeammateCarsEntity.Count && i != 0) {
                _sceneData.Value._inventoryUIView._selectCarButtons[i].sellCarText.text =
                    GetCarCost(inventoryCmp.PlayerTeammateCarsEntity[i]) + "$";
            }
        }
    }
    private float GetCarCost(int carEntity) {
        float carCost = 0;
        ref var carCmp = ref _carComponentsPool.Value.Get(carEntity);
       // ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);
        carCost = Mathf.FloorToInt(carConfig.CarUpgradeCost * 0.3f * carCmp.CarLevel * (carCmp.CarLevel + 1) / 2);
        foreach (var carPartInfo in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPartInfo.Key);
            var carPartConfig = GetCarPartConfig(carPartCmp.CarPartId);
            carCost += Mathf.FloorToInt(carPartConfig.MoneyCost +
                                        carPartConfig.UpgradedPartCost * 0.3f * carPartCmp.PartLevel * (carPartCmp.PartLevel + 1) / 2);
        }

        foreach (var carPartInfo in carCmp.CarPartGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPartInfo.Key);
            var carPartConfig = GetCarPartConfig(carPartCmp.CarPartId);
            carCost += Mathf.FloorToInt(carPartConfig.MoneyCost +
                                        carPartConfig.UpgradedPartCost * 0.3f * carPartCmp.PartLevel * (carPartCmp.PartLevel + 1) / 2);
        }

        return carCost*0.5f;
    }

    private void BuyShopItem(int entity) {
        var shopCellCmp = _shopCellComponentsPool.Value.Get(entity);
        if (RemoveMoneyForBuy(shopCellCmp))
            return;

        if (_shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).isModulesShop) {
            AddModuleInventoryItem(shopCellCmp.CurrentBuyItemId);
        } else {
            var carPartConfig = GetCarPartConfig(shopCellCmp.CurrentBuyItemId);
            var moduleIds = new int[(int)carPartConfig.RartRare + 1];
            Array.Fill(moduleIds, -1);
            AddInventoryItem(shopCellCmp.CurrentBuyItemId, 0, carPartConfig.MaxHealth, moduleIds);
        }
    }
    
    private void BuyShopCar(int entity) {
        var shopCellCmp = _shopCellCarComponentsPool.Value.Get(entity);
        if (_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).PlayerTeammateCarsEntity.Count > 4) {
            _sceneData.Value.PlayWarningAnimation(GetLocalization("No space in garage"), false);
           return; 
        }
            
        if (RemoveMoneyForBuy(shopCellCmp)) 
            return;

        AddNewTeammateCarToPlayer(shopCellCmp.CarMap);
    }

    private bool RemoveMoneyForBuy(ShopCellCarComponent shopCellCmp) {
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (shopCellCmp.Price > playerCurrencyCmp.MoneyCount) {
            _sceneData.Value._shopUIView.NotEnoughMoneyAnimation();
            return true;
        }

        _sceneData.Value._shopUIView._buttonsCarShop[shopCellCmp.ShopCarButtonIndex].SoldItemChangeButton();

        SetMoneyCount(-shopCellCmp.Price);
        return false;
    }

    private bool RemoveMoneyForBuy(ShopCellComponent shopCellCmp) {
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (shopCellCmp.Price > playerCurrencyCmp.MoneyCount) {
            _sceneData.Value._shopUIView.NotEnoughMoneyAnimation();
            return true;
        }

        _sceneData.Value._shopUIView._buttonsShop[shopCellCmp.ShopButtonIndex].SoldItemChangeButton();

        SetMoneyCount(-shopCellCmp.Price);
        return false;
    }

    private int GetCarPartEntityFromField() {
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var carCmp = _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity);
        var gridPosition = GetCurrentDragCarPartContainerGridPosition(Input.mousePosition);
        bool redactCarPartMode = inventoryCmp.IsCarPartsRedactorMode;
        if ((gridPosition.y < 0 && gridPosition.y >= carCmp.CarPartContainerGroupsGrid.GetLength(1) && gridPosition.x < 0 &&
             gridPosition.x >= carCmp.CarPartContainerGroupsGrid.GetLength(0))) return 0;
        int carPartEntity = 0;
        if (redactCarPartMode && carCmp.CarPartGroupsGrid != null && carCmp.CarPartGroupsGrid[gridPosition.x, gridPosition.y] != 0)
            carPartEntity = carCmp.CarPartGroupsGrid[gridPosition.x, gridPosition.y];
        else if (carCmp.CarPartContainerGroupsGrid[gridPosition.x, gridPosition.y] != 0)
            carPartEntity = carCmp.CarPartContainerGroupsGrid[gridPosition.x, gridPosition.y];
        return carPartEntity;
    }

    private void UpgradeMarkedCarPartLevel() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        // made -999 entity when tap on field grid
        if (_carPartComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
            var carPartConfig = _sceneData.Value.GetCarPartConfig(_sceneData.Value._carPartInfoUIView._currentCarPartConfig.Id);

            int carPartEntity = inventoryCmp.CurrentMarkedCellForPanelInfo;
            if (carPartEntity == 0) return;

            ref var carInfoPartCmp = ref _carPartInfoComponentsPool.Value.Get(carPartEntity);
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartEntity);
            float upgradeCost = GridUtils.GetUpgradeCost(carInfoPartCmp.PartLevel, carPartConfig, carInfoPartCmp.ModuleIds);
            if (playerCurrencyCmp.MoneyCount < upgradeCost) {
                _sceneData.Value._shopUIView.NotEnoughMoneyAnimation();
                return;
            }

            // playerCurrencyCmp.MoneyCount -= carPartConfig.UpgradedPartCost;
            float addedHealth = carPartConfig.MaxHealth *
                                (GridUtils.GetHealthLevelMultiplier(carInfoPartCmp.PartLevel + 1) -
                                 GridUtils.GetHealthLevelMultiplier(carInfoPartCmp.PartLevel)) *
                                GridUtils.GetModulesMultiplyers(carInfoPartCmp.ModuleIds).healthMultiplayer;
            carInfoPartCmp.PartLevel++;
            carInfoPartCmp.CurrentHealth += addedHealth;
            carPartCmp.MaxHealth = carPartConfig.MaxHealth *GridUtils.GetHealthLevelMultiplier(carInfoPartCmp.PartLevel);
            SetCarPartInfoPanel(inventoryCmp.CurrentMarkedCellForPanelInfo, false);
             
            SetMoneyCount(-upgradeCost);
            UpdateCarDescription(inventoryCmp.CurrentRedactedCarEntity);
        } else if (_inventoryCellComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
            ref var invCellCmp = ref _carPartInfoComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo);
            
            var carPartConfig = _sceneData.Value.GetCarPartConfig(invCellCmp.CarPartId);
            float upgradeCost = GridUtils.GetUpgradeCost( _sceneData.Value._carPartInfoUIView._level, carPartConfig,invCellCmp.ModuleIds);
            if (playerCurrencyCmp.MoneyCount < upgradeCost) {
                _sceneData.Value._shopUIView.NotEnoughMoneyAnimation();
                return;
            }
            
            SetMoneyCount(-upgradeCost);
            float addedHealth = carPartConfig.MaxHealth *
                                (GridUtils.GetHealthLevelMultiplier(invCellCmp.PartLevel+ 1) - GridUtils.GetHealthLevelMultiplier(invCellCmp.PartLevel ));
            invCellCmp.PartLevel++;
            invCellCmp.CurrentHealth += addedHealth;
            _sceneData.Value._carPartInfoUIView._level = invCellCmp.PartLevel;
            
            SetCarPartInfoPanel(inventoryCmp.CurrentMarkedCellForPanelInfo, false);
            SetInventoryCells();
        }  
    }
    
  /*  private void UpgradeMarkedCarPart() {
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
            SetCarPartInfoPanel(inventoryCmp.CurrentMarkedCellForPanelInfo);
            playerCurrencyCmp.moneyCount -= carPartConfig.UpgradedPartCost;
            SetMoneyText();
            UpdateCarDescription();
        } else if (_inventoryCellComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
            var invCellCmp = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo);
 
            (int needCarPartId,int level )=
                GetSortedCarPartList()[
                    inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length + invCellCmp.CellId];
            var carPartConfig = _sceneData.Value.GetCarPartConfig(needCarPartId);
            playerCurrencyCmp.moneyCount -= carPartConfig.UpgradedPartCost;
            SetMoneyText();

            var upgradedCarPartConfig = _sceneData.Value.GetCarPartConfig(carPartConfig.UpgradedPartId);
            int newLevel = Mathf.Max(Mathf.CeilToInt(level * ((float)carPartConfig.UpgradedPartCost / upgradedCarPartConfig.UpgradedPartCost)),
                1);
            if (newLevel != 1)
                //refund some money
                inventoryCmp.PartsInInventory[inventoryCmp.PartsInInventory.IndexOf((needCarPartId, level))] =
                    (upgradedCarPartConfig.Id, newLevel);

            SetInventoryCells();
            CloseShowInfoPanel();
        }  
    }*/

    private void SellMarkedCarPart() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        if (_moduleInventoryItemsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo))
            SellModule(inventoryCmp.CurrentMarkedCellForPanelInfo);
        else
            SellNeededCarPart(inventoryCmp.CurrentMarkedCellForPanelInfo, true);
    }

    private void SellModule(int entity) {
       var moduleCmp = _moduleInventoryItemsPool.Value.Get(entity);
        SetMoneyCount(_betweenScenesService.Value._carStuffListsConfig.ModuleConfigs[moduleCmp.ModuleId].Cost);
        _moduleInventoryItemsPool.Value.Del(entity);
        CloseShowInfoPanel();
        SetInventoryCells();
    }

    private void SellNeededCarPart(int carPartEntity, bool giveMoney) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        if (_inventoryCellComponentsPool.Value.Has(carPartEntity)) {
            _sceneData.Value._carPartInfoUIView._switchButtonsContainer.gameObject.SetActive(false);
            ref var carPartCmp = ref _carPartInfoComponentsPool.Value.Get(carPartEntity);

            _inventoryCellComponentsPool.Value.Del(carPartEntity);
            if (inventoryCmp.CurrentInventoryPage != 0 &&
                !_sceneData.Value._inventoryUIView._inventoryCellViews[1].gameObject.activeInHierarchy)
                inventoryCmp.CurrentInventoryPage--;
            var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartCmp.CarPartId);
            if (giveMoney)
                SetMoneyCount(GridUtils.GetSellValueForCarPart(carPartConfig, carPartCmp.PartLevel, carPartCmp.ModuleIds));
            SetInventoryCells();
            _carPartInfoComponentsPool.Value.Del(carPartEntity);
        } else if (_carPartComponentsPool.Value.Has(carPartEntity)) {
            ref var carCmp = ref _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity);
            if (carPartEntity == 0) return;

            ref var carPartCmp = ref _carPartInfoComponentsPool.Value.Get(carPartEntity);
            var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartCmp.CarPartId);
            if (giveMoney)
                SetMoneyCount(GridUtils.GetSellValueForCarPart(carPartConfig, carPartCmp.PartLevel, carPartCmp.ModuleIds));
            if (carPartConfig.PartType == CarPartType.WheelsContainer || carPartConfig.PartType == CarPartType.CarStuffContainer) {
                foreach (var carPartCellPosition in carCmp.CarPartContainerGroupsList[carPartEntity]) {
                    carCmp.CarPartContainerGroupsGrid[carPartCellPosition.x, carPartCellPosition.y] = 0;
                }

                carCmp.CarPartContainerGroupsList.Remove(carPartEntity);

                if (carPartConfig is WheelsContainerConfig wheelConfig)
                    carCmp.CurrentMaxWeight -= wheelConfig.MaxWeight * GridUtils.GetMaxWeightLevelMultiplier(carPartCmp.PartLevel);
            } else {
                foreach (var carPartCellPosition in carCmp.CarPartGroupsList[carPartEntity]) {
                    carCmp.CarPartGroupsGrid[carPartCellPosition.x, carPartCellPosition.y] = 0;
                }

                carCmp.CarPartGroupsList.Remove(carPartEntity);
            }

            _sceneData.Value.DestroyGameobject(_carPartComponentsPool.Value.Get(carPartEntity).CarPartCellImage.transform);

            _world.Value.DelEntity(carPartEntity);
        }

        CloseShowInfoPanel();
    }

    private void MoveCarPartContainerToInventory(int entity) {
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var carCmp = ref _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity);
        foreach (var carPartCellPosition in carCmp.CarPartContainerGroupsList[entity]) {
            carCmp.CarPartContainerGroupsGrid[carPartCellPosition.x, carPartCellPosition.y] = 0;
            if (carCmp.CarPartGroupsGrid[carPartCellPosition.x, carPartCellPosition.y] != 0)
                MoveToInventoryCellNeededCarPart(carCmp.CarPartGroupsGrid[carPartCellPosition.x, carPartCellPosition.y]);
        }

        MoveToInventoryCellNeededCarPart(entity);

        if (carCmp.CarPartContainerGroupsList.Count == 0) {
            carCmp.CarPartContainerGroupsGrid = new int[3, 3];
            _sceneData.Value._inventoryUIView._carPartsDownContainer.gameObject.GetComponent<CellView>()._gridPosition = -Vector2Int.one * 999;
            SetCarContainersContainerSize();
            return;
        }

        (int xOffsetUp, int yOffsetUp, int xOffsetDown, int yOffsetDown) = GetNonZeroOffsets(carCmp.CarPartContainerGroupsGrid);
        if (xOffsetUp != 0 || yOffsetUp != 0 || xOffsetDown != 0 || yOffsetDown != 0)
            carCmp.CarPartContainerGroupsGrid =
                new int[carCmp.CarPartContainerGroupsGrid.GetLength(0) - Mathf.Abs(yOffsetUp) - Mathf.Abs(yOffsetDown),
                    carCmp.CarPartContainerGroupsGrid.GetLength(1) - Mathf.Abs(xOffsetUp) - Mathf.Abs(xOffsetDown)];

        carCmp.CarPartGroupsGrid = new int[carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1)];
        SetCarContainersContainerSize();
        SetNewGridsFromLists(yOffsetUp, xOffsetUp, _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).OnCarCellSize);
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

    private void UpdateCarStats(int entity) {
          ref var carCmp = ref _carComponentsPool.Value.Get(entity);
        var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);

        (float baseSpeedMultiplier, float baseRotateSpeedMultiplier, float baseAccelerationMultiplier, float baseMaxFuelMultiplier) =
            GridUtils.GetCarLevelMultipliers(carCmp.CarLevel);

        float defaultMaxCarSpeed = carConfig.DefaultMaxSpeed + baseSpeedMultiplier;
        float carRotateSpeed = carConfig.RotateSpeed * baseRotateSpeedMultiplier;
        float carWeight = 0;
        float carMaxWeight = 0;
        
        ref var moveCmp = ref _movementComponentsPool.Value.Get(entity);
        
        float maxFuel = carConfig.DefaultMaxFuel * baseMaxFuelMultiplier;
        moveCmp.MaxBoostMoveSpeed = carConfig.BoostSpeedMultiplayer * baseAccelerationMultiplier;
        float defaultFuelConsumption = carConfig.FuelСonsumption;
        // ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var carPart in carCmp.CarPartGroupsList.Keys) {
            var carPartCellCmp = _carPartInfoComponentsPool.Value.Get(carPart);
            int carPartId = carPartCellCmp.CarPartId;
            var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartId);
            carWeight += carPartConfig.Weight;
            if (carPartConfig.PartType == CarPartType.Support) {
                var supportPartConfig = _sceneData.Value.GetSupportModuleFromCarPart(carPartId);
                (float firstMultiplayer,float secondMultiplayer)= GridUtils.GetSupportLevelMultipliers(carPartCellCmp.PartLevel, supportPartConfig.ModuleType);
                if (supportPartConfig.ModuleType == SupportModuleType.Fuel)
                    maxFuel += supportPartConfig.FirstCount*firstMultiplayer;
                else if (supportPartConfig.ModuleType == SupportModuleType.Turbine) {
                    defaultMaxCarSpeed += supportPartConfig.SecondCount*secondMultiplayer;
                    defaultFuelConsumption += supportPartConfig.FirstCount*firstMultiplayer;
                }
            }
        }
        ref var fuelCmp = ref _fuelComponentsPool.Value.Get(entity);
        fuelCmp.MaxFuel = maxFuel;
        fuelCmp.FuelConsumption = defaultFuelConsumption;
        foreach (var carPart in carCmp.CarPartContainerGroupsList.Keys) {
            var carPartCellCmp = _carPartInfoComponentsPool.Value.Get(carPart);
            int carPartId = carPartCellCmp.CarPartId;
            var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartId);
            carWeight += carPartConfig.Weight;
            if (carPartConfig is WheelsContainerConfig wheels)
                carMaxWeight += wheels.MaxWeight * GridUtils.GetMaxWeightLevelMultiplier(carPartCellCmp.PartLevel);
        }

        carCmp.CurrentWeight = carWeight;
        carCmp.CurrentMaxWeight = carMaxWeight;
        float weightFactor = carCmp.CurrentMaxWeight == 0 ? 0 : carWeight / carMaxWeight;
        defaultMaxCarSpeed *= 1 - weightFactor;
        carRotateSpeed *= 1 - weightFactor;
        defaultMaxCarSpeed = Mathf.Max(defaultMaxCarSpeed, 0);
        moveCmp.MaxBoostMoveSpeed += defaultMaxCarSpeed;
       
        moveCmp.MaxMoveSpeed = defaultMaxCarSpeed;
        moveCmp.RotateSpeed = carRotateSpeed;
    }

    private void UpdateCarDescription(int entity) {
        UpdateCarStats(entity);
        var moveCmp = _movementComponentsPool.Value.Get(entity);
        var carCmp = _carComponentsPool.Value.Get(entity);
        var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);
        var fuelCmp = _fuelComponentsPool.Value.Get(entity);
        _sceneData.Value._carPartInfoUIView.CarDescriptionText.text =
            $"{GetLocalization(carConfig.CarName)}\n {GetLocalization("Max move speed")}: {moveCmp.MaxMoveSpeed:F1} {GetLocalization("m/s")} " +
            $"\n {GetLocalization("Added boosted speed")}: {moveCmp.MaxBoostMoveSpeed:F1} " +
            $"{GetLocalization("m/s")} (+ {fuelCmp.FuelConsumption - carConfig.FuelСonsumption + carConfig.AccelerationFuelСonsumption} {GetLocalization("fuel consumption")})" +
            $" \n {GetLocalization("Default fuel consumption")} {fuelCmp.FuelConsumption} \n {GetLocalization("Rotate speed")}: {moveCmp.RotateSpeed} {GetLocalization("m/s")} " +
            $"\n {GetLocalization("Weight")}: {carCmp.CurrentWeight:F1}/{carCmp.CurrentMaxWeight} {GetLocalization("kg")} \n {GetLocalization("Fuel")}: {fuelCmp.MaxFuel}";
    }

    private void SetCarPartInfoPanel(int entity, bool isCanClose = true) {
        int needCarPartId = 0;
        bool showDestroyButton = false;
        bool showMoveButton = false;
        int level = 0;
        float healCost = 0;
        float currentHealth = 0;
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        int[] moduleIds = null;
        if (entity == -999 || _carPartComponentsPool.Value.Has(entity)) {
            int carPartEntity = entity == -999 ? GetCarPartEntityFromField() : inventoryCmp.CurrentMarkedCellForPanelInfo;
            if (carPartEntity == inventoryCmp.CurrentMarkedCellForPanelInfo && isCanClose) {
                CloseShowInfoPanel();
                return;
            }

            var cellCmp = _carPartInfoComponentsPool.Value.Get(carPartEntity);
            moduleIds = cellCmp.ModuleIds;
            if (inventoryCmp.CurrentMarkedCellForPanelInfo == entity && isCanClose) {
                CloseShowInfoPanel();
                return;
            }

            currentHealth = cellCmp.CurrentHealth;
            needCarPartId = cellCmp.CarPartId;
            
            var currentCarPartConfig = GetCarPartConfig(needCarPartId);
            healCost = GetHealCostForCarPart(_carPartComponentsPool.Value.Get(carPartEntity).MaxHealth, currentHealth, currentCarPartConfig);
            bool isContainer = GridUtils.IsContainerType(currentCarPartConfig);
            if (currentCarPartConfig.PartType != CarPartType.Main && !isContainer)
                showDestroyButton = true;
            showMoveButton = !isContainer || AreNonZeroAndNonDeletedConnected(
                _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity).CarPartContainerGroupsGrid, carPartEntity);
            level = cellCmp.PartLevel;
            _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(currentCarPartConfig.PartType != CarPartType.Main);
            _sceneData.Value._carPartInfoUIView._upgradeButton.enabled =
                GridUtils.GetUpgradeCost(level, currentCarPartConfig,moduleIds) <= playerCurrencyCmp.MoneyCount;
            inventoryCmp.CurrentMarkedCellForPanelInfo = carPartEntity;
        } 
        else if (_moduleInventoryItemsPool.Value.Has(entity)) {
            if (inventoryCmp.CurrentSelectedModuleCellId != -1) {
                EquipModuleToCarPart(entity);
                return;
            }
            if (inventoryCmp.CurrentMarkedCellForPanelInfo == entity && isCanClose) {
                CloseShowInfoPanel();
                return;
            }

            SetModuleInfoPanel(entity);
            return;
        }
        else if (_inventoryCellComponentsPool.Value.Has(entity)) {
           
            if (inventoryCmp.CurrentMarkedCellForPanelInfo == entity && isCanClose) {
                CloseShowInfoPanel();
                return;
            }

          //  int cellEntity =
         //       GetSortedCarPartList()[
              //      inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length + invCellCmp.CellId];
            var inventoryCellCmp = _carPartInfoComponentsPool.Value.Get(entity);
            needCarPartId = inventoryCellCmp.CarPartId;
            moduleIds = inventoryCellCmp.ModuleIds;
            level = inventoryCellCmp.PartLevel;
            currentHealth = inventoryCellCmp.CurrentHealth;  
            var currentCarPartConfig = GetCarPartConfig(needCarPartId);
            _sceneData.Value._carPartInfoUIView._switchButtonsContainer.gameObject.SetActive(false);
            _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(
                currentCarPartConfig.PartType != CarPartType.Main);
            _sceneData.Value._carPartInfoUIView._upgradeButton.enabled =
                GridUtils.GetUpgradeCost(level, currentCarPartConfig,moduleIds) <= playerCurrencyCmp.MoneyCount;
            inventoryCmp.CurrentMarkedCellForPanelInfo = entity;
            healCost = GetHealCostForCarPart(currentCarPartConfig.MaxHealth, currentHealth, currentCarPartConfig);
            if (currentCarPartConfig.PartType != CarPartType.Main && !GridUtils.IsContainerType(currentCarPartConfig))
                showDestroyButton = true;
        } else if (_shopCellComponentsPool.Value.Has(entity)) {
            if (inventoryCmp.CurrentMarkedCellForPanelInfo == entity && isCanClose) {
                CloseShowInfoPanel();
                return;
            }

            if (_shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).isModulesShop) {
                SetModuleInfoPanel(entity);
                return;
            }
            var shopCellCmp = _shopCellComponentsPool.Value.Get(entity);

            needCarPartId = shopCellCmp.CurrentBuyItemId;
            _sceneData.Value._carPartInfoUIView._switchButtonsContainer.gameObject.SetActive(false);
            _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(false);
            inventoryCmp.CurrentMarkedCellForPanelInfo = entity;
            currentHealth = GetCarPartConfig(shopCellCmp.CurrentBuyItemId).MaxHealth;
        }
        
        _sceneData.Value._carPartInfoUIView.gameObject.SetActive(true);
        _sceneData.Value._carPartInfoUIView.SetPanelForNeededPart(GetCarPartConfig(needCarPartId), showDestroyButton, showMoveButton, level,
            currentHealth, healCost, entity >= 0 && _shopCellComponentsPool.Value.Has(entity),moduleIds);
    }

    private void SetModuleInfoPanel(int entity) {
        var moduleCmp = _moduleInventoryItemsPool.Value.Get(entity);
        _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentMarkedCellForPanelInfo = entity;
        _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(false);
        _sceneData.Value._carPartInfoUIView.gameObject.SetActive(true);
        _sceneData.Value._carPartInfoUIView.SetPanelForModuleInfo(
            _betweenScenesService.Value._carStuffListsConfig.ModuleConfigs[moduleCmp.ModuleId]);
    }

    private void CloseShowInfoPanel() {
        _sceneData.Value._carPartInfoUIView.gameObject.SetActive(false);
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.CurrentMarkedCellForPanelInfo = 0;
        
        if(inventoryCmp.CurrentSelectedModuleCellId != -1)
            CancelModuleSelect();
    }
    
    private void SetVisibleMoneyCount(float needCount) {
        _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).VisibleMoneyCount = needCount;
    }

    private void SetMoneyCount(float count) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var playerCurrency = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        playerCurrency.VisibleMoneyCount = playerCurrency.MoneyCount;
        playerCurrency.MoneyCount += count;

        inventoryCmp.CurrentMoneyTween?.Kill();

        float visibleMoney = playerCurrency.VisibleMoneyCount;
        inventoryCmp.CurrentMoneyTween = DOTween.To(() => visibleMoney, x => visibleMoney = x, playerCurrency.MoneyCount, 1f)
            .SetEase(Ease.OutCubic).OnUpdate(() => {
                _sceneData.Value._shopUIView._moneyCountText.text = visibleMoney.ToString("N0") + "$";
                SetVisibleMoneyCount(visibleMoney);
            });

        // Анимация иконки
        _sceneData.Value._shopUIView.AddMoneyAnimation();
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
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var carCmp = _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity);

       
        return CanPlaceCarPartOnField(carDraggedCell, /*inventoryCmp.CurrentCellCarPart._gridPosition*/
            inventoryCmp.LastCheckedGridCellPosition, carCmp.CarPartGroupsGrid, carCmp.CarPartContainerGroupsGrid);
    }

    private bool CanPlaceCarPartOnField(CarPartConfig form, Vector2Int position, int[,] grid,
        int[,] gridContainers) /*need caar part containers grid*/ {
        var formCells = GridUtils.GetCellsFromField(position, form,
            _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRotateDegrees, grid);
        if (formCells.Count != form.PartForm.Length) return false;
        foreach (var cell in formCells) {
            if (grid[cell.x, cell.y] != 0 || gridContainers[cell.x, cell.y] == 0) return false;
        }

        return true;
    }

    private (CarPartConfig, int level, float health, int[] ) GetCurrentDraggedCellCarPartWithLevel() {
        int playerEntity = _sceneData.Value.PlayerEntity;

        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(playerEntity);
        if (_inventoryCellComponentsPool.Value.Has(inventoryCmp.CurrentDraggedCellEntity)) {
            int cellId = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CellId;
            int partsInInventoryIndex = inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length +
                                        cellId;
            var inventoryPartIndex = GetSortedCarPartList()[partsInInventoryIndex];
            var inventoryCellCmp = _carPartInfoComponentsPool.Value.Get(inventoryPartIndex);
            return (GetCarPartConfig(inventoryCellCmp.CarPartId),inventoryCellCmp.PartLevel, inventoryCellCmp.CurrentHealth, inventoryCellCmp.ModuleIds);
        } else {
            int cellId = _shopCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CurrentBuyItemId;
            var carPartConfig = GetCarPartConfig(cellId);
            var moduleIds = new int[(int)carPartConfig.RartRare + 1];
            Array.Fill(moduleIds, -1);
            return (carPartConfig, 1,carPartConfig.MaxHealth, moduleIds);
        }
        
    }

    private CarPartConfig GetCurrentDraggedCellCarPart() {
        int playerEntity = _sceneData.Value.PlayerEntity;

        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(playerEntity);
        if (_inventoryCellComponentsPool.Value.Has(inventoryCmp.CurrentDraggedCellEntity)) {
            int cellId = _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CellId;
            int partsInInventoryIndex = inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length +
                                        cellId;
            var inventoryPartIndex = _carPartInfoComponentsPool.Value.Get(GetSortedCarPartList()[partsInInventoryIndex]).CarPartId;
            return GetCarPartConfig(inventoryPartIndex);
        } else {
            int cellId = _shopCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity).CurrentBuyItemId;
            return GetCarPartConfig(cellId);
        }
    }
    public Vector2Int GetGridPosition(Vector2 localPosition)
    {
        Vector2 adjustedPosition = localPosition;
        
        var carCmp  =_carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity );
     
        Vector2 effectiveCellSize = new Vector2(200,200) / Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0),
            carCmp.CarPartContainerGroupsGrid.GetLength(1)) ;
        effectiveCellSize *= _sceneData.Value._inventoryUIView._carPartsContainer.localScale;
        int gridX = Mathf.FloorToInt((adjustedPosition.x) / effectiveCellSize.x  );
        int gridY = Mathf.FloorToInt((adjustedPosition.y) / effectiveCellSize.y);
        
        return new Vector2Int(gridX, gridY);
    }
    
    public Vector2 GetWorldPositionFromGrid(Vector2Int gridPosition, Transform container = null,Vector2 effectiveCellSize = new Vector2())
    {
        var carCmp  =_carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity );

        if (effectiveCellSize == Vector2.zero)
            effectiveCellSize = new Vector2(200, 200) / Mathf.Max(carCmp.CarPartContainerGroupsGrid.GetLength(0),
                carCmp.CarPartContainerGroupsGrid.GetLength(1));
        float posX = (gridPosition.x * effectiveCellSize.x) + (effectiveCellSize.x / 2);
        float posY = (gridPosition.y * effectiveCellSize.y) + (effectiveCellSize.y / 2);
        Vector2 localPosition = new (posX, posY);
        if (!container)
            container = _sceneData.Value._inventoryUIView._carPartsDownContainer;
        Vector3 worldPosition =  container.TransformPoint(localPosition);
        
        return worldPosition;
    }

    private Vector2Int GetCurrentDragCarPartContainerGridPosition(Vector2 needPos) {
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_sceneData.Value._inventoryUIView._carPartsDownContainer,needPos, null,
            out Vector2 localPosition);
        localPosition *= _sceneData.Value._inventoryUIView._carPartsContainer.localScale;
        Vector2Int gridPos = GetGridPosition(localPosition);

        return gridPos;
    }

    private void PlaceCarPartInCell(ref InventoryComponent inventoryCmp) {
        bool isBuyCell = _shopCellComponentsPool.Value.Has(inventoryCmp.CurrentDraggedCellEntity);
        int partIndex;
        float currentHealth;
        int level;
        int[] moduleIds;
        if (!isBuyCell) {
            ref var invCellCmp = ref _inventoryCellComponentsPool.Value.Get(inventoryCmp.CurrentDraggedCellEntity);
            int cellId = invCellCmp.CellId;
            if (inventoryCmp.CurrentMarkedCellForPanelInfo == _sceneData.Value._inventoryUIView._inventoryCellViews[cellId]._entity)
                CloseShowInfoPanel();

            int partsInInventoryIndex = inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length +
                                        cellId;
            int cellEntity = GetSortedCarPartList()[partsInInventoryIndex];
            var cellCmp = _carPartInfoComponentsPool.Value.Get(cellEntity);
            partIndex = cellCmp.CarPartId;
            level = cellCmp.PartLevel;
            currentHealth = cellCmp.CurrentHealth;
            moduleIds = new int[cellCmp.ModuleIds.Length];
            Array.Copy(cellCmp.ModuleIds,moduleIds,cellCmp.ModuleIds.Length);
            _inventoryCellComponentsPool.Value.Del(cellEntity);
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

            var carPartConfig = GetCarPartConfig(shopCellCmp.CurrentBuyItemId);
            moduleIds = new int[(int)carPartConfig.RartRare +1];
            Array.Fill(moduleIds, -1);
            level = 0;
            currentHealth = GetCarPartConfig(shopCellCmp.CurrentBuyItemId).MaxHealth;
            partIndex = shopCellCmp.CurrentBuyItemId;
        }

        int carPartEntity = _world.Value.NewEntity();
        ref var carPartCmp = ref _carPartComponentsPool.Value.Add(carPartEntity);
        ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Add(carPartEntity);
        carPartInfoCmp.CarPartId = partIndex;
        carPartInfoCmp.PartLevel = level;
        carPartInfoCmp.ModuleIds =new int[moduleIds.Length];
        Array.Copy(moduleIds,carPartInfoCmp.ModuleIds,moduleIds.Length);
        carPartInfoCmp.CurrentHealth = currentHealth;
        var partInfo = GetCarPartConfig(partIndex);
        carPartCmp.MaxHealth = partInfo.MaxHealth*GridUtils.GetHealthLevelMultiplier(carPartInfoCmp.PartLevel);
        
        Vector2Int carPartSize = GridUtils.GetCarPartSize(partInfo);
        Sequence returnSequence = DOTween.Sequence();
        
        if (inventoryCmp.CurrentRotateDegrees == 90 || inventoryCmp.CurrentRotateDegrees == 270) 
            carPartSize = new Vector2Int(carPartSize.y, carPartSize.x);
        
        Vector2 offset = carPartSize * inventoryCmp.OnCarCellSize - inventoryCmp.OnCarCellSize;
        if (carPartSize != Vector2Int.one) {
            offset = new Vector2(carPartSize.x - 1, carPartSize.y - 1) / 2 * inventoryCmp.OnCarCellSize *
                     _sceneData.Value.mainCanvas.localScale.x * _sceneData.Value._inventoryUIView._carPartsContainer.localScale.x;
        }

        var gridPos = inventoryCmp.LastCheckedGridCellPosition;
        var worldPos = GetWorldPositionFromGrid(gridPos);
        returnSequence.Join(_sceneData.Value._inventoryUIView._draggedInventoryCell.transform.DOMove(worldPos + offset, 0.1f)
            .SetEase(Ease.InOutQuad));

        returnSequence.OnComplete(() => { SetCarPartInfo(worldPos, partInfo, carPartEntity, gridPos, offset); });

        inventoryCmp.CurrentDraggedCellEntity = -1;
        inventoryCmp.CurrentCellCarPart = null;
    }

    private void AddNewTeammateCarToPlayer(EnemyCarPartsMaps carMap) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        int teammateEntity = _world.Value.NewEntity();
        inventoryCmp.PlayerTeammateCarsEntity.Add(teammateEntity);
        ref var carCmp = ref _carComponentsPool.Value.Add(teammateEntity);
        carCmp.CarId = carMap.CarId;
        carCmp.CarLevel = carMap.CarLevel;
        carCmp.CarPartContainerGroupsList = new Dictionary<int, List<Vector2Int>>();
        carCmp.CarPartGroupsList = new Dictionary<int, List<Vector2Int>>();
        carCmp.CarPartContainerGroupsGrid = new int[carMap.GridSize.x, carMap.GridSize.y];
        carCmp.CarPartGroupsGrid = new int[carMap.GridSize.x, carMap.GridSize.y];
        
        ref var moveCmp = ref _movementComponentsPool.Value.Add(teammateEntity);
        ref var fuelCmp = ref _fuelComponentsPool.Value.Add(teammateEntity);
        _enemyAIComponentsPool.Value.Add(teammateEntity);
        foreach (var carContainer in carMap.CarPartDownLevelGrid) 
            SetCarPartFromCarMap(teammateEntity, carContainer);
        
        foreach (var carContainer in carMap.CarPartUpLevelGrid) 
            SetCarPartFromCarMap(teammateEntity, carContainer);
        
        for (int i = 0; i < _sceneData.Value._inventoryUIView._selectCarButtons.Length; i++) {
            var selectCarButton = _sceneData.Value._inventoryUIView._selectCarButtons[i];
            selectCarButton.gameObject.SetActive(i < inventoryCmp.PlayerTeammateCarsEntity.Count);
            if (i < inventoryCmp.PlayerTeammateCarsEntity.Count) {
              //  Debug.Log(inventoryCmp.PlayerTeammateCarsEntity.Count + " deb int");
                _sceneData.Value._inventoryUIView._selectCarButtons[i].sellCarText.text =
                    GetCarCost(inventoryCmp.PlayerTeammateCarsEntity[i]) + "$";
            }
        }
    }

    private void SetCarPartFromCarMap(int mainEntity, CarPartTypeRow carPartsInfo) {
        ref var carCmp = ref _carComponentsPool.Value.Get(mainEntity);
        var carPartConfig = GetCarPartConfig(carPartsInfo.partId);
        var currentCarType = carPartConfig.PartType;

        if (currentCarType == CarPartType.None) return;

        int carPartCellEntity = _world.Value.NewEntity();

        if (GridUtils.IsContainerType(carPartConfig)) {
            var placedCells = GridUtils.GetCellsFromField(carPartsInfo.gridPosition, carPartConfig, carPartsInfo.rotate,
                carCmp.CarPartContainerGroupsGrid);
            carCmp.CarPartContainerGroupsList.Add(carPartCellEntity, new List<Vector2Int>());

            foreach (var cell in placedCells) {
                carCmp.CarPartContainerGroupsGrid[cell.x, cell.y] = carPartCellEntity;
                carCmp.CarPartContainerGroupsList[carPartCellEntity].Add(cell);
            }
        } else {
            var placedCells = GridUtils.GetCellsFromField(carPartsInfo.gridPosition, carPartConfig, carPartsInfo.rotate,
                carCmp.CarPartGroupsGrid);
            carCmp.CarPartGroupsList.Add(carPartCellEntity, new List<Vector2Int>());

            foreach (var cell in placedCells) {
                carCmp.CarPartGroupsGrid[cell.x, cell.y] = carPartCellEntity;
                carCmp.CarPartGroupsList[carPartCellEntity].Add(cell);
            }

            if (carPartConfig.PartType == CarPartType.Gun)
                _gunComponentsPool.Value.Add(carPartCellEntity);
            else if(carPartConfig.PartType == CarPartType.MeleeWeapon)
                _meleeWeaponComponentsPool.Value.Add(carPartCellEntity);
        }

        ref var carPartCmp = ref _carPartComponentsPool.Value.Add(carPartCellEntity);
        ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Add(carPartCellEntity);
        carPartCmp.MainEntity = mainEntity;
        carPartCmp.MaxHealth = carPartConfig.MaxHealth*GridUtils.GetHealthLevelMultiplier(carPartInfoCmp.PartLevel);
        carPartInfoCmp.CurrentHealth = carPartCmp.MaxHealth;
        carPartInfoCmp.CarPartId = carPartsInfo.partId;
        carPartCmp.RotateDegrees = carPartsInfo.rotate;
    }
    private void ClearAndSetNewCarUI(int newCarIndex) { 
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        
        if(inventoryCmp.PlayerTeammateCarsEntity[newCarIndex] == inventoryCmp.CurrentRedactedCarEntity || inventoryCmp.SelectCarButtonsNotInteractable)return;
        if (!CanUseThisCarPartContainers()) {
            _sceneData.Value.PlayWarningAnimation(GetLocalization("The car is unstable and needs wheels") + "!", false);
            return;
        }

        if (!MainPartIsPlaced()) {
            _sceneData.Value.PlayWarningAnimation(GetLocalization("Place the engine and driver's cabin on the car") + "!", false);
            return;
        }
        Sequence sequence = DOTween.Sequence();
        
        sequence.AppendCallback(ClearCarUI);
        inventoryCmp.SelectCarButtonsNotInteractable = true;
        sequence.AppendInterval(0.5f);
       
        sequence.AppendCallback(()=> SetNewCarUI(newCarIndex));
        
        sequence.Play();
    }

    private void ClearCarUI() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        int entity = inventoryCmp.CurrentRedactedCarEntity;
        ref var carCmp = ref _carComponentsPool.Value.Get(entity);
        foreach (var carPartInfo in carCmp.CarPartContainerGroupsList) {
            ClearNeededCarPartUi(carPartInfo.Key);
        }
        foreach (var carPartInfo in carCmp.CarPartGroupsList) {
            ClearNeededCarPartUi(carPartInfo.Key);
        }
    }

    private void ClearNeededCarPartUi(int entity) {
        var carPartCmp = _carPartComponentsPool.Value.Get(entity);
        var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(entity);
        var capPartImage = carPartCmp.CarPartCellImage;
        var carPartConfig = GetCarPartConfig(carPartInfoCmp.CarPartId);
        Image capPartUpImage = null;
        if (carPartConfig.PartType == CarPartType.Gun) {
            ref var gunCmp = ref _gunComponentsPool.Value.Get(entity);
            capPartUpImage = gunCmp.GunSpriteImage;
            gunCmp.GunSpriteImage = null;
        } else if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
            ref var meleeCmp = ref _meleeWeaponComponentsPool.Value.Get(entity);
            capPartUpImage = meleeCmp.MeleeSpriteImage;
            meleeCmp.MeleeSpriteImage = null;
        }

        capPartImage.transform.DOScale(0, 0.4f).OnComplete(() => {
            if (capPartUpImage != null)
                _sceneData.Value.ReleaseCarPartUIImage(capPartUpImage);
            _sceneData.Value.ReleaseCarPartUIImage(capPartImage);
        });
    }

    private void SetNewCarUI(int newCarIndex) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.CurrentRedactedCarEntity = inventoryCmp.PlayerTeammateCarsEntity[newCarIndex];
        SetCarContainersContainerSize();
        ref var carCmp = ref _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity);

        foreach (var carPartInfo in carCmp.CarPartContainerGroupsList) {
            Vector2Int edgePosition = GridUtils.GetEdgePointFromCarPart(carPartInfo.Value);
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartInfo.Key);
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(carPartInfo.Key);
            SetCarPartDownImageTransform(edgePosition, carPartInfo.Key, inventoryCmp.OnCarCellSize,
                _sceneData.Value._inventoryUIView._carPartsDownContainer, carPartCmp.RotateDegrees, carPartInfoCmp.CarPartId);
        }

        foreach (var carPartInfo in carCmp.CarPartGroupsList) {
            Vector2Int edgePosition = GridUtils.GetEdgePointFromCarPart(carPartInfo.Value);
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartInfo.Key);
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(carPartInfo.Key);
            SetCarPartDownImageTransform(edgePosition, carPartInfo.Key, inventoryCmp.OnCarCellSize,
                _sceneData.Value._inventoryUIView._carPartsUpContainer, carPartCmp.RotateDegrees, carPartInfoCmp.CarPartId);
        }

        UpdateCarDescription(inventoryCmp.CurrentRedactedCarEntity);
        UpdateHealAllCarPartButton();
        SetCarUpgradeButtonInfo();
        
        inventoryCmp.SelectCarButtonsNotInteractable = false;
    }
    private bool MainPartIsPlaced() {
        foreach (var cellEntity in _inventoryCellComponentsFilter.Value) {
            if (_sceneData.Value.GetCarPartConfig(_carPartInfoComponentsPool.Value.Get(cellEntity).CarPartId).PartType == CarPartType.Main)
                return false;
        }

        return true;
    }

    private void SetCarPartDownImageTransform(Vector2Int edgePosition, int entity, Vector2 cellSize, Transform container, int rotateDegrees,
        int carPartID) {
        var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartID);

        Vector2Int carPartSize = GridUtils.GetCarPartSize(carPartConfig);

        var carPartCellImage = _sceneData.Value.GetCarPartUIImage(container, container.position);

        bool isShopCell = _shopCellCarComponentsPool.Value.Has(entity);
        if (isShopCell)
            _shopCellCarComponentsPool.Value.Get(entity).CarPartsImages.Add(carPartCellImage);
        else
            _carPartComponentsPool.Value.Get(entity).CarPartCellImage = carPartCellImage;
        carPartCellImage.transform.localScale = Vector3.zero;
        carPartCellImage.sprite = carPartConfig.PartSprite;
        carPartCellImage.transform.localEulerAngles = new Vector3(0, 0, rotateDegrees);
        carPartCellImage.rectTransform.sizeDelta = carPartSize * cellSize;
        if (rotateDegrees == 90 || rotateDegrees == 270)
            carPartSize = new Vector2Int(carPartSize.y, carPartSize.x);
        float uiScale = isShopCell ? 1 : _sceneData.Value._inventoryUIView._carPartsContainer.localScale.x;
        Vector2 offset = carPartSize != Vector2Int.one
            ? new Vector2(carPartSize.x - 1, carPartSize.y - 1) / 2 * cellSize * _sceneData.Value.mainCanvas.localScale.x * uiScale
            : carPartSize * cellSize - cellSize;

        Vector3 finalPosition = GetWorldPositionFromGrid(edgePosition, isShopCell ? container : null, cellSize) + offset;

        carPartCellImage.transform.position = finalPosition;

        carPartCellImage.DOKill();
        carPartCellImage.transform.DOScale(1, 0.4f);

        if (GridUtils.IsWeaponType(carPartConfig)) {
            (Sprite upSprite, Vector2 upSize, Vector2 upPosition) = GetUpperPartSizesForUI(cellSize * carPartSize, carPartConfig);
            var upPartImage = _sceneData.Value.GetCarPartUIImage(carPartCellImage.transform, upPosition);
            upPartImage.transform.localPosition = upPosition;
            upPartImage.sprite = upSprite;
            upPartImage.transform.localEulerAngles = Vector3.zero;
            upPartImage.transform.localScale = Vector3.one;
            upPartImage.rectTransform.sizeDelta = upSize;
            if (isShopCell)
                _shopCellCarComponentsPool.Value.Get(entity).CarPartsImages.Add(upPartImage);
            else {
                if (carPartConfig.PartType == CarPartType.Gun)
                    _gunComponentsPool.Value.Get(entity).GunSpriteImage = upPartImage;
                else
                    _meleeWeaponComponentsPool.Value.Get(entity).MeleeSpriteImage = upPartImage;
            }
        }
    }
    private void SetCarPartInfo(Vector2 worldPosition, CarPartConfig partInfo, int carPartCmpEntity, Vector2Int gridPosition,
        Vector2 imageOffset) {
        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartCmpEntity);
        ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(carPartCmpEntity);
        _sceneData.Value._inventoryUIView._draggedInventoryCell.gameObject.SetActive(false);
        _sceneData.Value._gridMaskRect.gameObject.SetActive(false);
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var carCmp = ref _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity);

        var needCells = GridUtils.GetCellsFromField(gridPosition, partInfo, inventoryCmp.CurrentRotateDegrees, carCmp.CarPartGroupsGrid);

        foreach (var cell in needCells) {
            carCmp.CarPartGroupsGrid[cell.x, cell.y] = carPartCmpEntity;
        }

        carCmp.CarPartGroupsList.Add(carPartCmpEntity, needCells);
        Vector2Int carPartSize = GridUtils.GetCarPartSize(partInfo);
        Vector2 needPos = worldPosition + imageOffset;
        carPartCmp.CarPartCellImage =
            _sceneData.Value.GetCarPartUIImage(_sceneData.Value._inventoryUIView._carPartsUpLevelContainer.transform, needPos);
        carPartCmp.CarPartCellImage.sprite = partInfo.PartSprite;
        carPartCmp.RotateDegrees = inventoryCmp.CurrentRotateDegrees;
        carPartCmp.CarPartCellImage.transform.eulerAngles = new Vector3(0, 0, inventoryCmp.CurrentRotateDegrees);
        carPartCmp.MainEntity = inventoryCmp.CurrentRedactedCarEntity;
        carPartCmp.MaxHealth = partInfo.MaxHealth*GridUtils.GetHealthLevelMultiplier(carPartInfoCmp.PartLevel);
        carPartCmp.CarPartCellImage.rectTransform.sizeDelta = inventoryCmp.OnCarCellSize * carPartSize;
        if (partInfo.PartType == CarPartType.Gun) {
            ref var gunCmp = ref _gunComponentsPool.Value.Add(carPartCmpEntity);
            (Sprite upSprite, Vector2 upSize, Vector2 upPosition) =
                GetUpperPartSizesForUI(carPartCmp.CarPartCellImage.rectTransform.sizeDelta, partInfo);
            if (upSprite != _sceneData.Value._transparentPixel) {
                gunCmp.GunSpriteImage = _sceneData.Value.GetCarPartUIImage(carPartCmp.CarPartCellImage.rectTransform, Vector2.zero);
                gunCmp.GunSpriteImage.transform.localRotation = Quaternion.identity;
                gunCmp.GunSpriteImage.transform.localPosition = upPosition;
                gunCmp.GunSpriteImage.sprite = upSprite;
                gunCmp.GunSpriteImage.rectTransform.sizeDelta = upSize;
            }
        } else if (partInfo.PartType == CarPartType.MeleeWeapon) {
            ref var meleeCmp = ref _meleeWeaponComponentsPool.Value.Add(carPartCmpEntity);
            (Sprite upSprite, Vector2 upSize, Vector2 upPosition) =
                GetUpperPartSizesForUI(carPartCmp.CarPartCellImage.rectTransform.sizeDelta, partInfo);
            if (upSprite != _sceneData.Value._transparentPixel) {
                meleeCmp.MeleeSpriteImage = _sceneData.Value.GetCarPartUIImage(carPartCmp.CarPartCellImage.rectTransform, Vector2.zero);
                meleeCmp.MeleeSpriteImage.transform.localRotation = Quaternion.identity;
                meleeCmp.MeleeSpriteImage.transform.localPosition = upPosition;
                meleeCmp.MeleeSpriteImage.sprite = upSprite;
                meleeCmp.MeleeSpriteImage.rectTransform.sizeDelta = upSize;
            }
        }

        UpdateCarDescription(inventoryCmp.CurrentRedactedCarEntity);
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
        var carCmp = _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity );
        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPartContainer.Key);
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
        var needCells = GridUtils.GetCellsFromField(position, partInfo,0);
        var carCmp = _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity );
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
                        var wheelConfig = _sceneData.Value.GetWheelContainerFromCarPart(_carPartInfoComponentsPool.Value
                            .Get(carCmp.CarPartContainerGroupsGrid[checkedCell.x, checkedCell.y]).CarPartId);
                        if (!wheelConfig) continue;
                        if (wheelConfig.WheelsPositions.Length != 1 && cell.y == checkedCell.y) return false;
                    }
                }
            }
        }

        return hasCarPartContainersAround;
    }

    private void AddInventoryItem(int partId, int level, float currentHealth, int[] moduleIds) {
        var cellEntity = _world.Value.NewEntity();
        ref var inventoryCellCmp = ref _inventoryCellComponentsPool.Value.Add(cellEntity);
        ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Add(cellEntity);
        carPartInfoCmp.CarPartId = partId;
        carPartInfoCmp.PartLevel = level;
        carPartInfoCmp.ModuleIds = new int[moduleIds.Length];
        Array.Copy(moduleIds,carPartInfoCmp.ModuleIds,moduleIds.Length);
        carPartInfoCmp.CurrentHealth = currentHealth;
        SetInventoryCells();
    }

    private void SetInventoryCell(int cellEntity, int cellNumber) {
        var inventoryCell = _sceneData.Value._inventoryUIView._inventoryCellViews[cellNumber];
        inventoryCell.gameObject.SetActive(true);
           ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(cellEntity);
        var carPartConfig = _sceneData.Value._carStuffListsConfig.CarPartConfigs[carPartInfoCmp.CarPartId]; 
    
        if (GridUtils.IsWeaponType(carPartConfig)) {
            (Sprite upSprite, Vector2 size, Vector2 position) =
                GetUpperPartSizesForUI(inventoryCell._inventoryCellImage.rectTransform.sizeDelta, carPartConfig);
            inventoryCell.SetInventoryCellSprite(carPartConfig.PartSprite, upSprite, size, position,cellEntity);
        } else 
            inventoryCell.SetInventoryCellSprite(carPartConfig.PartSprite, _sceneData.Value._transparentPixel, Vector2.zero, Vector2.zero,cellEntity);
    }

    private void SetModuleInventoryCell(int cellEntity, int cellNumber) {
        var inventoryCell = _sceneData.Value._inventoryUIView._inventoryCellViews[cellNumber];
        inventoryCell.gameObject.SetActive(true);
        ref var moduleCmp = ref _moduleInventoryItemsPool.Value.Get(cellEntity);

        inventoryCell.SetInventoryCellSprite(_betweenScenesService.Value._carStuffListsConfig.ModuleConfigs[moduleCmp.ModuleId].ModuleIcon,
            null, Vector2.zero, Vector2.zero, cellEntity);
    }

    private ( Sprite upSprite, Vector2 size, Vector2 position) GetUpperPartSizesForUI(Vector2 cellSizeDelta, CarPartConfig carPartConfig) {
        Vector2 cellSize = Vector2.zero;
        Vector2 needPosition = Vector2.zero;
        Sprite upSprite = _sceneData.Value._transparentPixel;
        if (carPartConfig.PartType == CarPartType.Gun) {
            var gunConfig = _sceneData.Value.GetGunFromCarPart(carPartConfig.Id);
            if (gunConfig.UpPartSprite != null) {
                var carPartSize = GridUtils.GetCarPartSize(carPartConfig);
                cellSizeDelta = new Vector2(Math.Max(cellSizeDelta.x, cellSizeDelta.y), Math.Max(cellSizeDelta.x, cellSizeDelta.y));
                cellSize = cellSizeDelta / (Math.Max(carPartSize.x, carPartSize.y) * GridUtils.InWorldCellSize) * gunConfig.UpPartSprite.rect.size;
                needPosition = cellSizeDelta / Math.Max(carPartSize.x, carPartSize.y) * gunConfig.UpPartPosition;
                upSprite = gunConfig.UpPartSprite != null? gunConfig.UpPartSprite : _sceneData.Value._transparentPixel;
            }
        }
        if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
            var meleeConfig = _sceneData.Value.GetMeleeWeaponFromCarPart(carPartConfig.Id);
            if (meleeConfig.UpSprite != null) {
                var carPartSize = GridUtils.GetCarPartSize(carPartConfig);
                cellSizeDelta = new Vector2(Math.Max(cellSizeDelta.x, cellSizeDelta.y), Math.Max(cellSizeDelta.x, cellSizeDelta.y));
                cellSize = cellSizeDelta / (Math.Max(carPartSize.x, carPartSize.y) * GridUtils.InWorldCellSize) * meleeConfig.UpSprite.rect.size;
                needPosition = cellSizeDelta / Math.Max(carPartSize.x, carPartSize.y) * meleeConfig.UpSpritePosition;
                upSprite = meleeConfig.UpSprite != null? meleeConfig.UpSprite : _sceneData.Value._transparentPixel;
            }
        }

        return (upSprite, cellSize, needPosition);
    }
    

    private void SetupInventoryCells() {
        int i = 0;
        foreach (var inventoryCell in _sceneData.Value._inventoryUIView._inventoryCellViews) {
          
            int cellId = i;
            inventoryCell.Construct( /*_world.Value,*/ TakeItemFromInventory, cellId, SetCarPartInfoPanel);
            i++;
        }
        
        _sceneData.Value._inventoryUIView._inventoryModeButton.onClick.AddListener(ChangeInventoryModeForButton);
        _sceneData.Value._inventoryUIView._inventoryModeText.text = GetLocalization("Car");
        _sceneData.Value._inventoryUIView._changeToRedactModeText.text = GetLocalization("Redact down level");
    }

    private void TakeItemFromInventory(int cellEntity) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if(_moduleInventoryItemsPool.Value.Has(cellEntity))return;
        Debug.Log("drag entity "+cellEntity);
        inventoryCmp.IsTryDraggedCell = true;
        inventoryCmp.CurrentCellCarPart = null;
        inventoryCmp.CurrentDraggedCellEntity = cellEntity;
    }

    private void SetSortButtons() {
        for (int i = 0; i < _sceneData.Value._inventoryUIView._sortButtons.Length; i++) {
            CarPartType sortType = (CarPartType)i;
            
            var sortButton = _sceneData.Value._inventoryUIView._sortButtons[i];
            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => { SortCarParts(sortType); });

            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { AnimateSortButton(sortButton.transform, sortType, true, (PointerEventData)data); });

            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => { AnimateSortButton(sortButton.transform, sortType, false, (PointerEventData)data); });

            sortButton.SortEventTrigger.triggers.Add(clickEntry);
            sortButton.SortEventTrigger.triggers.Add(enterEntry);
            sortButton.SortEventTrigger.triggers.Add(exitEntry);
        }

        _sceneData.Value._inventoryUIView._nextInventoryCellsButton.onClick.AddListener(NextInventoryPage);
        _sceneData.Value._inventoryUIView._backInventoryCellsButton.onClick.AddListener(LastInventoryPage);
    }

    private void AnimateSortButton(Transform animateButton, CarPartType sortType, bool isEntry, PointerEventData data) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (isEntry) {
            if (inventoryCmp.SortedCarPartType.Contains(sortType) ||
                (sortType == CarPartType.All && inventoryCmp.SortedCarPartType.Count == 7) ||
                (sortType == CarPartType.None && inventoryCmp.SortedCarPartType.Count == 0))
                animateButton.DOLocalMoveY(70, 0.3f);
            else
                animateButton.DOLocalMoveY(63, 0.3f);
        } else {
            if (inventoryCmp.SortedCarPartType.Contains(sortType)||
                (sortType == CarPartType.All && inventoryCmp.SortedCarPartType.Count == 7) ||
                (sortType == CarPartType.None && inventoryCmp.SortedCarPartType.Count == 0))
                animateButton.DOLocalMoveY(67, 0.3f);
            else
                animateButton.DOLocalMoveY(60, 0.3f);
        }
    }
    private void SortCarParts(CarPartType carPartType, bool isInteractWithMouse = true) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (carPartType == CarPartType.None) {
            inventoryCmp.SortedCarPartType = new List<CarPartType>();
            
            int index = 0;
            foreach (var sortButton in _sceneData.Value._inventoryUIView._sortButtons) {
                if (index == 0) {
                    sortButton.buttonImage.sprite = sortButton.activeSprite;
                    sortButton.transform.DOLocalMoveY(70, 0.3f);
                } else {
                    sortButton.buttonImage.sprite = sortButton.unactiveSprite;
                    sortButton.transform.DOLocalMoveY(60, 0.3f);
                }
               
                index++;
            }
        } 
        else if (carPartType == CarPartType.All) {
            inventoryCmp.SortedCarPartType = new List<CarPartType>() {
                CarPartType.Armor,
                CarPartType.CarStuffContainer,
                CarPartType.Gun,
                CarPartType.Main,
                CarPartType.Support,
                CarPartType.WheelsContainer,
                CarPartType.MeleeWeapon
            };
            int index = 0;
            foreach (var sortButton in _sceneData.Value._inventoryUIView._sortButtons) {
                if (index == (int)CarPartType.All) {
                    sortButton.buttonImage.sprite = sortButton.activeSprite;
                    sortButton.transform.DOLocalMoveY(70, 0.3f);
                } else if (index != 0) {
                    sortButton.buttonImage.sprite = sortButton.activeSprite;
                    sortButton.transform.DOLocalMoveY(67, 0.3f);
                } else {
                    sortButton.buttonImage.sprite = sortButton.unactiveSprite;
                    sortButton.transform.DOLocalMoveY(60, 0.3f);
                }
                index++;
            }
        } else {
            if (inventoryCmp.SortedCarPartType.Contains(carPartType)) {
                inventoryCmp.SortedCarPartType.Remove(carPartType);
                var sortButton = _sceneData.Value._inventoryUIView._sortButtons[(int)carPartType];
                sortButton.buttonImage.sprite = sortButton.unactiveSprite;
                if (isInteractWithMouse)
                    sortButton.transform.DOLocalMoveY(63, 0.3f);
                else
                    sortButton.transform.DOLocalMoveY(60, 0.3f);
            } else {
                inventoryCmp.SortedCarPartType.Add(carPartType);
                var sortButton = _sceneData.Value._inventoryUIView._sortButtons[(int)carPartType];
                sortButton.buttonImage.sprite = sortButton.activeSprite;
                if (isInteractWithMouse)
                    sortButton.transform.DOLocalMoveY(70, 0.3f);
                else
                    sortButton.transform.DOLocalMoveY(67, 0.3f);
            }

            var noneButton = _sceneData.Value._inventoryUIView._sortButtons[0];
            var allButton = _sceneData.Value._inventoryUIView._sortButtons[(int)CarPartType.All];
            if (inventoryCmp.SortedCarPartType.Count == 0) {
                noneButton.buttonImage.sprite = noneButton.activeSprite;
                noneButton.transform.DOLocalMoveY(67, 0.3f);
            } else {
                noneButton.buttonImage.sprite = noneButton.unactiveSprite;
                noneButton.transform.DOLocalMoveY(60, 0.3f);
            }

            if (inventoryCmp.SortedCarPartType.Count == 7) {
                allButton.buttonImage.sprite = allButton.activeSprite;
                allButton.transform.DOLocalMoveY(67, 0.3f);
            } else {
                allButton.buttonImage.sprite = allButton.unactiveSprite;
                allButton.transform.DOLocalMoveY(60, 0.3f);
            }
        }
     
        inventoryCmp.CurrentInventoryPage = 0;

        SetInventoryCells();
    }

    private List<int> GetSortedCarPartList() {
        var SortedPartsInInventory = new List<int>();
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        foreach (var cellEntity in _inventoryCellComponentsFilter.Value) {
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(cellEntity);
            if (inventoryCmp.SortedCarPartType.Contains(GetCarPartConfig(carPartInfoCmp.CarPartId).PartType))
                SortedPartsInInventory.Add(cellEntity);
        }

        return SortedPartsInInventory;
    }

    private void NextInventoryPage() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.CurrentInventoryPage++;
        float itemsCount = inventoryCmp.IsModuleInventoryMode ? GetModulesList().Count : (float)GetSortedCarPartList().Count;
        if (inventoryCmp.CurrentInventoryPage > Mathf.FloorToInt(itemsCount / _sceneData.Value._inventoryUIView._inventoryCellViews.Length))
            inventoryCmp.CurrentInventoryPage = 0;

        SetInventoryCells();
    }

    private void LastInventoryPage() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        float itemsCount = inventoryCmp.IsModuleInventoryMode ? GetModulesList().Count : (float)GetSortedCarPartList().Count;
        inventoryCmp.CurrentInventoryPage--;
        if (inventoryCmp.CurrentInventoryPage < 0)
            inventoryCmp.CurrentInventoryPage = Mathf.FloorToInt(itemsCount /
                                                                 _sceneData.Value._inventoryUIView._inventoryCellViews.Length);

        SetInventoryCells();
    }

    private void SetCarGrid() {
        ref var carCmp = ref _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity );
      
        _sceneData.Value._inventoryUIView._carPartsDownContainer.GetComponent<CellView>().Construct(-Vector2Int.one*999, SetCurrentCarCell, -999, SetCarPartInfoPanel);
        
        carCmp.CarPartGroupsGrid = new int[0, 0];
        carCmp.CarPartGroupsList = new Dictionary<int, List<Vector2Int>>();
        carCmp.CarPartContainerGroupsGrid= new int[3,3];
        carCmp.CarPartContainerGroupsList = new Dictionary<int, List<Vector2Int>>();
        AddMainCarParts(_sceneData.Value.GetCarConfig(_sceneData.Value._defaultCarConfigId)); 
    }

    private void AddMainCarParts(CarConfig carConfig) {
        foreach (var carPartId in carConfig.StartCarParts) {
            var carPartConfig = GetCarPartConfig(carPartId);
            var moduleIds = new int[(int)carPartConfig.RartRare + 1];
            Array.Fill(moduleIds, -1);
            AddInventoryItem(carPartId,0, carPartConfig.MaxHealth,moduleIds);
        }
    }

    private void SetCurrentCarCell(CellView _cellPosition) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.CurrentCellCarPart = _cellPosition;
    }

    private void SetInventoryCells() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        int inventoryItemsCount = inventoryCmp.IsModuleInventoryMode
            ? GetModulesList().Count
            : GetSortedCarPartList().Count;
        if ((float)inventoryItemsCount / _sceneData.Value._inventoryUIView._inventoryCellViews.Length > 1.03f) {
            _sceneData.Value._inventoryUIView._inventoryArrowsContainer.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._pageNumberText.text = inventoryCmp.CurrentInventoryPage + "/" +
                                                                     (Mathf.FloorToInt((float)inventoryItemsCount /
                                                                                       _sceneData.Value._inventoryUIView._inventoryCellViews
                                                                                           .Length));
        } else {
            _sceneData.Value._inventoryUIView._pageNumberText.text = "";
            _sceneData.Value._inventoryUIView._inventoryArrowsContainer.gameObject.SetActive(false);
        }

        for (int i = 0; i < _sceneData.Value._inventoryUIView._inventoryCellViews.Length; i++) {
            int inventoryPartIndex = inventoryCmp.CurrentInventoryPage * _sceneData.Value._inventoryUIView._inventoryCellViews.Length + i;

            if (inventoryPartIndex < inventoryItemsCount) {
                if (inventoryCmp.IsModuleInventoryMode) {
                    int cellEntity = GetModulesList()[inventoryPartIndex];
                    SetModuleInventoryCell(cellEntity, i);
                    _moduleInventoryItemsPool.Value.Get(cellEntity).CellId = i;
                } else {
                    int cellEntity = GetSortedCarPartList()[inventoryPartIndex];
                    SetInventoryCell(cellEntity, i);
                    _inventoryCellComponentsPool.Value.Get(cellEntity).CellId = i;
                }
            } else
                _sceneData.Value._inventoryUIView._inventoryCellViews[i].gameObject.SetActive(false);
        }
    }

    private List<int> GetModulesList() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        List<int> modulesList = new List<int>();
        foreach (var moduleEntity in _moduleInventoryItemsFilter.Value) {
            if (inventoryCmp.CurrentSelectedModuleCellId == -1 || _sceneData.Value
                    .GetCarPartConfig(_carPartInfoComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo).CarPartId).ModuleTypes
                    .Contains(_betweenScenesService.Value._carStuffListsConfig
                        .ModuleConfigs[_moduleInventoryItemsPool.Value.Get(moduleEntity).ModuleId].ModuleType))
                modulesList.Add(moduleEntity);
        }

        return modulesList;
    }

    private void ChangeInventoryModeForButton() {
        if (_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentSelectedModuleCellId != -1) {
            CancelModuleSelect();
        } else {
            ChangeInventoryMode();
        }
    }
    private void ChangeInventoryMode() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        inventoryCmp.IsModuleInventoryMode = !inventoryCmp.IsModuleInventoryMode;
        if (inventoryCmp.IsModuleInventoryMode) {
            _sceneData.Value._inventoryUIView._inventoryModeText.text = GetLocalization("Modules");
        } else {
            _sceneData.Value._inventoryUIView._inventoryModeText.text = GetLocalization("Car");
        }

        inventoryCmp.CurrentInventoryPage = 0;
        
        SetInventoryCells();
    }

    private void AddModuleInventoryItem(int moduleId) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        int moduleEntity = _world.Value.NewEntity();
        _moduleInventoryItemsPool.Value.Add(moduleEntity).ModuleId = moduleId;
        if(inventoryCmp.IsModuleInventoryMode)
            SetInventoryCells();
        
    }
    private void SelectModuleCell(int cellId) {
      
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if(_carPartInfoComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo).ModuleIds[cellId] != -1)return;
        if (inventoryCmp.CurrentSelectedModuleCellId == cellId) {
            CancelModuleSelect();
            return;
        }
        inventoryCmp.CurrentSelectedModuleCellId = cellId;
        if (!inventoryCmp.IsModuleInventoryMode)
            ChangeInventoryMode();
        else
            SetInventoryCells();
        if(GetModulesList().Count == 0)
            _sceneData.Value._inventoryUIView._noModulesTextInventory.gameObject.SetActive(true);

        _sceneData.Value._inventoryUIView._cancelModuleTextPanelInfo.position =
            _sceneData.Value._carPartInfoUIView._moduleViews[cellId].transform.position;
        _sceneData.Value._inventoryUIView._cancelModuleTextPanelInfo.gameObject.SetActive(true);
    }

    private void SetModuleWindowInfo(bool isHide, int index) {
        if(!_carPartInfoComponentsPool.Value.Has(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentMarkedCellForPanelInfo))return;
        int moduleId = _carPartInfoComponentsPool.Value
            .Get(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentMarkedCellForPanelInfo).ModuleIds[index];

        if (moduleId == -1) return;

        if (!isHide) {
            _sceneData.Value._carPartInfoUIView._moduleWindow.gameObject.SetActive(true);
            var moduleConfig = _betweenScenesService.Value._carStuffListsConfig.ModuleConfigs[moduleId];
            var nameText = _sceneData.Value._carPartInfoUIView._nameModuleWindowText;
            var descriptionText = _sceneData.Value._carPartInfoUIView._descriptionModuleWindowText;
            nameText.text = GetLocalization(moduleConfig.ModuleName);
            descriptionText.text = _sceneData.Value._carPartInfoUIView.GetModuleDescription(moduleConfig);
            descriptionText.ForceMeshUpdate(true);
            descriptionText.gameObject.GetComponent<ContentSizeFitter>().SetLayoutVertical();

            var modulePosition = _sceneData.Value._carPartInfoUIView._moduleViews[index].transform.position;
            _sceneData.Value._carPartInfoUIView._moduleWindow.position =
                new Vector3(modulePosition.x - 200 * _sceneData.Value.mainCanvas.localScale.x, modulePosition.y, modulePosition.z);
            _sceneData.Value._carPartInfoUIView._moduleWindow.localScale = Vector3.zero;
            _sceneData.Value._carPartInfoUIView._moduleWindow.DOKill();
            _sceneData.Value._carPartInfoUIView._moduleWindow.DOScale(1, 0.3f);
            _sceneData.Value._carPartInfoUIView._moduleWindow.sizeDelta = new Vector2(
                _sceneData.Value._carPartInfoUIView._moduleWindow.sizeDelta.x,
                nameText.rectTransform.sizeDelta.y + descriptionText.rectTransform.sizeDelta.y + 20);
        } else {
            _sceneData.Value._carPartInfoUIView._moduleWindow.DOKill();
            _sceneData.Value._carPartInfoUIView._moduleWindow.DOScale(0, 0.3f)
                .OnComplete(() => _sceneData.Value._carPartInfoUIView._moduleWindow.gameObject.SetActive(false));
        }
        
    }

    private void CancelModuleSelect() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ChangeInventoryMode();
        inventoryCmp.CurrentSelectedModuleCellId = -1;
        _sceneData.Value._inventoryUIView._cancelModuleTextPanelInfo.gameObject.SetActive(false);
        _sceneData.Value._inventoryUIView._noModulesTextInventory.gameObject.SetActive(false);
    }

    private void EquipModuleToCarPart(int moduleEntity) {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo);
        var moduleCmp = _moduleInventoryItemsPool.Value.Get(moduleEntity);

        var moduleConfig = _betweenScenesService.Value._carStuffListsConfig.ModuleConfigs[moduleCmp.ModuleId];
        foreach (var buff in moduleConfig.ModuleBuffs) {
            if (buff.Buff == Buff.Health) {
                var addedHealth = GetCarPartConfig(carPartInfoCmp.CarPartId).MaxHealth *
                                  GridUtils.GetHealthLevelMultiplier(carPartInfoCmp.PartLevel) * buff.BuffCount;

                carPartInfoCmp.CurrentHealth += addedHealth;
                break;
            }
        }

        carPartInfoCmp.ModuleIds[inventoryCmp.CurrentSelectedModuleCellId] = moduleCmp.ModuleId;

       // var moduleView = _sceneData.Value._carPartInfoUIView._moduleViews[inventoryCmp.CurrentSelectedModuleCellId];
   //     moduleView.ModuleIcon.sprite = moduleConfig.ModuleIcon;
       // moduleView.ModuleIcon.color = Color.white;
        CancelModuleSelect();
        SetCarPartInfoPanel(inventoryCmp.CurrentMarkedCellForPanelInfo, false);
        
        _moduleInventoryItemsPool.Value.Del(moduleEntity);
    }
    
    private void ActivateGiveCarPartsWindow() {
        var giveCarPartsUiView = _sceneData.Value._givingCarPartsUIView;
       
        giveCarPartsUiView.gameObject.SetActive(ChangeGiveCarPartWindowInfo());
    }

    private void GetCarPartAndActiveNext() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var carPartConfig = GetCarPartConfig(inventoryCmp.CurrentGivenItems[0]);
        var moduleIds = new int[(int)carPartConfig.RartRare + 1];
        Array.Fill(moduleIds, -1);
        AddInventoryItem(inventoryCmp.CurrentGivenItems[0], 0,carPartConfig.MaxHealth,moduleIds);
        inventoryCmp.CurrentGivenItems.RemoveAt(0);
        ActivateGiveCarPartsWindow();
    }
    
    private void SellCarPartAndActiveNext() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        SetMoneyCount(GridUtils.GetSellValueForCarPart(_sceneData.Value.GetCarPartConfig(inventoryCmp.CurrentGivenItems[0]), 1, null));
        inventoryCmp.CurrentGivenItems.RemoveAt(0);
        ActivateGiveCarPartsWindow();
    }

    private bool ChangeGiveCarPartWindowInfo() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);     
        if (inventoryCmp.CurrentGivenItems.Count == 0) return false;
        
        var giveCarPartsUiView = _sceneData.Value._givingCarPartsUIView;
   
       var carPartConfig = _sceneData.Value.GetCarPartConfig(inventoryCmp.CurrentGivenItems[0]);

       giveCarPartsUiView._carPartImageDown.sprite = carPartConfig.PartSprite;
       
       giveCarPartsUiView._carPartImageDown.transform.localScale = Vector3.zero; 
       giveCarPartsUiView._carPartImageDown.transform.DOScale(1f, 0.3f);
       
       if (carPartConfig.PartType == CarPartType.MeleeWeapon || carPartConfig.PartType == CarPartType.Gun) {
           (Sprite upSprite, Vector2 size, Vector2 position) =
               GetUpperPartSizesForUI(giveCarPartsUiView._carPartImageDown.rectTransform.sizeDelta, carPartConfig);

           giveCarPartsUiView._carPartImageUp.sprite = upSprite;
           giveCarPartsUiView._carPartImageUp.rectTransform.sizeDelta = size;
           giveCarPartsUiView._carPartImageUp.transform.localPosition = position;
       } else {
           giveCarPartsUiView._carPartImageUp.sprite = _sceneData.Value._transparentPixel;
       }
       
       giveCarPartsUiView._remainingCarPartsText.text = inventoryCmp.CurrentGivenItems.Count > 1
           ? $"{GetLocalization("Remaining car parts")}: \n{inventoryCmp.CurrentGivenItems.Count}"
           : "";
       
       return true;
    }

    private void SetCarUpgradeButtonInfo() {
        ref var carCmp = ref _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRedactedCarEntity );
        var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);
        if (carCmp.CarLevel > 15) {
            _sceneData.Value._inventoryUIView._upgradeCarButton.enabled = false;
            _sceneData.Value._inventoryUIView._upgradeCarButtonText.text = $"{GetLocalization("Max level")}";
        } else {
            _sceneData.Value._inventoryUIView._upgradeCarButton.enabled = true;
            var upgradeCost = Mathf.FloorToInt(carConfig.CarUpgradeCost * (1 + carCmp.CarLevel * 0.3f));
            _sceneData.Value._inventoryUIView._upgradeCarButtonText.text = $"{GetLocalization("Upgrade car")} {upgradeCost}$";
        }
      
    }

    private void IncriseCarLevel() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var carCmp = ref _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity);
        var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var upgradeCost = Mathf.FloorToInt(carConfig.CarUpgradeCost * (1 + carCmp.CarLevel * 0.3f));
        if (playerCurrencyCmp.MoneyCount > upgradeCost) {
            SetMoneyCount(-upgradeCost);
            carCmp.CarLevel++;
            SetCarUpgradeButtonInfo();

            foreach (var carPartInfo in carCmp.CarPartGroupsList) {
                ref var carPartCmp = ref _carPartInfoComponentsPool.Value.Get(carPartInfo.Key);
                var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartCmp.CarPartId);

                if (carPartConfig.PartType == CarPartType.Main)
                    carPartCmp.PartLevel = carCmp.CarLevel;
            }

            foreach (var cellEntity in _inventoryCellComponentsFilter.Value) {
                ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(cellEntity);
                if (_sceneData.Value.GetCarPartConfig(carPartInfoCmp.CarPartId).PartType == CarPartType.Main)
                    carPartInfoCmp.PartLevel++;
            }

            //upgrade main car parts

            if (carConfig.CarPartForLevel.ContainsKey(carCmp.CarLevel)) {
                var carPartsList = carConfig.CarPartForLevel[carCmp.CarLevel];

                inventoryCmp.CurrentGivenItems.AddRange(carPartsList);

                ActivateGiveCarPartsWindow();
            }

            UpdateCarDescription(inventoryCmp.CurrentRedactedCarEntity);
        } else
            _sceneData.Value._shopUIView.NotEnoughMoneyAnimation();
    }

    private void SetHealthToMinimumLevel() {
        
        ClearCarUI();
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        float minHealthLevel = _betweenScenesService.Value.GetMapDifficultyConfig().HealedHealthPartAfterEndRound;
        List<int> destroyerCars = new List<int>();
        foreach (var carEntity in inventoryCmp.PlayerTeammateCarsEntity) {
            bool isDestroyCar = false;
            var carCmp = _carComponentsPool.Value.Get(carEntity);
            foreach (var containerInfo in carCmp.CarPartContainerGroupsList) {
                ref var carPartCmp = ref _carPartComponentsPool.Value.Get(containerInfo.Key);
                ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(containerInfo.Key);
                if (minHealthLevel != 0 && carPartInfoCmp.CurrentHealth / carPartCmp.MaxHealth < minHealthLevel)
                    carPartInfoCmp.CurrentHealth = carPartCmp.MaxHealth * minHealthLevel;

                else if (minHealthLevel == 0 && carPartInfoCmp.CurrentHealth <= 0)
                    carPartInfoCmp.CurrentHealth = 1;

                /*  else if (minHealthLevel == 0 && carPartCmp.CurrentHealth == 0) {
                      DestroyPlayerCarPart(containerInfo.Key);
                  }*/
            }

            var carPartGroupList = new Dictionary<int, List<Vector2Int>>(carCmp.CarPartGroupsList);
            foreach (var carPartInfo in carPartGroupList) {
                ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartInfo.Key);
                ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(carPartInfo.Key);
                if (minHealthLevel != 0 && carPartInfoCmp.CurrentHealth / carPartCmp.MaxHealth < minHealthLevel) {
                    carPartInfoCmp.CurrentHealth = carPartCmp.MaxHealth * minHealthLevel;
                } else if (minHealthLevel == 0 && carPartInfoCmp.CurrentHealth <= 0) {
                    if (DestroyPlayerCarPart(carPartInfo.Key)) {
                        isDestroyCar = true;
                    }
                }
                
            }

            if (isDestroyCar) {
                //delete from player teammate
                if (inventoryCmp.CurrentRedactedCarEntity == carEntity) {
                    inventoryCmp.CurrentRedactedCarEntity = _sceneData.Value.PlayerEntity;
                }

                destroyerCars.Add(carEntity);
            }
            //for 0 health make delete car parts
        }

        foreach (var carEntity in destroyerCars) {
            inventoryCmp.PlayerTeammateCarsEntity.Remove(carEntity); 
            _sceneData.Value._inventoryUIView._selectCarButtons[inventoryCmp.PlayerTeammateCarsEntity.Count].gameObject.SetActive(false);
        }

        //update grid
        SetNewCarUI(inventoryCmp.CurrentRedactedCarEntity);
    }

    private bool DestroyPlayerCarPart(int cellEntity) {
        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(cellEntity);
        var carPartConfig = GetCarPartConfig(_carPartInfoComponentsPool.Value.Get(cellEntity).CarPartId);
        ref var carCmp = ref _carComponentsPool.Value.Get(carPartCmp.MainEntity);
    
        foreach (var cellPos in carCmp.CarPartGroupsList[cellEntity]) {
            carCmp.CarPartGroupsGrid[cellPos.x, cellPos.y] = 0;
        }

        carCmp.CarPartGroupsList.Remove(cellEntity);

        _world.Value.DelEntity(cellEntity);

        return carPartConfig.PartType == CarPartType.Main;
    }
    private void HealAllCarPart() {
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var carCmp = ref _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity);
        float healCost = GetHealAllCarPartCost();
        if (_playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).MoneyCount < healCost) {
            _sceneData.Value._shopUIView.NotEnoughMoneyAnimation();
            return;
        }
        SetMoneyCount(-healCost);
        foreach (var carPartEntity in carCmp.CarPartContainerGroupsList)
            HealCarPart(carPartEntity.Key);

        foreach (var carPartEntity in carCmp.CarPartGroupsList)
            HealCarPart(carPartEntity.Key);
        
        _sceneData.Value._inventoryUIView._healCarButton.gameObject.SetActive(false);
    }

    private void UpdateHealAllCarPartButton() {
        float healCost = GetHealAllCarPartCost();
        _sceneData.Value._inventoryUIView._healCarButton.gameObject.SetActive(healCost>0);
        _sceneData.Value._inventoryUIView._healCarText.text = $"{GetLocalization("Heal all car for")} {healCost}$";
    }

    private float GetHealAllCarPartCost() {
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var carCmp = ref _carComponentsPool.Value.Get(inventoryCmp.CurrentRedactedCarEntity);
        float healCost = 0;
        foreach (var carPartEntity in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartEntity.Key);
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(carPartEntity.Key);
            healCost += GetHealCostForCarPart(carPartCmp.MaxHealth, carPartInfoCmp.CurrentHealth, GetCarPartConfig(carPartInfoCmp.CarPartId));
        }

        foreach (var carPartEntity in carCmp.CarPartGroupsList) {
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartEntity.Key);
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(carPartEntity.Key);
            healCost += GetHealCostForCarPart(carPartCmp.MaxHealth, carPartInfoCmp.CurrentHealth, GetCarPartConfig(carPartInfoCmp.CarPartId));
        }

        return healCost;
    }

    private float GetHealCostForCarPart(float maxHp,float currentHp, CarPartConfig carPartConfig) {
        return carPartConfig.MoneyCost * (1f-currentHp / maxHp) / 4;
    }

    private void HealMarkedCarPart() {
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        float cost = 0;
        if (_carPartComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo);
            ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo);
            cost = GetHealCostForCarPart(carPartCmp.MaxHealth, carPartInfoCmp.CurrentHealth, GetCarPartConfig(carPartInfoCmp.CarPartId));
        } else if (_inventoryCellComponentsPool.Value.Has(inventoryCmp.CurrentMarkedCellForPanelInfo)) {
            ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(inventoryCmp.CurrentMarkedCellForPanelInfo);
            var carPartConfig = GetCarPartConfig(carPartInfoCmp.CarPartId);
            cost = GetHealCostForCarPart(carPartConfig.MaxHealth, carPartInfoCmp.CurrentHealth, carPartConfig);
        }

        if (cost > _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).MoneyCount) {
            _sceneData.Value._shopUIView.NotEnoughMoneyAnimation();
            return;
        }
        HealCarPart(inventoryCmp.CurrentMarkedCellForPanelInfo);
        SetMoneyCount(-cost);
        _sceneData.Value._carPartInfoUIView._healCarPartButton.gameObject.SetActive(false);
        UpdateHealAllCarPartButton();
    }

    private void HealCarPart(int entity) {
        ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(entity);
        carPartInfoCmp.CurrentHealth = GetCarPartConfig(carPartInfoCmp.CarPartId).MaxHealth *
                                       GridUtils.GetHealthLevelMultiplier(carPartInfoCmp.PartLevel) *
                                       GridUtils.GetModulesMultiplyers(carPartInfoCmp.ModuleIds).healthMultiplayer;
        
        UpdateHealAllCarPartButton();
    }
    
    //for admin 

    private void SetUpAdminButtons() {
        _sceneData.Value._enemiesCarBuilderView.GetCarPartIdButton.onClick.AddListener(GetCarPartForAdmin);
        _sceneData.Value._enemiesCarBuilderView.GetMoneyButton.onClick.AddListener(GetMoneyForAdmin);
        _sceneData.Value._enemiesCarBuilderView.GetMediumButton.onClick.AddListener((() => SetShopChancesToNeeded(new List<float>(){0,1,0,0})));
        _sceneData.Value._enemiesCarBuilderView.GetHighButton.onClick.AddListener((() => SetShopChancesToNeeded(new List<float>(){0,0,1,0})));
        _sceneData.Value._enemiesCarBuilderView.GetVeryHighButton.onClick.AddListener((() => SetShopChancesToNeeded(new List<float>(){0,0,0,1})));
    }
    private void GetMoneyForAdmin() {
        int.TryParse(_sceneData.Value._enemiesCarBuilderView.MoneyInputField.text, out int money);
        SetMoneyCount(money);
    }

    private void GetCarPartForAdmin() {
        int.TryParse(_sceneData.Value._enemiesCarBuilderView.GetCarPartIdInputField.text, out int carPartId);
        if (_sceneData.Value._carStuffListsConfig.CarPartConfigs.Length > carPartId) {
            _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentGivenItems.Add(carPartId);
            ActivateGiveCarPartsWindow();
        }
    }

    private void SetShopChancesToNeeded(List<float> chances) {
        ref var shopCmp = ref _shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        shopCmp.CurrentShopItemsChances = chances;
    }
}
