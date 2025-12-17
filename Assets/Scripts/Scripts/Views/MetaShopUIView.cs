using UnityEngine;

public class MetaShopUIView : MonoBehaviour {
    public Transform ShopCellsContiner;
    public ShopButtonView ShopButtonPrefab;
    
    public ShopButtonView GetMetaShopButtonView() {
        return Instantiate(ShopButtonPrefab, ShopCellsContiner);
    }
}
