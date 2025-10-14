
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using DG.Tweening;

    public struct InventoryComponent {
        public List<int> PartsInInventory;
        public int CurrentInventoryPage;

        public float PressedButtonTime;
        public bool IsTryDraggedCell;
        public bool CellIsSetup;
        
        public int CurrentDraggedCellEntity;
        public CellView CurrentCellCarPart;
        public CarPartType SortedCarPartType;
        public int[,] GridCarPartCellsEntities;
        public Tween CurrentTween;
        public int CurrentRotateDegrees;
        public Vector3 StartMouseInputPosition;
        public Vector2 onCarCellSize;
        public bool IsCarPartsRedactorMode;
        public int CurrentMarkedCellForPanelInfo;
        
        public Vector2Int LastCheckedGridCellPosition;
        public bool IsSnappingToGrid;
    }
