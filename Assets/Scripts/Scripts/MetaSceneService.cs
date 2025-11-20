
using UnityEngine;
using UnityEngine.UI;

public class MetaSceneService : MonoBehaviour {
    [field: SerializeField] public SelectCarButtonView[] _selectCarButtonViews { get; private set; }
    [field: SerializeField] public Transform _selectCarUIContainer { get; private set; }
    [field: SerializeField] public Transform _selectMapUIContainer { get; private set; }
    [field: SerializeField] public SelectDifficltyUIView _selectMapDifficltyUIView { get; private set; }
    [field: SerializeField] public Button[] _selectMapButtons { get; private set; }
    private void Awake() {
        BetweenScenesService.Instance.HideBlackScreen();
    }
}
