using System.Collections.Generic;
using System.Linq;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameStatesSystem : IEcsInitSystem, IEcsRunSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;

    private EcsPoolInject<EndRoundEvent> _endRoundEventsPool;
    private EcsPoolInject<CarComponent> _carComponentsPool;
    private EcsPoolInject<MovementComponent> _movementComponentsPool;
    private EcsPoolInject<CarPartCellComponent> _cellComponentsPool;
    private EcsPoolInject<InventoryComponent> _inventoryComponentsPool;
    private EcsPoolInject<FuelComponent> _fuelComponentsPool;
    private EcsPoolInject<GunComponent> _gunComponentsPool;
    private EcsPoolInject<ShopComponent> _shopComponentsPool;
    private EcsPoolInject<CurrentGameComponent> _currentGameComponentsPool;
    private EcsPoolInject<ConstructMeleeCellEvent> _constructMeleeCellEventsPool;
    private EcsPoolInject<CarPartInGameUIComponent> _carPartInGameUIComponentsPool;
    private EcsPoolInject<AddInventoryItemEvent> _addInventoryItemEventsPool;
    private EcsPoolInject<TaskComponent> _taskComponentsPool;
    private EcsPoolInject<PlayerCurrencyComponent> _playerCurrencyComponentsPool;
    private EcsPoolInject<LaserComponent> _laserComponentsPool;
    private EcsPoolInject<FilledZoneComponent> _filledZoneComponentsPool;
    private EcsPoolInject<UpdateShopToSaleRoundEvent> _updateShopToSaleRoundEventsPool;
    private EcsPoolInject<FromPointToPointRoundComponent> _fromPointToPointRoundComponentsPool;
    
    private EcsFilterInject<Inc<CarPartCellComponent>> _cellComponentsFilter;
    private EcsFilterInject<Inc<CarComponent>> _carComponentsFilter;
    private EcsFilterInject<Inc<TaskComponent>> _taskComponentsFilter;
    private EcsFilterInject<Inc<CarPartInGameUIComponent>> _carPartInGameUIComponentsFilter;

    private Vector2 _cellOffset = new Vector2(0.5f, 0.5f);

    public void Init(IEcsSystems systems) {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Add(_sceneData.Value.PlayerEntity);
        curGameCmp.RoundTypes = new List<RoundType[]>();
        curGameCmp.CurrentBattleRounds = 2;
        _sceneData.Value._inventoryUIView._playButton.onClick.AddListener(() => ChangeRoundInfo());
        _sceneData.Value._mapPointPrefab.SetupPoint(UpdateMapPoint);
        _sceneData.Value._mapFilledZoneView.SetupFilledZone(TriggerFilledZone);
    }

    public void Run(IEcsSystems systems) {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        switch (curGameCmp.CurrentRoundType) {
            case RoundType.BattleDefault:
                curGameCmp.RoundTime -= Time.deltaTime;
                _sceneData.Value._playerUIView._gameTimerText.text = ((int)curGameCmp.RoundTime).ToString();
                if (curGameCmp.RoundTime <= 0) {
                    curGameCmp.CurrentRoundType = RoundType.None;
                    _sceneData.Value._playerUIView._gameTimerText.gameObject.SetActive(false);
                    EndRound();
                }

                break;

            case RoundType.FromAToB:
                if(!_fromPointToPointRoundComponentsPool.Value.Has(_sceneData.Value.PlayerEntity))return;
                ref var pointToPointRound = ref _fromPointToPointRoundComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
                var moveCmp = _movementComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

               /* if (Vector2.Distance(moveCmp.MovableObjectRigidbody.transform.position, pointToPointRound.PointsToCheck[0]) < 1) {
                    pointToPointRound.PointsToCheck.RemoveAt(0);
                    Debug.Log("complete point");
                    if (pointToPointRound.PointsToCheck.Count == 0) {
                        EndPointToPointRound();
                        EndRound();
                    } else {
                        _sceneData.Value._mapPointPrefab.transform.position = pointToPointRound.PointsToCheck[0];
                        //smth particle effect
                    }
                } else {*/
                    UpdateArrowDirection(pointToPointRound.PointsToCheck[0], moveCmp.MovableObjectRigidbody.gameObject.transform.position);
                   
               // }

                break;
        }
    }

    private void UpdateMapPoint() {
        ref var pointToPointRound = ref _fromPointToPointRoundComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        pointToPointRound.PointsToCheck.RemoveAt(0);
        Debug.Log("complete point");
        if (pointToPointRound.PointsToCheck.Count == 0) {
            EndPointToPointRound();
            EndRound();
        } else {
            _sceneData.Value._mapPointPrefab.transform.position = pointToPointRound.PointsToCheck[0];
            //smth particle effect
        }
    }

    private void SetSaleShopRound() {
        _sceneData.Value._shopUIView._buttonUpdateShop.gameObject.SetActive(false);
        _updateShopToSaleRoundEventsPool.Value.Add(_sceneData.Value.PlayerEntity);
    }
    
    
    
    private void UpdateArrowDirection(Vector2 target, Vector2 playerPosition)
    {
       // Debug.Log(target + "t/pl" + playerPosition);
        Vector3 directionToTarget = target - playerPosition;
        
        if (directionToTarget.magnitude > 0.1f)
        {
            // Вычисляем угол поворота
            float targetAngle = Mathf.Atan2(directionToTarget.y,directionToTarget.x ) * Mathf.Rad2Deg;
            
            // Плавный поворот стрелки
//            Debug.Log(directionToTarget);
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            _sceneData.Value._playerUIView._arrowImageTransform.parent.rotation = Quaternion.Lerp(
                _sceneData.Value._playerUIView._arrowImageTransform.parent.rotation, 
                targetRotation, 
                3 * Time.deltaTime
            );
        }
        Vector3 screenPos = _sceneData.Value.mainCamera.WorldToScreenPoint(playerPosition);
        _sceneData.Value._playerUIView._arrowImageTransform.parent.position = Vector2.Lerp(_sceneData.Value._playerUIView._arrowImageTransform.parent.position,screenPos, 4*Time.deltaTime) ;
    }
    
    private int TaskCount() {
        return _taskComponentsFilter.Value.GetEntitiesCount();
    }

    private void SetupStartRound() {
        
    }
    private void SetMoneyText() {
        _sceneData.Value._shopUIView._moneyCountText.text =
            _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).moneyCount + "$";
    }

    private void EndRound() {
        _endRoundEventsPool.Value.Add(_sceneData.Value.PlayerEntity);
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        int currentRound = curGameCmp.CurrentRound - 1;
        _sceneData.Value._mapUIView.RoundDifficltyContainer.gameObject.SetActive(false);
        int moneyCount = (int)(_sceneData.Value._gameProcessConfig.GameRounds[currentRound].MoneyCount * curGameCmp.RoundMoneyMultiplayer);
        curGameCmp.RoundMoneyMultiplayer = 1;
        _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).moneyCount += moneyCount;
        SetMoneyText();
        _sceneData.Value._endGameScreenView.SetEndScreenInfo(moneyCount, currentRound);
        ChangeInventoryState(true);
        ChangePlayerUIState(false);
        ReleaseAllCarPartUI();
    }

    private void ReleaseAllCarPartUI() {
        foreach (var carPartUiEntity in _carPartInGameUIComponentsFilter.Value) 
            _sceneData.Value.ReleaseCarPartInGameUIView( _carPartInGameUIComponentsPool.Value.Get(carPartUiEntity).view);
    }

    private void ChangeRoundInfo() {
        if (!CanStartRound()) return;
        _sceneData.Value._mapUIView.gameObject.SetActive(true);
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        var currentSupportRoundTypes = RoundUtilits.SupportRoundsTypes.Distinct().ToList();
        if (TaskCount() == 5)
            currentSupportRoundTypes.Remove(RoundType.Task);
        (var randomRounds, int moneyMultiplayerRound) =
            RoundUtilits.GetRandomBattleRound(curGameCmp.CurrentBattleRounds, 3, currentSupportRoundTypes);
        curGameCmp.RoundTypes.Add(randomRounds);
        curGameCmp.RoundButtons = new EventTrigger[randomRounds.Length];
        var mapImageRect = _sceneData.Value._mapUIView.MapImage.rectTransform;
        _sceneData.Value._mapUIView.MapImage.transform.position -= new Vector3(0,
            mapImageRect.rect.height / _sceneData.Value._gameProcessConfig.GameRounds.Length, 0);
        for (int i = 0; i < randomRounds.Length; i++) {
            var roundButton = _sceneData.Value.InstantiateRoundButton();
            int newIndex = i;
            roundButton.GetComponent<Image>().sprite = _sceneData.Value._mapUIView.RoundSprites[randomRounds[i]];

            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => { StartCurrentRoundType(randomRounds[newIndex],(PointerEventData)data); });
            
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { ChangeRoundDescription(randomRounds[newIndex],(PointerEventData)data); });
            
            roundButton.triggers.Add(clickEntry);
            roundButton.triggers.Add(enterEntry);
          //  roundButton.onClick.AddListener(() => StartCurrentRoundType(randomRounds[newIndex]));
            float posX = (newIndex + 1) * (mapImageRect.rect.width / ((float)randomRounds.Length + 1)) - mapImageRect.rect.width / 2;
            float posY = (curGameCmp.CurrentRound + 1) *
                (mapImageRect.rect.height / ((float)_sceneData.Value._gameProcessConfig.GameRounds.Length + 2)) - mapImageRect.rect.height / 2;
            roundButton.transform.localPosition = new Vector2(posX, posY);
            curGameCmp.RoundButtons[i] = roundButton;
            if (moneyMultiplayerRound == i) {
                float currentMultiplayer = Random.Range(12, 20) * 0.1f;
                _sceneData.Value._mapUIView.SetMoneyMultiplayerText(currentMultiplayer,
                    new Vector2(roundButton.transform.position.x, roundButton.transform.position.y-70));
                curGameCmp.RoundMoneyMultiplayer = currentMultiplayer;
            }
        }

        _sceneData.Value._mapUIView.RoundNumberText.text = "Round" + (curGameCmp.CurrentRound + 1);
        ChangeInventoryState(false);

        ChangeRoundDescription(RoundType.None);
    }

    private void ChangeRoundDescription(RoundType roundType, PointerEventData data = null) {
        var descriptionText = _sceneData.Value._mapUIView.RoundDescriptionText;
        switch (roundType) {
            case RoundType.None:
                descriptionText.text = "";
                break;
            case RoundType.Task:
                descriptionText.text = " Get task with reward";
                break;
            case RoundType.BattleDefault:
                descriptionText.text = " Fight until the timer runs out";
                break;
            case RoundType.Chest:
                descriptionText.text = " Get random free car part";
                break;
            case RoundType.FromAToB:
                descriptionText.text = " Check all the given points";
                break;
            case RoundType.Shop:
                descriptionText.text = " Go to shop with big sales";
                break;
            case RoundType.Transfer:
                descriptionText.text = " Exchange two of your parts for one of higher quality";
                break;
            case RoundType.ZoneDefence:
                descriptionText.text = " Stay and defence zone";
                break;
        }
    }
    
    private void StartCurrentRoundType(RoundType roundType,PointerEventData data) {
        _sceneData.Value._shopUIView._buttonUpdateShop.gameObject.SetActive(true);
        
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var button in curGameCmp.RoundButtons) {
            button.enabled = false;
        }

        switch (roundType) {
            case RoundType.BattleDefault:
                SetEnemiesCars();
                SetGameTimer();
                SetPlayerCarPrefab();
                break;
            case RoundType.FromAToB:
                SetEnemiesCars();
                SetPlayerCarPrefab();
                SetNewPointToPointRound();
                break;
            case RoundType.ZoneDefence:
                SetEnemiesCars();
                SetPlayerCarPrefab();
                SetFilledZoneRound();
                break;
            case RoundType.Chest:
                var shopCmp = _shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
                _addInventoryItemEventsPool.Value.Add(_sceneData.Value.PlayerEntity).CarPartId =
                    shopCmp.CurrentShopItems[Random.Range(0, shopCmp.CurrentShopItems.Count)].PartId;
                //visual
                ChangeInventoryState(true);
                break;
            case RoundType.Shop:
                SetSaleShopRound();
                ChangeInventoryState(true);
              //  SetEnemiesCars();
                //SetPlayerCarPrefab();
                break;
            case RoundType.Transfer:
               // SetEnemiesCars();
               // SetPlayerCarPrefab();
               ChangeInventoryState(true);
                break;
            case RoundType.Task:
                ref var taskCmp = ref _taskComponentsPool.Value.Add(_world.Value.NewEntity());
                taskCmp.CurrentCount = 0;
                taskCmp.NeedCount= Random.Range(3, 6);
                taskCmp.TaskType = TaskUtilits.GetRandomTaskType();
                taskCmp.MoneyReward = (curGameCmp.CurrentRound+taskCmp.NeedCount) * 5;
                taskCmp.TaskText = GetTextForTask();
                taskCmp.TaskText.gameObject.SetActive(true);
                TaskUtilits.UpdateTaskText(taskCmp);
                ChangeInventoryState(true);
                break;
        }
   
        curGameCmp.RoundButtons = null;
        curGameCmp.CurrentBattleRounds += RoundUtilits.IsBattleRound(roundType) ? -1 : 1;
        if (curGameCmp.CurrentBattleRounds < 1)
            curGameCmp.CurrentBattleRounds = 1;
        curGameCmp.CurrentRoundType = roundType;
        //curGameCmp.RoundWithMoneyMultiplayer = -1;
        _sceneData.Value._mapUIView.gameObject.SetActive(false);
        ChangePlayerUIState(true);
        
        curGameCmp.CurrentRound++;
    }

    private void TriggerFilledZone() {
       Debug.Log("zoneTriggered");
        ref var filledZoneCmp =ref _filledZoneComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        filledZoneCmp.CurrentZoneTime += Time.deltaTime;
        _sceneData.Value._mapFilledZoneView.FilledZoneUI.fillAmount = filledZoneCmp.CurrentZoneTime / filledZoneCmp.NeedZoneTime;
        if (filledZoneCmp.CurrentZoneTime >= filledZoneCmp.NeedZoneTime) {
            EndFilledZoneRound();
            _filledZoneComponentsPool.Value.Del(_sceneData.Value.PlayerEntity);
            EndRound();
        }
    }

    private void EndFilledZoneRound() {
        _sceneData.Value._mapFilledZoneView.FilledZoneUI.gameObject.SetActive(false);
        _sceneData.Value._mapFilledZoneView.gameObject.SetActive(false);
    }
    
    private void SetFilledZoneRound() {
        _sceneData.Value._mapFilledZoneView.FilledZoneUI.gameObject.SetActive(true);
        _sceneData.Value._mapFilledZoneView.gameObject.SetActive(true);
       _filledZoneComponentsPool.Value.Add(_sceneData.Value.PlayerEntity).NeedZoneTime = 10;
        _sceneData.Value._mapFilledZoneView.ChangeZonePosition(GetRandomPointAround(Vector2.zero, 10));
    }
    private void SetNewPointToPointRound() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        curGameCmp.CurrentRoundType = RoundType.FromAToB;
       
        ref var pointToPointRoundCmp = ref _fromPointToPointRoundComponentsPool.Value.Add(_sceneData.Value.PlayerEntity);
        pointToPointRoundCmp.PointsToCheck = new List<Vector2>();
        int pointsCount = 2+curGameCmp.CurrentRound/5;
        Vector2 posToSpawn = Vector2.zero;
        for (int i = 0; i < pointsCount; i++) {
            posToSpawn = GetRandomPointAround(posToSpawn, 15);
                   pointToPointRoundCmp.PointsToCheck.Add(posToSpawn);
        }

        var carPartsGrid = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).GridCarPartsEntities;
        _sceneData.Value._playerUIView._arrowImageTransform.parent.transform.position =
            _sceneData.Value.mainCamera.WorldToScreenPoint(Vector3.zero);
        float arrowPos =
            Mathf.Max(carPartsGrid.GetLength(0), carPartsGrid.GetLength(1)) * 20;

        _sceneData.Value._playerUIView._arrowImageTransform.localPosition = new Vector3(arrowPos, 0, 0);

        _sceneData.Value._mapPointPrefab.transform.position = pointToPointRoundCmp.PointsToCheck[0];
        _sceneData.Value._mapPointPrefab.gameObject.SetActive(true);
        _sceneData.Value._playerUIView._arrowImageTransform.gameObject.SetActive(true);
       
    }

    private void EndPointToPointRound() {
        _fromPointToPointRoundComponentsPool.Value.Del(_sceneData.Value.PlayerEntity);
        _sceneData.Value._mapPointPrefab.gameObject.SetActive(false);
        _sceneData.Value._playerUIView._arrowImageTransform.gameObject.SetActive(false);
        
    }
    public Vector2 GetRandomPointAround(Vector2 startPoint, float distance)
    {
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        
        return startPoint + direction * distance;
    }

    private TMP_Text GetTextForTask() {
        foreach (var taskText in _sceneData.Value._playerUIView._taskTexts) {
            if (!taskText.gameObject.activeInHierarchy) return taskText;
        }

        return null;
    }
    
    private void SetGameTimer() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        _sceneData.Value._playerUIView._gameTimerText.gameObject.SetActive(true);
        curGameCmp.RoundTime = 30;
    }
    
    private void SetEndPoint() {
        
    }

    private void SetEnemiesCars() {
        var currentRoundCarStuff = _sceneData.Value._gameProcessConfig.GameRounds[_currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRound];

        Dictionary<CarPartType, List<int>> currentCarPartsList = new Dictionary<CarPartType, List<int>>();
        foreach (var carPartId in currentRoundCarStuff.CarParts) {
            var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartId);
            if (!currentCarPartsList.TryAdd(carPartConfig.PartType, new List<int> { carPartId }))
                currentCarPartsList[carPartConfig.PartType].Add(carPartId);
        }
