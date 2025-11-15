using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGameScreenView : MonoBehaviour
{
    [field: SerializeField]
    public TMP_Text _mainEndSreenTitle { get; private set; }

    [field: SerializeField]
    public TMP_Text _moneyCountText { get; private set; }

    [field: SerializeField]
    public Button[] closeEndGameMenuButtons { get; private set; }

    public void OnInitialize() {
        foreach (var endGameButton in closeEndGameMenuButtons) {
            endGameButton.onClick.AddListener(CloseEndGameUI);
        }
    }

    public void SetEndScreenInfo(int moneyCount, int roundNumber) {
        _moneyCountText.text = $"You earned {moneyCount.ToString()} $";
        _mainEndSreenTitle.text = $"Round {roundNumber} is completed";
        gameObject.SetActive(true);
    }
    public void SetLoseScreenInfo(int roundNumber) {
        _moneyCountText.text = " click ro restart";
        _mainEndSreenTitle.text = $"You lose on rond {roundNumber}";
        foreach (var endGameButton in closeEndGameMenuButtons) {
            endGameButton.onClick.AddListener(GoToMetaScene);
        }
        gameObject.SetActive(true);
    }

    private void GoToMetaScene() {
        SceneManager.LoadScene("Scenes/MetaScene");
    }
    private void CloseEndGameUI() {
        gameObject.SetActive(false);
    }
    
    
}
