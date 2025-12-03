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
      public Button backButton { get; private set; }
  
   public Button[] DifficultyButtons;
   [SerializeField]
   private Image _difficultyColorGradient;
   public void SetupDifficultyScreen(MapConfig currentMapConfig) {
      gameObject.SetActive(true);
      _mapNameText.text = currentMapConfig.MapName + "\nSelect difficulty";
      _mapDescription.text = currentMapConfig.MapDescription;
      int maxUnlockedLevel = DataPersistenceManagerSystem._gameData.LevelRounds[BetweenScenesService.Instance.CurrentSelectedMap];
      for (int i = 0; i < DifficultyButtons.Length; i++) {
         DifficultyButtons[i].gameObject.SetActive(maxUnlockedLevel > i);
         if (maxUnlockedLevel > i)
            DifficultyButtons[i].transform.GetComponentInChildren<TMP_Text>().text = currentMapConfig.MapDifficltyConfigs[i].DifficultyName;
      }
   }

   public void SetDifficultyDescription(MapDifficultyConfig difficultyConfig) {
      _difficultyDescription.text =
         $"{difficultyConfig.DifficultyName}\nEnemy damage x{difficultyConfig.DamageMultiplayer}\nEnemy health x{difficultyConfig.HealthMultiplayer} \n" +
         $"Scrap for round {difficultyConfig.ScrapForRound}\nScrap for map complete{difficultyConfig.ScrapForMapComplete}" +
         $"\nStart money {difficultyConfig.StartMoney}$\nHeal after end round to {difficultyConfig.HealedHealthPartAfterEndRound * 100}%";
      _difficultyColorGradient.color = difficultyConfig.DifficltyUiColor;
   }
}
