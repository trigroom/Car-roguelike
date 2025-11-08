using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GivingCarPartsUIView : MonoBehaviour
{
    [field: SerializeField]
    public TMP_Text _remainingCarPartsText { get; private set; }
    [field: SerializeField]
    public Image _carPartImageDown { get; private set; }
    [field: SerializeField]
    public Image _carPartImageUp { get; private set; }
    [field: SerializeField]
    public Button _getCarPartButton { get; private set; }
    [field: SerializeField]
    public Button _destroyCarPartButton { get; private set; }
}
