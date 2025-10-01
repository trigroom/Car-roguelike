using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RedactorFieldButton : MonoBehaviour {
    private Action<Vector2Int> _onChange;
    private Vector2Int _coord;

    [SerializeField]
    private TextMeshProUGUI _nameText;

    [SerializeField]
    private Image _background;
    
    [SerializeField]
    private RectTransform _rect;

    public void SetData(Vector2Int coord, Action<Vector2Int> onChange, Vector2 size) {
        _coord = coord;
        _onChange = onChange;
        _rect.sizeDelta = size;
    }

    public void SetType(CarPartType type) {
        _nameText.text = type.ToString();
        _background.color = Colors[type];
    }

    public void OnChange() {
        _onChange?.Invoke(_coord);
    }

    public void OnChangeWithMouseCheck() {
        if (!Input.GetMouseButton(0)) {
            return;
        }

        OnChange();
    }

    public static readonly Dictionary<CarPartType, Color> Colors = new Dictionary<CarPartType, Color>() {
        { CarPartType.None, Color.white },
        { CarPartType.Support, Color.green },
        { CarPartType.MeleeWeapon, Color.red },
        { CarPartType.Gun, Color.yellow },
        { CarPartType.Armor, Color.blue },
        { CarPartType.Main, Color.magenta }
    };
}