using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using UnityEngine;

sealed class PlayerInputSystem : IEcsRunSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;

    private EcsPoolInject<FuelComponent> _fuelComponentsPool;
    private EcsPoolInject<CarComponent> _carComponentsPool;
    private EcsPoolInject<MovementComponent> _movementComponentsPool;

    public void Run(IEcsSystems systems) {
        ref var fuelCmp = ref _fuelComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var moveCmp = ref _movementComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        if (moveCmp.MovableObjectRigidbody == null) return;
        moveCmp.TargetPosition = _sceneData.Value.mainCamera.ScreenToWorldPoint(Input.mousePosition);
        
        if (Input.GetMouseButtonDown(0) && !fuelCmp.UseSpeedBoost && fuelCmp.CurrentFuel > 0) 
            fuelCmp.UseSpeedBoost = true;

        if ((Input.GetMouseButtonUp(0) || fuelCmp.CurrentFuel <= 0) && fuelCmp.UseSpeedBoost) 
            fuelCmp.UseSpeedBoost = false;
    }
}
