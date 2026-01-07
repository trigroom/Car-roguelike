using UnityEngine;
using UnityEngine.UI;

public class MainMenuView : MonoBehaviour
{
    [field: SerializeField] public Button _playButton { get; private set; }
    [field: SerializeField] public Button _exitButton { get; private set; }
    [field: SerializeField] public Button _shopButton { get; private set; }
    [field: SerializeField] public Button _achivementsButton { get; private set; }
    [field: SerializeField] public Button _catalogButton { get; private set; }
}
