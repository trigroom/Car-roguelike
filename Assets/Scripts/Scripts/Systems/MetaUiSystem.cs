using System.Collections.Generic;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;
using UnityEngine.SceneManagement;

sealed class MetaUiSystem : IEcsInitSystem {
        private EcsWorldInject _world;
        private EcsCustomInject<MetaSceneService> _metaSceneService;
        private EcsCustomInject<BetweenScenesService> _betweenSceneService;
        
        private EcsPoolInject<MetaUIComponent> _metaUIComponentsPool;
        public void Init (IEcsSystems systems) {
            _betweenSceneService.Value.HideBlackScreen();
            Time.timeScale = 1;
            _metaSceneService.Value.MainEntity = _world.Value.NewEntity();
            ref var metaUiComponent = ref _metaUIComponentsPool.Value.Add(_metaSceneService.Value.MainEntity);
            metaUiComponent.CurrentOpenedWindow =
                _metaSceneService.Value._mainMenuView.transform;
            metaUiComponent.CurrentOpenedAchievementId = -1;
            metaUiComponent.CurrentRewardedAchievementIds = new List<int>();
            SetupAchievements();
            SetupShopButtons();
            _metaSceneService.Value._scrapCountText.text = DataPersistenceManagerSystem._gameData.ScrapCount.ToString();
            for (int i = 0; i < _metaSceneService.Value._selectCarButtonViews.Length; i++) {
                if (DataPersistenceManagerSystem._gameData.CarsUnlocked[i]) {
                    var carConfig = _betweenSceneService.Value._carStuffListsConfig.CarConfigs[i];
                    _metaSceneService.Value._selectCarButtonViews[i].SelectCarSetup(SelectCar, carConfig.CarName, i);
                } else
                    _metaSceneService.Value._selectCarButtonViews[i].gameObject.SetActive(false);
               
            }

            for (int i = 0; i < _metaSceneService.Value._selectMapButtons.Length; i++) {
                int mapIndex = i;
                var selectMapButton = _metaSceneService.Value._selectMapButtons[i];
                selectMapButton.SetupSelectMapButton(SelectMap,mapIndex);
                selectMapButton.SetMapName(_betweenSceneService.Value._carStuffListsConfig.MapConfigs[mapIndex].MapName);
            }

            for (int i = 0; i < _metaSceneService.Value.selectMapDifficultyUIView.DifficultyButtons.Length; i++) {
                int difficultyIndex = i;
                _metaSceneService.Value.selectMapDifficultyUIView.DifficultyButtons[i].onClick.AddListener(() => SelectMapDifficulty(difficultyIndex));
            }

            foreach (var backButton in _metaSceneService.Value._backToMainMenuButtons) {
                backButton.onClick.AddListener(() =>
                    OpenNewWindow(_metaSceneService.Value._mainMenuView.transform));
            }
            
            _metaSceneService.Value.selectMapDifficultyUIView.backButton.onClick.AddListener(() =>
                OpenNewWindow(_metaSceneService.Value._selectMapUIContainer.transform));
            _metaSceneService.Value._backToCarSelectButton.onClick.AddListener(() =>
                OpenNewWindow(_metaSceneService.Value._selectCarUIContainer.transform));
            _metaSceneService.Value._mainMenuView._achivementsButton.onClick.AddListener(() =>
                OpenNewWindow(_metaSceneService.Value._achievementsUIView.transform));
            _metaSceneService.Value._mainMenuView._shopButton.onClick.AddListener(() =>
                OpenNewWindow(_metaSceneService.Value._shopUIContainer.transform));
            _metaSceneService.Value._mainMenuView._playButton.onClick.AddListener(Play);
            _metaSceneService.Value._mainMenuView._exitButton.onClick.AddListener(ExitFromGame);
            _metaSceneService.Value._staticTextsView.SetTextLanguage();
            SetupLocalizationButtons();
            _metaSceneService.Value._achievementsUIView.NextRewardedAchievementButton.onClick.AddListener(SetNextAchievementInfo);

            if (!DataPersistenceManagerSystem._gameData.TutorialIsCompleted) {
                _metaSceneService.Value._mainMenuView._achivementsButton.gameObject.SetActive(false);
                _metaSceneService.Value._mainMenuView._shopButton.gameObject.SetActive(false);
            }

            
        }

