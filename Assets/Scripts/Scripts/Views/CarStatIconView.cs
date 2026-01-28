using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CarStatIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField]
    private string _descriptionText;

    private Action<string, Vector2> _enterAction;
    private Action _exitAction;

    public void Construction(Action<string, Vector2> enterAction, Action exitAction) {
        _enterAction = enterAction;
        _exitAction = exitAction;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        _enterAction?.Invoke(_descriptionText, new Vector2(transform.position.x-250, transform.position.y) );
    }

    public void OnPointerExit(PointerEventData eventData) {
        _exitAction?.Invoke();
    }
}
