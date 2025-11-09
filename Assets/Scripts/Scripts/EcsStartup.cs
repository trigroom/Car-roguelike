using System;
using Client;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using UnityEngine;

public class EcsStartUp : MonoBehaviour
{
    [SerializeField] private SceneService _sceneService;

    EcsWorld _world;
    IEcsSystems _systemsUpdate;
    IEcsSystems _systemsLateUpdate;

    private void Awake() {
        Time.timeScale = 1;
    }

    void Start()
    {
        _world = new EcsWorld();
        _systemsUpdate = new EcsSystems(_world);
        _systemsLateUpdate = new EcsSystems(_world);

        _systemsUpdate
#if UNITY_EDITOR
            .Add(new Leopotam.EcsLite.UnityEditor.EcsWorldDebugSystem())
#endif 
            
            .Add(new InventorySystem())
            .Add(new GameStatesSystem())
            .Add(new MovementSystem())
            .Add(new PlayerInputSystem())
            .Add(new EnemyInputSystem())
            .Add(new AttackSystem())
            .Add(new DestroyDontUsedEntitesSystem())
           //  .Add(new UiControlSystem())
         //   .Add(new CellsSystem())
            .DelHere<DestroyEntityEvent>()
           .DelHere<ConstructMeleeCellEvent>()
           .DelHere<EndRoundEvent>()
            .DelHere<StartRoundEvent>()

         .Inject(_sceneService)
         .Init();
        _systemsLateUpdate
            .Add(new ForwardWheelsRotateSystem())
            .Inject(_sceneService)
            .Init();
    }
    void Update()
    {
        _systemsUpdate?.Run();
    }
    void LateUpdate()
    {
        _systemsLateUpdate?.Run();
    }
    void OnDestroy()
    {
        if (_systemsUpdate != null)
        {
            _systemsUpdate.Destroy();
            _systemsUpdate = null;
        }
        if (_systemsLateUpdate != null)
        {
            _systemsLateUpdate.Destroy();
            _systemsLateUpdate = null;
        }
        if (_world != null)
        {
            _world.Destroy();
            _world = null;
        }
    }
}
