using UnityEngine;
using UnityEngine.SceneManagement;

public class BetweenScenesService : MonoBehaviour {
    public int CurrentSelectedCar;
      [field: SerializeField] public CarStuffListsConfig _carStuffListsConfig { get; private set; }
   
   public static BetweenScenesService Instance;
    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);
        
        SceneManager.LoadScene("Scenes/MetaScene");
    }

}
