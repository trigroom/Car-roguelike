using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
    public struct CarPartComponent {
        public int CarPartId;
        public float CurrentHealth;
        public float MaxHealth;
        public int MainEntity;
        public List<CarPartStuffView> CellCarPartsStuffViews;
        public Image carPartCellImage;
        public SpriteRenderer CarPartCellRenderer;
        public int PartLevel;
    }
