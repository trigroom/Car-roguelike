
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
        _gameData = _dataHandler.Load();
        
        _gameData = null;
        
        if (_gameData == null) {
            NewGame();
        }

        _gameData.TutorialIsCompleted = true;
        _gameData.LevelRounds[0] = 2;
        
       SaveGame();
    }

    public enum SavePriority {
        fullSave,
        inGameSave
    }

    public static void SaveGame() {
        Debug.Log("Game saved");
        
        _dataHandler.Save(_gameData);
    }


}
