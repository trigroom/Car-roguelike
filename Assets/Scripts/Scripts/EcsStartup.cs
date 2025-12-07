using System;
using Client;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using UnityEngine;

public class EcsStartUp : MonoBehaviour
{
    [SerializeField] private SceneService _sceneService;
    [SerializeField] private BetweenScenesService _betweenSceneService;
    

    EcsWorld _world;
    IEcsSystems _systemsUpdate;
    IEcsSystems _systemsLateUpdate;

    private void Awake() {
        Time.timeScale = 1;
    }

    void Start()
    {
        _betweenSceneService = BetweenScenesService.Instance;
        _world = new EcsWorld();
        _systemsUpdate = new EcsSystems(_world);
        _systemsLateUpdate = new EcsSystems(_world);

        _systemsUpdate
#if UNITY_EDITOR
            .Add(new Leopotam.EcsLite.UnityEditor.EcsWorldDebugSystem())
#endif 
            
            .Add(new GameStatesSystem())
            .Add(new InventorySystem())
            .Add(new MovementSystem())
            .Add(new PlayerInputSystem())
            .Add(new EnemyInputSystem())
            .Add(new AttackSystem())
            .Add(new DestroyDontUsedEntitesSystem())
      
            .DelHere<DestroyEntityEvent>()
           .DelHere<ConstructMeleeCellEvent>()
            .DelHere<StartRoundEvent>()
            .DelHere<EndRoundEvent>()

         .Inject(_sceneService)
            .Inject(_betweenSceneService)
         .Init();
        _systemsLateUpdate
            .Add(new ForwardWheelsRotateSystem())
            .Inject(_sceneService)
            .Inject(_betweenSceneService)
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
