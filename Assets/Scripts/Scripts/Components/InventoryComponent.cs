
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using DG.Tweening;

    public struct InventoryComponent {
        public int CurrentInventoryPage;

        public float PressedButtonTime;
        public bool IsTryDraggedCell;
        public bool CellIsSetup;
        
        public int CurrentDraggedCellEntity;
        public CellView CurrentCellCarPart;
        public List<CarPartType> SortedCarPartType;
        public Sequence CurrentTween;
        public Sequence CurrentTweenChangeModeButton;
        public int CurrentRotateDegrees;
        public Vector3 StartMouseInputPosition;
        public Vector2 OnCarCellSize;
        public bool IsCarPartsRedactorMode;
        public int CurrentMarkedCellForPanelInfo;
        public CarPartInGameUIView[,] CarPartInfoUIViewsGrid;
        public CarPartInGameUIView[,] CarPartInfoUIViewsUpLevelGrid;
        public Vector2 StartOffsetFromCell;
        public Tween CurrentMoneyTween;
        public Sequence CurrentCarPartContainersTween;
        
        public Vector2Int LastCheckedGridCellPosition;
        public bool IsSnappingToGrid;
        public bool SelectCarButtonsNotInteractable;
        public bool IsModuleInventoryMode;
        public List<int> PlayerTeammateCarsEntity;
        public int CurrentRedactedCarEntity;
        public int CurrentSelectedModuleCellId;
        public List<int> CurrentGivenItems;
    }
