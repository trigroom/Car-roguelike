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
    private EcsPoolInject<StartRoundEvent> _startRoundEventsPool;
    private EcsPoolInject<CarPartComponent> _carPartComponentsPool;
    private EcsPoolInject<EnemyAIComponent> _enemyAIComponentsPool;
    private EcsPoolInject<MeleeWeaponComponent> _meleeWeaponComponentsPool;
    private EcsPoolInject<WheelContainerComponent> _wheelContainerComponentsPool;
    private EcsPoolInject<SettingsComponent> _settingsComponentsPool;
    private EcsPoolInject<CompleteTaskEvent> _completeTaskEventsPool;
    
    private EcsFilterInject<Inc<CarPartCellComponent>> _cellComponentsFilter;
    private EcsFilterInject<Inc<CarComponent>> _carComponentsFilter;
    private EcsFilterInject<Inc<FilledZoneComponent>> _filledZoneComponentsFilter;
    private EcsFilterInject<Inc<TaskComponent>> _taskComponentsFilter;
    private EcsFilterInject<Inc<CarPartInGameUIComponent>> _carPartInGameUIComponentsFilter;

    private Vector2 _cellOffset = new Vector2(0.5f, 0.5f);

    public void Init(IEcsSystems systems) {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        curGameCmp.RoundTypes = new List<RoundType[]>();
        curGameCmp.CurrentBattleRounds = 2;
        _settingsComponentsPool.Value.Add(_sceneData.Value.PlayerEntity).pixelizedEffectIsUsed = true;
        _sceneData.Value._inventoryUIView._playButton.onClick.AddListener(ChangeRoundInfo);
        _sceneData.Value._mapPointPrefab.SetupPoint(UpdateMapPoint);
        _sceneData.Value._mapFilledZoneView.SetupFilledZone(SetFeelingZoneState);
        
        var settingsView = _sceneData.Value._settingsView;
        settingsView._settingsChangeStateButton.onClick.AddListener(SetSettingsState);
        settingsView._saveSettingsButton.onClick.AddListener(SaveSettings);
        settingsView._blueColorSlider.onValueChanged.AddListener(ChangePickedEnemyProjectilesColor);
        settingsView._redColorSlider.onValueChanged.AddListener(ChangePickedEnemyProjectilesColor);
        settingsView._greenColorSlider.onValueChanged.AddListener(ChangePickedEnemyProjectilesColor);
        settingsView._setPixelizedEffectModeButton.onClick.AddListener(ChangePixelizedEffectsMode);
        SetColorSliderEnemyProjectilesValues();
        GenerateMap();
    }

    public void Run(IEcsSystems systems) {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        foreach (var filledZone in _filledZoneComponentsFilter.Value) {
            TriggerFilledZone();
        }

        switch (curGameCmp.CurrentRoundType) {
            case RoundType.BattleDefault:
                curGameCmp.RoundTime += Time.deltaTime;
                _sceneData.Value._playerUIView._gameTimerText.text = ((int)(30+curGameCmp.CurrentRound*3 - curGameCmp.RoundTime)).ToString();
                if (curGameCmp.RoundTime > 30+curGameCmp.CurrentRound*3) {
                    _sceneData.Value._playerUIView._gameTimerText.gameObject.SetActive(false);
                    EndRound();
                }

                break;

            case RoundType.FromAToB:
                curGameCmp.RoundTime += Time.deltaTime;
                if (!_fromPointToPointRoundComponentsPool.Value.Has(_sceneData.Value.PlayerEntity)) return;
                ref var pointToPointRound = ref _fromPointToPointRoundComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
                var moveCmp = _movementComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
                if (moveCmp.MovableObjectRigidbody != null)
                    UpdateArrowDirection(pointToPointRound.PointsToCheck[0], moveCmp.MovableObjectRigidbody.gameObject.transform.position);
                if (pointToPointRound.TimeAfterMarkPoint > 0)
                    pointToPointRound.TimeAfterMarkPoint -= Time.deltaTime;
                break;
            
            case RoundType.ZoneDefence:
                curGameCmp.RoundTime += Time.deltaTime;
                break;
        }
        
        if (curGameCmp.RoundTime / 30 > curGameCmp.RoundAddedEnemies) {
            if (curGameCmp.CurrentRoundType != RoundType.BattleDefault || curGameCmp.RoundTime < (25 + curGameCmp.CurrentRound * 3))
                SetEnemiesCars(1, false);
            curGameCmp.RoundAddedEnemies++;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) SetSettingsState();

    }

    private void SetSettingsState() {
        _sceneData.Value._settingsView._settingsContainer.gameObject.SetActive(!_sceneData.Value._settingsView._settingsContainer.gameObject.activeInHierarchy);
    }

    private void SetColorSliderEnemyProjectilesValues() {
        var settingsView = _sceneData.Value._settingsView;
        var needColor = _sceneData.Value._carStuffListsConfig.EnemyProjectilesMaterial.GetColor("_OffsetColor");
        settingsView._redColorSlider.value = needColor.r*settingsView._redColorSlider.maxValue;
            settingsView._greenColorSlider.value = needColor.g*settingsView._greenColorSlider.maxValue;
                settingsView._blueColorSlider.value = needColor.b*settingsView._blueColorSlider.maxValue;
    }

    private void SaveSettings() {
        var settingsView = _sceneData.Value._settingsView;

        var newColor = new Color(settingsView._redColorSlider.normalizedValue, settingsView._blueColorSlider.normalizedValue,
            settingsView._greenColorSlider.normalizedValue);
        _sceneData.Value._carStuffListsConfig.EnemyProjectilesMaterial.SetColor("_OffsetColor", newColor);
    }

    private void ChangePixelizedEffectsMode() {
        var settingsView = _sceneData.Value._settingsView; 
       
        bool isPixelized = !settingsView._setPixelizedEffectModeChecker.gameObject.activeInHierarchy;
        settingsView._setPixelizedEffectModeChecker.gameObject.SetActive(isPixelized);
        _sceneData.Value._pixelizedEffectsImage.gameObject.SetActive(isPixelized);
        _sceneData.Value._pixelizedEffectsUnderCarImage.gameObject.SetActive(isPixelized);
        if (!isPixelized) {
            _sceneData.Value.mainCamera.cullingMask |= (1 << 9);
            _sceneData.Value.mainCamera.cullingMask |= (1 << 3);
        } else {
            _sceneData.Value.mainCamera.cullingMask &= ~(1 << 9);
            _sceneData.Value.mainCamera.cullingMask &= ~(1 << 3);
        }
      
    }

    private void ChangePickedEnemyProjectilesColor(float value) {
        var settingsView = _sceneData.Value._settingsView;
        settingsView._projectilesOffsetColorImage.color = new Color(settingsView._redColorSlider.normalizedValue,
            settingsView._blueColorSlider.normalizedValue, settingsView._greenColorSlider.normalizedValue);
        
        settingsView._redColorText.text = settingsView._redColorSlider.value.ToString();
        settingsView._greenColorText.text = settingsView._greenColorSlider.value.ToString();
        settingsView._blueColorText.text = settingsView._blueColorSlider.value.ToString();
    }

    private void UpdateMapPoint() {
        if(!_fromPointToPointRoundComponentsPool.Value.Has(_sceneData.Value.PlayerEntity))return;
        ref var pointToPointRound = ref _fromPointToPointRoundComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if(pointToPointRound.TimeAfterMarkPoint > 0)return;
        pointToPointRound.PointsToCheck.RemoveAt(0);
        pointToPointRound.TimeAfterMarkPoint = 1;
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
            _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).MoneyCount + "$";
    }

    private void EndRound() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        curGameCmp.CurrentRoundType = RoundType.None;
        _endRoundEventsPool.Value.Add(_sceneData.Value.PlayerEntity);
        int currentRound = curGameCmp.CurrentRound - 1;
        _sceneData.Value._mapUIView.RoundDifficltyContainer.gameObject.SetActive(false);
        int moneyCount = (int)(_sceneData.Value._gameProcessConfig.GameRounds[currentRound].MoneyCount * curGameCmp.RoundMoneyMultiplayer);
        curGameCmp.RoundMoneyMultiplayer = 1;
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        int allPartCount = 0;
        int brokenPartCount = 0;
        foreach (var carPartInfo in carCmp.CarPartGroupsList) {
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartInfo.Key);
            allPartCount += carPartInfo.Value.Count;
            if (carPartCmp.CurrentHealth <= 0)
                brokenPartCount += carPartInfo.Value.Count;
            var supportModule = _sceneData.Value.GetSupportModuleFromCarPart(carPartCmp.CarPartId);
            if (supportModule && _carPartComponentsPool.Value.Get(carPartInfo.Key).CurrentHealth > 0 &&
                supportModule.ModuleType == SupportModuleType.Money) {
                moneyCount += (int)supportModule.FirstCount;
            }
                
        }

        foreach (var taskEntity in _taskComponentsFilter.Value) {
            ref var taskCmp = ref _taskComponentsPool.Value.Get(taskEntity);
            if (taskCmp.TaskType == TaskType.CarIntegrity && (float)brokenPartCount / allPartCount <= 0.1f) {
                taskCmp.CurrentCount++;
                _completeTaskEventsPool.Value.Add(taskEntity);
            }
        }
        
        _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).MoneyCount += moneyCount;
        SetMoneyText();
        _sceneData.Value._endGameScreenView.SetEndScreenInfo(moneyCount, currentRound);
        ChangeInventoryState(true);
        ChangePlayerUIState(false);
      //  ReleaseAllCarPartUI();
    }

    private void ReleaseAllCarPartUI() {
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var carPartUi in inventoryCmp.CarPartInfoUIViewsGrid) {
            if (carPartUi)
                _sceneData.Value.ReleaseCarPartInGameUIView(carPartUi);
        }
        foreach (var carPartUi in inventoryCmp.CarPartInfoUIViewsUpLevelGrid) {
            if (carPartUi)
                _sceneData.Value.ReleaseCarPartInGameUIView(carPartUi);
        }
        
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
            mapImageRect.rect.height/2* _sceneData.Value.mainCanvas.localScale.x/ _sceneData.Value._gameProcessConfig.GameRounds.Length, 0);
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
          float posX = (newIndex + 1) * (mapImageRect.rect.width / ((float)randomRounds.Length + 1)) -
                       mapImageRect.rect.width / 2 ;
          float posY = (curGameCmp.CurrentRound + 1) *
                       (mapImageRect.rect.height / ((float)_sceneData.Value._gameProcessConfig.GameRounds.Length + 2)) -
                       mapImageRect.rect.height / 2 ;
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
        curGameCmp.RoundTime = 0;
        curGameCmp.RoundAddedEnemies = 1;
        switch (roundType) {
            case RoundType.BattleDefault:
                SetPlayerCarPrefab();
                SetEnemiesCars(3);
                SetGameTimer();
                break;
            case RoundType.FromAToB:   
                SetPlayerCarPrefab();
                SetEnemiesCars(3);
                SetNewPointToPointRound();
                break;
            case RoundType.ZoneDefence:
                SetPlayerCarPrefab();
                SetEnemiesCars(3);
                SetFilledZoneRound();
                break;
            case RoundType.Chest:
                var shopCmp = _shopComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
                _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentGivenItems
                    .Add(shopCmp.CurrentShopItems[Random.Range(0, shopCmp.CurrentShopItems.Count)]);
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
                CreateNewTask(curGameCmp);
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
        
        curGameCmp.CurrentRound++;
        _startRoundEventsPool.Value.Add(_sceneData.Value.PlayerEntity);
    }

    private void CreateNewTask(CurrentGameComponent curGameCmp) {
        ref var taskCmp = ref _taskComponentsPool.Value.Add(_world.Value.NewEntity());
        taskCmp.CurrentCount = 0;
        taskCmp.NeedCount= Random.Range(3, 6);
        taskCmp.TaskType = TaskUtilits.GetRandomTaskType();
        taskCmp.MoneyReward = taskCmp.TaskType == TaskType.CarIntegrity? 1:(curGameCmp.CurrentRound+taskCmp.NeedCount) * 5;
        taskCmp.TaskText = GetTextForTask();
        taskCmp.TaskText.gameObject.SetActive(true);
        TaskUtilits.UpdateTaskText(taskCmp);
    }

    private void SetFeelingZoneState(bool isFeeling) {
        _filledZoneComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).ZoneIsFeeling = isFeeling;
    }
    private void TriggerFilledZone() {
        ref var filledZoneCmp =ref _filledZoneComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if(!filledZoneCmp.ZoneIsFeeling)return;
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

    private Vector2 GetPositionAroundPlayer() {
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        Vector2 carSize = new Vector2(carCmp.CarPartContainerGroupsGrid.GetLength(0)/2f, carCmp.CarPartContainerGroupsGrid.GetLength(1)/2f); 
        
        return GetRandomPointAround(carCmp.CarView.CarPartsContainer.transform.position, 10+Mathf.Max(carSize.x,carSize.y));
    }
    
    private void SetFilledZoneRound() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        
        _sceneData.Value._mapFilledZoneView.FilledZoneUI.gameObject.SetActive(true);
        _sceneData.Value._mapFilledZoneView.gameObject.SetActive(true);
       _filledZoneComponentsPool.Value.Add(_sceneData.Value.PlayerEntity).NeedZoneTime = 10+curGameCmp.CurrentRound*3;
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
                   Debug.Log(posToSpawn + "posToSpawn");
        }

        var carPartsGrid = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CarPartContainerGroupsGrid;
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
        int maxAttempts = 10;
        int attempts = 0;
    
        while (attempts < maxAttempts)
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            Vector2 targetPoint = startPoint + direction * distance;
        
            // Проверяем границы
            if (targetPoint.x > 100f || targetPoint.x < -100f || 
                targetPoint.y > 100f || targetPoint.y < -100f)
            {
                attempts++;
                continue;
            }
        
            // Проверяем коллайды в целевой точке
            if (!HasObstacleAtPoint(targetPoint))
            {
                return targetPoint;
            }
        
            attempts++;
        }
    
        Debug.LogWarning("Не удалось найти валидную точку после " + maxAttempts + " попыток");
        return startPoint;
    }

    private bool HasObstacleAtPoint(Vector2 point)
    {
        // Создаем маску для слоя Obstacle
        int obstacleLayerMask = 1 << LayerMask.NameToLayer("Obstacle");
    
        // Проверяем, есть ли коллайдер Obstacle в точке
        Collider2D hit = Physics2D.OverlapCircle(point, 0.1f, obstacleLayerMask);
    
        return hit != null;
    }

    private TMP_Text GetTextForTask() {
        foreach (var taskText in _sceneData.Value._playerUIView._taskTexts) {
            if (!taskText.gameObject.activeInHierarchy) return taskText;
        }

        return null;
    }
    
    private void SetGameTimer() {
        _sceneData.Value._playerUIView._gameTimerText.gameObject.SetActive(true);
       
    }
    
    private void SetEndPoint() {
        
    }

    private void BuildBorder(int entity) {
        var carCmp = _carComponentsPool.Value.Get(entity);
        PolygonCollider2D collider = carCmp.CarView.CarBaseCollider;
        
        var grid = carCmp.CarPartContainerGroupsGrid;
      
        List<Vector2> contour = GridUtils.FindBorderContour(grid);
       
        collider.SetPath(0, contour.ToArray());
    }

    private void SetEnemiesCars(int enemiesCount, bool onStartRoundSpawn = true) {
        int roundNumber = _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRound;
        if (!onStartRoundSpawn)
            roundNumber--;
        var currentRoundCarStuff =
            _sceneData.Value._gameProcessConfig.GameRounds[roundNumber];
        
        List<Transform> playerMainParts = new List<Transform>();
        var playerCarCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        foreach (var carPartCmpEntity in playerCarCmp.CarPartGroupsList) {
            playerMainParts.Add(_carPartComponentsPool.Value.Get(carPartCmpEntity.Key).CellCarPartsStuffViews[0].transform);
        }
        
        for (int i = 0; i < enemiesCount; i++) {
            int enemyEntity = _world.Value.NewEntity();
        
            _movementComponentsPool.Value.Add(enemyEntity);
            ref var fuelCmp = ref _fuelComponentsPool.Value.Add(enemyEntity);
            fuelCmp.CurrentFuel = 200;
            ref var carCmp = ref _carComponentsPool.Value.Add(enemyEntity);
            var carVariables = currentRoundCarStuff.CarPresets[Random.Range(0, currentRoundCarStuff.CarPresets.Length)];
            carCmp.CarId = carVariables.CarId;
            var carView = CarViewSetup(enemyEntity);
            
            carView.transform.position = GetPositionAroundPlayer();
            
            carCmp.CarPartGroupsGrid = new int[carVariables.GridSize.x, carVariables.GridSize.y];
            carCmp.CarPartContainerGroupsGrid = new int[carVariables.GridSize.x, carVariables.GridSize.y];
            carCmp.CarPartViewsGrid = new CarPartStuffView[carVariables.GridSize.x, carVariables.GridSize.y];
            
            carCmp.CarPartContainerGroupsList = new Dictionary<int, List<Vector2Int>>();
            carCmp.CarPartGroupsList = new Dictionary<int, List<Vector2Int>>();
            
            foreach (var carContainer in carVariables.CarPartDownLevelGrid) {
                var carPartConfig = _sceneData.Value.GetCarPartConfig(carContainer.partId);
                var currentCarType = carPartConfig.PartType;
                
                if (currentCarType == CarPartType.None) continue;
                
                int carPartCellEntity = _world.Value.NewEntity();
                var placedCells = GridUtils.GetCellsFromField(carContainer.gridPosition, carPartConfig, carContainer.rotate,
                    carCmp.CarPartContainerGroupsGrid);
                carCmp.CarPartContainerGroupsList.Add(carPartCellEntity, new List<Vector2Int>());
                
                foreach (var cell in placedCells) {
                    carCmp.CarPartContainerGroupsGrid[cell.x, cell.y] = carPartCellEntity;
                    carCmp.CarPartContainerGroupsList[carPartCellEntity].Add(cell);
                }

                ref var carPartCmp = ref _carPartComponentsPool.Value.Add(carPartCellEntity);

                carPartCmp.MainEntity = enemyEntity;
                carPartCmp.CarPartId = carContainer.partId;

                SpawnCarPartCellView(carPartCellEntity, carView, false, carContainer.rotate);
            }
            
            foreach (var carContainer in carVariables.CarPartUpLevelGrid) {
                var carPartConfig = _sceneData.Value.GetCarPartConfig(carContainer.partId);
                var currentCarType = carPartConfig.PartType;
                
                if (currentCarType == CarPartType.None) continue;
                
                int carPartCellEntity = _world.Value.NewEntity();
                var placedCells = GridUtils.GetCellsFromField(carContainer.gridPosition, carPartConfig, carContainer.rotate,
                    carCmp.CarPartGroupsGrid);
                carCmp.CarPartGroupsList.Add(carPartCellEntity, new List<Vector2Int>());
                
                foreach (var cell in placedCells) {
                    carCmp.CarPartGroupsGrid[cell.x, cell.y] = carPartCellEntity;
                    carCmp.CarPartGroupsList[carPartCellEntity].Add(cell);
                }

                ref var carPartCmp = ref _carPartComponentsPool.Value.Add(carPartCellEntity);

                carPartCmp.MainEntity = enemyEntity;
                carPartCmp.CarPartId = carContainer.partId;

                SpawnCarPartCellView(carPartCellEntity, carView, false, carContainer.rotate);
            }
            SetupEnemyMovementComponent(enemyEntity, _sceneData.Value.GetCarConfig(carCmp.CarId));
            SetupWheels(enemyEntity, carView);

            ref var enemyAiCmp = ref _enemyAIComponentsPool.Value.Add(enemyEntity);
            enemyAiCmp.CheckedEnemyLeftPoints = new List<Transform>();
            enemyAiCmp.CheckedEnemyRightPoints = new List<Transform>();
            enemyAiCmp.LastCheckedTime = 1f;
            enemyAiCmp.TargetToMove = playerMainParts[Random.Range(0, playerMainParts.Count)];
            SetEnemyCollisionCheckers(carCmp.CarPartContainerGroupsGrid, carView,ref enemyAiCmp);
            BuildBorder(enemyEntity);
        }
    }

    private void SetEnemyCollisionCheckers(int[,] grid, CarView carView, ref EnemyAIComponent enemyAiCmp) {
        for (int j = 0; j < grid.GetLength(0); j++) {
            for (int k = 0; k < grid.GetLength(1); k++) {
                if (grid[j, k] != 0) {
                    var obstacleCheckerTransform = _sceneData.Value.GetGunContainer(carView.CarPartsContainer);
                    obstacleCheckerTransform.localPosition = new Vector3(j - grid.GetLength(0) / 2f,
                        k + grid.GetLength(1) / 2f, 0);
                    if (j > grid.GetLength(0) / 2)
                        enemyAiCmp.CheckedEnemyLeftPoints.Add(obstacleCheckerTransform);
                    else
                        enemyAiCmp.CheckedEnemyRightPoints.Add(obstacleCheckerTransform);
                    break;
                }
            }
        }
    }

    private void SetPlayerCarPrefab() {
        
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        _fuelComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).UseSpeedBoost = false;
        var carView = CarViewSetup(_sceneData.Value.PlayerEntity);
        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        
        carCmp.CarPartViewsGrid =
            new CarPartStuffView[carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1)];
        carCmp.CurrentMaxWeight = 0;
        foreach (var carPartInfo in carCmp.CarPartContainerGroupsList) {
            SpawnCarPartCellView(carPartInfo.Key, carView, true,
                (int)_carPartComponentsPool.Value.Get(carPartInfo.Key).carPartCellImage.transform.eulerAngles.z);
        }

        foreach (var carPartInfo in carCmp.CarPartGroupsList) {
            SpawnCarPartCellView(carPartInfo.Key, carView, true,
                (int)_carPartComponentsPool.Value.Get(carPartInfo.Key).carPartCellImage.transform.eulerAngles.z);
        }
        
        SetupWheels(_sceneData.Value.PlayerEntity, carView);

        ArrangeImagesInGrid();
        BuildBorder(_sceneData.Value.PlayerEntity);
        _movementComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).MovableObjectRigidbody.mass = carCmp.CurrentWeight;
        ChangePlayerUIState(true);
    }

    private void ArrangeImagesInGrid() {
        float spacing = 0f;
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        RectTransform parentRect = _sceneData.Value._playerUIView._carPratsDownLevelHealthContainer;
        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;
        var upLevelpartsContainer = _sceneData.Value._playerUIView._carPratsUpLevelHealthContainer;
        int columns = carCmp.CarPartContainerGroupsGrid.GetLength(1);
        int rows = carCmp.CarPartContainerGroupsGrid.GetLength(0);
        inventoryCmp.CarPartInfoUIViewsGrid = new CarPartInGameUIView[rows, columns];
        inventoryCmp.CarPartInfoUIViewsUpLevelGrid = new CarPartInGameUIView[rows, columns];
        // Вычисляем размер каждого квадрата
        float cellSize = Mathf.Min(parentHeight / columns,  parentWidth/ rows);

        float gridWidth =  rows * cellSize  ;
        float gridHeight = columns* cellSize ;

        // Начальная позиция (левый верхний угол)
        float startX = (cellSize/2 - gridWidth/ 2) ;
        float startY = (cellSize/2 -gridHeight/ 2) ;
        SetCarPartGroupHealthUI(carCmp.CarPartContainerGroupsList, carCmp.CarPartContainerGroupsGrid, startX, cellSize, startY,
            parentRect, ref inventoryCmp.CarPartInfoUIViewsGrid);

        SetCarPartGroupHealthUI(carCmp.CarPartGroupsList, carCmp.CarPartGroupsGrid, startX, cellSize, startY, upLevelpartsContainer,
            ref inventoryCmp.CarPartInfoUIViewsUpLevelGrid);

       // GridUtils.PrintGrid(carCmp.CarPartContainerGroupsGrid);
    }

    private void SetCarPartGroupHealthUI(Dictionary<int, List<Vector2Int>> groupsList, int[,] groupsGrid, float startX, float cellSize,
        float startY, RectTransform parentRect, ref CarPartInGameUIView[,] inventoryCmp) {
        
        foreach (var carPartInfo in groupsList) {

           var medianCell =  GridUtils.FindMedianCell(carPartInfo.Value);
            
            foreach (var cell in carPartInfo.Value) {
                var row = cell.x;
                var col = cell.y;
                float posX = startX + row * cellSize;
                float posY = startY + col * cellSize;
                var cellUIHealth = _sceneData.Value.GetCarPartInGameUIView();
                cellUIHealth.transform.SetParent(parentRect);
                var cellCmp = _carPartComponentsPool.Value.Get(carPartInfo.Key);
                inventoryCmp[row, col] = cellUIHealth;
                bool hasText = cell == medianCell;
                cellUIHealth.gameObject.SetActive(true);
                cellUIHealth.SetSize(new Vector2(cellSize, cellSize), new Vector2(posX, posY),
                    _sceneData.Value._carStuffListsConfig.CarPartUIBordersSprites[GridUtils.GetTileSpriteIndex(groupsGrid, cell)],hasText);
                
                cellUIHealth.SetHealthCount(_sceneData.Value._healthGradient.Evaluate(cellCmp.CurrentHealth / cellCmp.MaxHealth),
                    (int)cellCmp.CurrentHealth, true);
                // }
            }
        }
    }

    private bool CanStartRound() {
        var carCmp = _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (carCmp.CurrentWeight > carCmp.CurrentMaxWeight) return false;
       if (MainPartIsPlaced(_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity)) &&  CanUseThisCarPartContainers()) return true;
        return false;
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

    private void SpawnCarPartCellView(int cellEntity, CarView carView, bool isPlayer, int rotateAngle) {
        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(cellEntity);
        ref var carCmp = ref _carComponentsPool.Value.Get(carPartCmp.MainEntity);
        var carPartConfig = _sceneData.Value._carStuffListsConfig.CarPartConfigs[carPartCmp.CarPartId];
        bool isWeapon = carPartConfig.PartType == CarPartType.MeleeWeapon || carPartConfig.PartType == CarPartType.Gun;
   
        var containersSize = new Vector2(carCmp.CarPartContainerGroupsGrid.GetLength(0),carCmp.CarPartContainerGroupsGrid.GetLength(1));
        bool isContainer = carPartConfig.PartType == CarPartType.CarStuffContainer || carPartConfig.PartType == CarPartType.WheelsContainer;
        int needSpriteLevel = isContainer ? 1 : 2;
        int needRotate = rotateAngle; 
        var needList = isContainer ? carCmp.CarPartContainerGroupsList : carCmp.CarPartGroupsList;
         Vector2Int carPartSize = GridUtils.GetCarPartSize(carPartConfig);
        carPartCmp.MaxHealth = carPartConfig.MaxHealth;
        carPartCmp.CurrentHealth = carPartConfig.MaxHealth;
       
        carPartCmp.CellCarPartsStuffViews = new List<CarPartStuffView>();
        foreach (var carPartInfo in needList[cellEntity]) {
            var carPart =  isContainer ? _sceneData.Value.GetCarPartCell(carView.CarPartsContainer): carCmp.CarPartViewsGrid[carPartInfo.x, carPartInfo.y];
            carPartCmp.CellCarPartsStuffViews.Add(carPart);
            if (isContainer) {
                string needLayer = isPlayer ? "CarBase" : "CarBaseEnemy";
                carPart.SetupCarPart(carPartInfo, needLayer, carPartCmp.MainEntity);
                carPart.transform.localPosition = new Vector2(carPartInfo.x, carPartInfo.y)
                                                  - new Vector2(containersSize.x/2f, containersSize.y/2f);
              //  carPart.transform.localRotation = Quaternion.Euler(0, 0, needRotate);
                carPart.SetColliderEnableState(true);
                carPart.gameObject.tag = isPlayer ? "Player" : "Enemy";
                carCmp.CarPartViewsGrid[carPartInfo.x, carPartInfo.y] = carPart;
            } else {
                string needLayer = isPlayer ? "UpCarPart" : "UpCarPartEnemy";
                   carPart.SetupCarPart(carPartInfo, needLayer, carPartCmp.MainEntity);
                   //carPart.transform.localRotation = Quaternion.Euler(0, 0, needRotate);
            }
             
            // need check
        }

        carPartCmp.CarPartCellRenderer = _sceneData.Value.GetCarPartSpriteRenderer(carView.transform);
        carPartCmp.CarPartCellRenderer.sprite = carPartConfig.PartSprite;
        carPartCmp.CarPartCellRenderer.sortingOrder = needSpriteLevel;

        if (needRotate == 90 || needRotate == 270)
            carPartSize = new Vector2Int(carPartSize.y, carPartSize.x);

        carPartCmp.CarPartCellRenderer.transform.localPosition = (Vector3Int)GridUtils.GetEdgePointFromCarPart(needList[cellEntity]) +
                                                              new Vector3((carPartSize.x - containersSize.x) / 2f,
                                                                  (carPartSize.y - containersSize.y) / 2f);
        carCmp.CurrentWeight += carPartConfig.Weight;
        carPartCmp.CarPartCellRenderer.transform.localRotation = Quaternion.Euler(0, 0, needRotate);

        if (carPartConfig.PartType == CarPartType.Gun) {
            //  carPart.SetColliderEnableState(false);
            
            var gunCmp = _gunComponentsPool.Value.Has(cellEntity)? _gunComponentsPool.Value.Get(cellEntity): _gunComponentsPool.Value.Add(cellEntity);
            var gunConfig = _sceneData.Value.GetGunFromCarPart(carPartCmp.CarPartId);
            if (gunConfig.UpPartSprite != null) {
                gunCmp.UpPartContainer = _sceneData.Value.GetGunContainer(carPartCmp.CarPartCellRenderer.transform);
                gunCmp.UpPartContainer.transform.localPosition = gunConfig.GunContainerPosition;
                gunCmp.UpPartContainer.transform.localRotation = Quaternion.Euler(0, 0, 0);

                gunCmp.FirePointTransform = _sceneData.Value.GetGunContainer(gunCmp.UpPartContainer);
                gunCmp.FirePointTransform.localPosition = gunConfig.FirePointPosition - gunConfig.GunContainerPosition;
                gunCmp.FirePointTransform.transform.localRotation = Quaternion.Euler(0, 0, 0);

                gunCmp.GunSpriteRenderer = _sceneData.Value.GetCarPartSpriteRenderer(gunCmp.UpPartContainer);
                gunCmp.GunSpriteRenderer.sprite = gunConfig.UpPartSprite;
                gunCmp.GunSpriteRenderer.sortingOrder = 3;
                gunCmp.GunSpriteRenderer.transform.localPosition = gunConfig.UpPartPosition - gunConfig.GunContainerPosition;
                gunCmp.GunSpriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, 0);
            } else {
                gunCmp.FirePointTransform = _sceneData.Value.GetGunContainer(carPartCmp.CarPartCellRenderer.transform);
                gunCmp.FirePointTransform.localPosition = gunConfig.FirePointPosition;
                gunCmp.FirePointTransform.localRotation = Quaternion.Euler(0, 0, 0);
            }

            (float damageMultiplier, float reloadMultiplier, float attackCountdownMultiplayer) =
                GridUtils.GetGunLevelMultipliers(gunConfig, carPartCmp.PartLevel);
            gunCmp.CurrentReloadSpeed = gunConfig.ReloadDuration / reloadMultiplier;
            gunCmp.CurrentMaxAttackCountdown = gunConfig.AttackCouldown / attackCountdownMultiplayer;
            gunCmp.CurrentDamage = gunConfig.Damage * damageMultiplier;
            gunCmp.CurrentCountdown = gunCmp.CurrentReloadSpeed;
            gunCmp.CurrentMagazineCount = gunConfig.BulletsCountInMagazine;
            //   carPart.transform.localPosition = Vector3.zero;
           
            if (gunConfig.MissleType == MissleType.laser) {
                if (_laserComponentsPool.Value.Has(cellEntity))
                    _laserComponentsPool.Value.Get(cellEntity).LaserLineRenderer = _sceneData.Value.GetLaserLineRenderer();
                else
                    _laserComponentsPool.Value.Add(cellEntity).LaserLineRenderer = _sceneData.Value.GetLaserLineRenderer();
            }
            
            _gunComponentsPool.Value.Get(cellEntity) = gunCmp;
        } else if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
            var meleeWeaponCmp = _meleeWeaponComponentsPool.Value.Has(cellEntity)
                ? _meleeWeaponComponentsPool.Value.Get(cellEntity)
                : _meleeWeaponComponentsPool.Value.Add(cellEntity);
            var meleeWeaponConfig = _sceneData.Value.GetMeleeWeaponFromCarPart(carPartCmp.CarPartId);
            if (meleeWeaponConfig.UpSprite != null) {
                meleeWeaponCmp.MeleeWeaponRenderer = _sceneData.Value.GetCarPartSpriteRenderer(carPartCmp.CarPartCellRenderer.transform);
                meleeWeaponCmp.MeleeWeaponRenderer.sprite = meleeWeaponConfig.UpSprite;
                meleeWeaponCmp.MeleeWeaponRenderer.sortingOrder = 3;
                meleeWeaponCmp.MeleeWeaponRenderer.transform.localPosition = meleeWeaponConfig.UpSpritePosition;
            }

            var meleeStuff = _sceneData.Value.GetMeleeCarPartCell(meleeWeaponCmp.MeleeWeaponRenderer.transform);
            meleeWeaponCmp.MeleeStuff = meleeStuff;
            meleeStuff.transform.localRotation = Quaternion.Euler(0, 0, 0);
            meleeStuff.transform.localPosition = Vector3.zero;
            meleeStuff.MakeTriggerCollider();
            meleeStuff.SetColliderEnableState(true);
            (float damageMultiplier, float damageBySpeedMultiplier) = GridUtils.GetMeleeLevelMultipliers(meleeWeaponConfig,carPartCmp.PartLevel);
            meleeWeaponCmp.CurrentDamage =meleeWeaponConfig.Damage* damageMultiplier;
            meleeWeaponCmp.DamageBySpeed =meleeWeaponConfig.DamageAddedFromOneSpeed* damageBySpeedMultiplier;
            _meleeWeaponComponentsPool.Value.Get(cellEntity) = meleeWeaponCmp;
            _constructMeleeCellEventsPool.Value.Add(cellEntity);
        } else if (carPartConfig.PartType == CarPartType.Support) {
            var supportCarPartConfig = _sceneData.Value.GetSupportModuleFromCarPart(carPartCmp.CarPartId);
            switch (supportCarPartConfig.ModuleType) {
                case SupportModuleType.Fuel:
                    ref var fuelCmp = ref _fuelComponentsPool.Value.Get(carPartCmp.MainEntity);
                    fuelCmp.CurrentFuel += supportCarPartConfig.FirstCount;
                    fuelCmp.MaxFuel += supportCarPartConfig.FirstCount;
                    Debug.Log(fuelCmp.MaxFuel); //id breack delete current fuel
                    break;
                case SupportModuleType.Turbine:
                    _fuelComponentsPool.Value.Get(carPartCmp.MainEntity).FuelConsumption += supportCarPartConfig.FirstCount;
                    _movementComponentsPool.Value.Get(carPartCmp.MainEntity).MaxMoveSpeed += supportCarPartConfig.SecondCount;
                    break;
            }
        } else if (carPartConfig.PartType == CarPartType.WheelsContainer) {
            var wheelsConfig = _sceneData.Value.GetWheelContainerFromCarPart(carPartConfig.Id);
            carCmp.CurrentMaxWeight += wheelsConfig.MaxWeight;
        }

        
    }

    private void SetupEnemyMovementComponent(int enemyEntity, CarConfig carConfig) {
        ref var movementComponent = ref _movementComponentsPool.Value.Get(enemyEntity);
        movementComponent.RotateSpeed = carConfig.RotateSpeed * 0.5f * _currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).RoundMoneyMultiplayer;
        movementComponent.Acceleration = carConfig.Acceleration;
        movementComponent.Deceleration = carConfig.Deceleration; 
        var carCmp = _carComponentsPool.Value.Get(enemyEntity);
        movementComponent.MovableObjectRigidbody.mass = carCmp.CurrentWeight;
       
        movementComponent.MaxMoveSpeed = carConfig.DefaultMaxSpeed * (1-carCmp.CurrentWeight / carCmp.CurrentMaxWeight);
        if (movementComponent.MaxMoveSpeed < 1)
            movementComponent.MaxMoveSpeed = 1;

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
        string tag = entity == _sceneData.Value.PlayerEntity ? "Player" : "Enemy";
        carView.Construct(entity, ColissionCarsHit, tag);
        ref var carCmp = ref _carComponentsPool.Value.Get(entity);
        carCmp.CarView = carView;
        var carConfig = _sceneData.Value.GetCarConfig(carCmp.CarId);
       // carView.CarPartsContainer.localPosition = new Vector3(-(float)carConfig.Width / 2, (float)carConfig.Height / 2, 0);
       
       // carCmp.CurrentWeight += carCmp.CurrentMaxWeight / 5f;
        carCmp.ForwardWheels = new List<Animator>();
        carCmp.ForwardWheelsParticles = new List<ParticleSystem>();
        carCmp.CurrentWeight = 0;
      
        return carView;
    }

    private void SetupWheels(int carCmpEntity, CarView carView) {
        int maxYposWheels = 0;
        ref var carCmp  =ref _carComponentsPool.Value.Get(carCmpEntity);
        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartConfig = _sceneData.Value.GetCarPartConfig(_carPartComponentsPool.Value.Get(carPartContainer.Key).CarPartId);
            if (carPartConfig.PartType == CarPartType.WheelsContainer) {
                maxYposWheels = maxYposWheels < carPartContainer.Value[0].y? carPartContainer.Value[0].y : maxYposWheels;
            }
        }

        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartContainer.Key);
            var carPartConfig = _sceneData.Value.GetCarPartConfig(carPartCmp.CarPartId);
            if (carPartConfig.PartType == CarPartType.WheelsContainer) {
                var wheelConfig = _sceneData.Value.GetWheelContainerFromCarPart(carPartConfig.Id);
                ref var wheelCmp  = ref _wheelContainerComponentsPool.Value.Add(carPartContainer.Key);
                wheelCmp.Wheels = new List<Animator>();
                foreach (var wheelPos in wheelConfig.WheelsPositions) {
                    var wheel = CreateWheel(wheelConfig.WheelId, carView, (Vector2)carPartCmp.CarPartCellRenderer.transform.position + wheelPos-(Vector2)carView.transform.position,
                        wheelPos.x > 0);
                    wheelCmp.Wheels.Add(wheel);
                    if (maxYposWheels == carPartContainer.Value[0].y)
                        carCmp.ForwardWheels.Add(wheel);
                    carCmp.ForwardWheelsParticles.Add(wheel.GetComponentInChildren<ParticleSystem>());
                }
            }
        }
    }

    private Animator CreateWheel(int wheelId, CarView carView, Vector2 pos, bool isLeftSide) {
        var wheelTransform = _sceneData.Value.InstantiateAnimator(_sceneData.Value._carStuffListsConfig.Wheels[wheelId].WheelAnimator,
            carView.ForwardWheelsContainer.transform);
        AnimatorClipInfo[] clipInfo = wheelTransform.GetCurrentAnimatorClipInfo(0);
        float clipLength = clipInfo[0].clip.length;

        float randomTime = Random.Range(0f, clipLength);
        wheelTransform.Play("Spin", 0, randomTime / clipLength);

     //   int scaleMultiplayer = pos.x > 0 ? 1 : -1;
        int scaleMultiplayer = isLeftSide ? 1 : -1;
        wheelTransform.transform.localScale = new Vector3(scaleMultiplayer, 1f, 1f);
        wheelTransform.transform.localPosition = pos;
        return wheelTransform;
    }

    private bool MainPartIsPlaced(InventoryComponent inventoryCmp) {
        foreach (var partId in inventoryCmp.PartsInInventory) {
            if (_sceneData.Value._carStuffListsConfig.CarPartConfigs[partId.Item1].PartType == CarPartType.Main)
                return false;
        }

        return true;
    }

    private void GenerateMap() {
        Vector2 randomOffset = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f));
        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 10; j++) {
                if (i == 5 && j == 5) continue;
                GenerateObstaclesInCell(i, j, randomOffset);
            }
        }
    }
    
    private void GenerateObstaclesInCell(int cellX, int cellY, Vector2 randomOffsetForNoise)
    {
        // Получаем значение шума Перлина для центра клетки
        float noiseValue = PerlinNoiseUtil.GetFractalNoise(
            cellX * 20 + 20 / 2f+randomOffsetForNoise.x,
            cellY * 20 + 20 / 2f+randomOffsetForNoise.y 
        );
        // Определяем количество препятствий на основе шума
        int obstacleCount = GetObstacleCountFromNoise(noiseValue);
        // Генерируем препятствия в клетке
        for (int k = 0; k < obstacleCount; k++)
        {
            Vector3 randomPosition = GetRandomPositionInCell(cellX, cellY);
            SpawnObstacle(randomPosition);
        }
    }
    
    private void SpawnObstacle(Vector2 position)
    {
        Transform obstaclePrefab = _sceneData.Value._mountains[Random.Range(0, _sceneData.Value._mountains.Length)];
        
        Quaternion randomRotation = Quaternion.Euler(0f, 0f, Random.Range(0, 4)*90);
        
        Transform obstacle = _sceneData.Value.InstantiateObject(obstaclePrefab, new Vector3(position.x, position.y, 0f));
       obstacle.localRotation = randomRotation;
       
    }
    
    private Vector2 GetRandomPositionInCell(int cellX, int cellY)
    {
        // Случайная позиция внутри клетки 10x10 единиц
        float randomX = Random.Range(0f, 20f);
        float randomY = Random.Range(0f, 20f);
        
        // Переводим в глобальные координаты (центрируем карту)
        float worldX = cellX * 20f + randomX - 200f / 2f;
        float worldY = cellY * 20f + randomY - 200f / 2f;
        
        return new Vector2(worldX, worldY);
    }

    
    private int GetObstacleCountFromNoise(float noiseValue)
    {
        if (noiseValue < 0.47f) return 0;
        else if (noiseValue < 0.7f) return 1;
        else if (noiseValue < 0.9f) return 2;
        else return 3;
    }
    private void ChangeInventoryState(bool state) => _sceneData.Value._inventoryUIView.gameObject.SetActive(state);
    private void ChangePlayerUIState(bool state) => _sceneData.Value._playerUIView.gameObject.SetActive(state);
}