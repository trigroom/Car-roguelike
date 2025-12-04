using UnityEngine;
using System.Collections.Generic;
using System.IO;


public class CSVLocalization : MonoBehaviour
{
     public static CSVLocalization Instance;
    
    [SerializeField] private string csvFileName = "localization.csv";
    [SerializeField] private string defaultLanguage = "en";
    
    private Dictionary<string, Dictionary<string, string>> localizationData;
    public string CurrentLanguage;
    
    public void SetupLocalizationManager()
    {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLocalization();
    }
    
    private void LoadLocalization()
    {
        localizationData = new Dictionary<string, Dictionary<string, string>>();
        
        TextAsset csvFile = Resources.Load<TextAsset>(csvFileName);
        
        if (csvFile == null)
        {
            Debug.LogError($"Файл локализации {csvFileName} не найден!");
            return;
        }
        
        string[] lines = csvFile.text.Split('\n');
        
        if (lines.Length < 2) return;
        
        // Первая строка - заголовки с кодами языков
        string[] languages = lines[0].Split(',');
        
        // Пропускаем заголовок и обрабатываем каждую строку
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            
            string[] values = lines[i].Split(',');
            if (values.Length < 2) continue;
            
            string key = values[0].Trim();
            
            for (int j = 1; j < values.Length && j < languages.Length; j++)
            {
                string lang = languages[j].Trim();
                string value = values[j].Trim().Replace("\\n", "\n");
                
                if (!localizationData.ContainsKey(lang))
                {
                    localizationData[lang] = new Dictionary<string, string>();
                }
                
                localizationData[lang][key] = value;
            }
        }
    }
    
    public string GetLocalizedValue(string key)
    {
        if (localizationData.ContainsKey(CurrentLanguage) && 
            localizationData[CurrentLanguage].ContainsKey(key))
        {
            return localizationData[CurrentLanguage][key];
        }
        
        if (localizationData.ContainsKey(defaultLanguage) && 
            localizationData[defaultLanguage].ContainsKey(key))
        {
            return localizationData[defaultLanguage][key];
        }
        
        return $"[{key}]";
    }
    
    public void ChangeLanguage(string newLanguage)
    {
        CurrentLanguage = newLanguage;
        DataPersistenceManagerSystem._gameData.CurrentLanguage = newLanguage;
        
        // Оповещаем все объекты об изменении языка
      /*  var textLocalizers = FindObjectsOfType<TextLocalizer>();
        foreach (var localizer in textLocalizers)
        {
            localizer.UpdateText();
        }*/
    }
}