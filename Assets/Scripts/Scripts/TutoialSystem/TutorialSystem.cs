using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using TMPro;
using UnityEngine;

sealed class TutorialSystem : IEcsRunSystem, IEcsInitSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;

    private EcsPoolInject<CurrentGameComponent> _сurrentGameComponentsPool;
    private EcsPoolInject<InventoryComponent> _inventoryComponentsPool;
    private EcsPoolInject<InventoryCellComponent> _inventoryCellComponentsPool;
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
        _sceneData.Value._tutorialStuffView.TutorialText = _sceneData.Value
            .InstantiateObject(_sceneData.Value._tutorialStuffView._tutorialTextPrefab.transform, _sceneData.Value._inventoryUIView.transform)
            .GetComponent<TMP_Text>();
        var textPos = new Vector2(
            _sceneData.Value._inventoryUIView._carPartsContainer.position.x,
            _sceneData.Value._inventoryUIView._carPartsContainer.position.y + 200*_sceneData.Value.mainCanvas.localScale.x);
        _sceneData.Value._tutorialStuffView.HideText(false,textPos,"Place car parts to car redactor field");
        //remove listeners from play again button
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
            _sceneData.Value._tutorialStuffView.HideText(false);
            var shopTextPosition = _sceneData.Value._tutorialStuffView._shopText.position;
            _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects("Buy car part in shop",
                new Vector2(shopTextPosition.x, shopTextPosition.y - 100*_sceneData.Value.mainCanvas.localScale.x), 30);
            //show buy item text
        } else if (_tutorialStep == 1) {
            foreach (var buttonShop in _sceneData.Value._shopUIView._buttonsShop) {
                if (!buttonShop.BuyButton.enabled) {
                    _tutorialStep = 2;
                    _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects("Place car part to car",
                        new Vector2(_sceneData.Value._inventoryUIView._carPartsContainer.position.x,
                            _sceneData.Value._inventoryUIView._carPartsContainer.position.y + 200*_sceneData.Value.mainCanvas.localScale.x), 30);
                    //place car part to car text
                    return;
                }
            }
        } else if ((_tutorialStep == 1 || _tutorialStep == 2) && _inventoryCellComponentsFilter.Value.GetEntitiesCount() != 0) {
            _tutorialStep = 3;
            _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects("Go play and select round type",
                new Vector2(_sceneData.Value._inventoryUIView._carPartsContainer.position.x,
                    _sceneData.Value._inventoryUIView._carPartsContainer.position.y + 200*_sceneData.Value.mainCanvas.localScale.x), 30);
            //hide buy item text
            //show place buy item text
        } else if ((_tutorialStep == 3 || _tutorialStep == 2) && _inventoryCellComponentsFilter.Value.GetEntitiesCount() == 0) {
            _tutorialStep = 4;
            _sceneData.Value._tutorialStuffView.HideText(true);
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(true);
            //hide buy item text
        } else if (_tutorialStep == 5 && _inventoryCellComponentsFilter.Value.GetEntitiesCount() != 0 ) {
            foreach (var inventoryCellEntity in _inventoryCellComponentsFilter.Value) {
                if (_sceneData.Value.GetCarPartConfig(_inventoryCellComponentsPool.Value.Get(inventoryCellEntity).CarPartId).PartType ==
                    CarPartType.CarStuffContainer) {
                    _tutorialStep = 6;
                    var carPartRedactModeButtonPosition = _sceneData.Value._inventoryUIView._changeToRedactModeButton.transform.position;
                    _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects("Click change mode button",
                        new Vector2(carPartRedactModeButtonPosition.x,
                            carPartRedactModeButtonPosition.y + 100 * _sceneData.Value.mainCanvas.localScale.x), 30);
                }
            }

            //place car parts and click change mode button
        } else if (_tutorialStep == 6 && !_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).IsCarPartsRedactorMode) {
            _tutorialStep = 7;
            _sceneData.Value._inventoryUIView._changeToRedactModeButton.interactable = false;
            _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects("Place new car parts",
                new Vector2(_sceneData.Value._inventoryUIView._carPartsContainer.position.x,
                    _sceneData.Value._inventoryUIView._carPartsContainer.position.y + 200*_sceneData.Value.mainCanvas.localScale.x), 20);
        } else if (_tutorialStep == 7 && _inventoryCellComponentsFilter.Value.GetEntitiesCount() == 0) {
            _sceneData.Value._tutorialStuffView._shopContainer.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(true);
            _tutorialStep = 8;
            _sceneData.Value._inventoryUIView._changeToRedactModeButton.interactable = true;
            _sceneData.Value._tutorialStuffView.HideText(true);
        } else if (_tutorialStep == 9 && _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).PlayerTeammateCarsEntity.Count > 1) {
            _tutorialStep = 10;
            foreach (var buttonShop in _sceneData.Value._shopUIView._buttonsShop) {
                buttonShop.BuyButton.enabled = true;
                buttonShop.enabled = true;
            }
                
            _sceneData.Value._shopUIView._shopModeButton.interactable = true;
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._upgradeCarButton.interactable = true;
            var shopTextPosition = _sceneData.Value._tutorialStuffView._shopText.position;
            _sceneData.Value._shopUIView._updateItemsText.gameObject.SetActive(false);
            _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects("Go win last round",
                new Vector2(shopTextPosition.x, shopTextPosition.y - 50 * _sceneData.Value.mainCanvas.localScale.x), 20);
        }

        _sceneData.Value._carPartInfoUIView._moveToInventoryButton.gameObject.SetActive(_tutorialStep <= 6 || _tutorialStep > 9);
        _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(_tutorialStep == 6 || _tutorialStep > 9);
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
            
            var textPos = new Vector2(
                _sceneData.Value._inventoryUIView._upgradeCarButton.transform.position.x,
                _sceneData.Value._inventoryUIView._upgradeCarButton.transform.position.y + 100*_sceneData.Value.mainCanvas.localScale.x);
            _sceneData.Value._tutorialStuffView.HideText(false,textPos,"Upgrade car to increase stats and det new car parts");
            //upgrade car text
        } else if (currentRound == 1) {
            _sceneData.Value._inventoryUIView._upgradeCarButton.interactable = false;
            _sceneData.Value._shopUIView._shopModeButton.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(false);
            _tutorialStep = 9;
            foreach (var buttonShop in _sceneData.Value._shopUIView._buttonsShop) {
                buttonShop.BuyButton.enabled = false;
                buttonShop.enabled = false;
            }
            var textPos = new Vector2(
                _sceneData.Value._shopUIView._shopModeButton.transform.position.x,
                _sceneData.Value._shopUIView._shopModeButton.transform.position.y - 50*_sceneData.Value.mainCanvas.localScale.x);
            _sceneData.Value._tutorialStuffView.HideText(false,textPos,"Change shop mode");
            //buy friend car text
        }

    }

    private void ShopModeCheck() {
        _sceneData.Value._shopUIView._shopModeButton.enabled = true;
        _sceneData.Value._shopUIView._shopModeButton.onClick.RemoveListener(ShopModeCheck);
        _sceneData.Value._shopUIView._updateItemsText.gameObject.SetActive(false);
        var shopTextPosition = _sceneData.Value._tutorialStuffView._shopText.position;
        var textPos = new Vector2(shopTextPosition.x, shopTextPosition.y - 50 * _sceneData.Value.mainCanvas.localScale.x);
        _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects("Buy friend car", textPos, 20);
        //buy car text
    }
}