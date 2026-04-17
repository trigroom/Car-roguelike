using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using Unity.Netcode;
using UnityEngine;

sealed class PlayerInputSystem : IEcsRunSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;
    private EcsCustomInject<NetcodeSceneService> _netcodeSceneService;

    private EcsPoolInject<FuelComponent> _fuelComponentsPool;
    private EcsPoolInject<CurrentGameComponent> _currentGameComponentsPool;
    private EcsPoolInject<CarComponent> _carComponentsPool;
    private EcsPoolInject<MovementComponent> _movementComponentsPool;

    public void Run(IEcsSystems systems) {
        ref var fuelCmp = ref _fuelComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        ref var moveCmp = ref _movementComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);

        if (NetworkManager.Singleton.IsServer) {
            if (moveCmp.MovableObjectRigidbody == null) return;
            moveCmp.TargetPosition = _sceneData.Value.mainCamera.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButtonDown(0) && !fuelCmp.UseSpeedBoost && fuelCmp.CurrentFuel > 0)
                fuelCmp.UseSpeedBoost = true;

            if ((Input.GetMouseButtonUp(0) || fuelCmp.CurrentFuel <= 0) && fuelCmp.UseSpeedBoost)
                fuelCmp.UseSpeedBoost = false;
        } else {
            if (!RoundUtilits.IsBattleRound(_currentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRoundType)) return;
            moveCmp.TickServerTime += Time.deltaTime;
            if (moveCmp.TickServerTime > 0.07f) {
                moveCmp.TickServerTime = 0;
bool IsUseSpeedBoost = Input.GetMouseButton(0);
Debug.Log(IsUseSpeedBoost + "IsUseSpeedBoostNow");
                _netcodeSceneService.Value.SetCarMovementServerRpc(_sceneData.Value.mainCamera.ScreenToWorldPoint(Input.mousePosition),
                    IsUseSpeedBoost);
            }
        }
    }
}
