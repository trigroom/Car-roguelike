using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine.SceneManagement;

sealed class MetaUiSystem : IEcsInitSystem {
        private EcsWorldInject _world;
        private EcsCustomInject<MetaSceneService> _metaSceneService;
        private EcsCustomInject<BetweenScenesService> _betweenSceneService;
        public void Init (IEcsSystems systems) {
            for (int i = 0; i < _metaSceneService.Value._selectCarButtonViews.Length; i++) {
                var carConfig = _betweenSceneService.Value._carStuffListsConfig.CarConfigs[i];
                _metaSceneService.Value._selectCarButtonViews[i].SelectCarSetup(SelectCar, carConfig.CarName,i);
            }

            for (int i = 0; i < _metaSceneService.Value._selectMapButtons.Length; i++) {
                int mapIndex = i;
                _metaSceneService.Value._selectMapButtons[i].onClick.AddListener(() => SelectMap(mapIndex));
            }
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
            _metaSceneService.Value._selectMapDifficltyUIView.SetupDifficltyScreen();
            _betweenSceneService.Value.CurrentSelectedMapDifficlty = 0;
            //go to select diff
        }
        
        public void SelectMapDifficlty(int difficltyLevel) {
            _betweenSceneService.Value.CurrentSelectedMapDifficlty = difficltyLevel;
            SceneManager.LoadScene("Scenes/SampleScene");
        }
    }
