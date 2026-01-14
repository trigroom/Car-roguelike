using UnityEngine;

public class SupportBoxStuffView : MonoBehaviour
{
    [field:SerializeField]
    public SpriteRenderer _supportBoxSprite { get; private set; }
    [field:SerializeField]
    public CarPartStuffView _supportBoxcarPartStuffView { get; private set; }
    [field:SerializeField]
    public Sprite defaultBoxSprite { get; private set; }
    [field:SerializeField]
    public Sprite rareBoxSprite { get; private set; }
    [field:SerializeField]
    public Transform supportBoxAirplane { get; private set; }
}
