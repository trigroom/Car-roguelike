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
  
   public Button[] DifficultyButtons;
   public void SetupDifficltyScreen(MapConfig currentMapConfig) {
      gameObject.SetActive(true);
      _mapNameText.text = currentMapConfig.MapName;
      _mapDescription.text = currentMapConfig.MapDescription;
      for (int i = 0; i < DifficultyButtons.Length; i++) {
         DifficultyButtons[i].gameObject.SetActive(currentMapConfig.MapDifficltyConfigs.Length>i); 
      }
   }

   public void SetDifficultyDescription(MapDifficultyConfig difficultyConfig) {
      _difficultyDescription.text = $"Enemy damage x{difficultyConfig.DamageMultiplayer}\nEnemy health x{difficultyConfig.HealthMultiplayer} \n" +
                                    $"Scrap for round {difficultyConfig.ScrapForRound}\nScrap for map complete{difficultyConfig.ScrapForMapComplete}";
   }
}
