using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Client;
using DG.Tweening;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class  GameStatesSystem : IEcsInitSystem, IEcsRunSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneService;
    private EcsCustomInject<BetweenScenesService> _betweenScenesService;

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
    private EcsPoolInject<AddTeammateEvent> _addTeammateEventsPool;
    private EcsPoolInject<InventoryCellComponent> _inventoryCellComponentsPool;
    
    private EcsFilterInject<Inc<CarPartCellComponent>> _cellComponentsFilter;
    private EcsFilterInject<Inc<InventoryCellComponent>> _inventoryCellComponentsFilter;
    private EcsFilterInject<Inc<CarComponent>> _carComponentsFilter;
    private EcsFilterInject<Inc<FilledZoneComponent>> _filledZoneComponentsFilter;
    private EcsFilterInject<Inc<TaskComponent>> _taskComponentsFilter;
    private EcsFilterInject<Inc<CarPartInGameUIComponent>> _carPartInGameUIComponentsFilter;

    private Vector2 _cellOffset = new Vector2(0.5f, 0.5f);

    public void Init(IEcsSystems systems) {
        int playerEntity = _world.Value.NewEntity();
        _sceneService.Value.PlayerEntity = playerEntity;
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Add(playerEntity);
      //  curGameCmp.RoundTypes = new List<RoundType[]>();
        curGameCmp.CurrentBattleRounds = 2;
        var diificultyConfig = _sceneService.Value._carStuffListsConfig.MapConfigs[_betweenScenesService.Value.CurrentSelectedMap]
            .MapDifficltyConfigs[_betweenScenesService.Value.CurrentSelectedMapDifficlty];
        curGameCmp.DifficultyDamageMultiplayer = diificultyConfig.DamageMultiplayer;
        curGameCmp.DifficultyHealthMultiplayer = diificultyConfig.HealthMultiplayer;
           
        _settingsComponentsPool.Value.Add(_sceneService.Value.PlayerEntity).pixelizedEffectIsUsed = true;
        _sceneService.Value._inventoryUIView._playButton.onClick.AddListener(ChangeRoundInfo);
        _sceneService.Value._mapPointPrefab.SetupPoint(UpdateMapPoint);
        _sceneService.Value._mapFilledZoneView.SetupFilledZone(SetFeelingZoneState);
        
        var settingsView = _sceneService.Value._settingsView;
        settingsView._settingsChangeStateButton.onClick.AddListener(SetSettingsState);
        settingsView._saveSettingsButton.onClick.AddListener(SaveSettings);
        settingsView._blueColorSlider.onValueChanged.AddListener(ChangePickedEnemyProjectilesColor);
        settingsView._redColorSlider.onValueChanged.AddListener(ChangePickedEnemyProjectilesColor);
        settingsView._greenColorSlider.onValueChanged.AddListener(ChangePickedEnemyProjectilesColor);
        settingsView._setPixelizedEffectModeButton.onClick.AddListener(ChangePixelizedEffectsMode);

        settingsView._quitButton.onClick.AddListener(GoToDesktop);
        settingsView._mainMenuButton.onClick.AddListener(GoToMainMenu);
        settingsView._restartButton.onClick.AddListener(RestartGame);
        
        settingsView._closeSettingsButton.onClick.AddListener(SetSettingsState);
        SetColorSliderEnemyProjectilesValues();
        GenerateMap();
        SetupSupportBox();
    }

    public void Run(IEcsSystems systems) {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

        foreach (var filledZone in _filledZoneComponentsFilter.Value) {
            TriggerFilledZone();
        }

        switch (curGameCmp.CurrentRoundType) {
            case RoundType.BattleDefault:
                curGameCmp.RoundTime += Time.deltaTime;
                _sceneService.Value._playerUIView._gameTimerText.text = ((int)(30+curGameCmp.CurrentRound*3 - curGameCmp.RoundTime)).ToString();
                if (curGameCmp.RoundTime > 30+curGameCmp.CurrentRound*3) {
                    _sceneService.Value._playerUIView._gameTimerText.gameObject.SetActive(false);
                    EndRound();
                }

                break;
            
            case RoundType.BossBattle:
                curGameCmp.RoundTime += Time.deltaTime;
                _sceneService.Value._playerUIView._gameTimerText.text = ((int)(30+curGameCmp.CurrentRound*3 - curGameCmp.RoundTime)).ToString();
                if (curGameCmp.RoundTime > 30+curGameCmp.CurrentRound*3) {
                    _sceneService.Value._playerUIView._gameTimerText.gameObject.SetActive(false);
                    EndRound();
                    WinGame();
                }

                break;

            case RoundType.FromAToB:
                curGameCmp.RoundTime += Time.deltaTime;
                if (!_fromPointToPointRoundComponentsPool.Value.Has(_sceneService.Value.PlayerEntity)) return;
                ref var pointToPointRound = ref _fromPointToPointRoundComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
                var moveCmp = _movementComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
                if (moveCmp.MovableObjectRigidbody != null)
                    UpdateArrowDirection(pointToPointRound.PointsToCheck[0], moveCmp.MovableObjectRigidbody.gameObject.transform.position);
                if (pointToPointRound.TimeAfterMarkPoint > 0)
                    pointToPointRound.TimeAfterMarkPoint -= Time.deltaTime;
                break;
            
            case RoundType.ZoneDefence:
                curGameCmp.RoundTime += Time.deltaTime;
                break;
        }

        float timeBetweenEnemiesSpawn = _betweenScenesService.Value.GetMapDifficultyConfig().TimeBetweenInRoundEnemySpawn;
        if (curGameCmp.RoundTime / timeBetweenEnemiesSpawn > curGameCmp.RoundAddedEnemies) {
            if (curGameCmp.CurrentRoundType != RoundType.BattleDefault || curGameCmp.RoundTime < ((timeBetweenEnemiesSpawn-5) + curGameCmp.CurrentRound * 3))
                SetEnemiesCars(1, false);
            curGameCmp.RoundAddedEnemies++;
        }

        if (RoundUtilits.IsBattleRound(curGameCmp.CurrentRoundType) && !curGameCmp.SupportBoxIsSend && curGameCmp.CurrentRound > 3 &&
            curGameCmp.CurrentRound % 2 == 0 && curGameCmp.RoundTime > 20)
            SendSupportBox();
        

        if (Input.GetKeyDown(KeyCode.Escape)) SetSettingsState();

    }

    private void SendSupportBox() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        curGameCmp.SupportBoxIsSend = true;
        var supportBoxView = _sceneService.Value._supportBoxStuffView;
        var supportBox = supportBoxView._supportBoxcarPartStuffView;
        //warning text
        supportBox.gameObject.SetActive(true);
        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(supportBox._mainEntity);
        float randomValue = Random.value;
        if (randomValue > 0.75f) {
            carPartCmp.MaxHealth = 100;
            supportBoxView._supportBoxSprite.sprite = supportBoxView.defaultBoxSprite;
        } else {
            carPartCmp.MaxHealth = 300;
            supportBoxView._supportBoxSprite.sprite = supportBoxView.rareBoxSprite;
        }
        carPartCmp.CurrentHealth = carPartCmp.MaxHealth;
       //set need box sprite
       supportBox.transform.position = GetPositionAroundPlayer();
    }

    private void SetupSupportBox() {
        var supportBox = _sceneService.Value._supportBoxStuffView._supportBoxcarPartStuffView;
        int entitySupportBox = _world.Value.NewEntity();
        _carPartComponentsPool.Value.Add(entitySupportBox).MainEntity = -10;
        supportBox.SetupCarPart(Vector2Int.one * -10, "UpCarPartEnemy" ,entitySupportBox);
    }

    private void WinGame() {
        Time.timeScale = 0;
        var difficultyConfig = _betweenScenesService.Value._carStuffListsConfig.MapConfigs[_betweenScenesService.Value.CurrentSelectedMap]
            .MapDifficltyConfigs[_betweenScenesService.Value.CurrentSelectedMapDifficlty];
        int scrapCount = difficultyConfig.ScrapForRound * _betweenScenesService.Value.GetMapConfig().GameProcessConfig.GameRounds.Length +
                         difficultyConfig.ScrapForMapComplete;
        DataPersistenceManagerSystem._gameData.ScrapCount += scrapCount;
        if (_betweenScenesService.Value.GetMapConfig().MapDifficltyConfigs.Length !=
            _betweenScenesService.Value.CurrentSelectedMapDifficlty + 1)
            DataPersistenceManagerSystem._gameData.LevelRounds[_betweenScenesService.Value.CurrentSelectedMap] =
                _betweenScenesService.Value.CurrentSelectedMapDifficlty + 2;
        DataPersistenceManagerSystem.SaveGame();
        _sceneService.Value._endGameScreenView.SetWinScreenInfo(scrapCount);
    }
    private void SetSettingsState() {
        _sceneService.Value._settingsView._settingsContainer.gameObject.SetActive(!_sceneService.Value._settingsView._settingsContainer.gameObject.activeInHierarchy);
    }

    private void GoToDesktop() {
        DataPersistenceManagerSystem.SaveGame();
        Application.Quit();
    }

    private void GoToMainMenu() {
        SceneManager.LoadScene("MetaScene");
    }

    private void RestartGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void SetColorSliderEnemyProjectilesValues() {
        var settingsView = _sceneService.Value._settingsView;
        var needColor = _sceneService.Value._carStuffListsConfig.EnemyProjectilesMaterial.GetColor("_OffsetColor");
        settingsView._redColorSlider.value = needColor.r*settingsView._redColorSlider.maxValue;
            settingsView._greenColorSlider.value = needColor.g*settingsView._greenColorSlider.maxValue;
                settingsView._blueColorSlider.value = needColor.b*settingsView._blueColorSlider.maxValue;
    }

    private void SaveSettings() {
        var settingsView = _sceneService.Value._settingsView;

        var newColor = new Color(settingsView._redColorSlider.normalizedValue, settingsView._blueColorSlider.normalizedValue,
            settingsView._greenColorSlider.normalizedValue);
        _sceneService.Value._carStuffListsConfig.EnemyProjectilesMaterial.SetColor("_OffsetColor", newColor);
    }

    private void ChangePixelizedEffectsMode() {
        var settingsView = _sceneService.Value._settingsView; 
       
        bool isPixelized = !settingsView._setPixelizedEffectModeChecker.gameObject.activeInHierarchy;
        settingsView._setPixelizedEffectModeChecker.gameObject.SetActive(isPixelized);
        _sceneService.Value._pixelizedEffectsImage.gameObject.SetActive(isPixelized);
        _sceneService.Value._pixelizedEffectsUnderCarImage.gameObject.SetActive(isPixelized);
        if (!isPixelized) {
            _sceneService.Value.mainCamera.cullingMask |= (1 << 9);
            _sceneService.Value.mainCamera.cullingMask |= (1 << 3);
        } else {
            _sceneService.Value.mainCamera.cullingMask &= ~(1 << 9);
            _sceneService.Value.mainCamera.cullingMask &= ~(1 << 3);
        }
      
    }

    private void ChangePickedEnemyProjectilesColor(float value) {
        var settingsView = _sceneService.Value._settingsView;
        settingsView._projectilesOffsetColorImage.color = new Color(settingsView._redColorSlider.normalizedValue,
            settingsView._blueColorSlider.normalizedValue, settingsView._greenColorSlider.normalizedValue);
        
        settingsView._redColorText.text = settingsView._redColorSlider.value.ToString();
        settingsView._greenColorText.text = settingsView._greenColorSlider.value.ToString();
        settingsView._blueColorText.text = settingsView._blueColorSlider.value.ToString();
    }

    private void UpdateMapPoint() {
        if(!_fromPointToPointRoundComponentsPool.Value.Has(_sceneService.Value.PlayerEntity))return;
        ref var pointToPointRound = ref _fromPointToPointRoundComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        if(pointToPointRound.TimeAfterMarkPoint > 0)return;
        pointToPointRound.PointsToCheck.RemoveAt(0);
        pointToPointRound.TimeAfterMarkPoint = 1;
        if (pointToPointRound.PointsToCheck.Count == 0) {
            EndPointToPointRound();
            EndRound();
        } else {
            _sceneService.Value._mapPointPrefab.transform.position = pointToPointRound.PointsToCheck[0];
            //smth particle effect
        }
    }

    private void SetSaleShopRound() {
        _sceneService.Value._shopUIView._buttonUpdateShop.gameObject.SetActive(false);
        _updateShopToSaleRoundEventsPool.Value.Add(_sceneService.Value.PlayerEntity);
    }
    
    
    
    private void UpdateArrowDirection(Vector2 target, Vector2 playerPosition)
    {
        Vector3 directionToTarget = target - playerPosition;
        
        if (directionToTarget.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(directionToTarget.y,directionToTarget.x ) * Mathf.Rad2Deg;
            
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            _sceneService.Value._playerUIView._arrowImageTransform.parent.rotation = Quaternion.Lerp(
                _sceneService.Value._playerUIView._arrowImageTransform.parent.rotation, 
                targetRotation, 
                3 * Time.deltaTime
            );
        }
        Vector3 screenPos = _sceneService.Value.mainCamera.WorldToScreenPoint(playerPosition);
        _sceneService.Value._playerUIView._arrowImageTransform.parent.position = Vector2.Lerp(_sceneService.Value._playerUIView._arrowImageTransform.parent.position,screenPos, 4*Time.deltaTime) ;
    }
    
    private int TaskCount() {
        return _taskComponentsFilter.Value.GetEntitiesCount();
    }

    private void SetupStartRound() {
        
    }
    private void SetMoneyText() {
        _sceneService.Value._shopUIView._moneyCountText.text =
            _playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MoneyCount + "$";
    }

    private void EndRound() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        curGameCmp.CurrentRoundType = RoundType.None;
        curGameCmp.SupportBoxIsSend = false;
        _sceneService.Value._supportBoxStuffView._supportBoxcarPartStuffView.gameObject.SetActive(false);
        _endRoundEventsPool.Value.Add(_sceneService.Value.PlayerEntity);
        Debug.Log("End round game st system");
        int currentRound = curGameCmp.CurrentRound - 1;
        _sceneService.Value._mapUIView.RoundDifficltyContainer.gameObject.SetActive(false);
        int moneyCount = (int)(_betweenScenesService.Value.GetMapConfig().GameProcessConfig.GameRounds[currentRound].MoneyCount * curGameCmp.RoundMoneyMultiplayer);
        curGameCmp.RoundMoneyMultiplayer = 1;
        var carCmp = _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        int allPartCount = 0;
        int brokenPartCount = 0;
        foreach (var carPartInfo in carCmp.CarPartGroupsList) {
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartInfo.Key);
            allPartCount += carPartInfo.Value.Count;
            if (carPartCmp.CurrentHealth <= 0)
                brokenPartCount += carPartInfo.Value.Count;
            var supportModule = _sceneService.Value.GetSupportModuleFromCarPart(carPartCmp.CarPartId);
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
        
        _playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MoneyCount += moneyCount;
        SetMoneyText();
        _sceneService.Value._endGameScreenView.SetEndScreenInfo(moneyCount, currentRound);
        ChangeInventoryState(true);
        ChangePlayerUIState(false);
      //  ReleaseAllCarPartUI();
    }

    private void ReleaseAllCarPartUI() {
        var inventoryCmp = _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        foreach (var carPartUi in inventoryCmp.CarPartInfoUIViewsGrid) {
            if (carPartUi)
                _sceneService.Value.ReleaseCarPartInGameUIView(carPartUi);
        }
        foreach (var carPartUi in inventoryCmp.CarPartInfoUIViewsUpLevelGrid) {
            if (carPartUi)
                _sceneService.Value.ReleaseCarPartInGameUIView(carPartUi);
        }
        
    }

    private void ChangeRoundInfo() {
        if (!CanStartRound()) return;
        _sceneService.Value._mapUIView.gameObject.SetActive(true);
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

        var currentSupportRoundTypes = RoundUtilits.SupportRoundsTypes.Distinct().ToList();
        if (TaskCount() == 5)
            currentSupportRoundTypes.Remove(RoundType.Task);
       /* if (_inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).PlayerTeammateCarsEntity.Count >= 5)
            currentSupportRoundTypes.Remove(RoundType.TeammateForSupport);*/
        
        
        (var randomRounds, int moneyMultiplayerRound) =
            curGameCmp.CurrentRound >= _betweenScenesService.Value.GetMapConfig().GameProcessConfig.GameRounds.Length - 1
                ? (new[] { RoundType.BossBattle }, -1)
                : RoundUtilits.GetRandomBattleRound(curGameCmp.CurrentBattleRounds, 3, currentSupportRoundTypes);
      //  curGameCmp.RoundTypes.Add(randomRounds);
        curGameCmp.RoundButtons = new EventTrigger[randomRounds.Length];
        var mapImageRect = _sceneService.Value._mapUIView.MapImage.rectTransform;

        Transform moneyMultPosition = null;
        _sceneService.Value._mapUIView.RoundDifficltyContainer.localScale = Vector3.zero;
        for (int i = 0; i < randomRounds.Length; i++) {
            var roundButton = _sceneService.Value.InstantiateRoundButton();
            int newIndex = i;
            roundButton.GetComponent<Image>().sprite = _sceneService.Value._mapUIView.RoundSprites[randomRounds[i]];

            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => { StartCurrentRoundType(randomRounds[newIndex], (PointerEventData)data); });

            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { ChangeRoundDescription(randomRounds[newIndex], (PointerEventData)data); });

            roundButton.triggers.Add(clickEntry);
            roundButton.triggers.Add(enterEntry);
            float posX = (newIndex + 1) * (mapImageRect.rect.width / ((float)randomRounds.Length + 1)) - mapImageRect.rect.width / 2;
            float posY = (curGameCmp.CurrentRound + 1) * (mapImageRect.rect.height /
                                                          ((float)_betweenScenesService.Value.GetMapConfig().GameProcessConfig.GameRounds
                                                              .Length + 2)) - mapImageRect.rect.height/2;
            roundButton.transform.localPosition = new Vector2(posX, posY);
            curGameCmp.RoundButtons[i] = roundButton;

            if (moneyMultiplayerRound == i) {
                moneyMultPosition = roundButton.transform;
                float currentMultiplayer = Random.Range(12, 20) * 0.1f;
                _sceneService.Value._mapUIView.SetMoneyMultiplayerText(currentMultiplayer);
                curGameCmp.RoundMoneyMultiplayer = currentMultiplayer;
            }
        }

        _sceneService.Value._mapUIView.MapImage.transform.DOMoveY(
                _sceneService.Value._mapUIView.MapImage.transform.position.y - (mapImageRect.rect.height *
                    _sceneService.Value.mainCanvas.localScale.x / _betweenScenesService.Value.GetMapConfig().GameProcessConfig.GameRounds.Length), 0.5f)
            .SetEase(Ease.InOutBack).OnComplete(() => {
                if (moneyMultPosition) {
                    _sceneService.Value._mapUIView.RoundDifficltyContainer.position =
                        new Vector2(moneyMultPosition.position.x, moneyMultPosition.position.y - 70);

                    _sceneService.Value._mapUIView.RoundDifficltyContainer.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                }
            });

        _sceneService.Value._mapUIView.RoundNumberText.text = GetLocalization("Round") + (curGameCmp.CurrentRound + 1);
        ChangeInventoryState(false);

        ChangeRoundDescription(RoundType.None);
    }
    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
    private void ChangeRoundDescription(RoundType roundType, PointerEventData data = null) {
        var descriptionText = _sceneService.Value._mapUIView.RoundDescriptionText;
        switch (roundType) {
            case RoundType.None:
                descriptionText.text = "";
                break;
            case RoundType.Task:
                descriptionText.text =  GetLocalization("RoundTask");
                break;
            case RoundType.BattleDefault:
                descriptionText.text = GetLocalization("RoundBattleDefault");
                break;
            case RoundType.Chest:
                descriptionText.text = GetLocalization("RoundChest");
                break;
            case RoundType.FromAToB:
                descriptionText.text = GetLocalization("RoundFromAToB");
                break;
            case RoundType.Shop:
                descriptionText.text = GetLocalization("RoundShop");
                break;
            case RoundType.Transfer:
                descriptionText.text = GetLocalization("RoundTransfer");
                break;
            case RoundType.ZoneDefence:
                descriptionText.text = GetLocalization("RoundZoneDefence");
                break;
            case RoundType.BossBattle:
                descriptionText.text = GetLocalization("RoundBossBattle");
                break;
            case RoundType.TeammateForSupport:
                descriptionText.text = GetLocalization("RoundTeammateForSupport");
                break;
        }
    }
    
    private void StartCurrentRoundType(RoundType roundType,PointerEventData data) {
        _sceneService.Value._shopUIView._buttonUpdateShop.gameObject.SetActive(true);
        
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        foreach (var button in curGameCmp.RoundButtons) {
            button.enabled = false;
        }
        curGameCmp.RoundTime = 0;
        curGameCmp.RoundAddedEnemies = 1;
        switch (roundType) {
            case RoundType.BattleDefault:
                SetPlayerCarPrefab();
               SetEnemiesCars(3);
             //   SpawnTeammateCar();
                SetGameTimer();
                break;
            case RoundType.BossBattle:
                SetPlayerCarPrefab();
                 SetEnemiesCars(1);
                SetGameTimer();
                break;
            case RoundType.FromAToB:   
                SetPlayerCarPrefab();
                SetEnemiesCars(3);
               // SpawnTeammateCar();
                SetNewPointToPointRound();
                break;
            case RoundType.ZoneDefence:
                SetPlayerCarPrefab();
                SetEnemiesCars(3);
               // SpawnTeammateCar();
                SetFilledZoneRound();
                break;
            case RoundType.Chest:
                var shopCmp = _shopComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

                int rareIndex = GridUtils.GetRandomIndexFromChancesList(shopCmp.CurrentShopItemsChances);
                _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentGivenItems
                    .Add(shopCmp.ListShopItemsForRares[rareIndex][Random.Range(0, shopCmp.ListShopItemsForRares[rareIndex].Count)]);
                ChangeInventoryState(true);
                break;
            case RoundType.Shop:
                SetSaleShopRound();
                ChangeInventoryState(true);
                break;
            case RoundType.Transfer:
               ChangeInventoryState(true);
                break;
            case RoundType.TeammateForSupport:
                _addTeammateEventsPool.Value.Add(_sceneService.Value.PlayerEntity);
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
        _sceneService.Value._mapUIView.gameObject.SetActive(false);
        
        curGameCmp.CurrentRound++;
        _startRoundEventsPool.Value.Add(_sceneService.Value.PlayerEntity);
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
        _filledZoneComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).ZoneIsFeeling = isFeeling;
    }
    private void TriggerFilledZone() {
        ref var filledZoneCmp =ref _filledZoneComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        if(!filledZoneCmp.ZoneIsFeeling)return;
        filledZoneCmp.CurrentZoneTime += Time.deltaTime;
        _sceneService.Value._mapFilledZoneView.FilledZoneUI.fillAmount = filledZoneCmp.CurrentZoneTime / filledZoneCmp.NeedZoneTime;
        if (filledZoneCmp.CurrentZoneTime >= filledZoneCmp.NeedZoneTime) {
            EndFilledZoneRound();
            _filledZoneComponentsPool.Value.Del(_sceneService.Value.PlayerEntity);
            EndRound();
        }
    }

    private void EndFilledZoneRound() {
        _sceneService.Value._mapFilledZoneView.FilledZoneUI.gameObject.SetActive(false);
        _sceneService.Value._mapFilledZoneView.gameObject.SetActive(false);
    }

    private Vector2 GetPositionAroundPlayer() {
        var carCmp = _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        Vector2 carSize = new Vector2(carCmp.CarPartContainerGroupsGrid.GetLength(0)/2f, carCmp.CarPartContainerGroupsGrid.GetLength(1)/2f);
        var needDistance = 15 + Mathf.Max(carSize.x, carSize.y);
        return GetRandomPointAround(carCmp.CarView.CarPartsContainer.transform.position,Random.Range(needDistance,needDistance+5 ));
    }
    
    private void SetFilledZoneRound() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        
        _sceneService.Value._mapFilledZoneView.FilledZoneUI.gameObject.SetActive(true);
        _sceneService.Value._mapFilledZoneView.gameObject.SetActive(true);
       _filledZoneComponentsPool.Value.Add(_sceneService.Value.PlayerEntity).NeedZoneTime = 10+curGameCmp.CurrentRound*3;
        _sceneService.Value._mapFilledZoneView.ChangeZonePosition(GetRandomPointAround(Vector2.zero, 10));
    }

    private void SetNewPointToPointRound() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        curGameCmp.CurrentRoundType = RoundType.FromAToB;

        ref var pointToPointRoundCmp = ref _fromPointToPointRoundComponentsPool.Value.Add(_sceneService.Value.PlayerEntity);
        pointToPointRoundCmp.PointsToCheck = new List<Vector2>();
        int pointsCount = 2 + curGameCmp.CurrentRound / 5;
        Vector2 posToSpawn = Vector2.zero;
        for (int i = 0; i < pointsCount; i++) {
            posToSpawn = GetRandomPointAround(posToSpawn, 15);
            pointToPointRoundCmp.PointsToCheck.Add(posToSpawn);
        }

        var carPartsGrid = _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CarPartContainerGroupsGrid;
        _sceneService.Value._playerUIView._arrowImageTransform.parent.transform.position =
            _sceneService.Value.mainCamera.WorldToScreenPoint(Vector3.zero);
        float arrowPos = Mathf.Max(carPartsGrid.GetLength(0), carPartsGrid.GetLength(1)) * 20;

        _sceneService.Value._playerUIView._arrowImageTransform.localPosition = new Vector3(arrowPos, 0, 0);

        _sceneService.Value._mapPointPrefab.transform.position = pointToPointRoundCmp.PointsToCheck[0];
        _sceneService.Value._mapPointPrefab.gameObject.SetActive(true);
        _sceneService.Value._playerUIView._arrowImageTransform.gameObject.SetActive(true);
    }

    private void EndPointToPointRound() {
        _fromPointToPointRoundComponentsPool.Value.Del(_sceneService.Value.PlayerEntity);
        _sceneService.Value._mapPointPrefab.gameObject.SetActive(false);
        _sceneService.Value._playerUIView._arrowImageTransform.gameObject.SetActive(false);
        
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

    private bool HasObstacleAtPoint(Vector2 point) {
        int obstacleLayerMask = 1 << LayerMask.NameToLayer("Obstacle");

        Collider2D hit = Physics2D.OverlapCircle(point, 0.1f, obstacleLayerMask);

        return hit != null;
    }

    private TMP_Text GetTextForTask() {
        foreach (var taskText in _sceneService.Value._playerUIView._taskTexts) {
            if (!taskText.gameObject.activeInHierarchy) return taskText;
        }

        return null;
    }
    
    private void SetGameTimer() {
        _sceneService.Value._playerUIView._gameTimerText.gameObject.SetActive(true);
       
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

    private void SpawnTeammateCar() {
        var curGameCmp = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        
        SpawnNpcCar(curGameCmp, GetRandomCarMap(), false);
    }

    private EnemyCarPartsMaps GetRandomCarMap(int needRoundNumber = -1) {
        var curGameCmp = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        int roundNumber = needRoundNumber == -1 ? curGameCmp.CurrentRound : needRoundNumber;

        var currentRoundCarStuff = _betweenScenesService.Value.GetMapConfig().GameProcessConfig.GameRounds[roundNumber];
        int needIndex = GridUtils.GetRandomIndexFromChancesList(currentRoundCarStuff.EnemyCarChances);
        var carsList = _betweenScenesService.Value.GetMapConfig().EnemyMapConfigs[needIndex].CarMapsList;
        return carsList[Random.Range(0, carsList.Count)];
    }
    private void SetEnemiesCars(int enemiesCount, bool onStartRoundSpawn = true) {
        var curGameCmp = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        int roundNumber = curGameCmp.CurrentRound;
        if (!onStartRoundSpawn)
            roundNumber--;
        
        for (int i = 0; i < enemiesCount; i++) 
            SpawnNpcCar(curGameCmp, GetRandomCarMap(roundNumber), true);
        
    }

     private void SpawnTeammateCar(int entity) {

        ref var fuelCmp = ref _fuelComponentsPool.Value.Get(entity);
           
        ref var carCmp = ref _carComponentsPool.Value.Get(entity);
        var carView = CarViewSetup(entity);
            
        carView.transform.position = GetPositionAroundPlayer();
            
        carCmp.CarPartViewsGrid = new CarPartStuffView[carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1)];
            
        foreach (var carContainer in carCmp.CarPartContainerGroupsList) {
            int carPartCellEntity = carContainer.Key;
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartCellEntity);
           
            SpawnCarPartCellView(carPartCellEntity, carView, true, carPartCmp.RotateDegrees);
        }
            
        foreach (var carContainer in carCmp.CarPartGroupsList) {
            int carPartCellEntity = carContainer.Key;
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartCellEntity);
             
            SpawnCarPartCellView(carPartCellEntity, carView, true, carPartCmp.RotateDegrees);
        } 
        fuelCmp.MaxFuel+= _sceneService.Value.GetCarConfig(carCmp.CarId ).DefaultMaxFuel;
           
        SetupEnemyMovementComponent(entity, _sceneService.Value.GetCarConfig(carCmp.CarId));
        SetupWheels(entity, carView);
        SetupCarStatsFromLevelBoost(entity);
        ref var enemyAiCmp = ref _enemyAIComponentsPool.Value.Get(entity);
        enemyAiCmp.CheckedEnemyLeftPoints = new List<Transform>();
        enemyAiCmp.CheckedEnemyRightPoints = new List<Transform>();
        enemyAiCmp.LastCheckedTime = 1f;
        enemyAiCmp.IsEnemy = false;
        
        SetEnemyCollisionCheckers(carCmp.CarPartContainerGroupsGrid, carView,ref enemyAiCmp);
        BuildBorder(entity);
    }
    private void SpawnNpcCar(CurrentGameComponent curGameCmp, /*List<Transform> ,*/ EnemyCarPartsMaps carMap, bool isEnemy) {
        int enemyEntity = _world.Value.NewEntity();

        _movementComponentsPool.Value.Add(enemyEntity);
        ref var fuelCmp = ref _fuelComponentsPool.Value.Add(enemyEntity);
           
        ref var carCmp = ref _carComponentsPool.Value.Add(enemyEntity);
        carCmp.CarId = carMap.CarId;
        var carView = CarViewSetup(enemyEntity);
            
        carView.transform.position = GetPositionAroundPlayer();
            
        carCmp.CarPartGroupsGrid = new int[carMap.GridSize.x, carMap.GridSize.y];
        carCmp.CarPartContainerGroupsGrid = new int[carMap.GridSize.x, carMap.GridSize.y];
        carCmp.CarPartViewsGrid = new CarPartStuffView[carMap.GridSize.x, carMap.GridSize.y];
            
        carCmp.CarPartContainerGroupsList = new Dictionary<int, List<Vector2Int>>();
        carCmp.CarPartGroupsList = new Dictionary<int, List<Vector2Int>>();
            
        foreach (var carContainer in carMap.CarPartDownLevelGrid) {
            var carPartConfig = _sceneService.Value.GetCarPartConfig(carContainer.partId);
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

            SpawnCarPartCellView(carPartCellEntity, carView, !isEnemy, carContainer.rotate);

            carPartCmp.MaxHealth *= curGameCmp.DifficultyHealthMultiplayer;
            carPartCmp.CurrentHealth = carPartCmp.MaxHealth;
        }
            
        foreach (var carContainer in carMap.CarPartUpLevelGrid) {
            var carPartConfig = _sceneService.Value.GetCarPartConfig(carContainer.partId);
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

            SpawnCarPartCellView(carPartCellEntity, carView, !isEnemy, carContainer.rotate);
        } 
        fuelCmp.MaxFuel+= _sceneService.Value.GetCarConfig(carCmp.CarId ).DefaultMaxFuel;
           
        SetupEnemyMovementComponent(enemyEntity, _sceneService.Value.GetCarConfig(carCmp.CarId));
        SetupWheels(enemyEntity, carView);
        SetupCarStatsFromLevelBoost(enemyEntity);
        ref var enemyAiCmp = ref _enemyAIComponentsPool.Value.Add(enemyEntity);
        enemyAiCmp.CheckedEnemyLeftPoints = new List<Transform>();
        enemyAiCmp.CheckedEnemyRightPoints = new List<Transform>();
        enemyAiCmp.LastCheckedTime = 1f;
        enemyAiCmp.IsEnemy = isEnemy;
        
      //  enemyAiCmp.TargetToMove = playerMainParts[Random.Range(0, playerMainParts.Count)];
      
        SetEnemyCollisionCheckers(carCmp.CarPartContainerGroupsGrid, carView,ref enemyAiCmp);
        BuildBorder(enemyEntity);
        SetReloadBoostSupportTypes(enemyEntity);
    }

    private void SetReloadBoostSupportTypes(int carEntity) {
        ref var carCmp = ref _carComponentsPool.Value.Get(carEntity);

        foreach (var carPartInfo in carCmp.CarPartGroupsList) {
            int carPartId = _carPartComponentsPool.Value.Get(carPartInfo.Key).CarPartId;
            if (_sceneService.Value.GetCarPartConfig(carPartId).PartType == CarPartType.Support) {
                var supportConfig = _sceneService.Value.GetSupportModuleFromCarPart(carPartId);
                if (supportConfig.ModuleType != SupportModuleType.Ammo) continue;
                var neighbors = GridUtils.GetNeighbors(carPartInfo.Value, carCmp.CarPartGroupsGrid);
                foreach (var neighbor in neighbors) {
                    int neighborCarPartId = _carPartComponentsPool.Value.Get(neighbor).CarPartId;
                    if (_sceneService.Value.GetCarPartConfig(neighborCarPartId).PartType == CarPartType.Gun)
                        _gunComponentsPool.Value.Get(neighbor).CurrentReloadSpeed *= 1f - supportConfig.FirstCount;
                }
            }
        }
    }

    private void SetEnemyCollisionCheckers(int[,] grid, CarView carView, ref EnemyAIComponent enemyAiCmp) {
        for (int j = 0; j < grid.GetLength(0); j++) {
            for (int k = 0; k < grid.GetLength(1); k++) {
                if (grid[j, k] != 0) {
                    var obstacleCheckerTransform = _sceneService.Value.GetGunContainer(carView.CarPartsContainer);
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
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

        _fuelComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).UseSpeedBoost = false;
        var carView = CarViewSetup(_sceneService.Value.PlayerEntity);
        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

        carCmp.CarPartViewsGrid =
            new CarPartStuffView[carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1)];
        carCmp.CurrentMaxWeight = 0;
        foreach (var carPartInfo in carCmp.CarPartContainerGroupsList) {
            SpawnCarPartCellView(carPartInfo.Key, carView, true,
                _carPartComponentsPool.Value.Get(carPartInfo.Key).RotateDegrees);
        }

        foreach (var carPartInfo in carCmp.CarPartGroupsList) {
            SpawnCarPartCellView(carPartInfo.Key, carView, true,
                _carPartComponentsPool.Value.Get(carPartInfo.Key).RotateDegrees);
        }

        SetupWheels(_sceneService.Value.PlayerEntity, carView);

        ArrangeImagesInGrid();
        SetupCarStatsFromLevelBoost(_sceneService.Value.PlayerEntity);
        BuildBorder(_sceneService.Value.PlayerEntity);
        _movementComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MovableObjectRigidbody.mass = carCmp.CurrentWeight;
        ChangePlayerUIState(true);
        SetReloadBoostSupportTypes(_sceneService.Value.PlayerEntity);
        foreach (var carEntity in inventoryCmp.PlayerTeammateCarsEntity) {
            if (carEntity != _sceneService.Value.PlayerEntity)
                SpawnTeammateCar(carEntity);
        }
    }

    private void SetupCarStatsFromLevelBoost(int enity) {
        ref var carCmp = ref _carComponentsPool.Value.Get(enity);
        (float baseSpeedMultiplier, float baseRotateSpeedMultiplier, float baseAccelerationMultiplier, float baseMaxFuelMultiplier) =
            GridUtils.GetCarLevelMultipliers(carCmp.CarLevel);
        var carConfig = _sceneService.Value.GetCarConfig(carCmp.CarId);

        ref var moveCmp = ref _movementComponentsPool.Value.Get(enity);
        moveCmp.Acceleration *= baseAccelerationMultiplier;
        moveCmp.Deceleration *= baseAccelerationMultiplier;
        moveCmp.MaxBoostMoveSpeed = carConfig.BoostSpeedMultiplayer + baseAccelerationMultiplier;

        moveCmp.MaxMoveSpeed *= baseSpeedMultiplier;

        moveCmp.RotateSpeed *= baseRotateSpeedMultiplier;

        ref var fuelCmp = ref _fuelComponentsPool.Value.Get(enity);
        fuelCmp.MaxFuel += carConfig.DefaultMaxFuel * (baseMaxFuelMultiplier - 1);
        fuelCmp.CurrentFuel = fuelCmp.MaxFuel;
    }
    

    private void ArrangeImagesInGrid() {
        float spacing = 0f;
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        RectTransform parentRect = _sceneService.Value._playerUIView._carPratsDownLevelHealthContainer;
        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;
        var upLevelpartsContainer = _sceneService.Value._playerUIView._carPratsUpLevelHealthContainer;
        int columns = carCmp.CarPartContainerGroupsGrid.GetLength(1);
        int rows = carCmp.CarPartContainerGroupsGrid.GetLength(0);
        inventoryCmp.CarPartInfoUIViewsGrid = new CarPartInGameUIView[rows, columns];
        inventoryCmp.CarPartInfoUIViewsUpLevelGrid = new CarPartInGameUIView[rows, columns];
        // Вычисляем размер каждого квадрата
        float cellSize = Mathf.Min(parentHeight / columns,  parentWidth/ rows);

        float gridWidth = rows * cellSize;
        float gridHeight = columns * cellSize;

        // Начальная позиция (левый верхний угол)
        float startX = (cellSize/2 - gridWidth/ 2) ;
        float startY = (cellSize/2 -gridHeight/ 2) ;
        SetCarPartGroupHealthUI(carCmp.CarPartContainerGroupsList, carCmp.CarPartContainerGroupsGrid, startX, cellSize, startY,
            parentRect, ref inventoryCmp.CarPartInfoUIViewsGrid);

        SetCarPartGroupHealthUI(carCmp.CarPartGroupsList, carCmp.CarPartGroupsGrid, startX, cellSize, startY, upLevelpartsContainer,
            ref inventoryCmp.CarPartInfoUIViewsUpLevelGrid);

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
                var cellUIHealth = _sceneService.Value.GetCarPartInGameUIView();
                cellUIHealth.transform.SetParent(parentRect);
                var cellCmp = _carPartComponentsPool.Value.Get(carPartInfo.Key);
                inventoryCmp[row, col] = cellUIHealth;
                bool hasText = cell == medianCell;
                cellUIHealth.gameObject.SetActive(true);
                cellUIHealth.SetSize(new Vector2(cellSize, cellSize), new Vector2(posX, posY),
                    _sceneService.Value._carStuffListsConfig.CarPartUIBordersSprites[GridUtils.GetTileSpriteIndex(groupsGrid, cell)],hasText);
                
                cellUIHealth.SetHealthCount(_sceneService.Value._healthGradient.Evaluate(cellCmp.CurrentHealth / cellCmp.MaxHealth),
                    (int)cellCmp.CurrentHealth, true);
                // }
            }
        }
    }

    private bool CanStartRound() {
        var carCmp = _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity)
            .CurrentRedactedCarEntity);
        Debug.Log(carCmp.CurrentWeight + ">" + carCmp.CurrentMaxWeight + " weight, " + MainPartIsPlaced() + " main parts, " +
                  CanUseThisCarPartContainers() + " car containers");
        if (carCmp.CurrentWeight > carCmp.CurrentMaxWeight) {
            _sceneService.Value.PlayWarningAnimation(GetLocalization("The weight of the vehicle exceeds the maximum") + "!");
            return false;
        }

        bool mainPartIsPlaced = MainPartIsPlaced();
        bool canUseThisCarPartContainers = CanUseThisCarPartContainers();
        if (!canUseThisCarPartContainers)
            _sceneService.Value.PlayWarningAnimation(GetLocalization("The car is unstable and needs wheels") + "!");
        else if (!mainPartIsPlaced)
            _sceneService.Value.PlayWarningAnimation(GetLocalization("Place the engine and driver's cabin on the car") + "!");

        if (mainPartIsPlaced && canUseThisCarPartContainers) return true;
        return false;
    }
    
    private bool CanUseThisCarPartContainers() {
        int wheelsCount = 0;
        Vector2Int lastWheelMinMaxY = Vector2Int.zero;
        var carCmp = _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRedactedCarEntity);
        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartContainer.Key);
            var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartCmp.CarPartId);
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
        var carPartConfig = _sceneService.Value._carStuffListsConfig.CarPartConfigs[carPartCmp.CarPartId];

        var containersSize = new Vector2(carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1));
        bool isContainer = GridUtils.IsContainerType(carPartConfig);
        int needSpriteLevel = isContainer ? 1 : 2;
        int needRotate = rotateAngle;
        var needList = isContainer ? carCmp.CarPartContainerGroupsList : carCmp.CarPartGroupsList;
        Vector2Int carPartSize = GridUtils.GetCarPartSize(carPartConfig);
        carPartCmp.MaxHealth = carPartConfig.MaxHealth;
        if (carPartCmp.CurrentHealth == 0)
            carPartCmp.CurrentHealth = carPartConfig.MaxHealth;

        carPartCmp.CellCarPartsStuffViews = new List<CarPartStuffView>();
        foreach (var carPartInfo in needList[cellEntity]) {
            var carPart = isContainer
                ? _sceneService.Value.GetCarPartCell(carView.CarPartsContainer)
                : carCmp.CarPartViewsGrid[carPartInfo.x, carPartInfo.y];
            carPartCmp.CellCarPartsStuffViews.Add(carPart);
            if (isContainer) {
                string needLayer = isPlayer ? "CarBase" : "CarBaseEnemy";
                carPart.SetupCarPart(carPartInfo, needLayer, carPartCmp.MainEntity);
                carPart.transform.localPosition =
                    new Vector2(carPartInfo.x, carPartInfo.y) - new Vector2(containersSize.x / 2f, containersSize.y / 2f);
                carPart.transform.localRotation = Quaternion.Euler(0, 0, needRotate);
                carPart.SetColliderEnableState(true);
                carPart.gameObject.tag = isPlayer ? "Player" : "Enemy";
                carCmp.CarPartViewsGrid[carPartInfo.x, carPartInfo.y] = carPart;
            } else {
                string needLayer = isPlayer ? "UpCarPart" : "UpCarPartEnemy";
                carPart.SetupCarPart(carPartInfo, needLayer, carPartCmp.MainEntity);
                carPart.transform.localRotation = Quaternion.Euler(0, 0, needRotate);
            }

            // need check
        }

        carPartCmp.CarPartCellRenderer = _sceneService.Value.GetCarPartSpriteRenderer(carView.transform);
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

            var gunCmp = _gunComponentsPool.Value.Has(cellEntity)
                ? _gunComponentsPool.Value.Get(cellEntity)
                : _gunComponentsPool.Value.Add(cellEntity);
            var gunConfig = _sceneService.Value.GetGunFromCarPart(carPartCmp.CarPartId);
            if (gunConfig.UpPartSprite != null) {
                gunCmp.UpPartContainer = _sceneService.Value.GetGunContainer(carPartCmp.CarPartCellRenderer.transform);
                gunCmp.UpPartContainer.transform.localPosition = gunConfig.GunContainerPosition;
                gunCmp.UpPartContainer.transform.localRotation = Quaternion.Euler(0, 0, 0);

                gunCmp.FirePointTransform = _sceneService.Value.GetGunContainer(gunCmp.UpPartContainer);
                gunCmp.FirePointTransform.localPosition = gunConfig.FirePointPosition - gunConfig.GunContainerPosition;
                gunCmp.FirePointTransform.transform.localRotation = Quaternion.Euler(0, 0, 0);

                gunCmp.GunSpriteRenderer = _sceneService.Value.GetCarPartSpriteRenderer(gunCmp.UpPartContainer);
                gunCmp.GunSpriteRenderer.sprite = gunConfig.UpPartSprite;
                gunCmp.GunSpriteRenderer.sortingOrder = 3;
                gunCmp.GunSpriteRenderer.transform.localPosition = gunConfig.UpPartPosition - gunConfig.GunContainerPosition;
                gunCmp.GunSpriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, 0);
            } else {
                gunCmp.FirePointTransform = _sceneService.Value.GetGunContainer(carPartCmp.CarPartCellRenderer.transform);
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
                    _laserComponentsPool.Value.Get(cellEntity).LaserLineRenderer = _sceneService.Value.GetLaserLineRenderer();
                else
                    _laserComponentsPool.Value.Add(cellEntity).LaserLineRenderer = _sceneService.Value.GetLaserLineRenderer();
            }

            _gunComponentsPool.Value.Get(cellEntity) = gunCmp;
        } else if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
            var meleeWeaponCmp = _meleeWeaponComponentsPool.Value.Has(cellEntity)
                ? _meleeWeaponComponentsPool.Value.Get(cellEntity)
                : _meleeWeaponComponentsPool.Value.Add(cellEntity);
            var meleeWeaponConfig = _sceneService.Value.GetMeleeWeaponFromCarPart(carPartCmp.CarPartId);
            if (meleeWeaponConfig.UpSprite != null) {
                meleeWeaponCmp.MeleeWeaponRenderer = _sceneService.Value.GetCarPartSpriteRenderer(carPartCmp.CarPartCellRenderer.transform);
                meleeWeaponCmp.MeleeWeaponRenderer.sprite = meleeWeaponConfig.UpSprite;
                meleeWeaponCmp.MeleeWeaponRenderer.sortingOrder = 3;
                meleeWeaponCmp.MeleeWeaponRenderer.transform.localPosition = meleeWeaponConfig.UpSpritePosition;
            }

            var meleeStuff = _sceneService.Value.GetMeleeCarPartCell(meleeWeaponCmp.MeleeWeaponRenderer.transform);
            meleeWeaponCmp.MeleeStuff = meleeStuff;
            meleeStuff.transform.localRotation = Quaternion.Euler(0, 0, 0);
            meleeStuff.transform.localPosition = Vector3.zero;
            meleeStuff.MakeTriggerCollider();
            meleeStuff.SetColliderEnableState(true);
            (float damageMultiplier, float damageBySpeedMultiplier) =
                GridUtils.GetMeleeLevelMultipliers(meleeWeaponConfig, carPartCmp.PartLevel);
            meleeWeaponCmp.CurrentDamage = meleeWeaponConfig.Damage * damageMultiplier;
            meleeWeaponCmp.DamageBySpeed = meleeWeaponConfig.DamageAddedFromOneSpeed * damageBySpeedMultiplier;
            _meleeWeaponComponentsPool.Value.Get(cellEntity) = meleeWeaponCmp;
            _constructMeleeCellEventsPool.Value.Add(cellEntity);
        } else if (carPartConfig.PartType == CarPartType.Support) {
            var supportCarPartConfig = _sceneService.Value.GetSupportModuleFromCarPart(carPartCmp.CarPartId);
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
            var wheelsConfig = _sceneService.Value.GetWheelContainerFromCarPart(carPartConfig.Id);
            carCmp.CurrentMaxWeight += wheelsConfig.MaxWeight;
        }
    }

    private void SetupEnemyMovementComponent(int enemyEntity, CarConfig carConfig) {
        ref var movementComponent = ref _movementComponentsPool.Value.Get(enemyEntity);
        var carCmp = _carComponentsPool.Value.Get(enemyEntity);
        movementComponent.RotateSpeed = carConfig.RotateSpeed *
                                        _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).RoundMoneyMultiplayer *
                                        (1 - carCmp.CurrentWeight / carCmp.CurrentMaxWeight);
        movementComponent.Acceleration = carConfig.Acceleration;
        movementComponent.Deceleration = carConfig.Deceleration;

        movementComponent.MovableObjectRigidbody.mass = carCmp.CurrentWeight;

        movementComponent.MaxMoveSpeed = carConfig.DefaultMaxSpeed * (1 - carCmp.CurrentWeight / carCmp.CurrentMaxWeight);
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
        var carView = _sceneService.Value.InstantiateCarView();
        _movementComponentsPool.Value.Get(entity).MovableObjectRigidbody = carView.Rigidbody;
        string tag = entity == _sceneService.Value.PlayerEntity ? "Player" : "Enemy";
        carView.Construct(entity, ColissionCarsHit, tag);
        ref var carCmp = ref _carComponentsPool.Value.Get(entity);
        carCmp.CarView = carView;
        var carConfig = _sceneService.Value.GetCarConfig(carCmp.CarId);
       // carView.CarPartsContainer.localPosition = new Vector3(-(float)carConfig.Width / 2, (float)carConfig.Height / 2, 0);
       
       // carCmp.CurrentWeight += carCmp.CurrentMaxWeight / 5f;
        carCmp.ForwardWheels = new List<Animator>();
        carCmp.ForwardWheelsParticles = new List<ParticleSystem>();
        carCmp.CurrentWeight = 0;
      
        return carView;
    }

    private void SetupWheels(int carEntity, CarView carView) {
        int maxYposWheels = 0;
        ref var carCmp  =ref _carComponentsPool.Value.Get(carEntity);
        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartConfig = _sceneService.Value.GetCarPartConfig(_carPartComponentsPool.Value.Get(carPartContainer.Key).CarPartId);
            if (carPartConfig.PartType == CarPartType.WheelsContainer) {
                maxYposWheels = maxYposWheels < carPartContainer.Value[0].y? carPartContainer.Value[0].y : maxYposWheels;
            }
        }

        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartContainer.Key);
            var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartCmp.CarPartId);
            if (carPartConfig.PartType == CarPartType.WheelsContainer) {
                var wheelConfig = _sceneService.Value.GetWheelContainerFromCarPart(carPartConfig.Id);

                if (!_wheelContainerComponentsPool.Value.Has(carPartContainer.Key))
                    _wheelContainerComponentsPool.Value.Add(carPartContainer.Key);
                ref var wheelCmp  = ref _wheelContainerComponentsPool.Value.Get(carPartContainer.Key);
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
        var wheelTransform = _sceneService.Value.InstantiateAnimator(_sceneService.Value._carStuffListsConfig.Wheels[wheelId].WheelAnimator,
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

    private bool MainPartIsPlaced() {
        foreach (var cellEntity in _inventoryCellComponentsFilter.Value) {
            if (_sceneService.Value.GetCarPartConfig(_inventoryCellComponentsPool.Value.Get(cellEntity).CarPartId).PartType == CarPartType.Main)
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
        Transform obstaclePrefab = _sceneService.Value._mountains[Random.Range(0, _sceneService.Value._mountains.Length)];
        
        Quaternion randomRotation = Quaternion.Euler(0f, 0f, Random.Range(0, 4)*90);
        
        Transform obstacle = _sceneService.Value.InstantiateObject(obstaclePrefab, new Vector3(position.x, position.y, 0f));
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
    private void ChangeInventoryState(bool state) => _sceneService.Value._inventoryUIView.gameObject.SetActive(state);
    private void ChangePlayerUIState(bool state) => _sceneService.Value._playerUIView.gameObject.SetActive(state);
}