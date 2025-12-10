using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

sealed class TutorialSystem : IEcsRunSystem, IEcsInitSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;

    private EcsPoolInject<UpdateCarsStatsInfoEvent> _updateCarsStatsInfoEventsPool;

    private EcsFilterInject<Inc<UpdateShopToSaleRoundEvent>> _updateShopToSaleRoundEventsFilter;

    public void Init(IEcsSystems systems) {
        _sceneData.Value._tutorialStuffView._shopContainer.gameObject.SetActive(false);
        _sceneData.Value._tutorialStuffView._sortButtonsContainer.gameObject.SetActive(false);
    }

    public void Run(IEcsSystems systems) { }
}