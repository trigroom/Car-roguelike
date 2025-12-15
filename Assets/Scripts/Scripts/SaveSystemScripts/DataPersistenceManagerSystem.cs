
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
        _gameData.CarPartsUnlocked = new bool[BetweenScenesService.Instance._carStuffListsConfig.CarPartConfigs.Length];
        for (int i = 0; i < _gameData.CarPartsUnlocked.Length; i++) 
            _gameData.CarPartsUnlocked[i] = true;
        
        _gameData.CarsUnlocked = new bool[BetweenScenesService.Instance._carStuffListsConfig.CarConfigs.Length];
        _gameData.CarsUnlocked[0] = true;
    }

    public void LoadGame() {
        _gameData = _dataHandler.Load();
       // _gameData = null;
        if (_gameData == null) {
            NewGame();
        }

       // SaveGame();
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
