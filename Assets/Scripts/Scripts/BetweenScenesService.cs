using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BetweenScenesService : MonoBehaviour {
    public AudioSource BackgroundMusicAudio;
    public AudioSource UiAudio;
    public int CurrentSelectedCar;
    public int CurrentSelectedMap;
    public bool IsCoop;
    public int CurrentSelectedMapDifficlty;
    public RectTransform BlackScreen;
    [SerializeField]
    private DataPersistenceManagerSystem DataPersistenceManagerSystem;
    public CSVLocalization CsvLocalization;
      [field: SerializeField] public CarStuffListsConfig _carStuffListsConfig { get; private set; }
   
   public static BetweenScenesService Instance;
   
   public List<int> CompletedAchievments = new List<int>();
    void Awake()
    { 
        Instance = this;
        DataPersistenceManagerSystem.SetupManager();
        DontDestroyOnLoad(this);
        DataPersistenceManagerSystem.Instance.LoadGame();
        CsvLocalization.SetupLocalizationManager();
        CsvLocalization.CurrentLanguage = DataPersistenceManagerSystem._gameData.CurrentLanguage;
            SceneManager.LoadScene("Scenes/MetaScene");
    }

    
    public void PlayUiAudioClip(AudioClip clip,SoundType soundType, float volumeScale = 1) {
        switch (soundType) {
            case SoundType.UI:
                UiAudio.PlayOneShot(clip, volumeScale * DataPersistenceManagerSystem._gameData.UIVolume);
                break;
            case SoundType.GameSound:
                Debug.Log("sound vol " + DataPersistenceManagerSystem._gameData.SoundVolume);
                UiAudio.PlayOneShot(clip, volumeScale * DataPersistenceManagerSystem._gameData.SoundVolume);
                break;
            case SoundType.Music:
                UiAudio.PlayOneShot(clip, volumeScale * DataPersistenceManagerSystem._gameData.MusicVolume);
                break;
        }
        
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
        BlackScreen.DOAnchorPosX(3000, 0.75f).SetEase(Ease.OutBack).OnComplete(()=>BlackScreen.gameObject.SetActive(false)).SetUpdate(true);
    }

    public void ShowBlackScreen(Action endAction = null) {
        BlackScreen.gameObject.SetActive(true);
        BlackScreen.anchoredPosition = new Vector3(-3000, BlackScreen.anchoredPosition.y);
        BlackScreen.DOKill();
        BlackScreen.DOAnchorPosX(0, 0.75f).SetEase(Ease.InBack).OnComplete(()=>endAction?.Invoke()).SetUpdate(true);
    }
    private string GetLocalization(string needString) => CsvLocalization.GetLocalizedValue(needString);
    public string GetAchievementReward(AchivementConfig achievement) {
        switch (achievement.rewardType) {
            case AchievementRewardType.Car:
                return GetLocalization(_carStuffListsConfig.CarConfigs[achievement.rewardCount].CarName);
                
            case AchievementRewardType.CarPart:
                return GetLocalization(_carStuffListsConfig.CarPartConfigs[achievement.rewardCount].PartName);
               
            case AchievementRewardType.Module:
                return GetLocalization(_carStuffListsConfig.ModuleConfigs[achievement.rewardCount].ModuleName);
            
            case AchievementRewardType.Scrap:
                return achievement.rewardCount +" "+ GetLocalization("Scrap");
               
            default: return "";
        }

        
    }
}
public enum SoundType {
        UI,
        GameSound,
        Music
    }