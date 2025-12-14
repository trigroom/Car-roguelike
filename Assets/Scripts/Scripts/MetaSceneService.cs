
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MetaSceneService : MonoBehaviour {
    public int MainEntity; 
    [field: SerializeField]public AchievementsUIView _achievementsUIView{ get; private set; }
    [field: SerializeField] public Button[] _backToMainMenuButtons { get; private set; }
    [field: SerializeField] public Transform _shopUIContainer { get; private set; }
    [field: SerializeField] public MainMenuView _mainMenuView { get; private set; }
    [field: SerializeField] public Button[] _localizationButtons { get; private set; }
    [field: SerializeField] public StaticTextsView _staticTextsView { get; private set; }
    [field: SerializeField] public SelectCarButtonView[] _selectCarButtonViews { get; private set; }
    [field: SerializeField] public Transform _selectCarUIContainer { get; private set; }
    [field: SerializeField] public Transform _selectMapUIContainer { get; private set; }
    [field: SerializeField] public SelectDifficultyUIView selectMapDifficultyUIView { get; private set; }
    [field: SerializeField] public SelectMapButtonView[] _selectMapButtons { get; private set; }
     [field: SerializeField] public Button _backToCarSelectButton { get; private set; }
     [field: SerializeField] public TMP_Text _scrapCountText { get; private set; }
     [field: SerializeField] public Button _backToMainMenuButton { get; private set; }
    private void Awake() {
        if (BetweenScenesService.Instance == null)
            SceneManager.LoadScene("Scenes/LoadingScene");
        else
            BetweenScenesService.Instance.HideBlackScreen();
    }
} 
