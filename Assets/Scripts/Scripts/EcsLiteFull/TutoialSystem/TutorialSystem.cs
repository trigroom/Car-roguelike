using System.Collections.Generic;
using DG.Tweening;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

sealed class TutorialSystem : IEcsRunSystem, IEcsInitSystem {
    private EcsWorldInject _world;
    private EcsCustomInject<SceneService> _sceneData;

    private EcsPoolInject<CurrentGameComponent> _сurrentGameComponentsPool;
    private EcsPoolInject<InventoryComponent> _inventoryComponentsPool;
    private EcsPoolInject<CarPartInfoComponent> _carPartInfoComponentsPool;
    private EcsPoolInject<CarComponent> _carComponentsPool;
    private EcsPoolInject<PlayerCurrencyComponent> _playerCurrencyComponentsPool;
    
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
        _sceneData.Value._carPartInfoUIView._moduleContainer.gameObject.SetActive(false);
        _sceneData.Value._inventoryUIView._inventoryModeButton.gameObject.SetActive(false);
        _sceneData.Value._shopUIView._shopModeButton.onClick.AddListener(ShopModeCheck);
        _sceneData.Value._settingsView._mainMenuButton.gameObject.SetActive(false);
        _sceneData.Value._shopUIView._upgradeShopButton.gameObject.SetActive(false);
        _сurrentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentBattleRounds = 3;
        RoundUtilits.SupportRoundsTypes.Remove(RoundType.ModulesShop);
        _sceneData.Value._tutorialStuffView.TutorialTextContainer = _sceneData.Value
            .InstantiateObject(_sceneData.Value._tutorialStuffView._tutorialTextPrefab.transform, _sceneData.Value._inventoryUIView.transform)
            .GetComponent<Image>();
        _sceneData.Value._tutorialStuffView.TutorialText =  _sceneData.Value._tutorialStuffView.TutorialTextContainer.transform.GetChild(0).GetComponent<TMP_Text>();
        _sceneData.Value._tutorialStuffView.TutorialInGoundText = _sceneData.Value
            .InstantiateObject(_sceneData.Value._tutorialStuffView._tutorialTextPrefab.transform, _sceneData.Value._playerUIView.transform).GetChild(0)
            .GetComponent<TMP_Text>();
        _sceneData.Value._tutorialStuffView.TutorialInGoundText.text = GetLocalization("CarDriveTutorial");
        _sceneData.Value._tutorialStuffView.TutorialInGoundText.color = Color.white;
        _sceneData.Value._tutorialStuffView.TutorialInGoundText.transform.parent.localPosition = new Vector3(0, 200*_sceneData.Value.mainCanvas.localScale.x, 0);
        var textPos = new Vector2(
            _sceneData.Value._inventoryUIView._carPartsContainer.position.x,
            _sceneData.Value._inventoryUIView._carPartsContainer.position.y + 200*_sceneData.Value.mainCanvas.localScale.x);
        _sceneData.Value._tutorialStuffView.HideText(false,textPos,GetLocalization("FirstTutorialStep"));
      
