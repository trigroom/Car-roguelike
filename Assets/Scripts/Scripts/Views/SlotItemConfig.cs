using UnityEngine;

[CreateAssetMenu(fileName = "SlotItemConfig", menuName = "Scriptable Objects/SlotItemConfig")]
public class SlotItemConfig : ScriptableObject
{
    public Sprite Icon;
    public float Count;
    public int Id;
    public float TripleMultiplayer;
    public float DoubleMultiplayer;
}
