using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonWithSpritesView : MonoBehaviour
{
    [field: SerializeField]
    public EventTrigger SortEventTrigger { get; private set; }
    [field: SerializeField]
    public Image buttonImage { get; private set; }
    [field: SerializeField]
    public Sprite activeSprite { get; private set; }
    [field: SerializeField]
    public Sprite unactiveSprite { get; private set; }
}
