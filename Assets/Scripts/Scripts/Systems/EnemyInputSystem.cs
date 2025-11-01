using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using UnityEngine;

namespace Client {
    sealed class EnemyInputSystem : IEcsRunSystem {        
        private EcsWorldInject _world;
        private EcsCustomInject<SceneService> _sceneData;

        private EcsPoolInject<FuelComponent> _fuelComponentsPool;
        private EcsPoolInject<CarComponent> _carComponentsPool;
        private EcsPoolInject<MovementComponent> _movementComponentsPool;
        
        private EcsFilterInject<Inc<MovementComponent>> _movementComponentsFilter;

        public void Run(IEcsSystems systems) {
            foreach (var entity in _movementComponentsFilter.Value) {
                if(entity == _sceneData.Value.PlayerEntity) continue;
                ref var moveCmp = ref _movementComponentsPool.Value.Get(entity);
              /*  if(_movementComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).MovableObjectRigidbody != null)
                moveCmp.TargetPosition = _movementComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).MovableObjectRigidbody.position;*/
            }
        }
    }
}