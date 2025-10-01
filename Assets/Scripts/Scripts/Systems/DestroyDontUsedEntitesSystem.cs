using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

sealed class DestroyDontUsedEntitesSystem : IEcsRunSystem {
    private EcsWorldInject _world;
    
        private EcsPoolInject<CarComponent> _carComponentsPool;
        private EcsPoolInject<GunComponent> _gunComponentsPool;
        private EcsPoolInject<MovementComponent> _movementComponentsPool;
        
        private EcsFilterInject<Inc<DestroyEntityEvent>> _destroyEntityEventsFilter;
        public void Run (IEcsSystems systems) {
            foreach (var entity in _destroyEntityEventsFilter.Value) {
                _world.Value.DelEntity(entity);
            }
        }
    }