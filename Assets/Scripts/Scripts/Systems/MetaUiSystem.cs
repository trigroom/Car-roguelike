using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

sealed class MetaUiSystem : IEcsInitSystem {
        private EcsWorldInject _world;
        private EcsCustomInject<MetaSceneService> _metaSceneService;
        private EcsCustomInject<BetweenScenesService> _betweenSceneService;
        public void Init (IEcsSystems systems) {
            for (int i = 0; i < _metaSceneService.Value._selectCarButtonViews.Length; i++) {
                var carConfig = _betweenSceneService.Value._carStuffListsConfig.CarConfigs[i];
                _metaSceneService.Value._selectCarButtonViews[i].SelectCarSetup(SelectCar, carConfig.CarName,i);
            }
        }
        
        public void SelectCar(int carId) {
            
            _betweenSceneService.Value.CurrentSelectedCar = carId;
        }
    }
