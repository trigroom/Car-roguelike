using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerUIView : MonoBehaviour {
    [SerializeField]
    private TMP_Text _playerNameText;
    [SerializeField]
    private Image _playerCarImage;
    [SerializeField]
    private Image _playerCarHighlightImage;
   

    public void ChangeCarSprite(Sprite carSprite, string playerName, bool isReady) {
        _playerCarHighlightImage.color = isReady ? new Color(0.53f,0.8f,0.53f,0.25f) : new Color(0.8f,0.53f,0.53f,0.25f);
        _playerNameText.text = playerName;
        _playerCarImage.sprite = carSprite;
    }
    
}