        private void SetNextAchievementInfo() {
            ref var metaUiComponent = ref _metaUIComponentsPool.Value.Get(_metaSceneService.Value.MainEntity);
            if (metaUiComponent.CurrentRewardedAchievementIds.Count == 0) {
                _metaSceneService.Value._achievementsUIView.AchievementsRewardContainer.gameObject.SetActive(false);
            } else {
                var achievementConfig =
                    _betweenSceneService.Value._carStuffListsConfig.AchievementConfigs[metaUiComponent.CurrentRewardedAchievementIds[0]];
                metaUiComponent.CurrentRewardedAchievementIds.RemoveAt(0);
                _metaSceneService.Value._achievementsUIView.SetRewardAchievementInfoPanel(achievementConfig);
            }
        }

        private void SetupShopButtons() {
             ref var metaUiComponent = ref _metaUIComponentsPool.Value.Get(_metaSceneService.Value.MainEntity);
             //close shop panel info
            for (int i = 0; i < DataPersistenceManagerSystem._gameData.ShopItemsBuyed.Length; i++) {
              
                if (!DataPersistenceManagerSystem._gameData.ShopItemsBuyed[i]) {
                    var shopItemConfig = _betweenSceneService.Value._carStuffListsConfig.ShopItemConfigs[i];
                    var shopButtonView = _metaSceneService.Value._shopUIContainer.GetMetaShopButtonView();
                    int index = i;
                    shopButtonView.BuyButton.onClick.AddListener(() => BuyMetaShopItem(index, shopButtonView.transform));
                    shopButtonView.Construct(i, null, ShowMetaShopItemInfo, null, null);
                    SetShopCellIcon(shopButtonView, shopItemConfig);
                }
//hide shop button after buy
              
            }
        }

        private void SetShopCellIcon(ShopButtonView shopButtonView, ShopItemConfig shopItemConfig) {
            Sprite sprite = _betweenSceneService.Value._carStuffListsConfig.CarPartConfigs[0].PartSprite;
            Sprite upperSprite =null;
            Vector2 upperImageSpritePos = Vector2.zero;
            Vector2 upperImageSize = Vector2.zero;
            switch (shopItemConfig.RewardType) {
                case AchievementRewardType.CarPart:
                    var carPartConfig = _betweenSceneService.Value._carStuffListsConfig.CarPartConfigs[shopItemConfig.RewardCount];
                    sprite = carPartConfig.PartSprite;
                    (upperSprite, upperImageSize, upperImageSpritePos) = GridUtils.GetUpperPartSizesForUI(
                        shopButtonView.ItemSprite.rectTransform.sizeDelta, carPartConfig);
                    break;
                case AchievementRewardType.Car:
                    break;
                case AchievementRewardType.Module:
                    var moduleConfig = _betweenSceneService.Value._carStuffListsConfig.ModuleConfigs[shopItemConfig.RewardCount];
                    sprite = moduleConfig.ModuleIcon;
                    break;
            }
            shopButtonView.SetShopCellInfo(shopItemConfig.ScrapCost, sprite,upperSprite,upperImageSpritePos,upperImageSize);
        }

        private void BuyMetaShopItem(int itemIndex, Transform shopButtonView) {
            var shopItemConfig = _betweenSceneService.Value._carStuffListsConfig.ShopItemConfigs[itemIndex];
            if (shopItemConfig.ScrapCost > DataPersistenceManagerSystem._gameData.ScrapCount) return;
            shopButtonView.gameObject.SetActive(false);
            DataPersistenceManagerSystem._gameData.ScrapCount -= shopItemConfig.ScrapCost;
            GiveAchievementReward(shopItemConfig.RewardType, shopItemConfig.RewardCount);
            DataPersistenceManagerSystem._gameData.ShopItemsBuyed[itemIndex] = true;
        }
        private void ShowMetaShopItemInfo(int itemIndex, bool isShow) {
            
        }

