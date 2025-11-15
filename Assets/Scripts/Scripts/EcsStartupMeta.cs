using System;
using Client;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using UnityEngine;

public class EcsStartUpMeta : MonoBehaviour
{
    [SerializeField] private MetaSceneService _metaSceneService;
    [SerializeField] private BetweenScenesService _betweenSceneService;
    

    EcsWorld _world;
    IEcsSystems _systemsUpdate;

    private void Awake() {
        Time.timeScale = 1;
    }

    void Start()
    {
        _betweenSceneService = BetweenScenesService.Instance;
        _world = new EcsWorld();
        _systemsUpdate = new EcsSystems(_world);

        _systemsUpdate
#if UNITY_EDITOR
            .Add(new Leopotam.EcsLite.UnityEditor.EcsWorldDebugSystem())
#endif 
            .Add(new MetaUiSystem())
        
         .Inject(_metaSceneService)
            .Inject(_betweenSceneService)
         .Init();
    }
    void Update()
    {
        _systemsUpdate?.Run();
    }
    void OnDestroy()
    {
        if (_systemsUpdate != null)
        {
            _systemsUpdate.Destroy();
            _systemsUpdate = null;
        }
        if (_world != null)
        {
            _world.Destroy();
            _world = null;
        }
    }
}
