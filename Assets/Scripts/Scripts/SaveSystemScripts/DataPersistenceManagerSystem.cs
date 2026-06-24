
using System.Collections.Generic;
using UnityEngine;

public class DataPersistenceManagerSystem : MonoBehaviour
{
    public static GameData _gameData { get; private set; }
    private string _fileName = "carroguelike";
    private bool _useEncryption = true;
    private static FileDataHandler _dataHandler;
    public static DataPersistenceManagerSystem Instance;

    public void SetupManager() {
        Instance = this;
        _dataHandler = new FileDataHandler(Application.persistentDataPath, _fileName, _useEncryption);
      
        LoadGame();
        DontDestroyOnLoad(this);
    }
    

    public void StartNewGame() {
        
    }

    public void NewGame() {
        _gameData = new GameData();
        _gameData.AchievementsCompleted = new bool[BetweenScenesService.Instance._carStuffListsConfig.AchievementConfigs.Length];
        _gameData.ShopItemsBuyed = new bool[BetweenScenesService.Instance._carStuffListsConfig.ShopItemConfigs.Length];
        _gameData.CarPartsUnlocked = new bool[BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs.Length];
        for (int i = 0; i < _gameData.CarPartsUnlocked.Length; i++)
            _gameData.CarPartsUnlocked[i] = true;

        _gameData.ModulesUnlocked = new bool[BetweenScenesService.Instance._carStuffListsConfig.ModuleConfigs.Length];
        for (int i = 0; i < _gameData.ModulesUnlocked.Length; i++)
            _gameData.ModulesUnlocked[i] = true;
        foreach (var achievement in BetweenScenesService.Instance._carStuffListsConfig.AchievementConfigs) { 
          
            if (achievement.rewardType == AchievementRewardType.CarPart)
                _gameData.CarPartsUnlocked[achievement.rewardCount] = false;
            else if (achievement.rewardType == AchievementRewardType.Module)
                _gameData.ModulesUnlocked[achievement.rewardCount] = false;
           
        }

        foreach (var shopItem in BetweenScenesService.Instance._carStuffListsConfig.ShopItemConfigs) {
            if (shopItem.RewardType == AchievementRewardType.CarPart)
                _gameData.CarPartsUnlocked[shopItem.RewardCount] = false;
            else if (shopItem.RewardType == AchievementRewardType.Module)
                _gameData.ModulesUnlocked[shopItem.RewardCount] = false;
        }

        _gameData.CarsUnlocked = new bool[BetweenScenesService.Instance._carStuffListsConfig.CarConfigs.Length];
        _gameData.CarsUnlocked[0] = true;
    }

    public void LoadGame() {
        bool isTestProgressReload = false;

        _gameData = _dataHandler.Load();
      if (isTestProgressReload)
            _gameData = null;

        if (_gameData == null || _gameData.lastGameVersion != Application.version) {
            NewGame();
        } else {
            var filteredResolutions = new List<Resolution>();

            RefreshRate currentRefreshRate = Screen.currentResolution.refreshRateRatio;
            foreach (Resolution res in Screen.resolutions) {
                if (res.refreshRateRatio.value == currentRefreshRate.value)
                    filteredResolutions.Add(res);
            }

            if (_gameData.CarPartsUnlocked.Length != BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs.Length) {
                var oldUnlockedCarPartsArray = _gameData.CarPartsUnlocked;
                _gameData.CarPartsUnlocked = new bool[BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs.Length];
                for (int i = 0; i < BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs.Length; i++) {
                    
                    if (oldUnlockedCarPartsArray.Length <= i) {
                        _gameData.CarPartsUnlocked[i] = false;
                        Debug.Log(i + " i");
                    }
                        
                    else
                        _gameData.CarPartsUnlocked[i] = oldUnlockedCarPartsArray[i];
                }
            }
            
            if (_gameData.ShopItemsBuyed.Length != BetweenScenesService.Instance._carStuffListsConfig.ShopItemConfigs.Length) {
                var oldShopUnlockedItemsArray = _gameData.ShopItemsBuyed;
                _gameData.ShopItemsBuyed = new bool[BetweenScenesService.Instance._carStuffListsConfig.ShopItemConfigs.Length];
                for (int i = 0; i < BetweenScenesService.Instance._carStuffListsConfig.ShopItemConfigs.Length; i++) {
                    if (oldShopUnlockedItemsArray.Length <=  i)
                        _gameData.ShopItemsBuyed[i] = false;
                    else
                        _gameData.ShopItemsBuyed[i] = oldShopUnlockedItemsArray[i];
                }
            }

            SaveGame();
            var needResolution = _gameData.ResolutionIndex == -1 ||filteredResolutions.Count <= _gameData.ResolutionIndex ? filteredResolutions[^1] : filteredResolutions[_gameData.ResolutionIndex];
            Screen.SetResolution(needResolution.width, needResolution.height, _gameData.IsFullScreen);
        }
        Application.targetFrameRate = _gameData.FpsCountLock;

         if (isTestProgressReload) {
              _gameData.TutorialIsCompleted = true;
              _gameData.LevelRounds[0] = 5;
              _gameData.LevelRounds[1] = 5;
              _gameData.CarsUnlocked[5] = true;
              _gameData.CarsUnlocked[4] = true;
              _gameData.CarsUnlocked[3] = true;
              _gameData.CarsUnlocked[2] = true;
              _gameData.CarsUnlocked[1] = true;
            SaveGame();
          }  
    }

    public static void SaveGame() {
        Debug.Log("Game saved");
        
        _dataHandler.Save(_gameData);
    }


}
