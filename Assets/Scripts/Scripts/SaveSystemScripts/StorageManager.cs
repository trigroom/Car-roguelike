using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Collections.Generic;
public class StorageManager
{
    public GameData gameDataMain = new GameData();
    public void SavaToJson()
    {
        string gameData = JsonUtility.ToJson(null);
        string filePath = Application.persistentDataPath + "/data.fish";
        Debug.Log(filePath);
        File.WriteAllText(filePath, gameData);
    }

    public void LoadFromJson()
    {
        string filePath = Application.persistentDataPath + "/data.fish";
        string gameData = File.ReadAllText(filePath);

        gameDataMain = JsonUtility.FromJson<GameData>(gameData);
    }

}
