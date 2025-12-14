using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class GameData
{
    //game statistics
    public float DistanceDrived;
    public int BrokenCars;
    public float PlayerDamage;
    public float MoneyEarnedInGame;
    public int SupportBoxOpened;
    //game info
    public string lastGameVersion;
    public int ScrapCount;
    public int[] LevelRounds;
    public string CurrentLanguage;
    
    public bool[] AchievementsCompleted;
    public bool[] CarPartsUnlocked;
    public bool[] CarsUnlocked;
    
    public bool TutorialIsCompleted;

    public GameData()
    {
        LevelRounds = new int[3];
        LevelRounds[0] = 1;
        lastGameVersion = Application.version;
        CurrentLanguage = GetSystemLanguageCode();
    }
    
    public static string GetSystemLanguageCode()
    {
        // Получаем системный язык
        SystemLanguage systemLang = Application.systemLanguage;
        Debug.Log(systemLang);
        // Маппим только на нужные языки
        switch (systemLang)
        {
            case SystemLanguage.Russian:
            case SystemLanguage.Ukrainian:    // Украинский → Русский (близкий язык)
            case SystemLanguage.Belarusian:   // Белорусский → Русский
                return "ru";
                
            case SystemLanguage.English:
                return "en";
                
            case SystemLanguage.German:
                return "de";
                
            case SystemLanguage.Spanish:
                return "es";
                
            case SystemLanguage.French:
                return "fr";
                
            // Языки похожие на испанский/французский
            case SystemLanguage.Portuguese:   // Португальский → Испанский (близкий)
            case SystemLanguage.Italian:      // Итальянский → Испанский (близкий)
                return "es";
                
            // Языки похожие на немецкий
            case SystemLanguage.Dutch:        // Голландский → Немецкий (близкий)
                return "de";
                
            // Все остальные → Английский
            default:
                return "en";
        }
    }
    
    public static string GetLanguageName(string code)
    {
        switch (code)
        {
            case "en": return "English";
            case "ru": return "Русский";
            case "de": return "Deutsch";
            case "es": return "Español";
            case "fr": return "Français";
            default: return code;
        }
    }
}
