using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerUIView : MonoBehaviour {
    [SerializeField]
    private TMP_Text _playerNameText;
    [SerializeField]
    private TMP_Text _readyStateText;
    [SerializeField]
    private Image _playerCarImage;
    [SerializeField]
    private Image _playerCarHighlightImage;
    [SerializeField]
    public Button _giveMoneyYesButton;
    [SerializeField]
    public Button _giveMoneyNoButton;
    [SerializeField]
    private TMP_Text _giveMoneyText;
    [SerializeField]
    private Transform _giveMoneyContainer;
   
    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);

    public void ChangeCarSprite(Sprite carSprite, string playerName, bool isReady) {
        if (_playerCarHighlightImage)
            _playerCarHighlightImage.color = isReady ? new Color(0.53f, 0.8f, 0.53f, 0.25f) : new Color(0.8f, 0.53f, 0.53f, 0.25f);
        else {
             _readyStateText.text = isReady ? GetLocalization("Ready")+"!" : GetLocalization("Not ready")+"!";
             _readyStateText.color = isReady ? new Color(0.53f, 0.8f, 0.53f, 1f) : new Color(0.8f, 0.53f, 0.53f, 1f);
        }

        _playerNameText.text = playerName;
        _playerCarImage.sprite = carSprite;
    }

    public void ShowGiveMoneyContainerAnimation(int moneyCount) {
        _giveMoneyText.text = GetLocalization("Give") + moneyCount + "<sprite name=\"CogIcon\">?";
        _giveMoneyContainer.DOKill();
        _giveMoneyContainer.DOLocalMoveY(-4, 0.5f);
        _giveMoneyYesButton.enabled = true;
        _giveMoneyNoButton.enabled = true;
    }

    public void HideGiveMoneyContainerAnimation() {
        _giveMoneyYesButton.enabled = false;
        _giveMoneyNoButton.enabled = false;
        _giveMoneyContainer.DOKill();
        _giveMoneyContainer.DOLocalMoveY(-74, 0.5f);
    }
    
}
