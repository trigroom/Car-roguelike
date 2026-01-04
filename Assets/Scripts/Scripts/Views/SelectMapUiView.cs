using UnityEngine;

public class SelectMapUiView : MonoBehaviour
{
    [field: SerializeField]public SpriteRenderer _groundFieldSpriteRenderer{ get; private set; }
    [field: SerializeField] public SelectMapButtonView[] _selectMapButtons { get; private set; }
}
