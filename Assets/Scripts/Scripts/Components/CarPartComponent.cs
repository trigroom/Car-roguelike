using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
    public struct CarPartComponent {
        public float MaxHealth;
        public int MainEntity;
        public List<CarPartStuffView> CellCarPartsStuffViews;
        public Image CarPartCellImage;
        public SpriteRenderer CarPartCellRenderer;
        public int RotateDegrees;
        
        public int HealthMaterialIndex;
    }