//return;
        for (int i = 0; i < 3; i++) {
            int enemyEntity = _world.Value.NewEntity();
            ref var moveCmp = ref _movementComponentsPool.Value.Add(enemyEntity);
            ref var fuelCmp = ref _fuelComponentsPool.Value.Add(enemyEntity);
            fuelCmp.CurrentFuel = 200;
            ref var carCmp = ref _carComponentsPool.Value.Add(enemyEntity);
            var carVariables = currentRoundCarStuff.CarPresets[Random.Range(0, currentRoundCarStuff.CarPresets.Length)];
            carCmp.CarId = carVariables.CarId;
            int currentMainPartCount = 0;
            var carView = CarViewSetup(enemyEntity);
            carView.transform.position = new Vector2(Random.Range(-20, 20), Random.Range(-20, 20));
            var carMap = carVariables.MapsForCar[Random.Range(0, carVariables.MapsForCar.Length)];
            int[,] GridCarPartsEntities = new int[carMap.CarPartTypeGrid.Length, carMap.CarPartTypeGrid[0].types.Length];
            for (int j = 0; j < carMap.CarPartTypeGrid.Length; j++) {
                for (int k = 0; k < carMap.CarPartTypeGrid[0].types.Length; k++) {
                    var currentCarType = carMap.CarPartTypeGrid[j].types[k];
                    if (currentCarType == CarPartType.None) continue;
                    int carPartCellEntity = _world.Value.NewEntity();
                    GridCarPartsEntities[j, k] = carPartCellEntity;
                 
                    ref var cellCmp = ref _cellComponentsPool.Value.Add(carPartCellEntity);
                    cellCmp.CarPartsId = new List<int>();

                    cellCmp.MainEntity = enemyEntity;
                    if (currentCarType != CarPartType.Main) {
                        if (currentCarType == CarPartType.Gun || currentCarType == CarPartType.MeleeWeapon)
                            cellCmp.CarPartsId.Add(currentCarPartsList[CarPartType.WeaponContainer][
                                Random.Range(0, currentCarPartsList[CarPartType.WeaponContainer].Count)]);
                        cellCmp.CarPartsId.Add(currentCarPartsList[currentCarType][Random.Range(0, currentCarPartsList[currentCarType].Count)]);
                    } else {
                        if (currentMainPartCount == 0)
                            cellCmp.CarPartsId.Add(_sceneData.Value.GetCarConfig(carCmp.CarId).CarEngine.Id);
                        else if (currentMainPartCount == 1)
                            cellCmp.CarPartsId.Add(_sceneData.Value.GetCarConfig(carCmp.CarId).DriverCabine.Id);
                        currentMainPartCount++;
                    }

                    SpawnCarPartCellView(carPartCellEntity, carView, k, j, 0, false);
                }
            }

            SetupEnemyMovementComponent(enemyEntity, _sceneData.Value.GetCarConfig(carCmp.CarId));
        }
    }

    private void SetPlayerCarPrefab() {
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        var carView = CarViewSetup(_sceneData.Value.PlayerEntity);

        for (int i = 0; i < inventoryCmp.GridCarPartsEntities.GetLength(0); i++) {
            for (int j = 0; j < inventoryCmp.GridCarPartsEntities.GetLength(1); j++) {
                int cellEntity = inventoryCmp.GridCarPartsEntities[i, j];
                var cellCmp = _cellComponentsPool.Value.Get(cellEntity);
                SpawnCarPartCellView(cellEntity, carView, j, i, cellCmp.CellView._imageCarPart.transform.localEulerAngles.z, true);
            }
        }

        ArrangeImagesInGrid();
        
        //_sceneData.Value.mainCamera.orthographicSize = 15 + Mathf.Max(inventoryCmp.GridCarPartsEntities.GetLength(0), inventoryCmp.GridCarPartsEntities.GetLength(1));
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        _movementComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).MovableObjectRigidbody.mass = carCmp.CurrentWeight;
    }

    private void ArrangeImagesInGrid() {
        float spacing = 4f;
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        RectTransform parentRect = _sceneData.Value._playerUIView._carPratsHealthContainer;
        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;

        int columns = inventoryCmp.GridCarPartsEntities.GetLength(1);
        int rows = inventoryCmp.GridCarPartsEntities.GetLength(0);
        // Вычисляем размер каждого квадрата
        float cellSize = Mathf.Min((parentWidth - (columns - 1) * spacing) / columns, (parentHeight - (rows - 1) * spacing) / rows);

        float gridWidth = columns * cellSize + (columns - 1) * spacing;
        float gridHeight = rows * cellSize + (rows - 1) * spacing;

        // Начальная позиция (левый верхний угол)
        float startX = -gridWidth / 2 + cellSize / 2;
        float startY = gridHeight / 2 - cellSize / 2;

        for (int row = 0; row < rows; row++) {
            for (int col = 0; col < columns; col++) {
                int cellEntity = inventoryCmp.GridCarPartsEntities[row, col];
                float posX = startX + col * (cellSize + spacing);
                float posY = startY - row * (cellSize + spacing);
                var cellUIHealth = _sceneData.Value.GetCarPartInGameUIView();
                _carPartInGameUIComponentsPool.Value.Get(cellEntity).view = cellUIHealth;
                var cellCmp = _cellComponentsPool.Value.Get(cellEntity);
                if (cellCmp.MaxHealth == 0) {
                    cellUIHealth.gameObject.SetActive(false);
                } else {
                    cellUIHealth.gameObject.SetActive(true);
                    cellUIHealth.SetSize(new Vector2(cellSize, cellSize), new Vector2(posX, posY));
                    cellUIHealth.SetHealthCount(_sceneData.Value._healthGradient.Evaluate(cellCmp.CurrentHealth / cellCmp.MaxHealth),
                        (int)cellCmp.CurrentHealth);
                }
            }
        }
    }

    private bool CanStartRound() {
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (carCmp.CurrentWeight > _sceneData.Value.GetCarConfig(carCmp.CarId).MaxWeight) return false;
       if (MainPartIsPlaced(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity))) return true;
        return false;
    }

    private void SpawnCarPartCellView(int cellEntity, CarView carView, int j, int i, float needRotate, bool isPlayer) {
        ref var cellCmp = ref _cellComponentsPool.Value.Get(cellEntity);
        for (int k = 0; k < cellCmp.CarPartsId.Count; k++) {
            var carPartConfig = _sceneData.Value._carStuffListsConfig.CarPartConfigs[cellCmp.CarPartsId[k]];
            bool isWeapon = carPartConfig.PartType == CarPartType.MeleeWeapon || carPartConfig.PartType == CarPartType.Gun;
            var carPart = carPartConfig.PartType == CarPartType.MeleeWeapon
                ? _sceneData.Value.GetMeleeCarPartCell(isWeapon ? cellCmp.MainCellPart.transform : carView.CarPartsContainer)
                : _sceneData.Value.GetCarPartCell(isWeapon ? cellCmp.MainCellPart.transform : carView.CarPartsContainer);

            int needSpriteLevel = isWeapon ? 2 : 1;
            carPart.SetupCarPart(cellEntity, carPartConfig.PartSprite, needSpriteLevel);
            if (isWeapon) {
                cellCmp.MaxHealth += carPartConfig.MaxHealth;
                cellCmp.CurrentHealth += carPartConfig.MaxHealth;
            } else {
                cellCmp.MaxHealth = carPartConfig.MaxHealth;
                cellCmp.CurrentHealth = carPartConfig.MaxHealth;
            }
        
            if (carPartConfig.PartType == CarPartType.Gun) {
                carPart.SetColliderEnableState(false);
                ref var gunCmp = ref _gunComponentsPool.Value.Add(cellEntity);
                var gunConfig = _sceneData.Value.GetGunFromCarPart(cellCmp.CarPartsId[k]);
                gunCmp.CurrentCouldown = gunConfig.ReloadDuration;
                gunCmp.CurrentMagazineCount = gunConfig.BulletsCountInMagazine;
                carPart.transform.localPosition = Vector3.zero;
                carPart.transform.localRotation = Quaternion.Euler(0, 0, 0);
                if (gunConfig.MissleType == MissleType.laser) {
                    _laserComponentsPool.Value.Add(cellEntity).LaserLineRenderer = _sceneData.Value.GetLaserLineRenderer();
                }
                
            } else if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
                carPart.transform.localRotation = Quaternion.Euler(0, 0, 0);
                carPart.transform.localPosition = Vector3.zero;
                carPart.MakeTriggerCollider();
                carPart.SetColliderEnableState(true);
                _constructMeleeCellEventsPool.Value.Add(cellEntity);
            } else {
                cellCmp.MainCellPart = carPart;
                carPart.transform.localPosition = new Vector2(j, i) + _cellOffset;
                carPart.transform.localRotation = Quaternion.Euler(0, 0, needRotate);
                carPart.SetColliderEnableState(true);
                if (carPartConfig.PartType == CarPartType.Support) {
                    var supportCarPartConfig = _sceneData.Value.GetSupportModuleFromCarPart(cellCmp.CarPartsId[k]);
                    switch (supportCarPartConfig.ModuleType) {
                        case SupportModuleType.Fuel:
                            ref var fuelCmp = ref _fuelComponentsPool.Value.Get(cellCmp.MainEntity);
                            fuelCmp.CurrentFuel += supportCarPartConfig.FirstCount;
                            fuelCmp.MaxFuel += supportCarPartConfig.FirstCount;
                            Debug.Log(fuelCmp.MaxFuel); //id breack delete current fuel
                            break;
                        case SupportModuleType.Turbine:
                            _fuelComponentsPool.Value.Get(cellCmp.MainEntity).FuelConsumption += supportCarPartConfig.FirstCount;
                            _movementComponentsPool.Value.Get(cellCmp.MainEntity).MaxMoveSpeed += supportCarPartConfig.SecondCount;
                            break;
                    }
                }
            }

            _carComponentsPool.Value.Get(cellCmp.MainEntity).CurrentWeight += carPartConfig.Weight;

            if (carPartConfig.PartType != CarPartType.MeleeWeapon)
                carPart.gameObject.layer = isPlayer ? LayerMask.NameToLayer("Player") : LayerMask.NameToLayer("Enemy");
            carPart.gameObject.tag = isPlayer ? "Player" : "Enemy";
            
                
        }
    }

    private void SetupEnemyMovementComponent(int enemyEntity, CarConfig carConfig) {
        ref var movementComponent = ref _movementComponentsPool.Value.Get(enemyEntity);
        movementComponent.RotateSpeed = carConfig.RotateSpeed * 0.5f * _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).RoundMoneyMultiplayer;
        movementComponent.Acceleration = carConfig.Acceleration;
        movementComponent.Deceleration = carConfig.Deceleration;
        movementComponent.MovableObjectRigidbody.mass = _carComponentsPool.Value.Get(enemyEntity).CurrentWeight;
        movementComponent.MaxMoveSpeed = carConfig.DefaultMaxSpeed * (1-_carComponentsPool.Value.Get(enemyEntity).CurrentWeight / carConfig.MaxWeight);
        if (movementComponent.MaxMoveSpeed < 4)
            movementComponent.MaxMoveSpeed = 4;
    }
    private void ColissionCarsHit(int entityOne, int entityTwo) {
        ref var firstMovementCmp = ref _movementComponentsPool.Value.Get(entityOne);
        var firstCarCmp = _carComponentsPool.Value.Get(entityOne);

        ref var secondMovementCmp = ref _movementComponentsPool.Value.Get(entityTwo);
        var secondCarCmp = _carComponentsPool.Value.Get(entityTwo);

        Vector2 direction = (firstMovementCmp.MovableObjectRigidbody.transform.position -
                             secondMovementCmp.MovableObjectRigidbody.transform.position).normalized;
        Vector2 relativeVelocity = secondMovementCmp.Velocity - firstMovementCmp.Velocity;
        float collisionForce = relativeVelocity.magnitude;

        // Соотношение масс
        float totalMass = firstCarCmp.CurrentWeight + secondCarCmp.CurrentWeight;
        float car1Ratio = secondCarCmp.CurrentWeight / totalMass;
        float minForce = 0.15f;
        if (collisionForce > minForce) {
            firstMovementCmp.CurrentMoveSpeed -= collisionForce * car1Ratio * secondCarCmp.CurrentWeight;
            if (firstMovementCmp.CurrentMoveSpeed < 0)
                firstMovementCmp.CurrentMoveSpeed = 0;
            if (collisionForce > 0.4f)
                collisionForce = 0.4f;
            firstMovementCmp.CantMoveTime = collisionForce;
            firstMovementCmp.MovableObjectRigidbody.AddForce(direction * secondCarCmp.CurrentWeight * car1Ratio / 3, ForceMode2D.Impulse);
        }
    }
    private CarView CarViewSetup(int entity) {
        var carView = _sceneData.Value.InstantiateCarView();
        _movementComponentsPool.Value.Get(entity).MovableObjectRigidbody = carView.Rigidbody;
        carView.Construct(entity,ColissionCarsHit);
        ref var carCmp = ref _carComponentsPool.Value.Get(entity);
        carCmp.CarView = carView;
        var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);
        carView.CarBaseSpriteRenderer.sprite = carConfig.CarBackground;
        carView.CarPartsContainer.localPosition = new Vector3(-(float)carConfig.Width / 2, (float)carConfig.Height / 2, 0);
        carCmp.CurrentWeight += carConfig.MaxWeight / 5f;
        carCmp.ForwardWheels = new Transform[carConfig.ForwardWheelsPositions.Length];
        carCmp.CurrentWeight = carConfig.CarWeight;
        int index = 0;
        foreach (var pos in carConfig.ForwardWheelsPositions) {
            Animator wheelTransform = CreateWheel(carConfig, carView, pos);
           // wheelTransform.GetComponent<FixedJoint2D>().connectedBody = carView.Rigidbody;
            carCmp.ForwardWheels[index] = wheelTransform.transform;
            wheelTransform.transform.SetParent(carView.ForwardWheelsContainer);
            index++;
        }

        foreach (var pos in carConfig.BackWheelsPositions) {
            CreateWheel(carConfig, carView, pos);
        }

        carView.CarBaseCollider.size = new Vector2(carConfig.Width, carConfig.Height);

        return carView;
    }

    private Animator CreateWheel(CarConfig carConfig, CarView carView, Vector2 pos) {
        var wheelTransform = _sceneData.Value.InstantiateAnimator(_sceneData.Value._carStuffListsConfig.Wheels[carConfig.WheelId],
            carView.CarBaseSpriteRenderer.transform);
        AnimatorClipInfo[] clipInfo = wheelTransform.GetCurrentAnimatorClipInfo(0);
        float clipLength = clipInfo[0].clip.length;

        float randomTime = Random.Range(0f, clipLength);
        wheelTransform.Play("Spin", 0, randomTime / clipLength);

        int scaleMultiplayer = pos.x > 0 ? 1 : -1;
        wheelTransform.transform.localScale = new Vector3(scaleMultiplayer, 1f, 1f);
        wheelTransform.transform.localPosition = pos;
        return wheelTransform;
    }

    private bool MainPartIsPlaced(InventoryComponent inventoryCmp) {
        foreach (var partId in inventoryCmp.PartsInInventory) {
            if (_sceneData.Value._carStuffListsConfig.CarPartConfigs[partId].PartType == CarPartType.Main)
                return false;
        }

        return true;
    }

    private void ChangeInventoryState(bool state) => _sceneData.Value._inventoryUIView.gameObject.SetActive(state);
    private void ChangePlayerUIState(bool state) => _sceneData.Value._playerUIView.gameObject.SetActive(state);
}