using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class RedactorMetaFieldButton : MonoBehaviour
{
    private Action<Vector2Int> _onChange;
    private Vector2Int _coord;

    [SerializeField]
    private TextMeshProUGUI _nameText;

    [SerializeField]
    private Image _background;

    public void SetData(Vector2Int coord, Action<Vector2Int> onChange) {
        _coord = coord;
        _onChange = onChange;
    }

    public void SetType(int type) {
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

    public static readonly Dictionary<int, Color> Colors = new Dictionary<int, Color>() {
        { 1, Color.white },
        { 2, Color.red },
        { 3, Color.blue },
        { 4, Color.yellow },
        { 5, Color.magenta },
        { 6, Color.green },
        { 7, Color.grey },
        { 8, Color.cyan },
        { 9, Color.white },
        { 10,Color.red },
        { 11, Color.blue  },
        { 12,  Color.yellow  },
        { 13, Color.magenta },
        { 14, Color.green },
        { 15, Color.grey },
    };
}
