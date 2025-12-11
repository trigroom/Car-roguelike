using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BetweenScenesService : MonoBehaviour {
    public int CurrentSelectedCar;
    public int CurrentSelectedMap;
    public int CurrentSelectedMapDifficlty;
    public RectTransform BlackScreen;
    [SerializeField]
    private DataPersistenceManagerSystem DataPersistenceManagerSystem;
    public CSVLocalization CsvLocalization;
      [field: SerializeField] public CarStuffListsConfig _carStuffListsConfig { get; private set; }
   
   public static BetweenScenesService Instance;
    void Awake()
    {
        DataPersistenceManagerSystem.SetupManager();
        Instance = this;
        DontDestroyOnLoad(this);
        DataPersistenceManagerSystem.Instance.LoadGame();
        CsvLocalization.SetupLocalizationManager();
        CsvLocalization.CurrentLanguage = DataPersistenceManagerSystem._gameData.CurrentLanguage;
        if (DataPersistenceManagerSystem._gameData.TutorialIsCompleted)
            SceneManager.LoadScene("Scenes/MetaScene");
        else
            SceneManager.LoadScene("Scenes/SampleScene");
    }

    public MapConfig GetMapConfig() {
       return _carStuffListsConfig.MapConfigs[CurrentSelectedMap];
    }
    public MapDifficultyConfig GetMapDifficultyConfig() {
        return _carStuffListsConfig.MapConfigs[CurrentSelectedMap].MapDifficltyConfigs[CurrentSelectedMapDifficlty];
    }

    public void HideBlackScreen() {
        BlackScreen.gameObject.SetActive(true);
        BlackScreen.anchoredPosition = new Vector3(0, BlackScreen.anchoredPosition.y);
        BlackScreen.DOKill();
        Debug.Log("Black Screen Hidden");
        BlackScreen.DOAnchorPosX(3000, 0.75f).SetEase(Ease.OutBack).OnComplete(()=>BlackScreen.gameObject.SetActive(false));
    }

    public void ShowBlackScreen(Action endAction = null) {
        BlackScreen.gameObject.SetActive(true);
        BlackScreen.anchoredPosition = new Vector3(-3000, BlackScreen.anchoredPosition.y);
        BlackScreen.DOKill();
        BlackScreen.DOAnchorPosX(0, 0.75f).SetEase(Ease.InBack).OnComplete(()=>endAction?.Invoke());
    }

}
