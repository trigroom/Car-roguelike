using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectDifficultyUIView : MonoBehaviour
{
   [SerializeField]
   private TMP_Text _mapNameText;
   [SerializeField]
   private TMP_Text _mapDescription;
   [SerializeField]
   private TMP_Text _difficultyDescription;
   [field:SerializeField]
   public Button _startMapButton { get; private set; }
      [field:SerializeField]
      public Button backButton { get; private set; }
  
   public Button[] DifficultyButtons;
   [SerializeField]
   private Image _difficultyColorGradient;

   public void SetupDifficultyScreen(MapConfig currentMapConfig) {
     // gameObject.SetActive(true);
      SetLocalizationText(currentMapConfig);
      int maxUnlockedLevel = DataPersistenceManagerSystem._gameData.LevelRounds[BetweenScenesService.Instance.CurrentSelectedMap];
      for (int i = 0; i < DifficultyButtons.Length; i++) {
         DifficultyButtons[i].gameObject.SetActive(maxUnlockedLevel >= i);
      }
   }

   public void SetLocalizationText(MapConfig currentMapConfig) {
      _mapNameText.text = GetLocalization(currentMapConfig.MapName) + "\n" + GetLocalization("Select difficulty");
      _mapDescription.text = GetLocalization(currentMapConfig.MapDescription);
      int maxUnlockedLevel = DataPersistenceManagerSystem._gameData.LevelRounds[BetweenScenesService.Instance.CurrentSelectedMap];
      for (int i = 0; i < DifficultyButtons.Length; i++) {
         if (maxUnlockedLevel >= i)
            DifficultyButtons[i].transform.GetComponentInChildren<TMP_Text>().text =
               GetLocalization(currentMapConfig.MapDifficltyConfigs[i].DifficultyName);
      }
      SetDifficultyDescription(BetweenScenesService.Instance.GetMapDifficultyConfig());
   }

   private string GetLocalization(string needString) => BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue(needString);

   public void SetDifficultyDescription(MapDifficultyConfig difficultyConfig) {
      _difficultyDescription.text = GetDifficultyDescription(difficultyConfig);
      _difficultyColorGradient.color = difficultyConfig.DifficltyUiColor;
   }

   public string GetDifficultyDescription(MapDifficultyConfig difficultyConfig) {
      string needString;
      needString = $"{GetLocalization(difficultyConfig.DifficultyName)}\n{GetLocalization("Enemy damage")} " +
                   $"x{difficultyConfig.DamageMultiplayer}\n{GetLocalization("Enemy health")} x{difficultyConfig.HealthMultiplayer} \n" +
                   $"{GetLocalization("Scrap for round")} {difficultyConfig.ScrapForRound}\n" +
                   $"{GetLocalization("Scrap for map complete")} {difficultyConfig.ScrapForMapComplete}" +
                   $"\n{GetLocalization("Start money")} {difficultyConfig.StartMoney}$\n{GetLocalization("Heal after end round to")}" +
                   $" {difficultyConfig.HealedHealthPartAfterEndRound * 100}%\n" +
                   $"{GetLocalization("Rounds count")}: {difficultyConfig.RoundsCount} ";
      if(!difficultyConfig.DestroyEnemyCarsAfterDeath)
         needString += "\n" + GetLocalization("Enemy cars don't destroy");
      if (difficultyConfig.MapEvents != null)
         foreach (var mapEvent in difficultyConfig.MapEvents) {
            needString += "\n" + GetMapEventDescription(mapEvent);
         }

      if (difficultyConfig.ShopInflationPerRound != 0)
         needString += $"\n {GetLocalization("Price rise per round")}: {(difficultyConfig.ShopInflationPerRound * 100f)}%";

      return needString;
   }

   private string GetMapEventDescription(MapEventInfo mapEventInfo) {
      string needString = "";
      switch (mapEventInfo.MapEventType) {
         case MapEventsType.Wind:
            switch (mapEventInfo.MapEventFirstCount) {
               case <= 80 :
                  needString = GetLocalization("Weak wind from");
                  break;
               case <= 175 :
                  needString = GetLocalization("Medium wind from");
                  break;
               default :
                  needString = GetLocalization("Strong wind from");
                  break;
            }
       //   needString += " "+GetLocalization(WindDirectionNames[mapEventInfo.MapEventSecondCount]);
            break;
         case MapEventsType.ThunderStorm :
            needString = GetLocalization("Thunderstorm");
            break;
         case MapEventsType.Tornado :
            switch (mapEventInfo.MapEventFirstCount) {
               case 0:
                  needString = GetLocalization("Tornado");
                  break;
               case 1:
                  needString = GetLocalization("Fire tornado");
                  break;
               case 2:
                  needString = GetLocalization("Frost tornado");
                  break;
            }
            break;
         case MapEventsType.Bomber :
            needString = GetLocalization("Bomber airplane");
            break;
      }
      return needString;
   }
   
  /* private string[] WindDirectionNames = new[] {
      "North",      // (0, 1)
      "West",       // (-1, 0)
      "South",      // (0, -1)
      "Southeast",  // (1, -1)
      "East",       // (1, 0)
      "Northeast",  // (1, 1)
      "Southwest",  // (-1, -1)
      "Northwest"   // (-1, 1)
   };*/
}
