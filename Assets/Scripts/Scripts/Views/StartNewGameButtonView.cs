using UnityEngine;
using UnityEngine.SceneManagement;

public class StartNewGameButtonView : MonoBehaviour
{
    public void StartGame() {
        BetweenScenesService.Instance.ShowBlackScreen( ()=>SceneManager.LoadScene("Scenes/SampleScene"));
       
    }
}
