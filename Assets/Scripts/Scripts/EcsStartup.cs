using System;
using Client;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using UnityEngine;

public class EcsStartUp : MonoBehaviour
{
    [SerializeField] private NetcodeSceneService _netcodeSceneService;
    [SerializeField] private SceneService _sceneService;
    [SerializeField] private BetweenScenesService _betweenSceneService;
    

    EcsWorld _world;
    IEcsSystems _systemsUpdate;
    IEcsSystems _systemsFixedUpdate;

    private void Awake() {
        Time.timeScale = 1;
    }

    void Start() {
        _betweenSceneService = BetweenScenesService.Instance;
        
        _world = new EcsWorld();
        _systemsUpdate = new EcsSystems(_world);
        _systemsFixedUpdate = new EcsSystems(_world);

        _systemsUpdate
#if UNITY_EDITOR
            .Add(new Leopotam.EcsLite.UnityEditor.EcsWorldDebugSystem())
#endif

            .Add(new GameStatesSystem()).Add(new InventorySystem()).Add(new MovementSystem()).Add(new PlayerInputSystem())
            .DelHere<DestroyEntityEvent>().Add(new EnemyInputSystem()).Add(new AttackSystem()).Add(new DestroyDontUsedEntitesSystem());

        if (!DataPersistenceManagerSystem._gameData.TutorialIsCompleted) {
            _systemsUpdate.Add(new TutorialSystem());
        }

        _systemsUpdate.DelHere<ConstructMeleeCellEvent>().DelHere<StartRoundEvent>().DelHere<EndRoundEvent>()
            .Inject(_sceneService).Inject(_betweenSceneService).Inject(_netcodeSceneService).Init();
        
        
        _systemsFixedUpdate.Add(new ForwardWheelsRotateSystem()).Inject(_sceneService).Inject(_betweenSceneService).Inject(_netcodeSceneService).Init();
    }
    void Update()
    {
        _systemsUpdate?.Run();
    }
    void FixedUpdate()
    {
        _systemsFixedUpdate?.Run();
    }
    void OnDestroy()
    {
        if (_systemsUpdate != null)
        {
            _systemsUpdate.Destroy();
            _systemsUpdate = null;
        }
        if (_systemsFixedUpdate != null)
        {
            _systemsFixedUpdate.Destroy();
            _systemsFixedUpdate = null;
        }
        if (_world != null)
        {
            _world.Destroy();
            _world = null;
        }
    }
}
