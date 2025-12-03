using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;
using UnityEngine.SceneManagement;

sealed class MetaUiSystem : IEcsInitSystem {
        private EcsWorldInject _world;
        private EcsCustomInject<MetaSceneService> _metaSceneService;
        private EcsCustomInject<BetweenScenesService> _betweenSceneService;
        public void Init (IEcsSystems systems) {
            Time.timeScale = 1;
            _metaSceneService.Value._scrapCountText.text = DataPersistenceManagerSystem._gameData.ScrapCount.ToString();
            for (int i = 0; i < _metaSceneService.Value._selectCarButtonViews.Length; i++) {
                var carConfig = _betweenSceneService.Value._carStuffListsConfig.CarConfigs[i];
                _metaSceneService.Value._selectCarButtonViews[i].SelectCarSetup(SelectCar, carConfig.CarName,i);
            }

            for (int i = 0; i < _metaSceneService.Value._selectMapButtons.Length; i++) {
                int mapIndex = i;
                _metaSceneService.Value._selectMapButtons[i].onClick.AddListener(() => SelectMap(mapIndex));
            }

            for (int i = 0; i < _metaSceneService.Value.selectMapDifficultyUIView.DifficultyButtons.Length; i++) {
                int difficltyIndex = i;
                _metaSceneService.Value.selectMapDifficultyUIView.DifficultyButtons[i].onClick.AddListener(() => SelectMapDifficulty(difficltyIndex));
            }
            _metaSceneService.Value.selectMapDifficultyUIView.backButton.onClick.AddListener(BackToMapSelect);
            _metaSceneService.Value._backToCarSelectButton.onClick.AddListener(BackToCarSelect);
            
            _metaSceneService.Value._staticTextsView.SetTextLanguage();
            SetupLocalizationButtons();
        }

        string[] languages = { "en", "ru", "de", "es", "fr" };
        private void SetupLocalizationButtons() {
            for (int i = 0; i < _metaSceneService.Value._localizationButtons.Length; i++) {
                int langIndex = i;
                _metaSceneService.Value._localizationButtons[langIndex].onClick
                    .AddListener(()=> {
                        _betweenSceneService.Value.CsvLocalization.ChangeLanguage(languages[langIndex]);
                        _metaSceneService.Value._staticTextsView.SetTextLanguage();
                        Debug.Log("set to "+languages[langIndex] + " language");
                    });
            }
        }
        
        public void BackToCarSelect() {
            _metaSceneService.Value._selectCarUIContainer.gameObject.SetActive(true);
            _metaSceneService.Value._selectMapUIContainer.gameObject.SetActive(false);
        }

        public void BackToMapSelect() {
            _metaSceneService.Value._selectMapUIContainer.gameObject.SetActive(true);
            _metaSceneService.Value.selectMapDifficultyUIView.gameObject.SetActive(false);
        }
        public void SelectCar(int carId) {
            _betweenSceneService.Value.CurrentSelectedCar = carId;
            _metaSceneService.Value._selectCarUIContainer.gameObject.SetActive(false);
            _metaSceneService.Value._selectMapUIContainer.gameObject.SetActive(true);
            //go to select map
        }

        public void SelectMap(int mapId) {
            _betweenSceneService.Value.CurrentSelectedMap = mapId;
            _metaSceneService.Value._selectMapUIContainer.gameObject.SetActive(false);
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
