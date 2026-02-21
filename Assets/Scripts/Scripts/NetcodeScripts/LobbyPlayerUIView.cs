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
   
    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);

    public void ChangeCarSprite(Sprite carSprite, string playerName, bool isReady) {
        if (_playerCarHighlightImage)
            _playerCarHighlightImage.color = isReady ? new Color(0.53f, 0.8f, 0.53f, 0.25f) : new Color(0.8f, 0.53f, 0.53f, 0.25f);
        else
            _readyStateText.text = isReady ? GetLocalization("Ready")+"!" : GetLocalization("Not ready")+"!";

        _playerNameText.text = playerName;
        _playerCarImage.sprite = carSprite;
    }
    
}
