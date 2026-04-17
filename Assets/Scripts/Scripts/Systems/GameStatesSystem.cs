using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Client;
using DG.Tweening;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class  GameStatesSystem : IEcsInitSystem, IEcsRunSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneService;
    private EcsCustomInject<NetcodeSceneService> _netcodeSceneService;
    private EcsCustomInject<BetweenScenesService> _betweenScenesService;

    private EcsPoolInject<EndRoundEvent> _endRoundEventsPool;
    private EcsPoolInject<WindComponent> _windComponentsPool;
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
    private EcsPoolInject<CarPartInfoComponent> _carPartInfoComponentsPool;
    private EcsPoolInject<QuicksandComponent> _quicksandComponentsPool;
    private EcsPoolInject<ThunderstormComponent> _thunderstormComponentsPool;
    private EcsPoolInject<TornadoSpawnControllerComponent> _tornadoSpawnControllerComponentsPool;
    private EcsPoolInject<TornadoComponent> _tornadoComponentsPool;
    private EcsPoolInject<BomberPlaneControllerComponent> _bomberPlaneControllerComponentsPool;
    private EcsPoolInject<BossComponent> _bossComponentsPool;
    private EcsPoolInject<VersusBattleComponent> _versusBattleComponentsPool;
    private EcsPoolInject<FireExtinguisherComponent> _fireExtinguisherComponentsPool;
    private EcsPoolInject<TrainControllerComponent> _trainControllerComponentsPool;
    private EcsPoolInject<HpBarComponent> _hpBarComponentsPool;
    private EcsPoolInject<PlayerComponent> _playerComponentsPool;
    private EcsPoolInject<PlayerTagComponent> _playerTagComponentsPool;
    private EcsPoolInject<ServerValuesComponent> _serverValuesComponentsPool;
    private EcsPoolInject<UpdateCarStatsEvent> _updateCarStatsEventsPool;

    private EcsFilterInject<Inc<CarPartCellComponent>> _cellComponentsFilter;
    private EcsFilterInject<Inc<InventoryCellComponent>> _inventoryCellComponentsFilter;
    private EcsFilterInject<Inc<CarComponent>> _carComponentsFilter;
    private EcsFilterInject<Inc<FilledZoneComponent>> _filledZoneComponentsFilter;
    private EcsFilterInject<Inc<TaskComponent>> _taskComponentsFilter;
    private EcsFilterInject<Inc<CarPartInGameUIComponent>> _carPartInGameUIComponentsFilter;
    private EcsFilterInject<Inc<BossComponent>> _bossComponentsFilter;
    private EcsFilterInject<Inc<DestroyEntityEvent>> _destroyEntityEventsFilter;
    private EcsFilterInject<Inc<PlayerTagComponent>> _playerTagComponentsFilter;

    private Vector2 _cellOffset = new Vector2(0.5f, 0.5f);

    public void Init(IEcsSystems systems) {
       // Application.targetFrameRate = 144;
       int playerEntity = _world.Value.NewEntity();
       ref var curGameCmp = ref _currentGameComponentsPool.Value.Add(playerEntity);
       NetcodeBetweenScenesService.Instance.SetupService(UpdateMapGridClientRpc,SetQuicksandMethods, AddTrainTrailInList, StartCurrentRoundType);
       if (NetworkManager.Singleton.IsServer)
           _serverValuesComponentsPool.Value.Add(_sceneService.Value.PlayerEntity).ClientsEntities = new Dictionary<ulong, int>();
        _playerCurrencyComponentsPool.Value.Add(playerEntity);
        _sceneService.Value.PlayerEntity = playerEntity;

        _netcodeSceneService.Value.SetupService(GetHpBar, DeleteHpBarComponent, SetupLaserComponent, CarViewSetup, SetupWheelComponents,
            SetupCarPartSprites, SetupClientCar, SetMovementComponentValues, SetClientCarView, SetFuelValues,SetHpUiValues, BrokeCarPartSprite,
            SetFilledZonePositionToClient,SetFilledZoneFillAmountToClient, SetMapPointForClients,EndRound);
        //  curGameCmp.RoundTypes = new List<RoundType[]>();
        curGameCmp.CurrentBattleRounds = 2;
        
        _versusBattleComponentsPool.Value.Add(playerEntity);
        SetupVersusBattleUI();
        
        if (!DataPersistenceManagerSystem._gameData.TutorialIsCompleted)
            _betweenScenesService.Value.CurrentSelectedMap = _betweenScenesService.Value._carStuffListsConfig.MapConfigs.Length-1;
        var mapConfig = _betweenScenesService.Value.GetMapConfig();
        _sceneService.Value.globalLight.color = mapConfig.LightColor;
        var difficultyConfig = _sceneService.Value._carStuffListsConfig.MapConfigs[_betweenScenesService.Value.CurrentSelectedMap]
            .MapDifficltyConfigs[_betweenScenesService.Value.CurrentSelectedMapDifficlty];
        curGameCmp.DifficultyDamageMultiplayer = difficultyConfig.DamageMultiplayer;
        curGameCmp.DifficultyHealthMultiplayer = difficultyConfig.HealthMultiplayer;
        curGameCmp.LocalTimeScale = 1;
        _sceneService.Value._shopUIView._inflationText.transform.parent.gameObject.SetActive(difficultyConfig.ShopInflationPerRound != 0);
        //map path
        
        for (int i = 0; i < 20-difficultyConfig.RoundsCount; i++) 
              _sceneService.Value._mapUIView.PathElementsImage[^(i+1)].gameObject.SetActive(false);
        
        if (difficultyConfig.MapEvents != null)
            foreach (var mapEvent in difficultyConfig.MapEvents) {
                if (mapEvent.MapEventType == MapEventsType.Wind) {
                    ref var windCmp = ref _windComponentsPool.Value.Add(playerEntity);
                    windCmp.WindDirection = WindDirections[Random.Range(0, WindDirections.Length)];
                    windCmp.WindSpeed = mapEvent.MapEventFirstCount;

                    var instanceMaterial = new Material(_sceneService.Value._cloudsSpriteRenderer.material);
                    _sceneService.Value._cloudsSpriteRenderer.material = instanceMaterial;

                    instanceMaterial.SetVector("_Speed",
                        new Vector4(-windCmp.WindDirection.x * windCmp.WindSpeed / 2000, -windCmp.WindDirection.y * windCmp.WindSpeed / 2000, 0,
                            0));
                }
                else if (mapEvent.MapEventType == MapEventsType.ThunderStorm) {
                    
                    _thunderstormComponentsPool.Value.Add(playerEntity).TimeToNextThunder = 5;

                    _sceneService.Value._cloudsSpriteRenderer.material.SetTexture("_CloudTex",
                        _sceneService.Value._mapEventsContentView._thunderCloudsSprite.texture);
                    _sceneService.Value.globalLight.color = Color.gray;
                    _sceneService.Value.globalLight.intensity = 0.92f;
                } else if (mapEvent.MapEventType == MapEventsType.Tornado) {
                    if (_tornadoSpawnControllerComponentsPool.Value.Has(playerEntity))
                        _tornadoSpawnControllerComponentsPool.Value.Get(playerEntity).TornadoToSpawn.Add((int)mapEvent.MapEventFirstCount);
                    else
                        _tornadoSpawnControllerComponentsPool.Value.Add(playerEntity).TornadoToSpawn =
                            new List<int>() { (int)mapEvent.MapEventFirstCount };
                }
                else if (mapEvent.MapEventType == MapEventsType.Bomber) {
                    ref var bomberController = ref _bomberPlaneControllerComponentsPool.Value.Add(playerEntity);
                    bomberController.BombIndex = (int)mapEvent.MapEventFirstCount;
                    var bomberSpriteRenderer = _sceneService.Value.GetSpriteRenderer();
                    bomberController.BomberShadowTransform = bomberSpriteRenderer.transform;
                    bomberSpriteRenderer.sprite = _sceneService.Value._mapEventsContentView._bomberSprite;
                    bomberSpriteRenderer.color = new Color(0, 0, 0, 0.3f);
                    bomberSpriteRenderer.transform.localScale = Vector3.one*4.5f;
                    bomberSpriteRenderer.gameObject.SetActive(false);
                    bomberSpriteRenderer.sortingOrder = 15;
                    //  if (_tornadoSpawnControllerComponentsPool.Value.Has(playerEntity))
                    // _tornadoSpawnControllerComponentsPool.Value.Get(playerEntity).TornadoToSpawn.Add((int)mapEvent.MapEventFirstCount);
                    //    else
                    //   _tornadoSpawnControllerComponentsPool.Value.Add(playerEntity).TornadoToSpawn =
                    //     new List<int>() { (int)mapEvent.MapEventFirstCount };
                }
            }

        if (!_windComponentsPool.Value.Has(playerEntity)) {
            var instanceMaterial = new Material(_sceneService.Value._cloudsSpriteRenderer.material);
            _sceneService.Value._cloudsSpriteRenderer.material = instanceMaterial;
            var windDirection = WindDirections[Random.Range(0, WindDirections.Length)];
            instanceMaterial.SetVector("_Speed",
                new Vector4(windDirection.x * 0.005f, windDirection.y * 0.005f, 0,
                    0));
        }
        
        _settingsComponentsPool.Value.Add(_sceneService.Value.PlayerEntity).pixelizedEffectIsUsed = DataPersistenceManagerSystem._gameData.UsePixelizedEffects;
        
        if (_betweenScenesService.Value.IsCoop) {
            _sceneService.Value._inventoryUIView._playButton.onClick.AddListener(ChangeGameReadyState);
            NetcodeBetweenScenesService.Instance.playerReadyStates.OnListChanged += OnPlayerReadyStatesChanged;
        } else {
            _sceneService.Value._inventoryUIView._playButton.onClick.AddListener(ChangeRoundInfo);
            _sceneService.Value._inventoryUIView._playButtonText.text = GetLocalization("Start game") + "!";
        }
            
        
        _sceneService.Value._mapPointPrefab.SetupPoint(UpdateMapPoint);
        _sceneService.Value._mapFilledZoneView.SetupFilledZone(SetFeelingZoneState);
        _sceneService.Value._mapFilledZoneView.FilledZoneGround.color = mapConfig.MapVisualInfoConfigs[0].WheelParticlesColor;
        var settingsView = _sceneService.Value._settingsView;
        settingsView._settingsChangeStateButton.onClick.AddListener(SetSettingsState);
        settingsView._saveSettingsButton.onClick.AddListener(SaveSettings);
        settingsView._blueColorSlider.onValueChanged.AddListener(ChangePickedEnemyProjectilesColor);
        settingsView._redColorSlider.onValueChanged.AddListener(ChangePickedEnemyProjectilesColor);
        settingsView._greenColorSlider.onValueChanged.AddListener(ChangePickedEnemyProjectilesColor);
        
        settingsView._setPixelizedEffectModeButton.onClick.AddListener(()=>ChangePixelizedEffectsMode(true));
        settingsView._setHealthBarsButton.onClick.AddListener(()=>ChangeHealthBarsMode(true));
        settingsView._setDamageNumbersButton.onClick.AddListener(()=>ChangeDamageNumbersMode(true));
        SetSettingCheckers();
        
        settingsView._quitButton.onClick.AddListener(GoToDesktop);
        settingsView._mainMenuButton.onClick.AddListener(GoToMainMenu);
        settingsView._restartButton.onClick.AddListener(RestartGame);
        
        settingsView._closeSettingsButton.onClick.AddListener(SetSettingsState);
        curGameCmp.CarPartsToEnemyBuilding = new Dictionary<CarPartType, List<int>>() {
            {CarPartType.Gun, new List<int>()} ,
            {CarPartType.MeleeWeapon, new List<int>()} ,
            {CarPartType.Armor, new List<int>()} ,
            {CarPartType.Support, new List<int>()} ,
        };
        if (mapConfig.GameProcessConfig.GameRounds[0].AddedCarPartsToEnemyPool != null)
            foreach (var addedCarPartId in mapConfig.GameProcessConfig.GameRounds[0].AddedCarPartsToEnemyPool) {
                curGameCmp.CarPartsToEnemyBuilding[_sceneService.Value.GetCarPartConfig(addedCarPartId).PartType].Add(addedCarPartId);
            }

        
        _shopComponentsPool.Value.Add(_sceneService.Value.PlayerEntity).ShopLevel = -1;
        UpgradeShop();
        _sceneService.Value._shopUIView._upgradeShopButton.onClick.AddListener(UpgradeShop);
        
        SetColorSliderEnemyProjectilesValues();
        NetcodeBetweenScenesService.Instance.readyClientsCount.OnValueChanged += OnSceneLoadComplete;
        NetcodeBetweenScenesService.Instance.ReportSceneLoadedServerRpc();
        SetupSupportBox();
        SetCoopUI();
     
    }
    private void OnSceneLoadComplete(int first,int second)
    {
        if ( NetcodeBetweenScenesService.Instance.readyClientsCount.Value >= NetcodeBetweenScenesService.Instance.playerReadyStates.Count)
        { 
              GenerateMap();
        }
    }

    private Vector2[] WindDirections = new[] {
        new Vector2(0, 1), new Vector2(-1, 0), new Vector2(0, -1), new Vector2(1, -1), new Vector2(1, 0), new Vector2(1, 1), new Vector2(-1, -1),
        new Vector2(-1, 1)
    };
    
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
                    EndRoundForAll();
                 
                }

                break;
            
            case RoundType.BossBattle:
                if (_bossComponentsFilter.Value.GetEntitiesCount() == 0) {
                    _sceneService.Value._playerUIView._gameTimerText.gameObject.SetActive(false);
                    EndRoundForAll();
                    WinGame();
                }

                break;

            case RoundType.FromAToB:
                curGameCmp.RoundTime += Time.deltaTime;
                if (!_fromPointToPointRoundComponentsPool.Value.Has(_sceneService.Value.PlayerEntity)) return;
                ref var pointToPointRound = ref _fromPointToPointRoundComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
                var moveCmp = _movementComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
                if (moveCmp.MovableObjectRigidbody != null)
                    UpdateArrowDirection( _sceneService.Value._mapPointPrefab.transform.position, moveCmp.MovableObjectRigidbody.gameObject.transform.position);
                if (pointToPointRound.TimeAfterMarkPoint > 0)
                    pointToPointRound.TimeAfterMarkPoint -= Time.deltaTime;
                break;
            
            case RoundType.ZoneDefence:
                curGameCmp.RoundTime += Time.deltaTime;

                if (NetworkManager.Singleton.IsServer &&
                    _serverValuesComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).TickFuelServerTime == 0) {
                    var filledZoneCmp = _filledZoneComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
                    _netcodeSceneService.Value.SetZoneFillAmountClientRpc(filledZoneCmp.CurrentZoneTime / filledZoneCmp.NeedZoneTime);
                }
               
                break;
            
            case RoundType.VersusBattle:
                ref var versusBattleCmp = ref _versusBattleComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

                if (versusBattleCmp.IsBattleEnded) {
                    if (Input.GetMouseButtonDown(0)) 
                        CompleteVersusBattle();
                    break;
                }
                foreach (var needEntity in _destroyEntityEventsFilter.Value) {
                    versusBattleCmp.LeftOpponentEntity =
                        versusBattleCmp.LeftOpponentEntity == needEntity ? 0 : versusBattleCmp.LeftOpponentEntity;
                    versusBattleCmp.RightOpponentEntity =
                        versusBattleCmp.RightOpponentEntity == needEntity ? 0 : versusBattleCmp.RightOpponentEntity;
                }

                if (versusBattleCmp.LeftOpponentEntity == 0 || versusBattleCmp.RightOpponentEntity == 0) {
                    EndVersusBattle();
                    break;
                }
                    

                curGameCmp.RoundTime += Time.deltaTime;
                var versusBattleView = _sceneService.Value._versusBattleView;
                versusBattleView._timerText.text = ((int)curGameCmp.RoundTime).ToString();
                if (curGameCmp.RoundTime > 15 && !versusBattleCmp.IsCircularSawsSended) {
                    versusBattleView._circularSawLeft.transform.DOMoveX(-5, 20);
                    versusBattleView._circularSawRight.transform.DOMoveX(5, 20);
                    versusBattleCmp.IsCircularSawsSended = true;
                }

                break;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) SetSettingsState();

        if (!RoundUtilits.IsBattleRound(curGameCmp.CurrentRoundType)) return;

        if (_betweenScenesService.Value.IsCoop && _serverValuesComponentsPool.Value.Has(_sceneService.Value.PlayerEntity) ) {
            ref var serverCmp = ref _serverValuesComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
          serverCmp.TickFuelServerTime+= Time.deltaTime;
          serverCmp.TickCarPartHealthServerTime += Time.deltaTime;
          if (serverCmp.TickFuelServerTime >= 0.3f) {
              serverCmp.TickFuelServerTime = 0;
              FuelDataForRpc[] fuelDataForPlayers = new FuelDataForRpc[serverCmp.ClientsEntities.Count];
              int i = 0;
              foreach (var clientEntity in serverCmp.ClientsEntities) {
                  int playerEntity = _playerComponentsPool.Value.Get(clientEntity.Value).PlayerEntity;

                  var fuelCmp = _fuelComponentsPool.Value.Get(playerEntity);
            //      Debug.Log("fuel count for client " + fuelCmp.MaxFuel + " "+ fuelCmp.CurrentFuel);
                  fuelDataForPlayers[i] = new FuelDataForRpc(clientEntity.Key, fuelCmp.CurrentElectricity, fuelCmp.MaxElectricity,
                      fuelCmp.CurrentFuel, fuelCmp.MaxFuel);
                  i++;
              }
              
              _netcodeSceneService.Value.SetFuelValuesClientRpc(fuelDataForPlayers);
          }

          if (serverCmp.TickCarPartHealthServerTime >= 0.7f) {
              serverCmp.TickCarPartHealthServerTime = 0;
              List<CarPartsHpDataForRpc> healthDataForPlayers = new List<CarPartsHpDataForRpc>();

               foreach (var clientEntity in serverCmp.ClientsEntities) {

                   int playerEntity = _playerComponentsPool.Value.Get(clientEntity.Value).PlayerEntity;
                //  ulong clientId = serverCmp.ClientsEntities.FirstOrDefault(x => x.Value == playerEntity).Key;
                  foreach (var changedCarPartEntity in _playerTagComponentsPool.Value.Get(playerEntity).ChangedCarPartsHp) {
                      var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(changedCarPartEntity.Key);
                      var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartInfoCmp.CarPartId);
                      Debug.Log(" checked car part info"+ carPartConfig.PartName);
                      bool isContainerType = GridUtils.IsContainerType(carPartConfig);
                      Vector2Int gridPosition = isContainerType
                          ? _carComponentsPool.Value.Get(playerEntity).CarPartContainerGroupsList[changedCarPartEntity.Key][0]
                          : _carComponentsPool.Value.Get(playerEntity).CarPartGroupsList[changedCarPartEntity.Key][0];
                      healthDataForPlayers.Add(new CarPartsHpDataForRpc(gridPosition, changedCarPartEntity.Value, clientEntity.Key, !isContainerType));
                  }
                  _playerTagComponentsPool.Value.Get(playerEntity).ChangedCarPartsHp.Clear();
              
}
              if (healthDataForPlayers.Count != 0)
                  _netcodeSceneService.Value.SetCarPartsHpDataClientRpc(healthDataForPlayers.ToArray());
          }
        }
        
        float timeBetweenEnemiesSpawn = _betweenScenesService.Value.GetMapDifficultyConfig().TimeBetweenInRoundEnemySpawn;
        if (curGameCmp.RoundTime / timeBetweenEnemiesSpawn > curGameCmp.RoundAddedEnemies) {
            if (curGameCmp.CurrentRoundType != RoundType.BattleDefault ||
                curGameCmp.RoundTime < (timeBetweenEnemiesSpawn - 5 + curGameCmp.CurrentRound * 3))
                SendEnemyAirplane();

            curGameCmp.RoundAddedEnemies++;
        } else if (curGameCmp.EnemySendPhase == 1)
            CheckEnemyAirplane();
                     

        if (curGameCmp.CurrentRound > 3 && curGameCmp.CurrentRound % 2 == 0 && curGameCmp.RoundTime > 20 && curGameCmp.SupportBoxPhase < 2) {
            if (curGameCmp.SupportBoxTargetPosition.x == 999f)
                SendSupportAirplane();
            else if (curGameCmp.SupportBoxPhase == 1)
                CheckSupportBoxAirPlane();
        }

        if (_trainControllerComponentsPool.Value.Has(_sceneService.Value.PlayerEntity)) {
            ref var trainControllerCmp = ref _trainControllerComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            trainControllerCmp.TimeToNextTrain -= Time.deltaTime;
            if (trainControllerCmp.TimeToNextTrain <= 0)
                SpawnTrain();
        }
    }

    private void BrokeCarPartSprite(SpriteRenderer carPartSprite) {
        carPartSprite.sortingOrder -= 5;
        carPartSprite.color = _sceneService.Value._carStuffListsConfig.DestroyedCarPartColor;
        carPartSprite.transform.parent = _sceneService.Value._brokenCarPartsContainer;
        var sprites = carPartSprite.GetComponentsInChildren<SpriteRenderer>(true);
    
        foreach (var sprite in sprites)
        {
            sprite.sortingOrder -= 5;
            sprite.color = _sceneService.Value._carStuffListsConfig.DestroyedCarPartColor;
        }
    }
    private void SetHpUiValues(Vector2Int gridPosition, float health, bool isUpperPart) {
        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        int carPartEntity = isUpperPart?carCmp.CarPartGroupsGrid[gridPosition.x, gridPosition.y]:carCmp.CarPartContainerGroupsGrid[gridPosition.x, gridPosition.y];
        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartEntity);
        ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(carPartEntity);
        var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartEntity);
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        carPartInfoCmp.CurrentHealth = health;
        var healthPercent = carPartInfoCmp.CurrentHealth / carPartCmp.MaxHealth;
        var needList =isUpperPart? carCmp.CarPartGroupsList[carPartEntity]:carCmp.CarPartContainerGroupsList[carPartEntity];
        foreach (var carPartStuff in needList) {
            if (carPartConfig.PartType == CarPartType.WheelsContainer || carPartConfig.PartType == CarPartType.CarStuffContainer)
                inventoryCmp.CarPartInfoUIViewsGrid[carPartStuff.x, carPartStuff.y]
                    .SetHealthCount(_sceneService.Value._healthGradient.Evaluate(healthPercent), (int)carPartInfoCmp.CurrentHealth);
            else {
                if (carPartConfig.PartType == CarPartType.Main) {
                    _sceneService.Value._playerUIView._lampLightImage.DOKill();
                    _sceneService.Value._playerUIView._lampLightImage.DOColor(new Color(1, 0, 0, 0.6f), 0.45f).OnComplete(() =>
                        _sceneService.Value._playerUIView._lampLightImage?.DOColor(new Color(1, 0, 0, 0), 0.45f));
                }
                
                inventoryCmp.CarPartInfoUIViewsUpLevelGrid[carPartStuff.x, carPartStuff.y]
                    .SetHealthCount(_sceneService.Value._healthGradient.Evaluate(healthPercent), (int)carPartInfoCmp.CurrentHealth);
            }
        }
    }
    private void SendEnemyAirplane() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        curGameCmp.EnemyTargetPosition = GetPositionAroundPlayer();
        curGameCmp.EnemySendPhase = 1;
        _sceneService.Value._enemyAirplane.position = new Vector2 (curGameCmp.EnemyTargetPosition.x,-120);
        _sceneService.Value._enemyAirplane.DOMoveY(120, 20).SetEase(Ease.Linear).OnComplete(() => 
            _sceneService.Value._enemyAirplane.gameObject.SetActive(false)
        );
        _sceneService.Value._enemyAirplane.gameObject.SetActive(true);

    }

    private void SetFuelValues(float fuelValue, float maxFuelValue, float electricityValue, float maxElectricity) {
        //ref var fuelCmp = ref _fuelComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        if (fuelValue / maxFuelValue < 0.25f && !_sceneService.Value._playerUIView._fuelLampLightImage.gameObject.activeInHierarchy) {
            var fuelLampLight = _sceneService.Value._playerUIView._fuelLampLightImage;
            fuelLampLight.color = Color.clear;
            fuelLampLight.gameObject.SetActive(true);
            fuelLampLight.DOFade(1, 0.25f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InSine);
        }

      Debug.Log("fuel values"+fuelValue + " "+ maxFuelValue);
        if(maxFuelValue == 0)return;
        var targetAngle = -65f + 130f * fuelValue / maxFuelValue;
        _sceneService.Value._playerUIView._fuelArrowImage.transform.DORotate(new Vector3(0, 0, targetAngle), 0.3f);
        _sceneService.Value._playerUIView._fuelText.text = $"{(int)fuelValue}";
    }
    private void OnPlayerReadyStatesChanged(NetworkListEvent<PlayerReadyState> changeEvent) {
        UpdateLobbyUI();
        if (NetcodeBetweenScenesService.Instance.CheckAllPlayersReady()) {
            if (NetcodeBetweenScenesService.Instance.IsHost)
                ChangeRoundInfo();
            else {
                ChangeInventoryState(false);
                _netcodeSceneService.Value.ShowMessageClient("Host select round");
            }
        }
    }
    private void CheckEnemyAirplane() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        if (_sceneService.Value._enemyAirplane.position.y > curGameCmp.EnemyTargetPosition.y)
            SendEnemyCar();
    }

    private void DeleteHpBarComponent(int entity) {
        if (_hpBarComponentsPool.Value.Has(entity))
            _hpBarComponentsPool.Value.Del(entity);
    }
    private void SetupLaserComponent( NetworkObject newText, Sequence needSequence,int entity) {
        ref var laserCmp = ref _laserComponentsPool.Value.Get(entity);
        laserCmp.LaserDamageText = newText;
        laserCmp.CurrentSequence = needSequence;
    }
    private void SendEnemyCar() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        curGameCmp.EnemySendPhase = 2;
        var parachute = _sceneService.Value._parachuteSpriteRenderer;
        parachute.transform.localScale = Vector3.one * 2f;
        parachute.color = new Color(0, 0, 0, 0.3f);
        parachute.transform.position = curGameCmp.EnemyTargetPosition;
        parachute.gameObject.SetActive(true);
        parachute.transform.DOScale(4, 2f).OnComplete(() => {
            parachute.transform.localScale = Vector3.one * 12;
            parachute.color = Color.white;
            parachute.transform.DOScale(4, 2f).OnComplete(() => {
                ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
                curGameCmp.SupportBoxPhase = 3;
                parachute.gameObject.SetActive(false);
                SetEnemiesCars(1, false);
                curGameCmp.EnemyTargetPosition = Vector2.one * 999;
            });
        });
    }

    private void GetHpBar(NetworkObject newHpBar, int entity) {
        _hpBarComponentsPool.Value.Get(entity).hpBarImage = newHpBar;
    }
    private void SpawnTrain() {
        ref var trainControllerCmp = ref _trainControllerComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        var needTrail = trainControllerCmp.TrainRails[Random.Range(0, trainControllerCmp.TrainRails.Count)];
        bool isFirstSide = Random.value > 0.5f;
        if (needTrail.localEulerAngles.z != 0) {
            if (isFirstSide) {
                 trainControllerCmp.TrainTransform.localEulerAngles = new Vector3(0, 0, 270 );
                 trainControllerCmp.TrainTransform.position = new Vector3(needTrail.position.x, 110, 0);
                 trainControllerCmp.TrainTransform.DOMoveY(-130, 12);
            } else {
                trainControllerCmp.TrainTransform.localEulerAngles = new Vector3(0, 0,90);
                trainControllerCmp.TrainTransform.position = new Vector3(needTrail.position.x, -110, 0);
                trainControllerCmp.TrainTransform.DOMoveY(130, 12);
            }
           
        } else {
            if (isFirstSide) {
                trainControllerCmp.TrainTransform.localEulerAngles = new Vector3(0, 0, 180);
                trainControllerCmp.TrainTransform.position = new Vector3(110, needTrail.position.y, 0);
                trainControllerCmp.TrainTransform.DOMoveX(-130, 12);
            } else {
                trainControllerCmp.TrainTransform.localEulerAngles = new Vector3(0, 0, 0);
                trainControllerCmp.TrainTransform.position = new Vector3(-110, needTrail.position.y, 0);
                trainControllerCmp.TrainTransform.DOMoveX(130, 12);
            }
        }
        trainControllerCmp.TrainTransform.gameObject.SetActive(true);
        trainControllerCmp.TimeToNextTrain += 15;
    }
    private void CheckSupportBoxAirPlane() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        var supportBoxView = _sceneService.Value._supportBoxStuffView;
        if (supportBoxView.supportBoxAirplane.position.x > curGameCmp.SupportBoxTargetPosition.x ) 
            SendSupportBox( );
    }
    
    

    private void SendSupportAirplane() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        var supportBoxView = _sceneService.Value._supportBoxStuffView; 
        curGameCmp.SupportBoxTargetPosition = GetPositionAroundPlayer();
        curGameCmp.SupportBoxPhase = 1;
        supportBoxView.supportBoxAirplane.position = new Vector2 (-120,curGameCmp.SupportBoxTargetPosition.y);
        supportBoxView.supportBoxAirplane.DOMoveX(120, 20).SetEase(Ease.Linear).OnComplete(() => 
            supportBoxView.supportBoxAirplane.gameObject.SetActive(false)
        );
        supportBoxView.supportBoxAirplane.gameObject.SetActive(true);
        _sceneService.Value.PlayWarningAnimation(GetLocalization("Supply drop")+"!",true);
        
    }
    private void SendSupportBox() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        curGameCmp.SupportBoxPhase = 2;
        var supportBoxView = _sceneService.Value._supportBoxStuffView;
        var supportBox = supportBoxView._supportBoxcarPartStuffView;
        supportBox._carPartCollider.enabled = false;
        curGameCmp.SupportBoxTargetPosition = Vector2.one * 999;
        supportBox.gameObject.SetActive(true);
        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(supportBox._mainEntity);
        float randomValue = Random.value;
        if (randomValue < 0.75f) {
            carPartCmp.MaxHealth = 100;
            supportBoxView._supportBoxSprite.sprite = supportBoxView.defaultBoxSprite;
        } else {
            carPartCmp.MaxHealth = 300;
            supportBoxView._supportBoxSprite.sprite = supportBoxView.rareBoxSprite;
        }
        _carPartInfoComponentsPool.Value.Get(supportBox._mainEntity).CurrentHealth = carPartCmp.MaxHealth;
       
        supportBox.transform.localScale = Vector3.one * 0.5f;
        supportBoxView._supportBoxSprite.color =  new Color(0,0,0,0.3f);

        supportBox.transform.DOScale(1, 2f).OnComplete(() => {
            supportBox.transform.localScale = Vector3.one * 3;
            supportBoxView._supportBoxSprite.color = Color.white;
            supportBox.transform.DOScale(1, 2f).OnComplete(() => {
                _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).SupportBoxPhase = 3;
                supportBox._carPartCollider.enabled = true;
            });
        });
        
        
       supportBox.transform.position = supportBoxView.supportBoxAirplane.position;
       
    }

    private void SetupSupportBox() {
        var supportBox = _sceneService.Value._supportBoxStuffView._supportBoxcarPartStuffView;
        int entitySupportBox = _world.Value.NewEntity();
        _carPartComponentsPool.Value.Add(entitySupportBox).MainEntity = -10;
        _carPartInfoComponentsPool.Value.Add(entitySupportBox);
        supportBox.SetupCarPart(Vector2Int.one * -10, "Obstacle" ,entitySupportBox,_netcodeSceneService.Value.OwnerClientId);
    }

    private void WinGame() {
        Time.timeScale = 0;
        var difficultyConfig = _betweenScenesService.Value.GetMapDifficultyConfig();
        int scrapCount = difficultyConfig.ScrapForRound * difficultyConfig.RoundsCount +
                         difficultyConfig.ScrapForMapComplete;
        DataPersistenceManagerSystem._gameData.ScrapCount += scrapCount;
        if (_betweenScenesService.Value.GetMapConfig().MapDifficltyConfigs.Length !=
            _betweenScenesService.Value.CurrentSelectedMapDifficlty + 1)
            DataPersistenceManagerSystem._gameData.LevelRounds[_betweenScenesService.Value.CurrentSelectedMap] =
                _betweenScenesService.Value.CurrentSelectedMapDifficlty + 1;
        DataPersistenceManagerSystem.SaveGame();
        CheckCoreAchievements();
        _sceneService.Value._endGameScreenView.SetWinScreenInfo(scrapCount);
    }

    private void CheckCoreAchievements() {
        var carCmp = _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        int upgradedCount = carCmp.CarLevel;
        float maxFuel = _sceneService.Value.GetCarConfig(carCmp.CarId).DefaultMaxFuel *
                        GridUtils.GetCarLevelMultipliers(carCmp.CarLevel).baseMaxFuelMultiplier;
        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPartContainer.Key);
            upgradedCount += carPartCmp.PartLevel;
        }

        foreach (var carPart in carCmp.CarPartGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPart.Key);
            upgradedCount += carPartCmp.PartLevel;

            var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartCmp.CarPartId);
            if (carPartConfig is SupportModuleCarPartConfig supportConfig) {
                if (supportConfig.ModuleType == SupportModuleType.Fuel)
                    maxFuel += supportConfig.FirstCount * GridUtils.GetSupportLevelMultipliers(carPartCmp.PartLevel, supportConfig.ModuleType)
                        .firstMultiplayer;
            }
        }

        foreach (var achievementConfig in _betweenScenesService.Value._carStuffListsConfig.AchievementConfigs) {
            if (DataPersistenceManagerSystem._gameData.AchievementsCompleted[achievementConfig.achievementId])
                switch (achievementConfig.achievementType) {
                    case AchievementType.ModulesOnOneCarPart:
                        if (achievementConfig.needCount <= GetMaxModulesOnCarPart())
                            _betweenScenesService.Value.CompletedAchievments.Add(achievementConfig.achievementId);
                        break;
                    case AchievementType.MaxFuelInCar:
                        if (achievementConfig.needCount <= maxFuel)
                            _betweenScenesService.Value.CompletedAchievments.Add(achievementConfig.achievementId);
                        break;
                    case AchievementType.WinWithoutUpgrades:
                        if (upgradedCount == 0)
                            _betweenScenesService.Value.CompletedAchievments.Add(achievementConfig.achievementId);
                        break;
                }
        }
    }

    private void SetMovementComponentValues(ulong playerId,Vector2 moveInput,bool isUseSpeedBoost) { 
    //   Debug.Log(playerId + "playerid move");
        int playerEntity = _serverValuesComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).ClientsEntities[playerId];
        int needEntity = _playerComponentsPool.Value.Get(playerEntity).PlayerEntity;
      
        
        if (_enemyAIComponentsPool.Value.Has(needEntity))
            _enemyAIComponentsPool.Value.Del(needEntity);
        ref var moveCmp = ref _movementComponentsPool.Value.Get(needEntity);
        ref var fuelCmp = ref _fuelComponentsPool.Value.Get(needEntity);
        
        moveCmp.TargetPosition = moveInput;
    Debug.Log($" mms {moveCmp.MaxMoveSpeed}, cmt {moveCmp.CantMoveTime}, {moveCmp.CurrentMoveSpeed}, id: {playerId}, isUseSpeedBoost {isUseSpeedBoost}");    
        if (isUseSpeedBoost && !fuelCmp.UseSpeedBoost && fuelCmp.CurrentFuel > 0)
            fuelCmp.UseSpeedBoost = true;

        if ((!isUseSpeedBoost || fuelCmp.CurrentFuel <= 0) && fuelCmp.UseSpeedBoost)
            fuelCmp.UseSpeedBoost = false;
    }
    private int GetMaxModulesOnCarPart() {
        var carCmp = _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        int maxModules = 0;
        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPartContainer.Key);
            int modulesOnCarPart = 0;
            foreach (var moduleId in carPartCmp.ModuleIds) {
                if (moduleId != -1)
                    modulesOnCarPart++;
            }
            maxModules = Mathf.Max(maxModules, modulesOnCarPart);
        }

        foreach (var carPart in carCmp.CarPartGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPart.Key);
            int modulesOnCarPart = 0;
            foreach (var moduleId in carPartCmp.ModuleIds) {
                if (moduleId != -1)
                    modulesOnCarPart++;
            }
            maxModules = Mathf.Max(maxModules, modulesOnCarPart);
        }

        return maxModules;
    }
    private void SetSettingsState() {
        bool isPause = !_sceneService.Value._settingsView._settingsContainer.gameObject.activeInHierarchy;
        _sceneService.Value._settingsView._settingsContainer.gameObject.SetActive(isPause);
        Time.timeScale = isPause ? 0 : _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).LocalTimeScale;
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
        settingsView._redColorSlider.value = needColor.r * settingsView._redColorSlider.maxValue;
        settingsView._greenColorSlider.value = needColor.g * settingsView._greenColorSlider.maxValue;
        settingsView._blueColorSlider.value = needColor.b * settingsView._blueColorSlider.maxValue;
    }

    private void SaveSettings() {
        var settingsView = _sceneService.Value._settingsView;

        var newColor = new Color(settingsView._redColorSlider.normalizedValue, settingsView._blueColorSlider.normalizedValue,
            settingsView._greenColorSlider.normalizedValue);
        _sceneService.Value._carStuffListsConfig.EnemyProjectilesMaterial.SetColor("_OffsetColor", newColor);
        DataPersistenceManagerSystem.SaveGame();
    }

    private void ChangePixelizedEffectsMode(bool changeMode) {
        var settingsView = _sceneService.Value._settingsView; 
       
        bool isPixelized = changeMode?!DataPersistenceManagerSystem._gameData.UsePixelizedEffects:DataPersistenceManagerSystem._gameData.UsePixelizedEffects;
        DataPersistenceManagerSystem._gameData.UsePixelizedEffects = isPixelized;
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
    private void ChangeHealthBarsMode(bool changeMode) {
        var settingsView = _sceneService.Value._settingsView;

        bool useDamageNumbers = changeMode?!DataPersistenceManagerSystem._gameData.UseHpBars:DataPersistenceManagerSystem._gameData.UseHpBars;
        DataPersistenceManagerSystem._gameData.UseHpBars = useDamageNumbers;
        settingsView._setHealthBarsChecker.gameObject.SetActive(useDamageNumbers);
    }
    private void ChangeDamageNumbersMode(bool changeMode) {
        var settingsView = _sceneService.Value._settingsView;

        bool useDamageNumbers = changeMode?!DataPersistenceManagerSystem._gameData.UseDamageNumbers:DataPersistenceManagerSystem._gameData.UseDamageNumbers;
        DataPersistenceManagerSystem._gameData.UseDamageNumbers = useDamageNumbers;
        settingsView._setDamageNumbersChecker.gameObject.SetActive(useDamageNumbers);
    }

    private void SetSettingCheckers() {
        ChangeDamageNumbersMode(false);
        ChangeHealthBarsMode(false);
        ChangePixelizedEffectsMode(false);
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
           
            EndRoundForAll();
        } else {
            _netcodeSceneService.Value.SetMapPointClientRpc(pointToPointRound.PointsToCheck[0]);
            _sceneService.Value._mapPointPrefab.transform.position = pointToPointRound.PointsToCheck[0];
            //smth particle effect
        }
    }

    private void SetSaleShopRound() {
        _sceneService.Value._shopUIView._updateItemsText.gameObject.SetActive(false);
        _updateShopToSaleRoundEventsPool.Value.Add(_sceneService.Value.PlayerEntity);
    }

    private void UpdateArrowDirection(Vector2 target, Vector2 playerPosition) {
        Vector3 directionToTarget = target - playerPosition;

        if (directionToTarget.magnitude > 0.1f) {
            float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;

            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            _sceneService.Value._playerUIView._arrowImageTransform.parent.rotation = Quaternion.Lerp(
                _sceneService.Value._playerUIView._arrowImageTransform.parent.rotation, targetRotation, 3 * Time.deltaTime);
        }

        Vector3 screenPos = _sceneService.Value.mainCamera.WorldToScreenPoint(playerPosition);
        _sceneService.Value._playerUIView._arrowImageTransform.parent.position =
            Vector2.Lerp(_sceneService.Value._playerUIView._arrowImageTransform.parent.position, screenPos, 4 * Time.deltaTime);
    }
    
    private int TaskCount() {
        return _taskComponentsFilter.Value.GetEntitiesCount();
    }

    private void SetMoneyText() {
        _sceneService.Value._shopUIView._moneyCountText.text =
            _playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MoneyCount + "$";
    }

    private void EndRoundForAll() {
        EndRound();
        if (_betweenScenesService.Value.IsCoop)
            _netcodeSceneService.Value.EndRoundClientRpc();
    }

    private void EndRound() {
        
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        switch (curGameCmp.CurrentRoundType) {
            case RoundType.ZoneDefence: 
                EndFilledZoneRound();
                break;
            case RoundType.FromAToB: 
                EndPointToPointRound();
                break;
        }
        curGameCmp.CurrentRoundType = RoundType.None;
        curGameCmp.SupportBoxPhase = 0;
        var supportBoxStuffView = _sceneService.Value._supportBoxStuffView;
        supportBoxStuffView._supportBoxcarPartStuffView.transform.DOKill();
        supportBoxStuffView.supportBoxAirplane.DOKill(true);
        supportBoxStuffView._supportBoxcarPartStuffView.gameObject.SetActive(false);
        curGameCmp.SupportBoxTargetPosition = Vector2.one * 999f;
        _sceneService.Value._playerUIView._mainTaskText.text = "";
        ChangePlayerUIState(false);
        var difficultyConfig = _betweenScenesService.Value.GetMapDifficultyConfig();
        if(curGameCmp.CurrentRound >= difficultyConfig.RoundsCount)return;
        int currentRound = curGameCmp.CurrentRound - 1;
        _sceneService.Value._mapUIView.RoundDifficltyContainer.gameObject.SetActive(false);
        int moneyCount = (int)(_betweenScenesService.Value.GetMapConfig().GameProcessConfig.GameRounds[currentRound].MoneyCount * curGameCmp.RoundMoneyMultiplayer);
        Debug.Log(curGameCmp.RoundMoneyMultiplayer + " curGameCmp.RoundMoneyMultiplayer"+currentRound);
        curGameCmp.RoundMoneyMultiplayer = 1;
        var carCmp = _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        int allPartCount = 0;
        int brokenPartCount = 0;
        foreach (var carPartInfo in carCmp.CarPartGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPartInfo.Key);
            allPartCount += carPartInfo.Value.Count;
            if (carPartCmp.CurrentHealth <= 0)
                brokenPartCount += carPartInfo.Value.Count;
            var supportModule = _sceneService.Value.GetSupportModuleFromCarPart(carPartCmp.CarPartId);
            if (supportModule && carPartCmp.CurrentHealth > 0 &&
                supportModule.ModuleType == SupportModuleType.Money) {
                moneyCount += (int)(supportModule.FirstCount * GridUtils.GetSupportLevelMultipliers(carPartCmp.PartLevel, supportModule.ModuleType).firstMultiplayer);
            }
                
        }

        foreach (var taskEntity in _taskComponentsFilter.Value) {
            ref var taskCmp = ref _taskComponentsPool.Value.Get(taskEntity);
            if (taskCmp.TaskType == TaskType.CarIntegrity && (float)brokenPartCount / allPartCount <= 0.1f) {
                taskCmp.CurrentCount++;
                _completeTaskEventsPool.Value.Add(taskEntity);
            }
        }

        DataPersistenceManagerSystem._gameData.MoneyEarnedInGame += moneyCount;
        _playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MoneyCount += moneyCount;
        SetMoneyText();
        _sceneService.Value._endGameScreenView.SetEndScreenInfo(moneyCount, currentRound);
        ChangeInventoryState(true);
        var checkedCarCmp =
            _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRedactedCarEntity);
        var upgradeCost =
            Mathf.FloorToInt(_sceneService.Value.GetCarConfig(checkedCarCmp.CarId).CarUpgradeCost * (1 + checkedCarCmp.CarLevel * 0.3f));
        if (upgradeCost <= _playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MoneyCount &&
            checkedCarCmp.CarLevel < currentRound / 2)
            _sceneService.Value._inventoryUIView.FadeAnimation(_sceneService.Value._inventoryUIView._highlightUpgradeImage, false); 
        _endRoundEventsPool.Value.Add(_sceneService.Value.PlayerEntity);
        if (_tornadoSpawnControllerComponentsPool.Value.Has(_sceneService.Value.PlayerEntity)) {
            _tornadoSpawnControllerComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).TimeToNextTornado = 0;
        }

        if (_trainControllerComponentsPool.Value.Has(_sceneService.Value.PlayerEntity)) {
            ref var trainControllerCmp = ref _trainControllerComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            trainControllerCmp.TimeToNextTrain = 10;   
            trainControllerCmp.TrainTransform.DOKill();
            trainControllerCmp.TrainTransform.gameObject.SetActive(false);
        }

        curGameCmp.EnemyTargetPosition = Vector2.one * 999; 
        _sceneService.Value._parachuteSpriteRenderer.DOKill();
        _sceneService.Value._enemyAirplane.DOKill();
        _sceneService.Value._enemyAirplane.gameObject.SetActive(false);
        _sceneService.Value._parachuteSpriteRenderer.gameObject.SetActive(false);

        if (_serverValuesComponentsPool.Value.Has(_sceneService.Value.PlayerEntity)) {
            ref var servarCmp = ref _serverValuesComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
            servarCmp.DeathPlayers = 0;
            servarCmp.ClientsEntities = new Dictionary<ulong, int>();
        }
            
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

    private void SetClientCarView(CarView carView) {
        Debug.Log(" car setupped");
        _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CarView = carView;
    }
    private void ChangeGameReadyState() {
        if (!CanStartRound()) return;
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        curGameCmp.IsReady = !curGameCmp.IsReady;
        NetcodeBetweenScenesService.Instance.SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId, curGameCmp.IsReady);
    }
    private void ChangeRoundInfo() {
       
        if (!CanStartRound()) return;
        if (_betweenScenesService.Value.IsCoop)
            NetcodeBetweenScenesService.Instance.SetPlayersToNotReadyServerRpc();
       // _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentDraggedCellEntity = -1;
        _sceneService.Value._mapUIView.gameObject.SetActive(true);
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
      
        var currentSupportRoundTypes = RoundUtilits.SupportRoundsTypes.Distinct().ToList();
        if (TaskCount() == 5)
            currentSupportRoundTypes.Remove(RoundType.Task);
        if (_playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MoneyCount < 5) 
            currentSupportRoundTypes.Remove(RoundType.VersusBattle);
        
        (var randomRounds, int moneyMultiplayerRound) =
            curGameCmp.CurrentRound >= _betweenScenesService.Value.GetMapDifficultyConfig().RoundsCount - 1
                ? (new[] { RoundType.BossBattle }, -1)
                : RoundUtilits.GetRandomBattleRound(curGameCmp.CurrentBattleRounds, 3, currentSupportRoundTypes);
      //  curGameCmp.RoundTypes.Add(randomRounds);
        curGameCmp.RoundButtons = new EventTrigger[randomRounds.Length];
        var mapImageRect = _sceneService.Value._mapUIView.MapImage.rectTransform;

        Transform moneyMultPosition = null;
        _sceneService.Value._mapUIView.RoundDifficltyContainer.localScale = Vector3.zero;
        float posY = (curGameCmp.CurrentRound + 1) * (mapImageRect.rect.height /
                                                      ((float)_betweenScenesService.Value.GetMapDifficultyConfig().RoundsCount +
                                                       2)) - mapImageRect.rect.height / 2;
        for (int i = 0; i < randomRounds.Length; i++) {
            var roundButton = _sceneService.Value.InstantiateRoundButton();
            int newIndex = i;
            roundButton.GetComponent<Image>().sprite = _sceneService.Value._mapUIView.RoundSprites[randomRounds[i]];

            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => { StartCurrentRoundTypeOnClick(randomRounds[newIndex], (PointerEventData)data); });

            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { ChangeRoundDescription(randomRounds[newIndex], (PointerEventData)data); });

            roundButton.triggers.Add(clickEntry);
            roundButton.triggers.Add(enterEntry);
            float posX = (newIndex + 1) * (mapImageRect.rect.width / ((float)randomRounds.Length + 1)) - mapImageRect.rect.width / 2;
           
            roundButton.transform.localPosition = new Vector2(posX, posY);
            curGameCmp.RoundButtons[i] = roundButton;

            if (moneyMultiplayerRound == i) {
                moneyMultPosition = roundButton.transform;
                float currentMultiplayer = Random.Range(12, 20) * 0.1f;
                _sceneService.Value._mapUIView.SetMoneyMultiplayerText(currentMultiplayer);
                curGameCmp.RoundMoneyMultiplayer = currentMultiplayer;
            }
        }

        _sceneService.Value._mapUIView.MapImage.transform.DOLocalMoveY(-posY, 0.5f)
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
        _sceneService.Value._mapUIView.RoundDescriptionText.text = GetRoundDescription(roundType);
    }

    private string GetRoundDescription(RoundType roundType) {
        switch (roundType) {
            case RoundType.None:
                return "";
            case RoundType.Task:
                return GetLocalization("RoundTask");
            case RoundType.BattleDefault:
                return GetLocalization("RoundBattleDefault");
            case RoundType.Chest:
                return GetLocalization("RoundChest");
            case RoundType.FromAToB:
                return GetLocalization("RoundFromAToB");
            case RoundType.Shop:
                return GetLocalization("RoundShop");
            case RoundType.ModulesShop:
                return GetLocalization("RoundModulesShop");
            case RoundType.Transfer:
                return GetLocalization("RoundTransfer");
            case RoundType.ZoneDefence:
                return GetLocalization("RoundZoneDefence");
            case RoundType.BossBattle:
                return GetLocalization("RoundBossBattle");
            case RoundType.TeammateForSupport:
                return GetLocalization("RoundTeammateForSupport");
            case RoundType.VersusBattle:
                return GetLocalization("RoundVersusBattle");
        }

        return "";
    }

    private void StartCurrentRoundType(RoundType roundType) {
        ref var shopCmp = ref _shopComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        _netcodeSceneService.Value.HideMessageClient();
        
        ChangePlayerUIState(RoundUtilits.IsBattleRound(roundType));
        
        if (shopCmp.IsModulesShop)
            _sceneService.Value._shopUIView._shopModeButton.gameObject.SetActive(true);
        _sceneService.Value._shopUIView._buttonUpdateShop.gameObject.SetActive(true);
        shopCmp.IsModulesShop = false;
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        var mapConfig = _betweenScenesService.Value.GetMapConfig();
        if (curGameCmp.CurrentRound > 0) {
            if (mapConfig.GameProcessConfig.GameRounds[curGameCmp.CurrentRound].AddedCarPartsToEnemyPool != null)
                foreach (var addedCarPartId in mapConfig.GameProcessConfig.GameRounds[curGameCmp.CurrentRound].AddedCarPartsToEnemyPool) {
                    curGameCmp.CarPartsToEnemyBuilding[_sceneService.Value.GetCarPartConfig(addedCarPartId).PartType].Add(addedCarPartId);
                }

            if (mapConfig.GameProcessConfig.GameRounds[curGameCmp.CurrentRound].RemovedCarPartsFromEnemyPool != null)
                foreach (var removedCarPartId in mapConfig.GameProcessConfig.GameRounds[curGameCmp.CurrentRound].RemovedCarPartsFromEnemyPool) {
                    curGameCmp.CarPartsToEnemyBuilding[_sceneService.Value.GetCarPartConfig(removedCarPartId).PartType]
                        .Remove(removedCarPartId);
                }
        }

        var difficultyConfig = _betweenScenesService.Value.GetMapDifficultyConfig();
        if (difficultyConfig.ShopInflationPerRound != 0)
            _sceneService.Value._shopUIView._inflationText.text =
                (difficultyConfig.ShopInflationPerRound * (curGameCmp.CurrentRound + 1) * 100) + "%";

        if (NetcodeBetweenScenesService.Instance.IsHost)
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
                SpawnBossCars();
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
                int rareIndex = GridUtils.GetRandomIndexFromChancesList(shopCmp.CurrentShopItemsChances);
                _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentGivenItems.Add(
                    shopCmp.ListShopItemsForRares[rareIndex][Random.Range(0, shopCmp.ListShopItemsForRares[rareIndex].Count)]);
                ChangeInventoryState(true);
                break;
            case RoundType.Shop:
                SetSaleShopRound();
                ChangeInventoryState(true);
                break;
            case RoundType.ModulesShop:
                shopCmp.IsModulesShop = true;
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
            case RoundType.VersusBattle:
                SetVersusBattleUI();
                break;
        }

        curGameCmp.RoundButtons = null;
        bool isBattleRound = RoundUtilits.IsBattleRound(roundType);
        curGameCmp.CurrentBattleRounds += isBattleRound ? -1 : 1;
        if (curGameCmp.CurrentBattleRounds < 1)
            curGameCmp.CurrentBattleRounds = 1;
        if (isBattleRound)
            _sceneService.Value._playerUIView._mainTaskText.text = GetRoundDescription(roundType);
        curGameCmp.CurrentRoundType = roundType;
        //curGameCmp.RoundWithMoneyMultiplayer = -1;
        _sceneService.Value._mapUIView.gameObject.SetActive(false);
        _sceneService.Value._mapUIView.PathElementsImage[curGameCmp.CurrentRound].sprite =
            _sceneService.Value._mapUIView.RoundSprites[roundType];
        curGameCmp.CurrentRound++;

        if (difficultyConfig.AutoUpgradeShopToRoundLevel && shopCmp.ShopLevel < curGameCmp.CurrentRound)
            UpgradeShop();

        _startRoundEventsPool.Value.Add(_sceneService.Value.PlayerEntity);
    }

    private void StartCurrentRoundTypeOnClick(RoundType roundType, PointerEventData data) {
        if (!_betweenScenesService.Value.IsCoop)
            StartCurrentRoundType(roundType);
        else
            NetcodeBetweenScenesService.Instance.StartRoundClientRpc(roundType);
    }

    private void SetVersusBattleUI() {
        var versusBattleView = _sceneService.Value._versusBattleView;
        versusBattleView.gameObject.SetActive(true);
        versusBattleView._versusMapContainer.gameObject.SetActive(true);
        _sceneService.Value._mapContainer.gameObject.SetActive(false);
        versusBattleView._betContainer.gameObject.SetActive(true);
        versusBattleView._speedGameSpeedBoostText.text = ">";

        versusBattleView._rightVersusBattleParticipantInfo.NameText.text =
            $"{GetLocalization(VersusBattlesNames[Random.Range(0, VersusBattlesNames.Length)])} {GetLocalization(VersusBattlesSurnames[Random.Range(0, VersusBattlesSurnames.Length)])}";
        versusBattleView._leftVersusBattleParticipantInfo.NameText.text =
            $"{GetLocalization(VersusBattlesNames[Random.Range(0, VersusBattlesNames.Length)])} {GetLocalization(VersusBattlesSurnames[Random.Range(0, VersusBattlesSurnames.Length)])}";
        
        versusBattleView._circularSawLeft.transform.position = new Vector3(-50, 0, 0);
        versusBattleView._circularSawRight.transform.position = new Vector3(50, 0, 0);
        versusBattleView._timerText.text = "0";
        SetCarsToVersusBattle();
        SetCarToVersusGrid();
        _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).LocalTimeScale = 0;
        Time.timeScale = 0;
        _sceneService.Value.mainCamera.transform.position = new Vector3(0,0,-10);
        ref var versusBattleCmp = ref _versusBattleComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        versusBattleCmp.IsCircularSawsSended = false;
        versusBattleCmp.CurrentBet = 0;
        ChangeBet(5);
    }

    private void UpgradeShop() {
        ref var shopCmp = ref _shopComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

        var shopView = _sceneService.Value._shopUIView;
        var shopUpgradeCost = 5 + shopCmp.ShopLevel * 5;

        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        if (shopUpgradeCost > playerCurrencyCmp.MoneyCount) return;
        SetMoneyCount(-shopUpgradeCost);
        shopCmp.ShopLevel++;
        if (shopCmp.ShopLevel == 19) {
            shopView._upgradeShopButton.enabled = false;
            shopView._upgradeShopButtonText.text = GetLocalization("Max level");
        } else
            shopView._upgradeShopButtonText.text = $"{GetLocalization("Upgrade shop")} {5 + shopUpgradeCost}$";

        shopCmp.CurrentShopItemsChances = _sceneService.Value._carStuffListsConfig.RoundShopChances[shopCmp.ShopLevel].Chances;
        float sumChances = shopCmp.CurrentShopItemsChances.Sum() / 100f;
        shopView._shopChancesText.text =
            $"<color=white>{(shopCmp.CurrentShopItemsChances[0] / sumChances):F1}% {GetLocalization("Low")}</color>" +
            $"\n<color=#00BFFF>{(shopCmp.CurrentShopItemsChances[1] / sumChances):F1}% {GetLocalization("Medium")}</color>" +
            $"\n<color=#802880>{(shopCmp.CurrentShopItemsChances[2] / sumChances):F1}% {GetLocalization("High")}</color>" +
            $"\n<color=#FFA500>{(shopCmp.CurrentShopItemsChances[3] / sumChances):F1}% {GetLocalization("VeryHigh")}</color>";
    }
    private void SetVisibleMoneyCount(float needCount) {
        _playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).VisibleMoneyCount = needCount;
    }

    private void SetMoneyCount(float count) {
        if(count == 0)return;
        ref var inventoryCmp = ref _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        ref var playerCurrency = ref _playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        playerCurrency.VisibleMoneyCount = playerCurrency.MoneyCount;
        playerCurrency.MoneyCount += count;

        inventoryCmp.CurrentMoneyTween?.Kill();

        float visibleMoney = playerCurrency.VisibleMoneyCount;
        inventoryCmp.CurrentMoneyTween = DOTween.To(() => visibleMoney, x => visibleMoney = x, playerCurrency.MoneyCount, 1f)
            .SetEase(Ease.OutCubic).OnUpdate(() => {
                _sceneService.Value._shopUIView._moneyCountText.text = visibleMoney.ToString("N0") + "$";
                SetVisibleMoneyCount(visibleMoney);
            });

        _sceneService.Value._shopUIView.AddMoneyAnimation();
    }
    private void StartVersusBattle(bool isLeft) {
        var versusBattleView = _sceneService.Value._versusBattleView;
        versusBattleView._betContainer.gameObject.SetActive(false);
       
        _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).LocalTimeScale = 1;
       // _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).RoundTime = 0;
        Time.timeScale = 1;
        _versusBattleComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).BetOnLeft = isLeft;
    }

    private void EndVersusBattle() {
        ref var versusBattleCmp = ref _versusBattleComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
       
        bool leftIsDead = versusBattleCmp.LeftOpponentEntity == 0;
        bool rightIsDead = versusBattleCmp.RightOpponentEntity == 0;
        var versusBattleView = _sceneService.Value._versusBattleView;
        versusBattleView._circularSawLeft.DOKill();
        versusBattleView._circularSawRight.DOKill();
       
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

        versusBattleCmp.IsBattleEnded = true;

        curGameCmp.LocalTimeScale = 0;
       Time.timeScale = curGameCmp.LocalTimeScale;

        if (leftIsDead && rightIsDead) {
            versusBattleView._timerText.text =  GetLocalization("Draw");
            return;
        }

        string needBetText;
        needBetText = versusBattleCmp.BetOnLeft
            ? versusBattleView._leftVersusBattleParticipantInfo.NameText.text
            : versusBattleView._rightVersusBattleParticipantInfo.NameText.text;

        if ((versusBattleCmp.BetOnLeft && leftIsDead) || (!versusBattleCmp.BetOnLeft && rightIsDead)) {
            versusBattleCmp.CurrentBet *= -1;

            needBetText += $" {GetLocalization("lose")}!\n";
        } else {
            DataPersistenceManagerSystem._gameData.MoneyEarnedInGame += versusBattleCmp.CurrentBet;
            needBetText += $" {GetLocalization("win")}!\n";
        }

        needBetText += versusBattleCmp.CurrentBet > 0 ? "+" : "";
        needBetText += $"{versusBattleCmp.CurrentBet}$\n {GetLocalization("Click to continue")}";

        playerCurrencyCmp.MoneyCount += versusBattleCmp.CurrentBet;
        versusBattleView._timerText.text = needBetText;
        SetMoneyText();
    }

    private string[] VersusBattlesNames = new[] {
        "Buck", "Kyle", "Jack", "Travis", "Duke", "Billy", "Jake", "Clint", "Jedd", "Darryl", "Cody", "Shane", "Boyd", "Floyd", "Garrett",
        "Lou", "Milo", "Otis", "Roscoe", "Jed", "Lyle", "Wade", "Curtis", "Russ", "Dallas", "Elvis", "Merle", "Coyote", "Jared", "Lance",
        "Cletus", "Bo", "Waylon", "Hank", "Jethro", "Beau"
    };

    private string[] VersusBattlesSurnames = new[] {
        "Viper", "Coyote", "Buzzard", "Mole", "Hound", "Scorpion", "Jackal", "Badger", "Vulture", "Roach",
       
        "Red", "Baldy", "Scar", "Beardo", "Chick", "Slick", "Deadeye", "Tooth", "Fang", "Crutch", "Stitches",
       
        "Sledge", "Dynamite", "Gas-Wrench", "Poker", "Chainsaw", "Solder", "Hammer", "Wrench", "Torch", "Greaser",
       
        "Preacher", "Doc", "Liar", "Quiet", "Grouch", "Sneak", "Hustler", "Joker", "Lucky", "Boomer", "Stranger", "Rook",
        
        "Rusty", "Diesel", "Exhaust", "Propane", "Spark", "Tar", "Dust", "Fume", "Scale", "Radiator",
       
        "Crusher", "Claw", "Grip", "Nightmare", "Ghost", "Raven", "Spider", "Hawk", "Thunder", "Reaper"
    };
    private void CompleteVersusBattle() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

        var versusBattleView = _sceneService.Value._versusBattleView;

        versusBattleView.gameObject.SetActive(false);
        ChangeInventoryState(true);
        versusBattleView._versusMapContainer.gameObject.SetActive(false);
        _sceneService.Value._mapContainer.gameObject.SetActive(true);

        curGameCmp.CurrentRoundType = RoundType.None;
        _endRoundEventsPool.Value.Add(_sceneService.Value.PlayerEntity);
        if (_tornadoSpawnControllerComponentsPool.Value.Has(_sceneService.Value.PlayerEntity))
            _tornadoSpawnControllerComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).TimeToNextTornado = 0;

        ref var versusBattleCmp = ref _versusBattleComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        ClearHealthUI(versusBattleCmp.LeftCarPartInfoUIViewsUpLevelGrid, versusBattleCmp.LeftCarPartInfoUIViewsGrid);
        ClearHealthUI(versusBattleCmp.RightCarPartInfoUIViewsUpLevelGrid, versusBattleCmp.RightCarPartInfoUIViewsGrid);
        versusBattleCmp.IsBattleEnded = false;
        curGameCmp.LocalTimeScale = 1;
        Time.timeScale = curGameCmp.LocalTimeScale;
    }

    private void SetupClientCar(CarPartDataForRpc[] upParts, CarPartDataForRpc[] downParts, int carLevel, ulong userId, bool isPlayer,
        int carId, Vector2 carSize) {
        var enemyCarMap = BuildCarMap(upParts, downParts, carId, carLevel, carSize);

        int carEntity = SpawnNpcCar(_currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity), enemyCarMap, false,
            GetRandomPointAround(Vector2.zero, 10));

        if (isPlayer) {
            int playerCmpEntity = _world.Value.NewEntity();
            ref var playerCmp = ref _playerComponentsPool.Value.Add(playerCmpEntity);
            playerCmp.PlayerEntity = carEntity;
            ref var playerTagCmp = ref _playerTagComponentsPool.Value.Add(carEntity);
                playerTagCmp.ChangedCarPartsHp = new Dictionary<int, float>();
            //    playerTagCmp.ClientId = userId;

          //  Debug.Log(carEntity + " setup pl cmp 1");
            playerCmp.TeammatesEntities = new List<int>();
            _serverValuesComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).ClientsEntities.TryAdd(userId, playerCmpEntity);

            _netcodeSceneService.Value.SetupCarViewToClientRpc(
                _carComponentsPool.Value.Get(carEntity).CarView.transform.parent.gameObject.GetComponent<NetworkObject>(), userId);

            _updateCarStatsEventsPool.Value.Add(carEntity);
        } else {
            int playerCmpEntity = _serverValuesComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).ClientsEntities[userId];
            _playerComponentsPool.Value.Get(playerCmpEntity).TeammatesEntities.Add(carEntity);
        }
    }
    private void ClearHealthUI(CarPartInGameUIView[,] upHealthUI,  CarPartInGameUIView[,] downHealthUI) {
        
        for (int i = 0; i < downHealthUI.GetLength(0); i++) {
            for (int j = 0; j < downHealthUI.GetLength(1); j++) {
                if (downHealthUI[i, j]) {
                    _sceneService.Value.ReleaseCarPartInGameUIView(downHealthUI[i, j]);
                    downHealthUI[i, j] = null;
                }
            }
        }
          
        for (int i = 0; i < upHealthUI.GetLength(0); i++) {
            for (int j = 0; j < upHealthUI.GetLength(1); j++) {
                if (upHealthUI[i, j]) {
                    _sceneService.Value.ReleaseCarPartInGameUIView(upHealthUI[i, j]);
                    upHealthUI[i, j] = null;
                }
            }
        }
    }

    private void SetCarsToVersusBattle() {
        var curGameCmp = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        ref var versusBattleCmp = ref _versusBattleComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

        versusBattleCmp.RightOpponentEntity = SpawnNpcCar(curGameCmp, GetRandomCarMap(), true, new Vector2(20, 0));
        versusBattleCmp.LeftOpponentEntity = SpawnNpcCar(curGameCmp, GetRandomCarMap(), false, new Vector2(-20, 0));
    }

    private void SetCarToVersusGrid() {
        ref var versusBattleCmp = ref _versusBattleComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        
        ref var leftCarCmp = ref _carComponentsPool.Value.Get(versusBattleCmp.LeftOpponentEntity);
        ref var rightCarCmp = ref _carComponentsPool.Value.Get(versusBattleCmp.RightOpponentEntity);
        
        var versusBattleView = _sceneService.Value._versusBattleView;
        RectTransform parentRect = versusBattleView._leftVersusBattleParticipantInfo.CarPratsDownLevelHealthContainer;
        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;
        
        int columnsLeft = leftCarCmp.CarPartContainerGroupsGrid.GetLength(1);
        int rowsLeft = leftCarCmp.CarPartContainerGroupsGrid.GetLength(0);
        int columnsRight = rightCarCmp.CarPartContainerGroupsGrid.GetLength(1);
        int rowsRight = rightCarCmp.CarPartContainerGroupsGrid.GetLength(0);
        
        versusBattleCmp.LeftCarPartInfoUIViewsGrid = new CarPartInGameUIView[rowsLeft, columnsLeft];
        versusBattleCmp.LeftCarPartInfoUIViewsUpLevelGrid = new CarPartInGameUIView[rowsLeft, columnsLeft];
        versusBattleCmp.RightCarPartInfoUIViewsGrid = new CarPartInGameUIView[rowsRight, columnsRight];
        versusBattleCmp.RightCarPartInfoUIViewsUpLevelGrid = new CarPartInGameUIView[rowsRight, columnsRight];
        // Вычисляем размер каждого квадрата
        float cellSizeLeft = Mathf.Min(parentHeight / columnsLeft,  parentWidth/ rowsLeft);
        float cellSizeRight = Mathf.Min(parentHeight / columnsRight,  parentWidth/ rowsRight);

        float gridWidthLeft = rowsLeft * cellSizeLeft;
        float gridHeightLeft = columnsLeft * cellSizeLeft;
        float gridWidthRight = rowsRight * cellSizeRight;
        float gridHeightRight = columnsRight * cellSizeRight;

        // Начальная позиция (левый верхний угол)
        float startXLeft = (cellSizeLeft/2 - gridWidthLeft/ 2) ;
        float startYLeft = (cellSizeLeft/2 -gridHeightLeft/ 2) ;
        float startXRight = (cellSizeRight/2 - gridWidthRight/ 2) ;
        float startYRight = (cellSizeRight/2 -gridHeightRight/ 2) ;
        
        SetCarPartGroupHealthUI(leftCarCmp.CarPartContainerGroupsList, leftCarCmp.CarPartContainerGroupsGrid, startXLeft, cellSizeLeft, startYLeft, parentRect,
            ref versusBattleCmp.LeftCarPartInfoUIViewsGrid);

        SetCarPartGroupHealthUI(leftCarCmp.CarPartGroupsList, leftCarCmp.CarPartGroupsGrid, startXLeft, cellSizeLeft, startYLeft,
            versusBattleView._leftVersusBattleParticipantInfo.CarPratsUpLevelHealthContainer,
            ref versusBattleCmp.LeftCarPartInfoUIViewsUpLevelGrid);

        SetCarPartGroupHealthUI(rightCarCmp.CarPartContainerGroupsList, rightCarCmp.CarPartContainerGroupsGrid, startXRight, cellSizeLeft, startYRight,
            versusBattleView._rightVersusBattleParticipantInfo.CarPratsDownLevelHealthContainer,
            ref versusBattleCmp.RightCarPartInfoUIViewsGrid);

        SetCarPartGroupHealthUI(rightCarCmp.CarPartGroupsList, rightCarCmp.CarPartGroupsGrid, startXRight, cellSizeLeft, startYRight,
            versusBattleView._rightVersusBattleParticipantInfo.CarPratsUpLevelHealthContainer,
            ref versusBattleCmp.RightCarPartInfoUIViewsUpLevelGrid);
    }

    private void ChangeBet(int count) {
        ref var versusBattleComponent = ref _versusBattleComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        var playerCurrencyCmp = _playerCurrencyComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
      
        versusBattleComponent.CurrentBet += count;

        versusBattleComponent.CurrentBet = Mathf.Clamp(versusBattleComponent.CurrentBet, 5, (int)playerCurrencyCmp.MoneyCount);
        _sceneService.Value._versusBattleView._betText.text = $" {GetLocalization("Current money")}: {playerCurrencyCmp.MoneyCount}$ \n" +
                                                              $"{GetLocalization("Bet")}: {versusBattleComponent.CurrentBet}$";       
    }

    private void SetupVersusBattleUI() {
        var versusBattleView = _sceneService.Value._versusBattleView;
        
        versusBattleView._bet1Button.onClick.AddListener(()=>ChangeBet(1));
        versusBattleView._bet10Button.onClick.AddListener(()=>ChangeBet(10));
        versusBattleView._bet100Button.onClick.AddListener(()=>ChangeBet(100));
        
        versusBattleView._betRemove1Button.onClick.AddListener(()=>ChangeBet(-1));
        versusBattleView._betRemove10Button.onClick.AddListener(()=>ChangeBet(-10));
        versusBattleView._betRemove100Button.onClick.AddListener(()=>ChangeBet(-100));
        
        versusBattleView._leftVersusBattleParticipantInfo.BetButton.onClick.AddListener(()=>StartVersusBattle(true));
        versusBattleView._rightVersusBattleParticipantInfo.BetButton.onClick.AddListener(()=>StartVersusBattle(false));
        
        versusBattleView._speedGameSpeedBoostButton.onClick.AddListener(ChangeGameSpeedVersusBattles);
    }

    private void ChangeGameSpeedVersusBattles() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        if (curGameCmp.LocalTimeScale != 0) {
            var versusBattleView = _sceneService.Value._versusBattleView;
            if (curGameCmp.LocalTimeScale == 1) {
                curGameCmp.LocalTimeScale = 2;
                versusBattleView._speedGameSpeedBoostText.text = ">>";
            } else if (curGameCmp.LocalTimeScale == 2) {
                curGameCmp.LocalTimeScale = 3;
                versusBattleView._speedGameSpeedBoostText.text = ">>>";
            } else {
                curGameCmp.LocalTimeScale = 1;
                versusBattleView._speedGameSpeedBoostText.text = ">";
            }

            Time.timeScale = curGameCmp.LocalTimeScale;
        }
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
        var fillAmount = filledZoneCmp.CurrentZoneTime / filledZoneCmp.NeedZoneTime;
        _sceneService.Value._mapFilledZoneView.FilledZoneUI.fillAmount = fillAmount;
        _sceneService.Value._mapFilledZoneView.FilledZoneUI.color = _sceneService.Value._healthGradient.Evaluate(fillAmount);
        if (filledZoneCmp.CurrentZoneTime >= filledZoneCmp.NeedZoneTime) {
         
            EndRoundForAll();
            _filledZoneComponentsPool.Value.Del(_sceneService.Value.PlayerEntity);
            
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
        if(!NetworkManager.Singleton.IsServer)return;
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        
        _sceneService.Value._mapFilledZoneView.FilledZoneUI.gameObject.SetActive(true);
        _sceneService.Value._mapFilledZoneView.gameObject.SetActive(true);
       _filledZoneComponentsPool.Value.Add(_sceneService.Value.PlayerEntity).NeedZoneTime = 10+curGameCmp.CurrentRound*3;
       var zonePosition = GetRandomPointAround(Vector2.zero, 10);
      _netcodeSceneService.Value.SetZonePositionClientRpc(zonePosition);
        _sceneService.Value._mapFilledZoneView.ChangeZonePosition(zonePosition);
    }

    private void SetFilledZonePositionToClient(Vector2 zonePosition) {
        _sceneService.Value._mapFilledZoneView.FilledZoneUI.gameObject.SetActive(true);
        _sceneService.Value._mapFilledZoneView.gameObject.SetActive(true);
        _sceneService.Value._mapFilledZoneView.ChangeZonePosition(zonePosition);
    }

    private void SetFilledZoneFillAmountToClient(float fillAmount) {
        _sceneService.Value._mapFilledZoneView.FilledZoneUI.DOKill();
        _sceneService.Value._mapFilledZoneView.FilledZoneUI.DOColor(_sceneService.Value._healthGradient.Evaluate(fillAmount), 0.3f);
      //  _sceneService.Value._mapFilledZoneView.FilledZoneUI.color = _sceneService.Value._healthGradient.Evaluate(fillAmount);
        _sceneService.Value._mapFilledZoneView.FilledZoneUI.DOFillAmount(fillAmount, 0.3f);
    }

    private void SetNewPointToPointRound() {
        ref var curGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        curGameCmp.CurrentRoundType = RoundType.FromAToB;

        ref var pointToPointRoundCmp = ref _fromPointToPointRoundComponentsPool.Value.Add(_sceneService.Value.PlayerEntity);
        pointToPointRoundCmp.PointsToCheck = new List<Vector2>();
        int pointsCount = 2 + curGameCmp.CurrentRound / 2;
        Vector2 posToSpawn = Vector2.zero;
        for (int i = 0; i < pointsCount; i++) {
            posToSpawn = GetRandomPointAround(posToSpawn, 15+ (float)curGameCmp.CurrentRound / 2);
            pointToPointRoundCmp.PointsToCheck.Add(posToSpawn);
        }

        var carPartsGrid = _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CarPartContainerGroupsGrid;
        _sceneService.Value._playerUIView._arrowImageTransform.parent.transform.position =
            _sceneService.Value.mainCamera.WorldToScreenPoint(Vector3.zero);
        float arrowPos = Mathf.Max(carPartsGrid.GetLength(0), carPartsGrid.GetLength(1)) * 20;

        _sceneService.Value._playerUIView._arrowImageTransform.localPosition = new Vector3(arrowPos, 0, 0);

        _sceneService.Value._mapPointPrefab.transform.position = pointToPointRoundCmp.PointsToCheck[0];
        _netcodeSceneService.Value.SetMapPointClientRpc(pointToPointRoundCmp.PointsToCheck[0]);
        _sceneService.Value._mapPointPrefab.gameObject.SetActive(true);
        _sceneService.Value._playerUIView._arrowImageTransform.gameObject.SetActive(true);
    }

    private void SetMapPointForClients(Vector2 mapPointPosition) {
        _sceneService.Value._mapPointPrefab.transform.position = mapPointPosition;
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
                return targetPoint;
        
            attempts++;
        }
    
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
       // PolygonCollider2D collider = carCmp.CarView.CarBaseCollider;
        
        var grid = carCmp.CarPartContainerGroupsGrid;
      
        List<Vector2> contour = GridUtils.FindBorderContour(grid);
        carCmp.CarView.SyncRigidbodyView.ChangePolygonServerRpc(contour.ToArray());
        //collider.SetPath(0, contour.ToArray());
    }

    private void SpawnTeammateCar() {
        var curGameCmp = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        
        SpawnNpcCar(curGameCmp, GetRandomCarMap(), false,GetPositionAroundPlayer());
    }

    private EnemyCarPartsMaps GetRandomCarMap() {
        var curGameCmp = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        int roundNumber = curGameCmp.CurrentRound;

        var mapConfig = _betweenScenesService.Value.GetMapConfig();
        var carId = mapConfig.GameProcessConfig.EnemyCarIndexes[Random.Range(0, mapConfig.GameProcessConfig.EnemyCarIndexes.Length)];
        EnemyCarPartsMaps carMap = ScriptableObject.CreateInstance<EnemyCarPartsMaps>();
        var carBaseList = _sceneService.Value._carStuffListsConfig.CarBasesForCars[carId].CarPartBasesForLevel;
        List<CarBaseMapConfig> carBases;
        if (!carBaseList.TryGetValue(roundNumber, out carBases)) {
            for (int i = roundNumber-1; i >= 0; i--) {
                if (carBaseList.TryGetValue(i, out carBases))
                    break;
            }
        }

        var carBase = carBases[Random.Range(0, carBases.Count)];

        carMap.CarPartDownLevelGrid = carBase.CarPartDownLevelGrid;
        carMap.CarId = carId;
        carMap.CarLevel = roundNumber == 0 ? 0 : roundNumber / 2;
        carMap.GridSize = carBase.GridSize;
        carMap.CarPartUpLevelGrid =
            GridUtils.GetPlacedCarPatsOnCarBase(carBase, carId, curGameCmp.CarPartsToEnemyBuilding, 40 + roundNumber * 20).ToArray();

        for (int i = 0; i < carMap.CarPartUpLevelGrid.Length; i++) {
            var carPart = carMap.CarPartUpLevelGrid[i];
            var carPartConfig = _sceneService.Value.GetCarPartConfig(carPart.partId);
            if (carPartConfig.PartType != CarPartType.Main)
                carMap.CarPartUpLevelGrid[i].level = Mathf.Min((roundNumber > 0 ? 0 : roundNumber / 2),
                    GridUtils._maxLevelsForRare[carPartConfig.RartRare]);
        }

        return carMap;
    }

    private EnemyCarPartsMaps BuildCarMap(CarPartDataForRpc[] upParts, CarPartDataForRpc[] downParts, int carId, int carLevel, Vector2 carSize) {
       
        EnemyCarPartsMaps carMap = ScriptableObject.CreateInstance<EnemyCarPartsMaps>();

        var downGrid = new List<CarPartTypeRow>();
        foreach (var carPart in downParts) 
            downGrid.Add(new CarPartTypeRow(carPart.EdgePosition, carPart.Id, carPart.RotateAngle, carPart.Level));
        
        var upGrid = new List<CarPartTypeRow>();
        foreach (var carPart in upParts) 
            upGrid.Add(new CarPartTypeRow(carPart.EdgePosition, carPart.Id, carPart.RotateAngle, carPart.Level));
        
        carMap.CarPartDownLevelGrid = downGrid.ToArray();
        carMap.CarPartUpLevelGrid = upGrid.ToArray();
        carMap.CarId = carId;
        carMap.CarLevel = carLevel;
        carMap.GridSize = new Vector2Int((int)carSize.x, (int)carSize.y);
       
        for (int i = 0; i < downParts.Length; i++) {
            var carPart = downParts[i];
            carMap.CarPartDownLevelGrid[i].level = carPart.Level;
        }
        
        for (int i = 0; i < upParts.Length; i++) {
            var carPart = upParts[i];
            carMap.CarPartUpLevelGrid[i].level = carPart.Level;
        }

        return carMap;
    }
    private void SetEnemiesCars(int enemiesCount, bool onStartRoundSpawn = true) {
        if(_betweenScenesService.Value.IsCoop && !NetcodeBetweenScenesService.Instance.IsHost)return;
        var curGameCmp = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

        for (int i = 0; i < enemiesCount; i++)
            SpawnNpcCar(curGameCmp, GetRandomCarMap(), true,
                onStartRoundSpawn ? GetPositionAroundPlayer() : curGameCmp.EnemyTargetPosition);
    }

    private void SpawnBossCars() {
        var bossCars = _betweenScenesService.Value.GetMapDifficultyConfig().BossCars;
        var curGameCmp = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        foreach (var enemyCarPartsMap in bossCars) 
              _bossComponentsPool.Value.Add(SpawnNpcCar(curGameCmp, enemyCarPartsMap, true,GetPositionAroundPlayer()));
        
    }

    private void SpawnTeammateCar(int entity) {
        ref var fuelCmp = ref _fuelComponentsPool.Value.Get(entity);
        SetComponentsValuesToDefault(entity);
        ref var carCmp = ref _carComponentsPool.Value.Get(entity);
      
        var carConfig = _sceneService.Value.GetCarConfig(carCmp.CarId);
        fuelCmp.AddedFuelConsumption = carConfig.AccelerationFuelСonsumption;
        carCmp.CarPartViewsGrid =
            new CarPartStuffView[carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1)];

        SetupEnemyMovementComponent(entity, _sceneService.Value.GetCarConfig(carCmp.CarId));

        _netcodeSceneService.Value.InstantiateCarViewServerRpc(entity, true);
    }
    private int SpawnNpcCar(CurrentGameComponent curGameCmp, /*List<Transform> ,*/ EnemyCarPartsMaps carMap, bool isEnemy, Vector2 spawnPosition) {
        int enemyEntity = _world.Value.NewEntity();

        _movementComponentsPool.Value.Add(enemyEntity);
        _fuelComponentsPool.Value.Add(enemyEntity);
           
        ref var carCmp = ref _carComponentsPool.Value.Add(enemyEntity);
        carCmp.CarId = carMap.CarId;
        
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
                Debug.Log(carPartCellEntity + " car part cell added");
            foreach (var cell in placedCells) {
                carCmp.CarPartContainerGroupsGrid[cell.x, cell.y] = carPartCellEntity;
                carCmp.CarPartContainerGroupsList[carPartCellEntity].Add(cell);
            }

            ref var carPartCmp = ref _carPartComponentsPool.Value.Add(carPartCellEntity);
            ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Add(carPartCellEntity);
            carPartCmp.MainEntity = enemyEntity;
            carPartInfoCmp.CarPartId = carContainer.partId;
            carPartCmp.RotateDegrees = carContainer.rotate;
          //  SpawnCarPartCellView(carPartCellEntity, carCmp.CarView, !isEnemy, carContainer.rotate);

            if (curGameCmp.CurrentRoundType != RoundType.VersusBattle && isEnemy) {
                carPartCmp.MaxHealth *= curGameCmp.DifficultyHealthMultiplayer;
                            carPartInfoCmp.CurrentHealth = carPartCmp.MaxHealth;
            }
            
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
            ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Add(carPartCellEntity);
            carPartCmp.MainEntity = enemyEntity;
            carPartInfoCmp.CarPartId = carContainer.partId;
            carPartCmp.RotateDegrees = carContainer.rotate;
            if (curGameCmp.CurrentRoundType != RoundType.VersusBattle && isEnemy) {
                carPartCmp.MaxHealth *= curGameCmp.DifficultyHealthMultiplayer;
                carPartInfoCmp.CurrentHealth = carPartCmp.MaxHealth;
            }
            
        //    SpawnCarPartCellView(carPartCellEntity, carCmp.CarView, !isEnemy, carContainer.rotate);
        }
        _enemyAIComponentsPool.Value.Add(enemyEntity).IsEnemy = true;
     
