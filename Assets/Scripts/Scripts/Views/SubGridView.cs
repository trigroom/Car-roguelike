using System;
using UnityEngine;
using UnityEngine.UI;

public class SubGridView : MonoBehaviour {
    [SerializeField]
    private Image _dragedCellCanvasGroup;
    [SerializeField]
    private Image[] _subGridImages;
    [SerializeField]
    private Color _canPlaceColor;
    [SerializeField]
    private Color _canNotPlaceColor;
    public void SetSubGridSize(int gridSize) {
        foreach (var gridChild in _subGridImages) {
            gridChild.pixelsPerUnitMultiplier = gridSize;
        }
    }


    public bool GetCurrentState() {
        return _subGridImages[0].color == _canPlaceColor;
    }
    
    
    public void SetSubGridState(bool state) {
        foreach (var gridChild in _subGridImages) {
            gridChild.color = state ? _canPlaceColor : _canNotPlaceColor;
        }
        _dragedCellCanvasGroup.color = state ? new Color(1,1,1,1f) : new Color(1,1,1,0.5f);
    }


}