        _sceneData.Value._inventoryUIView._selectCarButtons[1].sellCarText.transform.parent.gameObject.SetActive(false);
        //cursor move car parts animation
         var cellPosition = _sceneData.Value._inventoryUIView._inventoryCellViews[0].transform.position;
        _sceneData.Value._tutorialStuffView.CursorContainer = _sceneData.Value
            .InstantiateObject(_sceneData.Value._tutorialStuffView._cursorPrefab.transform, _sceneData.Value._inventoryUIView.transform,
                cellPosition)
            .GetComponent<Image>();
        var containerPosition = _sceneData.Value._inventoryUIView._carPartsContainer.position;
        var cursorTransform = _sceneData.Value._tutorialStuffView.CursorContainer;
        var cursorSequense = _sceneData.Value._tutorialStuffView._cursorSequence;
        cursorSequense = DOTween.Sequence();
        cursorSequense.Append(cursorTransform.transform.DOScale(0.8f, 0.35f));
        cursorSequense.AppendInterval(0.25f);
        cursorSequense.Append(cursorTransform.transform.DOMove(containerPosition, 1f));
        cursorSequense.AppendInterval(0.25f);
        cursorSequense.Append(cursorTransform.transform.DOScale(1f, 0.35f));
        cursorSequense.AppendInterval(0.25f);
        cursorSequense.Append(cursorTransform.transform.DOMove(cellPosition, 1f));
        cursorSequense.AppendInterval(0.25f);
        cursorSequense.SetEase(Ease.InOutSine);
        cursorSequense.SetLoops(-1, LoopType.Restart);
    }

    private int _tutorialStep;
    private float _tutorialStepTime;

    public void Run(IEcsSystems systems) {
        _tutorialStepTime += Time.deltaTime;
        var curGameCmp = _сurrentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
        int currentRound = curGameCmp.CurrentRound;
        if (_tutorialStep == 0 && _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).IsCarPartsRedactorMode) {
            _tutorialStep = 1;
            CompleteStepSendAnlyticEvent();
            var textPos = new Vector2(_sceneData.Value._inventoryUIView._carPartsContainer.position.x,
                _sceneData.Value._inventoryUIView._carPartsContainer.position.y + 200 * _sceneData.Value.mainCanvas.localScale.x);
            _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects(GetLocalization("SecondTutorialStep"), textPos, 30);
        } else if (_tutorialStep == 1 && currentRound == 0 &&
                   !_sceneData.Value._tutorialStuffView._shopContainer.gameObject.activeInHierarchy &&
                   _inventoryCellComponentsFilter.Value.GetEntitiesCount() == 0) {
            _sceneData.Value._tutorialStuffView._cursorSequence.Kill();
           _sceneData.Value.DestroyGameobject(_sceneData.Value._tutorialStuffView.CursorContainer.transform); 
            _sceneData.Value._inventoryUIView._changeToRedactModeButton.gameObject.SetActive(true);
            _sceneData.Value._tutorialStuffView._shopContainer.gameObject.SetActive(true);
            _sceneData.Value._tutorialStuffView._sortButtonsContainer.gameObject.SetActive(true);
            _tutorialStep = 2;
            CompleteStepSendAnlyticEvent();
            _sceneData.Value._tutorialStuffView.HideText(false);
            var shopTextPosition = _sceneData.Value._tutorialStuffView._shopText.position;
            _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects(GetLocalization("Buy car part in shop"),
                new Vector2(shopTextPosition.x, shopTextPosition.y - 100 * _sceneData.Value.mainCanvas.localScale.x), 30);
            //show buy item text
        } else if (_tutorialStep == 2) {
            foreach (var buttonShop in _sceneData.Value._shopUIView._buttonsShop) {
                if (!buttonShop.BuyButton.enabled) {
                    _tutorialStep = 3;
                    CompleteStepSendAnlyticEvent();
                    if (_inventoryCellComponentsFilter.Value.GetEntitiesCount() != 0) {
                        _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects(GetLocalization("Place car parts to car"),
                            new Vector2(_sceneData.Value._inventoryUIView._carPartsContainer.position.x,
                                _sceneData.Value._inventoryUIView._carPartsContainer.position.y +
                                200 * _sceneData.Value.mainCanvas.localScale.x), 30);
                    }

                    //place car part to car text
                    return;
                }
            }
        } else if (_inventoryCellComponentsFilter.Value.GetEntitiesCount() == 0 && _tutorialStep == 3) {
            _tutorialStep = 4;
            CompleteStepSendAnlyticEvent();
            _sceneData.Value._shopUIView._upgradeShopButton.gameObject.SetActive(true);
            _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects(GetLocalization("Go play and select round type"),
                new Vector2(_sceneData.Value._inventoryUIView._carPartsContainer.position.x,
                    _sceneData.Value._inventoryUIView._carPartsContainer.position.y + 200*_sceneData.Value.mainCanvas.localScale.x), 30);
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(true);
        } else if (_tutorialStep == 5 && curGameCmp.CurrentRoundType == RoundType.None && currentRound == 1) {
            _tutorialStep = 6;
            CompleteStepSendAnlyticEvent();
            var textPos = new Vector2(
                _sceneData.Value._inventoryUIView._upgradeCarButton.transform.position.x + 250*_sceneData.Value.mainCanvas.localScale.x,
                _sceneData.Value._inventoryUIView._upgradeCarButton.transform.position.y);
            _sceneData.Value._tutorialStuffView.HideText(false,textPos,GetLocalization("Upgrade car to increase stats and get new car parts"));
            _sceneData.Value._inventoryUIView.FadeAnimation(_sceneData.Value._inventoryUIView._highlightUpgradeImage, true);
  
        } else if (_tutorialStep == 6 && _inventoryCellComponentsFilter.Value.GetEntitiesCount() != 0) {
            foreach (var inventoryCellEntity in _inventoryCellComponentsFilter.Value) {
                if (_sceneData.Value.GetCarPartConfig(_carPartInfoComponentsPool.Value.Get(inventoryCellEntity).CarPartId).PartType ==
                    CarPartType.CarStuffContainer) {
                    _tutorialStep = 7;
                    CompleteStepSendAnlyticEvent();
                    var carPartRedactModeButtonPosition = _sceneData.Value._inventoryUIView._changeToRedactModeButton.transform.position;
                    _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects(GetLocalization("Change redacted car level"),
                        new Vector2(carPartRedactModeButtonPosition.x,
                            carPartRedactModeButtonPosition.y + 100 * _sceneData.Value.mainCanvas.localScale.x), 30);
                    _sceneData.Value._inventoryUIView.EndAnimation();
                    _sceneData.Value._inventoryUIView._highlightUpgradeImage.color = Color.clear;
                }
            }

            //place car parts and click change mode button
        } else if (_tutorialStep == 7 && !_inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).IsCarPartsRedactorMode) {
            _tutorialStep = 8;
            CompleteStepSendAnlyticEvent();
            _sceneData.Value._inventoryUIView._changeToRedactModeButton.interactable = false;
            _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects(GetLocalization("Place new car parts"),
                new Vector2(_sceneData.Value._inventoryUIView._carPartsContainer.position.x,
                    _sceneData.Value._inventoryUIView._carPartsContainer.position.y + 200*_sceneData.Value.mainCanvas.localScale.x), 20);
        } else if (_tutorialStep == 8 && _inventoryCellComponentsFilter.Value.GetEntitiesCount() == 0) {
            _sceneData.Value._tutorialStuffView._shopContainer.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(true);
            _tutorialStep = 9;
            CompleteStepSendAnlyticEvent();
            _sceneData.Value._inventoryUIView._changeToRedactModeButton.interactable = true;
            _sceneData.Value._tutorialStuffView.HideText(true);
        } else if (_tutorialStep == 10 && _inventoryComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).PlayerTeammateCarsEntity.Count > 1) {
            _tutorialStep = 11;
            CompleteStepSendAnlyticEvent();
            foreach (var buttonShop in _sceneData.Value._shopUIView._buttonsShop) {
                buttonShop.BuyButton.interactable = true;
                buttonShop.enabled = true;
            }
            _sceneData.Value._shopUIView._upgradeShopButton.enabled = true;
            _sceneData.Value._shopUIView._shopModeButton.interactable = true;
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._upgradeCarButton.interactable = true;
            var shopTextPosition = _sceneData.Value._tutorialStuffView._shopText.position;
            _sceneData.Value._shopUIView._updateItemsText.gameObject.SetActive(false);
            _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects(GetLocalization("Use the right mouse button to indicate the target to allied vehicles"),
                new Vector2(shopTextPosition.x, shopTextPosition.y - 50 * _sceneData.Value.mainCanvas.localScale.x), 20);
        }

        if (_tutorialStep is <= 3 or 6 or 10) {
            // if (_tutorialStep > 3)
            if(_tutorialStep == 0 && _carComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CarPartContainerGroupsList.Count == 3)return;
            _sceneData.Value._carPartInfoUIView._moveToInventoryButton.gameObject.SetActive(false);
            _sceneData.Value._carPartInfoUIView._upgradeButton.gameObject.SetActive(false);
            _sceneData.Value._carPartInfoUIView._destroyButton.gameObject.SetActive(false);
        }
       
    }

    private void RoundCheck() {
        int currentRound = _сurrentGameComponentsPool.Value.Get(_sceneData.Value.PlayerEntity).CurrentRound;

        if (currentRound == 2) {
           
            DataPersistenceManagerSystem._gameData.TutorialIsCompleted = true;
            DataPersistenceManagerSystem.SaveGame();
        } else if (currentRound == 0) {
            _sceneData.Value._inventoryUIView._upgradeCarButton.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(false);
            _tutorialStep = 5;
            CompleteStepSendAnlyticEvent();
            _sceneData.Value._tutorialStuffView._shopContainer.gameObject.SetActive(false);
            _sceneData.Value._shopUIView._updateItemsText.gameObject.SetActive(true);
            ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            playerCurrencyCmp.MoneyCount += 30;
            _sceneData.Value._shopUIView._moneyCountText.text = playerCurrencyCmp.MoneyCount + "<sprite name=\"CogIcon\">";
         RoundUtilits.SupportRoundsTypes = new List<RoundType> { RoundType.Chest }; //.Add(RoundType.ModulesShop);
            //upgrade car text
        } else if (currentRound == 1) {
            RoundUtilits.SupportRoundsTypes = new List<RoundType> {
                                      RoundType.Chest, RoundType.Shop, RoundType.Task, RoundType.SlotMachine, RoundType.ModulesShop, RoundType.VersusBattle
                                  };
            _sceneData.Value._tutorialStuffView.TutorialInGoundText.transform.parent.gameObject.SetActive(false);
            _sceneData.Value._inventoryUIView._upgradeCarButton.interactable = false;
            _sceneData.Value._shopUIView._shopModeButton.gameObject.SetActive(true);
            _sceneData.Value._inventoryUIView._playButton.gameObject.SetActive(false);
            _tutorialStep = 10;
            CompleteStepSendAnlyticEvent();
            ref var playerCurrencyCmp = ref _playerCurrencyComponentsPool.Value.Get(_sceneData.Value.PlayerEntity);
            playerCurrencyCmp.MoneyCount += 120;
            _sceneData.Value._shopUIView._moneyCountText.text = playerCurrencyCmp.MoneyCount + "<sprite name=\"CogIcon\">";
            foreach (var buttonShop in _sceneData.Value._shopUIView._buttonsShop) {
                buttonShop.BuyButton.interactable = false;
                buttonShop.enabled = false;
            }
            _sceneData.Value._shopUIView._upgradeShopButton.enabled = false;
            var textPos = new Vector2(
                _sceneData.Value._shopUIView._shopModeButton.transform.position.x,
                _sceneData.Value._shopUIView._shopModeButton.transform.position.y - 110*_sceneData.Value.mainCanvas.localScale.x);
            _sceneData.Value._tutorialStuffView.HideText(false,textPos,GetLocalization("Change shop mode"));
            //buy friend car text
        }

    }

    private void CompleteStepSendAnlyticEvent() {
        Analytics.CustomEvent("tutorialEndStep",
            new Dictionary<string, object> {
                { "time", _tutorialStepTime },
                { "round_number",_tutorialStep}
            });
        _tutorialStepTime = 0;
    }
    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);

    private void ShopModeCheck() {
        _sceneData.Value._shopUIView._shopModeButton.enabled = true;
        _sceneData.Value._shopUIView._shopModeButton.onClick.RemoveListener(ShopModeCheck);
        _sceneData.Value._shopUIView._updateItemsText.gameObject.SetActive(false);
        var shopTextPosition = _sceneData.Value._tutorialStuffView._shopText.position;
        var textPos = new Vector2(shopTextPosition.x, shopTextPosition.y - 50 * _sceneData.Value.mainCanvas.localScale.x);
        _sceneData.Value._tutorialStuffView.AnimateTMPWithEffects(GetLocalization("Buy teammate car"), textPos, 20);
        //buy car text
    }
}