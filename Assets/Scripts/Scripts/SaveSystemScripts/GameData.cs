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
    public float TasksCompleted;
    public int SupportBoxOpened;
    //game info
    public string lastGameVersion;
    public int ScrapCount;
    public int[] LevelRounds;
    public string CurrentLanguage;
    public string CoopPlayerName;
    
    public bool[] AchievementsCompleted;
    public bool[] CarPartsUnlocked;
    public bool[] CarsUnlocked;
    public bool[] ModulesUnlocked;
    
    public bool[] ShopItemsBuyed;
    
    public bool TutorialIsCompleted;

    public bool UseDamageNumbers;
    public bool UseHpBars;
    public bool UsePixelizedEffects;

    public float MusicVolume;
    public float SoundVolume;
    public float UIVolume;
    public GameData()
    {
        LevelRounds = new int[3];
        UseHpBars = true;
        UsePixelizedEffects = true;
        UseDamageNumbers= true;
        CoopPlayerName = "Player";
        lastGameVersion = Application.version;
        CurrentLanguage = GetSystemLanguageCode();
        MusicVolume = 0.3f;
        SoundVolume = 0.3f;
        UIVolume = 0.3f;
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