        private void SetupAchievements() {
            ref var metaUiComponent = ref _metaUIComponentsPool.Value.Get(_metaSceneService.Value.MainEntity);
            _metaSceneService.Value._achievementsUIView.BackFromAchievementButton.onClick.AddListener(() => {
                _metaSceneService.Value._achievementsUIView.CloseAchievementInfoPanel();
                _metaUIComponentsPool.Value.Get(_metaSceneService.Value.MainEntity).CurrentOpenedAchievementId = -1;
            });
            for (int i = 0; i < DataPersistenceManagerSystem._gameData.AchievementsCompleted.Length; i++) {
                var achievementConfig = _betweenSceneService.Value._carStuffListsConfig.AchievementConfigs[i];
                if (!DataPersistenceManagerSystem._gameData.AchievementsCompleted[i]) {
                    if (CheckAchievementCount(achievementConfig)) {
                        Debug.Log("Achievement Completed");
                        metaUiComponent.CurrentRewardedAchievementIds.Add(i);
                        GiveAchievementReward(achievementConfig.rewardType, achievementConfig.rewardCount);
                        DataPersistenceManagerSystem._gameData.AchievementsCompleted[i] = true;
                    }
                }

                var achievementStuffView = _metaSceneService.Value._achievementsUIView.GetAchievementStuffView();

                achievementStuffView.achievementInfoButton.onClick.AddListener(() => SetAchievementInfoPanel(achievementConfig));
                if (!DataPersistenceManagerSystem._gameData.AchievementsCompleted[i])
                    achievementStuffView.SetAchievementInfo(achievementConfig, GetCountForAchievement(achievementConfig.achievementType));
                else
                    achievementStuffView.CompleteAchievement(achievementConfig);
            }

            if (metaUiComponent.CurrentRewardedAchievementIds.Count != 0) {
                var achievementConfig =
                    _betweenSceneService.Value._carStuffListsConfig.AchievementConfigs[metaUiComponent.CurrentRewardedAchievementIds[0]];
                metaUiComponent.CurrentRewardedAchievementIds.RemoveAt(0);
                _metaSceneService.Value._achievementsUIView.SetRewardAchievementInfoPanel(achievementConfig);
            }

            DataPersistenceManagerSystem.SaveGame();
        }

        private void SetAchievementInfoPanel(AchivementConfig achievement) {
            ref var metaUICmp = ref _metaUIComponentsPool.Value.Get(_metaSceneService.Value.MainEntity);
            if (metaUICmp.CurrentOpenedAchievementId == achievement.achievementId) {
                _metaSceneService.Value._achievementsUIView.CloseAchievementInfoPanel();
                metaUICmp.CurrentOpenedAchievementId = -1;
                return;
            }
            metaUICmp.CurrentOpenedAchievementId = achievement.achievementId;
            _metaSceneService.Value._achievementsUIView.SetAchievementInfoPanel(achievement);
        }

        private float GetCountForAchievement(AchievementType type) {
            switch (type) {
                case AchievementType.CarKills:
                    return  DataPersistenceManagerSystem._gameData.BrokenCars;
                    break;
                case AchievementType.DistanceDrived:
                    return  DataPersistenceManagerSystem._gameData.DistanceDrived;
                    break;
                case AchievementType.PlayerDamage:
                    return DataPersistenceManagerSystem._gameData.PlayerDamage;
                    break;
                case AchievementType.GetMoney:
                    return  DataPersistenceManagerSystem._gameData.MoneyEarnedInGame;
                    break;
                case AchievementType.CompleteMap:
                    return 0;
                    break;
                default: return 0;
            }
        }

