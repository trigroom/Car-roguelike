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
    private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);
    public void SetEndScreenInfo(int moneyCount, int roundNumber) {
        _moneyCountText.text = $"{GetLocalization("You earned")} {moneyCount.ToString()} $";
        _mainEndSreenTitle.text = $"{GetLocalization("Round")} {roundNumber} {GetLocalization("is completed")}";
        gameObject.SetActive(true);
    }
    public void SetLoseScreenInfo(int roundNumber, int scrapCount) {
        _moneyCountText.text = $"{GetLocalization("You earned")} {scrapCount} {GetLocalization("Scrap")}" +
                               $"\n{GetLocalization("Click to restart")}";
        _mainEndSreenTitle.text = $"{GetLocalization("You lose on round")} {roundNumber}";
        foreach (var endGameButton in closeEndGameMenuButtons) {
            endGameButton.onClick.RemoveAllListeners();
            endGameButton.onClick.AddListener(GoToMetaScene);
        }
        gameObject.SetActive(true);
    }
    
    public void SetWinScreenInfo(int scrapCount) {
        _moneyCountText.text = $"{GetLocalization("You earned")} {scrapCount} {GetLocalization("Scrap")}!";
        _mainEndSreenTitle.text = $"{GetLocalization("You complete all rounds")}!";
        foreach (var endGameButton in closeEndGameMenuButtons) {
            endGameButton.onClick.RemoveAllListeners();
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
