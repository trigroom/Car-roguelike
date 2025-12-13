
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
