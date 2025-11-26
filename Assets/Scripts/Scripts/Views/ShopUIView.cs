using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIView : MonoBehaviour
{
    [field:SerializeField]
    public ShopButtonView[] _buttonsCarShop { get; private set; }
    [field:SerializeField]
    public Transform _shopCarContainer { get; private set; }
    [field:SerializeField]
    public Transform _shopCarPartsContainer { get; private set; }
     [field:SerializeField]
        public Button _shopModeButton { get; private set; }
    [field:SerializeField]
    public ShopButtonView[] _buttonsShop { get; private set; }
    [field:SerializeField]
    public Button _buttonUpdateShop { get; private set; }
    [field:SerializeField]
    public TMP_Text _moneyCountText { get; private set; }
    [field:SerializeField]
    public TMP_Text _updateItemsText { get; private set; }
}
