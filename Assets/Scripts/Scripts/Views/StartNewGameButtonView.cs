using UnityEngine;
using UnityEngine.SceneManagement;

public class StartNewGameButtonView : MonoBehaviour
{
    public void StartGame() {
        SceneManager.LoadScene("Scenes/SampleScene");
    }
}
