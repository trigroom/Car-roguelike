using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

sealed class TutorialSystem : IEcsRunSystem, IEcsInitSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;

    private EcsPoolInject<CurrentGameComponent> _сurrentGameComponentsPool;
    private EcsPoolInject<InventoryComponent> _inventoryComponentsPool;
    private EcsPoolInject<CarComponent> _carComponentsPool;
    
    private EcsFilterInject<Inc<EndRoundEvent>> _endRoundEventsFilter;
    private EcsFilterInject<Inc<InventoryCellComponent>> _inventoryCellComponentsFilter;

    public void Init(IEcsSystems systems) {
        _sceneData.Value._tutorialStuffView._shopContainer.gameObject.SetActive(false);
        _sceneData.Value._tutorialStuffView._sortButtonsContainer.gameObject.SetActive(false);
        _sceneData.Value._inventoryUIView._upgradeCarButton.gameObject.SetActive(false);
        _sceneData.Value._inventoryUIView._changeToRedactModeButton.gameObject.SetActive(false);
        _sceneData.Value._shopUIView._shopModeButton.gameObject.SetActive(false);
        _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(false);
        _sceneData.Value._inventoryUIView._playButton.onClick.AddListener(RoundCheck);
        _sceneData.Value._shopUIView._updateItemsText.gameObject.SetActive(false);
        _sceneData.Value._shopUIView._shopModeButton.onClick.AddListener(ShopModeCheck);
        _sceneData.Value._settingsView._mainMenuButton.gameObject.SetActive(false);
    }

    private int _tutorialStep;
    public void Run(IEcsSystems systems) {
        int currentRound = _сurrentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRound;
       
        if (_tutorialStep == 0 && currentRound == 0 && !_sceneData.Value._tutorialStuffView._shopContainer.gameObject.activeInHierarchy &&
            _inventoryCellComponentsFilter.Value.GetEntitiesCount() == 0) {
            _sceneData.Value._inventoryUIView._changeToRedactModeButton.gameObject.SetActive(true);
            _sceneData.Value._tutorialStuffView._shopContainer.gameObject.SetActive(true);
            _sceneData.Value._tutorialStuffView._sortButtonsContainer.gameObject.SetActive(true);
            _tutorialStep = 1;
            //show buy item text
        } else if (_tutorialStep == 1) {
            foreach (var buttonShop in _sceneData.Value._shopUIView._buttonsShop) {
                if (!buttonShop.BuyButton.enabled) {
                    _tutorialStep = 2;
                    //place car part to car text
                    return;
                }
            }
        } else if ((_tutorialStep == 1 || _tutorialStep == 2) &&
                   _inventoryCellComponentsFilter.Value.GetEntitiesCount() != 0) {
            _tutorialStep = 3;
            //hide buy item text
            //show place buy item text
        } 
        else if ((_tutorialStep == 3|| _tutorialStep == 2) &&
                 _inventoryCellComponentsFilter.Value.GetEntitiesCount() == 0) {
            _tutorialStep = 4;
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(true);
            //hide buy item text
            //show place buy item text
        } 
        else if (_tutorialStep == 5 &&_inventoryCellComponentsFilter.Value.GetEntitiesCount() != 0) {
            _tutorialStep = 6;
            //place car parts and click change mode button
           
        }
        else if (_tutorialStep == 6 && !_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).IsCarPartsRedactorMode) {
            _tutorialStep = 7;
            _sceneData.Value._inventoryUIView._changeToRedactModeButton.interactable = false;
        } 
        else if (_tutorialStep == 7 && _inventoryCellComponentsFilter.Value.GetEntitiesCount() == 0) {
            _sceneData.Value._tutorialStuffView._shopContainer.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(true);
            _tutorialStep = 8;
            _sceneData.Value._inventoryUIView._changeToRedactModeButton.interactable = true;
        }
        
        
        else if (_tutorialStep == 9 && _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).PlayerTeammateCarsEntity.Count > 1) {
            _tutorialStep = 10;
            foreach (var buttonShop in _sceneData.Value._shopUIView._buttonsShop) 
                buttonShop.BuyButton.enabled = true;
            _sceneData.Value._shopUIView._shopModeButton.interactable = true;
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._upgradeCarButton.interactable = true;
        }
        //hide upg button for car part info
        _sceneData.Value._carPartInfoUIView._moveToInventoryButton.gameObject.SetActive(_tutorialStep <= 6 ||_tutorialStep > 9);
        _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(_tutorialStep == 6 || _tutorialStep > 9);
        //learn to buy friend car on round 2
    }

    private void RoundCheck() {
        int currentRound = _сurrentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRound;

        if (currentRound > 1) {
            DataPersistenceManagerSystem._gameData.TutorialIsCompleted = true;
            DataPersistenceManagerSystem.SaveGame();
        } else if (currentRound == 0) {
            _sceneData.Value._inventoryUIView._upgradeCarButton.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(false);
            _tutorialStep = 5;
            _sceneData.Value._tutorialStuffView._shopContainer.gameObject.SetActive(false);
            _sceneData.Value._shopUIView._updateItemsText.gameObject.SetActive(true);
            Debug.Log("upgrade car tut");
            //upgrade car text
        } else if (currentRound == 1) {
            _sceneData.Value._inventoryUIView._upgradeCarButton.interactable = false;
            _sceneData.Value._shopUIView._shopModeButton.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(false);
            _tutorialStep = 9;
            foreach (var buttonShop in _sceneData.Value._shopUIView._buttonsShop) {
                buttonShop.BuyButton.enabled = false;
            }
            //buy friend car text
        }

        Debug.Log("cur check tutor lvl " + currentRound);
    }

    private void ShopModeCheck() {
        _sceneData.Value._shopUIView._shopModeButton.enabled = false;
        _sceneData.Value._shopUIView._shopModeButton.onClick.RemoveListener(ShopModeCheck);
        //buy car text
    }
}