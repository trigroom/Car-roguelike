using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIView : MonoBehaviour
{
    [field:SerializeField]
    public ShopButtonView[] _buttonsShop { get; private set; }
    [field:SerializeField]
    public Button _buttonUpdateShop { get; private set; }
    [field:SerializeField]
    public TMP_Text _moneyCountText { get; private set; }
    [field:SerializeField]
    public TMP_Text _updateItemsText { get; private set; }
}