        private void GiveAchievementReward(AchievementRewardType rewardType,int count) {
            switch (rewardType) {
                case AchievementRewardType.Car:
                    DataPersistenceManagerSystem._gameData.CarsUnlocked[count] = true;
                    break;
                case AchievementRewardType.CarPart:
                    DataPersistenceManagerSystem._gameData.CarPartsUnlocked[count] = true;
                    break;
                case AchievementRewardType.Module:
                    DataPersistenceManagerSystem._gameData.ModulesUnlocked[count] = true;
                    break;
                case AchievementRewardType.Scrap:
                    DataPersistenceManagerSystem._gameData.ScrapCount += count;
                    
                    break;
            }
        }
        private bool CheckAchievementCount(AchivementConfig achievementConfig) {
            switch (achievementConfig.achievementType) {
                case AchievementType.CarKills:
                    return achievementConfig.needCount <= DataPersistenceManagerSystem._gameData.BrokenCars;
                    break;
                case AchievementType.DistanceDrived:
                    return achievementConfig.needCount <= DataPersistenceManagerSystem._gameData.DistanceDrived;
                    break;
                case AchievementType.PlayerDamage:
                    return achievementConfig.needCount <= DataPersistenceManagerSystem._gameData.PlayerDamage;
                    break;
                case AchievementType.GetMoney:
                    return achievementConfig.needCount <= DataPersistenceManagerSystem._gameData.MoneyEarnedInGame;
                    break;
                case AchievementType.CompleteMap:
                    return achievementConfig.needCount <= DataPersistenceManagerSystem._gameData.LevelRounds[achievementConfig.needSecondCount];
                    break;
                default: return false;
            }

            return false;
        }
        private void OpenNewWindow(Transform newWindow) {
            ref var metaUICmp = ref _metaUIComponentsPool.Value.Get(_metaSceneService.Value.MainEntity);
            metaUICmp.CurrentOpenedWindow.gameObject.SetActive(false);
            newWindow.gameObject.SetActive(true);
            metaUICmp.CurrentOpenedWindow = newWindow;
        }
        
        

        private void Play() {
            if (DataPersistenceManagerSystem._gameData.TutorialIsCompleted) {
                OpenNewWindow(_metaSceneService.Value._selectCarUIContainer.transform);
            } else {
                BetweenScenesService.Instance.ShowBlackScreen( ()=>SceneManager.LoadScene("Scenes/SampleScene"));
            }
        }
        private void ExitFromGame() {
                BetweenScenesService.Instance.ShowBlackScreen( Application.Quit);
        }
        
        string[] languages = { "en", "ru", "de", "es", "fr" };
        private void SetupLocalizationButtons() {
            for (int i = 0; i < _metaSceneService.Value._localizationButtons.Length; i++) {
                int langIndex = i;
                _metaSceneService.Value._localizationButtons[langIndex].onClick
                    .AddListener(()=> {
                        ChangeLocalization(langIndex);
                    });
            }
        }

        private void ChangeLocalization(int langIndex) {
            _betweenSceneService.Value.CsvLocalization.ChangeLanguage(languages[langIndex]);
            _metaSceneService.Value._staticTextsView.SetTextLanguage();
            Debug.Log("set to " + languages[langIndex] + " language");
            for (int i = 0; i < _metaSceneService.Value._selectCarButtonViews.Length; i++) {
                var carConfig = _betweenSceneService.Value._carStuffListsConfig.CarConfigs[i];
                _metaSceneService.Value._selectCarButtonViews[i].SetCarButtonText(carConfig.CarName);
            }
            for (int i = 0; i < _metaSceneService.Value._selectMapButtons.Length; i++) {
                int mapIndex = i;
                _metaSceneService.Value._selectMapButtons[i].SetMapName(_betweenSceneService.Value._carStuffListsConfig.MapConfigs[mapIndex].MapName);
            }
            _metaSceneService.Value.selectMapDifficultyUIView.SetLocalizationText(_betweenSceneService.Value.GetMapConfig());
        }
        public void SelectCar(int carId) {
            _betweenSceneService.Value.CurrentSelectedCar = carId;
            OpenNewWindow(_metaSceneService.Value._selectMapUIContainer.transform);
            //go to select map
        }

        public void SelectMap(int mapId) {
            _betweenSceneService.Value.CurrentSelectedMap = mapId;
            OpenNewWindow(_metaSceneService.Value.selectMapDifficultyUIView.transform);
            _metaSceneService.Value.selectMapDifficultyUIView.SetupDifficultyScreen(
                _betweenSceneService.Value._carStuffListsConfig.MapConfigs[_betweenSceneService.Value.CurrentSelectedMap]);
            SelectMapDifficulty(0);
            //go to select diff
        }

        public void SelectMapDifficulty(int difficultyLevel) {
            _betweenSceneService.Value.CurrentSelectedMapDifficlty = difficultyLevel;
            _metaSceneService.Value.selectMapDifficultyUIView.SetDifficultyDescription(_betweenSceneService.Value._carStuffListsConfig
                .MapConfigs[_betweenSceneService.Value.CurrentSelectedMap].MapDifficltyConfigs[difficultyLevel]);
            //SceneManager.LoadScene("Scenes/SampleScene");
        }
    }
