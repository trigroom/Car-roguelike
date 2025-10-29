
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using DG.Tweening;

    public struct InventoryComponent {
        public List<(int Id, int Level)> PartsInInventory;
        public int CurrentInventoryPage;

        public float PressedButtonTime;
        public bool IsTryDraggedCell;
        public bool CellIsSetup;
        
        public int CurrentDraggedCellEntity;
        public CellView CurrentCellCarPart;
        public List<CarPartType> SortedCarPartType;
        public int[,] GridCarPartCellsEntities;
        public Sequence CurrentTween;
        public Sequence CurrentTweenChangeModeButton;
        public int CurrentRotateDegrees;
        public Vector3 StartMouseInputPosition;
        public Vector2 onCarCellSize;
        public bool IsCarPartsRedactorMode;
        public int CurrentMarkedCellForPanelInfo;
        public CarPartInGameUIView[,] CarPartInfoUIViewsGrid;
        public CarPartInGameUIView[,] CarPartInfoUIViewsUpLevelGrid;
        public Vector2 StartOffsetFromCell;
        public Vector3 EdgeInputPosition;
        public Tween CurrentMoneyTween;
        
        public Vector2Int LastCheckedGridCellPosition;
        public bool IsSnappingToGrid;
    }
