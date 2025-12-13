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
            Time.timeScale = 1;
            _metaSceneService.Value.MainEntity = _world.Value.NewEntity();
            _metaUIComponentsPool.Value.Add(_metaSceneService.Value.MainEntity).CurrentOpenedWindow =
                _metaSceneService.Value._mainMenuView.transform;
            _metaSceneService.Value._scrapCountText.text = DataPersistenceManagerSystem._gameData.ScrapCount.ToString();
            for (int i = 0; i < _metaSceneService.Value._selectCarButtonViews.Length; i++) {
                var carConfig = _betweenSceneService.Value._carStuffListsConfig.CarConfigs[i];
                _metaSceneService.Value._selectCarButtonViews[i].SelectCarSetup(SelectCar, carConfig.CarName,i);
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
                OpenNewWindow(_metaSceneService.Value._achievementsUIContainer.transform));
            _metaSceneService.Value._mainMenuView._shopButton.onClick.AddListener(() =>
                OpenNewWindow(_metaSceneService.Value._shopUIContainer.transform));
            _metaSceneService.Value._mainMenuView._playButton.onClick.AddListener(Play);
            _metaSceneService.Value._mainMenuView._exitButton.onClick.AddListener(ExitFromGame);
            _metaSceneService.Value._staticTextsView.SetTextLanguage();
            SetupLocalizationButtons();

            if (!DataPersistenceManagerSystem._gameData.TutorialIsCompleted) {
                _metaSceneService.Value._mainMenuView._achivementsButton.gameObject.SetActive(false);
                _metaSceneService.Value._mainMenuView._shopButton.gameObject.SetActive(false);
            }
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