_netcodeSceneService.Value.InstantiateCarViewServerRpc(enemyEntity, !isEnemy);
        return enemyEntity;
    }

    private void SetReloadBoostSupportTypes(int carEntity) {
        ref var carCmp = ref _carComponentsPool.Value.Get(carEntity);

        foreach (var carPartInfo in carCmp.CarPartGroupsList) {
            int carPartId = _carPartInfoComponentsPool.Value.Get(carPartInfo.Key).CarPartId;
            if (_sceneService.Value.GetCarPartConfig(carPartId).PartType == CarPartType.Support) {
                var supportConfig = _sceneService.Value.GetSupportModuleFromCarPart(carPartId);
                if (supportConfig.ModuleType != SupportModuleType.Ammo) continue;
                var neighbors = GridUtils.GetNeighbors(carPartInfo.Value, carCmp.CarPartGroupsGrid);
                foreach (var neighbor in neighbors) {
                    var neighborCarPartInfoCmp = _carPartInfoComponentsPool.Value.Get(neighbor);
                    int neighborCarPartId = neighborCarPartInfoCmp.CarPartId;
                    if (_sceneService.Value.GetCarPartConfig(neighborCarPartId).PartType == CarPartType.Gun)
                        _gunComponentsPool.Value.Get(neighbor).CurrentReloadSpeed *= 1f - supportConfig.FirstCount *
                            GridUtils.GetSupportLevelMultipliers(neighborCarPartInfoCmp.PartLevel, supportConfig.ModuleType)
                                .firstMultiplayer;
                }
            }
        }
    }

    private void SetEnemyCollisionCheckers(int[,] grid, CarView carView, ref EnemyAIComponent enemyAiCmp) {
        for (int j = 0; j < grid.GetLength(0); j++) {
            for (int k = 0; k < grid.GetLength(1); k++) {
                if (grid[j, k] != 0) {
                    var obstacleCheckerTransform = _sceneService.Value.GetGunContainer(carView.CarPartsContainer);
                    obstacleCheckerTransform.localRotation = new Quaternion();
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
        SetComponentsValuesToDefault(_sceneService.Value.PlayerEntity);

        if (!NetworkManager.Singleton.IsServer) {
            foreach (var carEntity in inventoryCmp.PlayerTeammateCarsEntity) 
                SendCarBuildToServer(carEntity);
            ArrangeImagesInGrid();
            return;
        }
        ref var carCmp = ref _carComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

        carCmp.CarPartViewsGrid =
            new CarPartStuffView[carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1)];
     
        _netcodeSceneService.Value.InstantiateCarViewServerRpc(_sceneService.Value.PlayerEntity, true);
        foreach (var carEntity in inventoryCmp.PlayerTeammateCarsEntity) {
            if (carEntity != _sceneService.Value.PlayerEntity)
                SpawnTeammateCar(carEntity);
        }
       
    }

    public void SendCarBuildToServer(int entity) {
        var carCmp = _carComponentsPool.Value.Get(entity);
        var downParts = new List<CarPartDataForRpc>();
        foreach (var carPartInfo in carCmp.CarPartContainerGroupsList) {
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(carPartInfo.Key);
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartInfo.Key);
            downParts.Add(new CarPartDataForRpc(GridUtils.GetEdgePointFromCarPart(carPartInfo.Value), carPartCmp.RotateDegrees,
                carPartInfoCmp.ModuleIds, carPartInfoCmp.CarPartId, carPartInfoCmp.PartLevel, (int)carPartInfoCmp.CurrentHealth,
                carPartInfo.Key));
        }

        var upParts = new List<CarPartDataForRpc>();
        foreach (var carPartInfo in carCmp.CarPartGroupsList) {
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(carPartInfo.Key);
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartInfo.Key);
            upParts.Add(new CarPartDataForRpc(GridUtils.GetEdgePointFromCarPart(carPartInfo.Value), carPartCmp.RotateDegrees,
                carPartInfoCmp.ModuleIds, carPartInfoCmp.CarPartId, carPartInfoCmp.PartLevel, (int)carPartInfoCmp.CurrentHealth,
                carPartInfo.Key));
        }
        _netcodeSceneService.Value.SetCarToServerRpc(upParts.ToArray(), downParts.ToArray(), carCmp.CarLevel,
            entity == _sceneService.Value.PlayerEntity, carCmp.CarId,
            new Vector2(carCmp.CarPartGroupsGrid.GetLength(0), carCmp.CarPartGroupsGrid.GetLength(1)));
    }

    public void SetModuleBuffs(int carPartEntity) {
        ref var carPartInfo = ref _carPartInfoComponentsPool.Value.Get(carPartEntity);
        var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartInfo.CarPartId);
        var moduleIds = carPartInfo.ModuleIds;

        (float damageMultiplayer, float attackSpeedMultiplayer, float healthMultiplayer, int targetPiercingAddedCount,
            float reloadSpeedMultiplayer,float maxWeightMultiplier,float wheelRotateSpeedMultiplier, float magSizeMultiplier) = GridUtils.GetModulesMultipliers(moduleIds);

        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartEntity);

        carPartCmp.MaxHealth *= healthMultiplayer;
        if (carPartConfig.PartType == CarPartType.Gun) {
            ref var gunCmp = ref _gunComponentsPool.Value.Get(carPartEntity);
            gunCmp.CurrentDamage *= damageMultiplayer;
            gunCmp.CurrentMaxAttackCountdown *= attackSpeedMultiplayer;
            gunCmp.CurrentReloadSpeed *= reloadSpeedMultiplayer;
            gunCmp.MaxPiercingCount += targetPiercingAddedCount;
            gunCmp.CurrentMaxBulletsInMagazine = Mathf.FloorToInt(gunCmp.CurrentMaxBulletsInMagazine*magSizeMultiplier);
            gunCmp.CurrentMagazineCount = gunCmp.CurrentMaxBulletsInMagazine;
        } else if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
            ref var meleeCmp = ref _meleeWeaponComponentsPool.Value.Get(carPartEntity);
            meleeCmp.CurrentDamage *= damageMultiplayer;
            meleeCmp.CurrentMaxCountdown *= attackSpeedMultiplayer;
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
        fuelCmp.MaxFuel += carConfig.DefaultMaxFuel * baseMaxFuelMultiplier;
        fuelCmp.CurrentFuel = fuelCmp.MaxFuel;
        fuelCmp.CurrentElectricity = fuelCmp.MaxElectricity;
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
                var carPartCmp = _carPartComponentsPool.Value.Get(carPartInfo.Key);
                var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(carPartInfo.Key);
                inventoryCmp[row, col] = cellUIHealth;
                bool hasText = cell == medianCell;
                cellUIHealth.gameObject.SetActive(true);
                cellUIHealth.SetSize(new Vector2(cellSize, cellSize), new Vector2(posX, posY),
                    _sceneService.Value._carStuffListsConfig.CarPartUIBordersSprites[GridUtils.GetTileSpriteIndex(groupsGrid, cell)],hasText);
                
                cellUIHealth.SetHealthCount(_sceneService.Value._healthGradient.Evaluate(carPartInfoCmp.CurrentHealth / carPartCmp.MaxHealth),
                    (int)carPartInfoCmp.CurrentHealth, true);
                // }
            }
        }
    }

    private bool CanStartRound() {
        var carCmp = _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity)
            .CurrentRedactedCarEntity);
        if (carCmp.CurrentWeight > carCmp.CurrentMaxWeight) {
            _sceneService.Value.PlayWarningAnimation(GetLocalization("The weight of the vehicle exceeds the maximum") + "!",false);
            return false;
        }

        bool mainPartIsPlaced = MainPartIsPlaced();
        bool canUseThisCarPartContainers = CanUseThisCarPartContainers();
        bool carIsStable = CheckAllCarContainersForStability();
        if (!canUseThisCarPartContainers || !carIsStable)
            _sceneService.Value.PlayWarningAnimation(GetLocalization("The car is unstable and needs wheels") + "!",false);
        else if (!mainPartIsPlaced)
            _sceneService.Value.PlayWarningAnimation(GetLocalization("Place the engine and driver's cabin on the car") + "!",false);

        if (mainPartIsPlaced && canUseThisCarPartContainers && carIsStable) return true;
        return false;
    }
    
    private bool CanUseThisCarPartContainers() {
        int wheelsCount = 0;
        Vector2Int lastWheelMinMaxY = Vector2Int.zero;
        var carCmp = _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRedactedCarEntity);
        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartInfoComponentsPool.Value.Get(carPartContainer.Key);
            var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartCmp.CarPartId);
            if (carPartConfig.PartType == CarPartType.WheelsContainer) {
                wheelsCount++;
                
                Vector2Int edgePoint = GridUtils.GetEdgePointFromCarPart(carPartContainer.Value);
                Vector2Int partSize = GridUtils.GetCarPartSize(carPartConfig);
                if(_sceneService.Value.GetWheelContainerFromCarPart(carPartCmp.CarPartId).CanBeMonoWheel)
                    return true;
                if (wheelsCount == 1) {
                    lastWheelMinMaxY = new Vector2Int(edgePoint.y, edgePoint.y + partSize.y - 1);
                } else if( lastWheelMinMaxY.x > (edgePoint.y + partSize.y - 1) || lastWheelMinMaxY.y < edgePoint.y) {
                    return true;
                }
            }
        }

        return false;
    }
    
     private bool CheckAllCarContainersForStability() {
        List<Vector2Int> needCells = new List<Vector2Int>();
        var carCmp = _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRedactedCarEntity);
        foreach (var carPart in carCmp.CarPartContainerGroupsList) {
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(carPart.Key);
            if (_sceneService.Value.GetCarPartConfig(carPartInfoCmp.CarPartId).PartType == CarPartType.CarStuffContainer) 
                needCells.AddRange(carPart.Value);
            
        }
        
        return CheckCellsForStability(needCells);
    }

    private bool CheckCellsForStability(List<Vector2Int> needCells) {
        List<Vector2Int> cellsToCheck = needCells.ToList();
        var carCmp = _carComponentsPool.Value.Get(_inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).CurrentRedactedCarEntity);
        foreach (var carPart in carCmp.CarPartContainerGroupsList) {
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(carPart.Key);
            if (_sceneService.Value.GetCarPartConfig(carPartInfoCmp.CarPartId).PartType == CarPartType.WheelsContainer) {
                int checkRadius = _sceneService.Value.GetWheelContainerFromCarPart(carPartInfoCmp.CarPartId).StabilityRadius;
                foreach (var wheelCell in carPart.Value) {
                    for (int i = 0; i < cellsToCheck.Count; i++) {
                        if (Mathf.Abs(wheelCell.x - cellsToCheck[i].x) <= checkRadius &&
                            Mathf.Abs(wheelCell.y - cellsToCheck[i].y) <= checkRadius) {
                            
                            cellsToCheck.RemoveAt(i);
                            if (cellsToCheck.Count == 0) 
                                return true;
                            

                            i--;
                        }
                    }
                }
            }
        }

        return false;
    }

    private void SpawnCarPartCellView(int cellEntity, CarView carView, bool isPlayer, int rotateAngle) {
        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(cellEntity);
        ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(cellEntity);
        ref var carCmp = ref _carComponentsPool.Value.Get(carPartCmp.MainEntity);
        var carPartConfig = _sceneService.Value._carStuffListsConfig.CarPartConfigs[carPartInfoCmp.CarPartId];

       // var containersSize = new Vector2(carCmp.CarPartContainerGroupsGrid.GetLength(0), carCmp.CarPartContainerGroupsGrid.GetLength(1));
        bool isContainer = GridUtils.IsContainerType(carPartConfig);
       // int needSpriteLevel = isContainer ? 1 : 2;
        //int needRotate = rotateAngle;
        var needList = isContainer ? carCmp.CarPartContainerGroupsList : carCmp.CarPartGroupsList;
        Debug.Log("nee list " + needList.Count);
     //   Vector2Int carPartSize = GridUtils.GetCarPartSize(carPartConfig);
        carPartCmp.MaxHealth = carPartConfig.MaxHealth*GridUtils.GetHealthLevelMultiplier(carPartInfoCmp.PartLevel);
     
        if (carPartInfoCmp.CurrentHealth == 0)
            carPartInfoCmp.CurrentHealth = carPartCmp.MaxHealth;

   
        carCmp.CurrentWeight += carPartConfig.Weight;
  //      carPartCmp.CarPartCellRenderer.transform.localRotation = Quaternion.Euler(0, 0, needRotate);
        ref var fuelCmp = ref _fuelComponentsPool.Value.Get(carPartCmp.MainEntity);
        if (carPartConfig.PartType == CarPartType.Gun) {
            //  carPart.SetColliderEnableState(false);

            var gunCmp = _gunComponentsPool.Value.Has(cellEntity)
                ? _gunComponentsPool.Value.Get(cellEntity)
                : _gunComponentsPool.Value.Add(cellEntity);
            var gunConfig = _sceneService.Value.GetGunFromCarPart(carPartInfoCmp.CarPartId);
         

            (float damageMultiplier, float reloadMultiplier, float attackCountdownMultiplayer) =
                GridUtils.GetGunLevelMultipliers(gunConfig, carPartInfoCmp.PartLevel);
            gunCmp.CurrentReloadSpeed = gunConfig.ReloadDuration / reloadMultiplier;
            gunCmp.CurrentMaxAttackCountdown = gunConfig.AttackCouldown / attackCountdownMultiplayer;
            gunCmp.CurrentDamage = gunConfig.Damage * damageMultiplier;
            gunCmp.CurrentCountdown = gunCmp.CurrentReloadSpeed;
            gunCmp.CurrentMaxBulletsInMagazine = gunConfig.BulletsCountInMagazine;
            gunCmp.CurrentMagazineCount = gunCmp.CurrentMaxBulletsInMagazine;
            gunCmp.MaxPiercingCount = gunConfig.BreakingThroughCount;
          

            _gunComponentsPool.Value.Get(cellEntity) = gunCmp;
        } else if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
            var meleeWeaponCmp = _meleeWeaponComponentsPool.Value.Has(cellEntity)
                ? _meleeWeaponComponentsPool.Value.Get(cellEntity)
                : _meleeWeaponComponentsPool.Value.Add(cellEntity);
            var meleeWeaponConfig = _sceneService.Value.GetMeleeWeaponFromCarPart(carPartInfoCmp.CarPartId);
        

            fuelCmp.ConstantFuelConsumption += meleeWeaponConfig.FuelConsumption;
          
            meleeWeaponCmp.CurrentMaxCountdown = meleeWeaponConfig.AttackCountdown;
         
            
            (float damageMultiplier, float damageBySpeedMultiplier) =
                GridUtils.GetMeleeLevelMultipliers(meleeWeaponConfig, carPartInfoCmp.PartLevel);
            meleeWeaponCmp.CurrentDamage = meleeWeaponConfig.Damage * damageMultiplier;
            meleeWeaponCmp.DamageBySpeed = meleeWeaponConfig.DamageAddedFromOneSpeed * damageBySpeedMultiplier;
            _meleeWeaponComponentsPool.Value.Get(cellEntity) = meleeWeaponCmp;
          
        } else if (carPartConfig.PartType == CarPartType.Support) {
            var supportCarPartConfig = _sceneService.Value.GetSupportModuleFromCarPart(carPartInfoCmp.CarPartId);
            (float firstMultiplayer,float secondMultiplayer)= GridUtils.GetSupportLevelMultipliers(carPartInfoCmp.PartLevel, supportCarPartConfig.ModuleType);
            switch (supportCarPartConfig.ModuleType) {
                case SupportModuleType.Fuel:
                    
                    fuelCmp.CurrentFuel += supportCarPartConfig.FirstCount*firstMultiplayer;
                    fuelCmp.MaxFuel += supportCarPartConfig.FirstCount*firstMultiplayer;
                    break;
                case SupportModuleType.Money:
                   fuelCmp.AddedFuelConsumption += supportCarPartConfig.FirstCount*firstMultiplayer;
                    _movementComponentsPool.Value.Get(carPartCmp.MainEntity).MaxBoostMoveSpeed += supportCarPartConfig.SecondCount*secondMultiplayer;
                    break;
                case SupportModuleType.FireExtinguisher or SupportModuleType.Heater:
                  ref var fireExtinguisher = ref  _fireExtinguisherComponentsPool.Value.Add(cellEntity);
                  fireExtinguisher.CurrentFireRemove = supportCarPartConfig.FirstCount*firstMultiplayer;
                  fireExtinguisher.CountdownTime = 1;
                  if (supportCarPartConfig.ModuleType == SupportModuleType.Heater) {
                      fireExtinguisher.EffectType = EffectType.fire;
                      var consumption = supportCarPartConfig.SecondCount*supportCarPartConfig.FirstCount*firstMultiplayer/25f;
                      fuelCmp.ConstantElectricityConsumption += consumption;
                  }
                      
                  else
                      fireExtinguisher.EffectType = EffectType.froze;
                  fireExtinguisher.CheckedCells =
                      GridUtils.GetNonZeroNeighbors8Dir(carCmp.CarPartContainerGroupsGrid, carCmp.CarPartGroupsList[cellEntity],(int)supportCarPartConfig.SecondCount);
                  break;
                case SupportModuleType.Battery:
                    fuelCmp.MaxElectricity += supportCarPartConfig.FirstCount*firstMultiplayer;
                    break;
                case SupportModuleType.ElectricGenerator:
                    if (supportCarPartConfig.SecondCount != 0) {
                        fuelCmp.ConstantFuelElectricityReplenishment += supportCarPartConfig.FirstCount * firstMultiplayer;
                        fuelCmp.ConstantFuelToElectricityConsumption += supportCarPartConfig.SecondCount * secondMultiplayer;
                    } else
                        fuelCmp.ConstantElectricityReplenishment += supportCarPartConfig.FirstCount * firstMultiplayer;

                    break;
            }
        } else if (carPartConfig.PartType == CarPartType.WheelsContainer) {
            var wheelsConfig = _sceneService.Value.GetWheelContainerFromCarPart(carPartConfig.Id);
            carCmp.CurrentMaxWeight += wheelsConfig.MaxWeight * GridUtils.GetMaxWeightLevelMultiplier(carPartInfoCmp.PartLevel)
                                                              * GridUtils.GetModulesMultipliers(carPartInfoCmp.ModuleIds).maxWeightMultiplier;
        }

        if (isPlayer)
            SetModuleBuffs(cellEntity);

        var carPartStuffPositionsX = new int[needList[cellEntity].Count];
        var carPartStuffPositionsY = new int[needList[cellEntity].Count];
        for (int i = 0; i < needList[cellEntity].Count; i++) {
        //    Debug.Log(needList[cellEntity][i].x + " "+needList[cellEntity][i].y + " ent: "+carPartCmp.MainEntity + " cell: "+cellEntity);
            carPartStuffPositionsX[i] = needList[cellEntity][i].x;
            carPartStuffPositionsY[i] = needList[cellEntity][i].y;
        }
        
        _netcodeSceneService.Value.SpawnCarPartSpritesServerRpc(carPartCmp.MainEntity, cellEntity,carPartInfoCmp.CarPartId, rotateAngle,isPlayer,
            carCmp.CarPartContainerGroupsGrid.GetLength(0),carCmp.CarPartContainerGroupsGrid.GetLength(1),
            carView.transform.parent.GetComponent<NetworkObject>(),carPartStuffPositionsX, carPartStuffPositionsY);
    }

    private void SetupCarPartSprites(int cellEntity, SpriteRenderer spriteBase, Transform weaponContainerTransform,
        SpriteRenderer weaponSpriteRenderer, LineRenderer laserLineRenderer, CarPartStuffView[] carStuffViews, Transform firePointTransform,
        MeleeCarPartStuffView meleeStuff) {
        ref var carPartCmp = ref _carPartComponentsPool.Value.Get(cellEntity);
        ref var carPartInfoCmp = ref _carPartInfoComponentsPool.Value.Get(cellEntity);
        var carPartConfig = _sceneService.Value._carStuffListsConfig.CarPartConfigs[carPartInfoCmp.CarPartId];

        carPartCmp.CellCarPartsStuffViews = new List<CarPartStuffView>();
        Debug.Log(carStuffViews.Length + "carStuffViews.Length");
        for (int i = 0; i < carStuffViews.Length; i++) {
            carPartCmp.CellCarPartsStuffViews.Add(carStuffViews[i]);
          /*  if (GridUtils.IsContainerType(carPartConfig)) {
                string needLayer = _inventoryComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).PlayerTeammateCarsEntity.Contains(carPartCmp.MainEntity) ? "UpCarPart" : "UpCarPartEnemy";
                carStuffViews[i].SetupCarPart( carStuffViews[i]._gridPosition, needLayer, carPartCmp.MainEntity,_netcodeSceneService.Value.OwnerClientId);
                carStuffViews[i].transform.localRotation = Quaternion.Euler(0, 0, carPartCmp.RotateDegrees);
            }*/
        }
            

        carPartCmp.CarPartCellRenderer = spriteBase;

        if (carPartConfig.PartType == CarPartType.Gun) {
            ref var gunCmp = ref _gunComponentsPool.Value.Get(cellEntity);
            var gunConfig = _sceneService.Value.GetGunFromCarPart(carPartInfoCmp.CarPartId);
            if (gunConfig.UpPartSprite != null) {
                gunCmp.UpPartContainer = weaponContainerTransform;

                gunCmp.FirePointTransform = firePointTransform;

                gunCmp.GunSpriteRenderer = weaponSpriteRenderer;
            } else {
                gunCmp.FirePointTransform = firePointTransform;
            }

            if (gunConfig.MissleType == MissleType.laser) {
                if (_laserComponentsPool.Value.Has(cellEntity))
                    _laserComponentsPool.Value.Get(cellEntity).LaserLineRenderer = laserLineRenderer;
                else
                    _laserComponentsPool.Value.Add(cellEntity).LaserLineRenderer = laserLineRenderer;
            }
        } else if (carPartConfig.PartType == CarPartType.MeleeWeapon) {
            ref var meleeWeaponCmp = ref _meleeWeaponComponentsPool.Value.Get(cellEntity);
            var meleeWeaponConfig = _sceneService.Value.GetMeleeWeaponFromCarPart(carPartInfoCmp.CarPartId);
            if (meleeWeaponConfig.UpSprite != null) {
                meleeWeaponCmp.MeleeWeaponRenderer = weaponSpriteRenderer;
            }

            meleeWeaponCmp.MeleeStuff = meleeStuff;

            _constructMeleeCellEventsPool.Value.Add(cellEntity);
        } else if (!GridUtils.IsContainerType(carPartConfig) && _carComponentsPool.Value.Get(carPartCmp.MainEntity).CarView.Wheels.Count == 0) {
            Debug.Log( " wheel setup");
            SetupWheels(carPartCmp.MainEntity, _carComponentsPool.Value.Get(carPartCmp.MainEntity).CarView);
        }
    
        
    
    }
    private void SetComponentsValuesToDefault(int enemyEntity) {
        ref var movementComponent = ref _movementComponentsPool.Value.Get(enemyEntity);
        movementComponent.MaxMoveSpeed = 0;
        movementComponent.Acceleration = 0;
        movementComponent.Deceleration = 0;
        movementComponent.MaxBoostMoveSpeed = 0;
        movementComponent.CurrentRotateSpeed = 0;
        ref var fuelComponent = ref _fuelComponentsPool.Value.Get(enemyEntity);
        fuelComponent.MaxElectricity = 0;
        fuelComponent.CurrentElectricity = 0;
        fuelComponent.MaxFuel = 0;
        fuelComponent.UseSpeedBoost = false;
        ref var carCmp = ref _carComponentsPool.Value.Get(enemyEntity);
        carCmp.CurrentWeight = 0;
        carCmp.CurrentMaxWeight = 0;
    }
    private void SetupEnemyMovementComponent(int enemyEntity, CarConfig carConfig) {
        ref var movementComponent = ref _movementComponentsPool.Value.Get(enemyEntity);
        var carCmp = _carComponentsPool.Value.Get(enemyEntity);
      
        float rotateSpeedMultiplier = 1;
        foreach (var carPart in carCmp.CarPartContainerGroupsList.Keys) {
            var carPartCellCmp = _carPartInfoComponentsPool.Value.Get(carPart);
            int carPartId = carPartCellCmp.CarPartId;
            var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartId);
            if (carPartConfig is WheelsContainerConfig wheels) {
                rotateSpeedMultiplier += GridUtils.GetModulesMultipliers(carPartCellCmp.ModuleIds).wheelRotateSpeedMultiplier - 1;
            }
              
        }

        movementComponent.RotateSpeed = carConfig.RotateSpeed *
                                        _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).RoundMoneyMultiplayer *
                                        (1 - carCmp.CurrentWeight / carCmp.CurrentMaxWeight)*rotateSpeedMultiplier;
        movementComponent.Acceleration = carConfig.Acceleration;
        movementComponent.Deceleration = carConfig.Deceleration;

      //  carCmp.CarView.SyncRigidbodyView.SetMassServerRpc(carCmp.CurrentWeight);
        //movementComponent.MovableObjectRigidbody.mass = carCmp.CurrentWeight;
        movementComponent.MaxMoveSpeed += carConfig.DefaultMaxSpeed * (1 - carCmp.CurrentWeight / carCmp.CurrentMaxWeight);
        Debug.Log("max move speed "+movementComponent.MaxMoveSpeed);
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
            _carComponentsPool.Value.Get(entityOne).CarView.SyncRigidbodyView.RequestPushImpulseServerRpc(direction * secondCarCmp.CurrentWeight * car1Ratio / 3);
        }
    }

    private void CarViewSetup(CarView carView, int entity, bool isPlayer) {
        _movementComponentsPool.Value.Get(entity).MovableObjectRigidbody = carView.Rigidbody;
       
        string tag = isPlayer ? "Player" : "Enemy";
        carView.Construct(entity, ColissionCarsHit, tag);
        ref var carCmp = ref _carComponentsPool.Value.Get(entity);
        carCmp.CarView = carView;
        carCmp.ForwardWheels = new List<Animator>();
        carCmp.ForwardWheelsParticles = new List<ParticleSystem>();
        carCmp.CurrentWeight = 0;

        carCmp.CarView.transform.position = GetPositionAroundPlayer();

        foreach (var carContainer in carCmp.CarPartContainerGroupsList) {
            int carPartCellEntity = carContainer.Key;
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartCellEntity);

            SpawnCarPartCellView(carPartCellEntity, carCmp.CarView, isPlayer, carPartCmp.RotateDegrees);
        }

        foreach (var carContainer in carCmp.CarPartGroupsList) {
            int carPartCellEntity = carContainer.Key;
            ref var carPartCmp = ref _carPartComponentsPool.Value.Get(carPartCellEntity);

            SpawnCarPartCellView(carPartCellEntity, carCmp.CarView, isPlayer, carPartCmp.RotateDegrees);
        }

        SetupEnemyMovementComponent(entity, _sceneService.Value.GetCarConfig(carCmp.CarId));
        SetReloadBoostSupportTypes(entity);
        SetupCarStatsFromLevelBoost(entity);
        carCmp.CarView.SyncRigidbodyView.SetMassServerRpc(carCmp.CurrentWeight);
    
        if (_enemyAIComponentsPool.Value.Has(entity)) {
            ref var enemyAiCmp = ref _enemyAIComponentsPool.Value.Get(entity);
            enemyAiCmp.CheckedEnemyLeftPoints = new List<Transform>();
            enemyAiCmp.CheckedEnemyRightPoints = new List<Transform>();
            enemyAiCmp.LastCheckedTime = 1f;
       //     enemyAiCmp.IsEnemy = false;
            SetEnemyCollisionCheckers(carCmp.CarPartContainerGroupsGrid, carCmp.CarView, ref enemyAiCmp);
        } else if(_sceneService.Value.PlayerEntity == entity)
            ArrangeImagesInGrid();

        BuildBorder(entity);
    }

    private void SetupWheels(int carEntity, CarView carView) {
        float maxYposWheels = -100;
        ref var carCmp  =ref _carComponentsPool.Value.Get(carEntity);
        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartConfig = _sceneService.Value.GetCarPartConfig(_carPartInfoComponentsPool.Value.Get(carPartContainer.Key).CarPartId);
            if (carPartConfig.PartType == CarPartType.WheelsContainer) {
                var carPartCmp = _carPartComponentsPool.Value.Get(carPartContainer.Key);
                maxYposWheels = maxYposWheels < carPartCmp.CarPartCellRenderer.transform.localPosition.y
                    ? carPartCmp.CarPartCellRenderer.transform.localPosition.y
                    : maxYposWheels;
            }
        }

        List<Vector4> wheelsPositions = new List<Vector4>();
        foreach (var carPartContainer in carCmp.CarPartContainerGroupsList) {
            var carPartCmp = _carPartComponentsPool.Value.Get(carPartContainer.Key);
            var carPartInfoCmp = _carPartInfoComponentsPool.Value.Get(carPartContainer.Key);
            var carPartConfig = _sceneService.Value.GetCarPartConfig(carPartInfoCmp.CarPartId);
            if (carPartConfig.PartType == CarPartType.WheelsContainer) {
                var wheelConfig = _sceneService.Value.GetWheelContainerFromCarPart(carPartConfig.Id);

                if (!_wheelContainerComponentsPool.Value.Has(carPartContainer.Key))
                    _wheelContainerComponentsPool.Value.Add(carPartContainer.Key);
                ref var wheelCmp = ref _wheelContainerComponentsPool.Value.Get(carPartContainer.Key);
                wheelCmp.Wheels = new List<Animator>();
                foreach (var wheelPos in wheelConfig.WheelsPositions) {
                    //     Debug.Log((Vector2)carPartCmp.CarPartCellRenderer.transform.position);
                    Vector2 wheelPosition = (Vector2)carPartCmp.CarPartCellRenderer.transform.position + wheelPos -
                                            (Vector2)carView.transform.position;
                    Vector4 wheelInfo = new(wheelPosition.x, wheelPosition.y, wheelConfig.WheelId, carPartContainer.Key);
                    wheelsPositions.Add(wheelInfo);
                    //    var wheel = CreateWheel(wheelConfig.WheelId, carView, (Vector2)carPartCmp.CarPartCellRenderer.transform.position + wheelPos-(Vector2)carView.transform.position,
                    //      wheelPos.x > 0);
                    // wheelCmp.Wheels.Add(wheel);
                    //    if (maxYposWheels == carPartContainer.Value[0].y)
                    //       carCmp.ForwardWheels.Add(wheel);
                    //     var particleSystem = wheel.GetComponentInChildren<ParticleSystem>();
                    //    carCmp.ForwardWheelsParticles.Add(particleSystem);
                    //       var mainParticle = particleSystem.main;
                    //        mainParticle.startColor = _betweenScenesService.Value.GetMapConfig().MapVisualInfoConfigs[0].WheelParticlesColor;
                }
            }
        }

        _netcodeSceneService.Value.SpawnWheelsClientRpc(carView.GetComponentInParent<NetworkObject>(),
            wheelsPositions.Select(v => v.x).ToArray(), wheelsPositions.Select(v => v.y).ToArray(), maxYposWheels,
            wheelsPositions.Select(v => (int)v.z).ToArray(), wheelsPositions.Select(v => (int)v.w).ToArray(),
            _netcodeSceneService.Value.OwnerClientId);
        
        
    }

    private void SetupWheelComponents(Animator[] wheels, int[] wheelEntity) {
        for (int i = 0; i < wheelEntity.Length; i++) {
            ref var wheelCmp = ref _wheelContainerComponentsPool.Value.Get(wheelEntity[i]);
            wheelCmp.Wheels.Add(wheels[i]);
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
            if (_sceneService.Value.GetCarPartConfig(_carPartInfoComponentsPool.Value.Get(cellEntity).CarPartId).PartType == CarPartType.Main)
                return false;
        }

        return true;
    }
    public static bool AreAllNeighborsNonZero(int[,] grid, int row, int col)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
    
        // Проверяем все 8 соседних клеток
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                // Пропускаем саму клетку
                if (i == 0 && j == 0)
                    continue;
                
                int newRow = row + i;
                int newCol = col + j;
            
                // Проверяем границы массива
                if (newRow >= 0 && newRow < rows && 
                    newCol >= 0 && newCol < cols)
                {
                    if (grid[newRow, newCol] != 0)
                        return false;
                }
            }
        }
    
        return true; // Все соседи не равны 0
    }
   
    private void UpdateMapVisual() {
        if (!_currentGameComponentsPool.Value.Has(_sceneService.Value.PlayerEntity))
            _currentGameComponentsPool.Value.Add(_sceneService.Value.PlayerEntity);
        _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MapGrid = new int[40, 40];
        var mapConfig = _betweenScenesService.Value.GetMapConfig();
        var mapVisualConfig = mapConfig.MapVisualInfoConfigs[0];
        _sceneService.Value._behindMapGroundSpriteRenderer.material = mapVisualConfig.BehindMapGroundMaterial;
        _sceneService.Value._behindMapBorderSpriteRenderer.material = mapVisualConfig.BehindMapGroundMaterial;
        _sceneService.Value._mapGroundSpriteRenderer.material = mapVisualConfig.GroundMaterial;

        //make different prefab to plates
        var versusBattleView = _sceneService.Value._versusBattleView;
        versusBattleView._behindMapGroundSpriteRenderer.material = mapVisualConfig.BehindMapGroundMaterial;
        versusBattleView._behindMapBorderSpriteRenderer.material = mapVisualConfig.BehindMapGroundMaterial;
        versusBattleView._mapGroundSpriteRenderer.material = mapVisualConfig.GroundMaterial;
            
    }

    private void UpdateMapGridClientRpc(int[] mapGrid) {
        ref var currentGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        for (int i = 0; i < 20; i++) {
            for (int j = 0; j < 20; j++) {
                if(mapGrid[i * 20+j] == 0)continue;
                currentGameCmp.MapGrid[i * 2, j * 2] = 1;
                currentGameCmp.MapGrid[i * 2 + 1, j * 2 + 1] = 1;
                currentGameCmp.MapGrid[i * 2 + 1, j * 2] = 1;
                currentGameCmp.MapGrid[i * 2, j * 2 + 1] = 1;
            }
        }     
    }

    private void GeneratePlatesFromGrid() {
        
    }

    private void GenerateMap() {
        UpdateMapVisual();
        var mapConfig = _betweenScenesService.Value.GetMapConfig();
        if (!_betweenScenesService.Value.IsCoop || NetcodeBetweenScenesService.Instance.IsHost) {
        //    _netcodeSceneService.Value.GetComponent<NetworkObject>().Spawn();
           // _netcodeSceneService.Value.networkObjectsMapContainer.Spawn();
            ref var currentGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);

            Vector2 randomOffset = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f));

            if (mapConfig.MapId == 1) {
                var buildigsMap = new int[20, 20];

                GridUtils.GenerateRandomBuildings(ref buildigsMap);

                int[] newArray = new int[buildigsMap.GetLength(0) * buildigsMap.GetLength(1)];
                int index = 0;
                foreach (var mapIndex in buildigsMap) {
                    newArray[index] = mapIndex;
                    index++;
                }

                NetcodeBetweenScenesService.Instance.ChangeMapClientRpc(newArray);
                var buildingVisualConfig = mapConfig.MapVisualInfoConfigs[1];

                for (int i = 0; i < currentGameCmp.MapGrid.GetLength(0); i++) {
                    for (int j = 0; j < currentGameCmp.MapGrid.GetLength(1); j++) {
                        if (currentGameCmp.MapGrid[i, j] == 0) continue;

                        var locPosition = new Vector3(i * 5 - 97.5f, j * 5 - 97.5f, 0f);

                        NetcodeBetweenScenesService.Instance.SpawnGroundClientRpc(locPosition);

                        if (i % 2 == 1 || j % 2 == 1) continue;
                        Vector2 obstaclePosition = new(i * 5f - 100, j * 5f - 90);
                        SpawnPillar(obstaclePosition, buildingVisualConfig);
                        if (j == 40 || i == 0 || (currentGameCmp.MapGrid[i, j + 2] == 0 && currentGameCmp.MapGrid[i + 2, j] == 0)) {
                            Vector2 downObstaclePosition = new(obstaclePosition.x + 10, obstaclePosition.y);
                            SpawnPillar(downObstaclePosition, buildingVisualConfig);
                        }

                        if (i == 0 || currentGameCmp.MapGrid[i, j - 1] == 0) {
                            Vector2 downObstaclePosition = new(obstaclePosition.x, obstaclePosition.y - 10);
                            SpawnPillar(downObstaclePosition, buildingVisualConfig);
                        }

                        if (i == 40 || currentGameCmp.MapGrid[i + 2, j] == 0) {
                            Vector2 downObstaclePosition = new(obstaclePosition.x + 10, obstaclePosition.y - 10);
                            SpawnPillar(downObstaclePosition, buildingVisualConfig);
                        } /**/
                    }
                }

                for (int i = 0; i < 10; i++) {
                    for (int j = 0; j < 10; j++) {
                        if (i == 4 && j == 5) continue;
                        GenerateObstaclesInCell(i, j, randomOffset);
                    }
                }

                return;
            } else if (mapConfig.MapId == 0 && _betweenScenesService.Value.CurrentSelectedMapDifficlty > 0) {
                if (_betweenScenesService.Value.CurrentSelectedMapDifficlty > 2) {
                    ref var trainControllerCmp = ref _trainControllerComponentsPool.Value.Add(_sceneService.Value.PlayerEntity);
                    trainControllerCmp.TrainTransform =
                        _sceneService.Value.InstantiateObject(_sceneService.Value._mapEventsContentView._trainPrefab.transform);
                    trainControllerCmp.TrainTransform.gameObject.SetActive(false);
                    trainControllerCmp.TrainTransform.GetComponent<NetworkObject>().Spawn(true);
                    trainControllerCmp.TimeToNextTrain = 7f;
                    trainControllerCmp.TrainRails = new List<Transform>();
                    List<int> needPositions = Enumerable.Range(0, 61).ToList();
                    for (int i = 0; i < 5; i++) {
                        //var trainTrail = _sceneService.Value.InstantiateObject(_sceneService.Value._mapEventsContentView._trainTunnelsPrefab);
                        int needIndex = Random.Range(0, needPositions.Count);
                        var needValue = needPositions[needIndex];
                        NetcodeBetweenScenesService.Instance.SpawnTrainTrailsClientRpc(needValue);
                        /*   if (needValue < 30) {
                               int needGridLine = 5 + needValue;
                               trainTrail.localEulerAngles = new Vector3(0, 0, 90);
                               trainTrail.position = new Vector3((needGridLine) * 5 - 97.5f, 1.3f, 0f);
                               for (int j = 0; j < currentGameCmp.MapGrid.GetLength(0); j++)
                                   currentGameCmp.MapGrid[needGridLine, j] = 1;

                               //1 line to grid
                           } else {
                               int needGridLine = needValue - 25;
                               trainTrail.position = new Vector3(-7.5f, (needGridLine) * 5 - 97.5f, 0f);
                               for (int j = 0; j < currentGameCmp.MapGrid.GetLength(0); j++)
                                   currentGameCmp.MapGrid[j, needGridLine] = 1;
                           }*/

                        needPositions.Remove(needValue + 1);
                        needPositions.Remove(needValue - 1);
                        needPositions.Remove(needValue);
                        //  trainControllerCmp.TrainRails.Add(trainTrail);
                    }
                }

           //     GridUtils.PrintGrid(currentGameCmp.MapGrid);
                var quicksandVisualConfig = _sceneService.Value._mapEventsContentView._quicksandPrefabs;
                for (int i = 0; i < 10; i++) {
                    for (int j = 0; j < 10; j++)
                        GenerateQuicksandInCell(i, j, randomOffset, quicksandVisualConfig);
                }
            }

            for (int i = 0; i < 10; i++) {
                for (int j = 0; j < 10; j++)
                    GenerateObstaclesInCell(i, j, randomOffset);
            }
        }
    }

    private void AddTrainTrailInList(Transform trainTrail, int needValue) {
        ref var currentGameCmp = ref _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity);
        
        if (needValue < 30) {
            int needGridLine = 5 + needValue;
            trainTrail.localEulerAngles = new Vector3(0, 0, 90);
            trainTrail.position = new Vector3((needGridLine) * 5 - 97.5f, 1.3f, 0f);
            for (int j = 0; j < currentGameCmp.MapGrid.GetLength(0); j++)
                currentGameCmp.MapGrid[needGridLine, j] = 1;

            //1 line to grid
        } else {
            int needGridLine = needValue - 25;
            trainTrail.position = new Vector3(-7.5f, (needGridLine) * 5 - 97.5f, 0f);
            for (int j = 0; j < currentGameCmp.MapGrid.GetLength(0); j++)
                currentGameCmp.MapGrid[j, needGridLine] = 1;
        }
        if (_betweenScenesService.Value.IsCoop && !NetcodeBetweenScenesService.Instance.IsHost) return;
        _trainControllerComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).TrainRails.Add(trainTrail);
    }

    private void SpawnPillar(Vector2 obstaclePosition, MapVisualInfoConfig buildingVisualConfig) {
        SpawnPillarObjectForClients(obstaclePosition,
            Random.Range(0, buildingVisualConfig.MapObstacles.Length));
        if (Random.value > 0.8f) {
            int randomBarrel = Random.Range(0, _sceneService.Value._mapEventsContentView._barrelPrefabs.Length);
            var needObstacle = SpawnNetworkObstacle(obstaclePosition + (Random.insideUnitCircle * 5f).normalized * 2.5f,
                _sceneService.Value._mapEventsContentView._barrelPrefabs[randomBarrel]);
            if (!needObstacle) return;
            needObstacle.GetComponent<NetworkObject>().TrySetParent(_netcodeSceneService.Value.networkObjectsMapContainer);
            int barrelEntity = _world.Value.NewEntity();
            _carPartInfoComponentsPool.Value.Add(barrelEntity).CurrentHealth = 150;
            ref var carPartCmp = ref _carPartComponentsPool.Value.Add(barrelEntity);
            carPartCmp.MainEntity = -11 - randomBarrel;
            carPartCmp.CellCarPartsStuffViews = new List<CarPartStuffView>();

            carPartCmp.CellCarPartsStuffViews.Add(needObstacle.gameObject.GetComponent<CarPartStuffView>());

            carPartCmp.CellCarPartsStuffViews[0].SetupCarPart(Vector2Int.one * (-11 - randomBarrel), "Obstacle", barrelEntity,_netcodeSceneService.Value.OwnerClientId);
        }
    }

    
    private void EnterInQuicksand(Vector2Int gridPosition, int mainEntity, int quicksandEntity) {
        var carPartComponent = _carComponentsPool.Value.Get(mainEntity).CarPartContainerGroupsGrid[gridPosition.x, gridPosition.y];
        if (_movementComponentsPool.Value.Has(mainEntity) && _carPartInfoComponentsPool.Value.Has(carPartComponent) &&
            _sceneService.Value.GetCarPartConfig(_carPartInfoComponentsPool.Value.Get(carPartComponent).CarPartId).PartType ==
            CarPartType.WheelsContainer) {
            if (!_quicksandComponentsPool.Value.Has(mainEntity))
                _quicksandComponentsPool.Value.Add(mainEntity).wheelsCount++;
            else
                _quicksandComponentsPool.Value.Get(mainEntity).wheelsCount++;
        }
    }

    private void ExitFromQuicksand(Vector2Int gridPosition, int mainEntity, int quicksandEntity) {
        var carPartComponent = _carComponentsPool.Value.Get(mainEntity).CarPartContainerGroupsGrid[gridPosition.x, gridPosition.y];
        if (_movementComponentsPool.Value.Has(mainEntity) && _carPartInfoComponentsPool.Value.Has(carPartComponent) &&
            _sceneService.Value.GetCarPartConfig(_carPartInfoComponentsPool.Value.Get(carPartComponent).CarPartId).PartType ==
            CarPartType.WheelsContainer) {
            ref var quicksandCmp = ref _quicksandComponentsPool.Value.Get(mainEntity);
            quicksandCmp.wheelsCount--;
            if (quicksandCmp.wheelsCount <= 0)
                _quicksandComponentsPool.Value.Del(mainEntity);
        }
    }

    private Vector2 GetCornerPositionForBuilding(Vector2 buildingSize, int index) {
        Vector2 needPosition = Vector2.zero;
        float halfWidth = buildingSize.x / 2f;
        float halfDepth = buildingSize.y / 2f;

        float sign = index == 1 || index == 2 ? -1f : 1f;
        needPosition.x = sign * Random.Range(halfWidth, 100 - halfWidth);

        float signY = index == 3 || index == 2 ? -1f : 1f;
        needPosition.y = signY * Random.Range(halfDepth, 100 - halfDepth);

        if (halfDepth + 20 > needPosition.y && halfWidth + 20 > needPosition.x) {
            bool isXFix = Random.value > 0.5f;
            if (isXFix)
                needPosition.x += sign * 20;
            else
                needPosition.y += signY * 20;
        }
        
        return needPosition;
    }

    private Transform GenerateBuilding() {
      return  _sceneService.Value.InstantiateObject(_sceneService.Value._mapGroundSpriteRenderer.transform, _sceneService.Value._mapContainer, Vector2.zero);
        
    }

    private void GenerateQuicksandInCell(int cellX, int cellY, Vector2 randomOffsetForNoise, Transform[] obstacles) {
        float noiseValue = PerlinNoiseUtil.GetFractalNoise(
            cellX * 20 + 20 / 2f + randomOffsetForNoise.x, cellY * 20 + 20 / 2f + randomOffsetForNoise.y);
        if (noiseValue > 0.4f) return;
        Vector3 randomPosition = GetRandomPositionInCell(cellX, cellY);
        bool canPlaceObstacle = AreAllNeighborsNonZero(_currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MapGrid,
            Mathf.FloorToInt((randomPosition.x + 100) / 5), Mathf.FloorToInt((randomPosition.y + 100) / 5));
        Debug.Log(canPlaceObstacle + " canPlace");
        if (canPlaceObstacle)
            SpawnQuicksandObjectForClients(randomPosition, Random.Range(0, obstacles.Length));
    }

    private void SetQuicksandMethods(Transform quicksand) {
        quicksand?.GetComponent<QuicksandView>().Construct(EnterInQuicksand, ExitFromQuicksand, 0);
    }
    
    private void GenerateObstaclesInCell(int cellX, int cellY, Vector2 randomOffsetForNoise)
    {
        float noiseValue = PerlinNoiseUtil.GetFractalNoise(
            cellX * 20 + 20 / 2f+randomOffsetForNoise.x,
            cellY * 20 + 20 / 2f+randomOffsetForNoise.y 
        );
        var mapGrid = _currentGameComponentsPool.Value.Get(_sceneService.Value.PlayerEntity).MapGrid;
        var mapVisualConfig = _betweenScenesService.Value.GetMapConfig().MapVisualInfoConfigs[0];
        int obstacleCount = GetObstacleCountFromNoise(noiseValue, mapVisualConfig.NoiseObstaclesCountChances);

        for (int k = 0; k < obstacleCount; k++) {
            Vector3 randomPosition = GetRandomPositionInCell(cellX, cellY);
            bool canPlaceObstacle = AreAllNeighborsNonZero(mapGrid,Mathf.FloorToInt((randomPosition.x+100)/5),Mathf.FloorToInt((randomPosition.y+100)/5));
            
            if (canPlaceObstacle)
                SpawnMapObstacle(randomPosition, Random.Range(0, mapVisualConfig.MapObstacles.Length));
        }
    }

    private Transform SpawnNetworkObstacle(Vector2 position, NetworkObject obstaclePrefab) {
        if ((position.x < 10 && position.x > -10) && (position.y < 10 && position.y > -10)) return null;

        Quaternion randomRotation = Quaternion.Euler(0f, 0f, Random.Range(0, 4) * 90);

        NetworkObject obstacle = _netcodeSceneService.Value.InstantiateObject(true, obstaclePrefab, new Vector3(position.x, position.y, 0f),
            randomRotation);

        return obstacle.transform;
    }

    private void SpawnMapObstacle(Vector2 position, int prefabIndex) {
        if ((position.x < 10 && position.x > -10) && (position.y < 10 && position.y > -10)) return;
   
        NetcodeBetweenScenesService.Instance.SpawnMapObstacleClientRpc(prefabIndex, Random.Range(0, 4),
            new Vector3(position.x, position.y, 0f));
    }
    private Transform SpawnObstacle(Vector2 position, Transform obstaclePrefab) {
        if ((position.x < 10 && position.x > -10) && (position.y < 10 && position.y > -10)) return null;
        // Transform obstaclePrefab = _sceneService.Value._mountains[Random.Range(0, _sceneService.Value._mountains.Length)];

        Quaternion randomRotation = Quaternion.Euler(0f, 0f, Random.Range(0, 4) * 90);

        Transform obstacle =
            _sceneService.Value.InstantiateObject(obstaclePrefab, _sceneService.Value._mapContainer, new Vector3(position.x, position.y, 0f));
          obstacle.parent = _sceneService.Value._mapContainer;
        obstacle.localRotation = randomRotation;
        return obstacle;
    }
    private void SpawnPillarObjectForClients(Vector2 position, int prefabIndex) {
        if ((position.x < 10 && position.x > -10) && (position.y < 10 && position.y > -10)) return ;
        
        NetcodeBetweenScenesService.Instance.SpawnMapPillarClientRpc(prefabIndex, Random.Range(0, 4),
            new Vector3(position.x, position.y, 0f));
    }
    
    private void SpawnQuicksandObjectForClients(Vector2 position, int quicksandIndex) {
        if ((position.x < 10 && position.x > -10) && (position.y < 10 && position.y > -10)) return ;
        
        NetcodeBetweenScenesService.Instance.SpawnQuicksandClientRpc(quicksandIndex, Random.Range(0, 4),
            new Vector3(position.x, position.y, 0f));
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

    private int GetObstacleCountFromNoise(float noiseValue, float[] chances) {
        for (int k = 0; k < chances.Length; k++) {
            if (noiseValue < chances[k])
                return k;
        }

        /* if (noiseValue < 0.47f) return 0;
         else if (noiseValue < 0.7f) return 1;
         else if (noiseValue < 0.9f) return 2;*/
        return 0;
    }

    private void SetCoopUI() {
        if (!_betweenScenesService.Value.IsCoop) {
            _sceneService.Value._inventoryUIView._lobbyPlayersUI[0].transform.parent.gameObject.SetActive(false);
            return;
        }
        for (int i = 0; i < _sceneService.Value._inventoryUIView._lobbyPlayersUI.Length; i++) {
            _sceneService.Value._inventoryUIView._lobbyPlayersUI[i].gameObject.SetActive(NetcodeBetweenScenesService.Instance.playerReadyStates.Count - 2 >= i);
        }

        UpdateLobbyUI();
    }

    private void UpdateLobbyUI() {
        int index = 0;
        for (int i = 0; i < NetcodeBetweenScenesService.Instance.playerReadyStates.Count; i++) {
            var playerState = NetcodeBetweenScenesService.Instance.playerReadyStates[i];
            if (NetworkManager.Singleton.LocalClientId != playerState.ClientId) {
                _sceneService.Value._inventoryUIView._lobbyPlayersUI[index].ChangeCarSprite(
                    BetweenScenesService.Instance._carStuffListsConfig.CarConfigs[playerState.PlayerCarId].CarIcon, playerState.GetNickname(),
                    playerState.IsReady);
                index++;
            } else
                _sceneService.Value._inventoryUIView._playButtonText.text =
                    playerState.IsReady ? GetLocalization("Ready") + "!" : GetLocalization("Not ready") + "!";
        }
    }
    private void ChangeInventoryState(bool state) => _sceneService.Value._inventoryUIView.gameObject.SetActive(state);
    private void ChangePlayerUIState(bool state) => _sceneService.Value._playerUIView.gameObject.SetActive(state);
}