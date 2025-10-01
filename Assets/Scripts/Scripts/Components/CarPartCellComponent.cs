

using System.Collections.Generic;
using UnityEngine;

public struct CarPartCellComponent
{
   public List<int> CarPartsId;
   public float CurrentHealth;
   public float MaxHealth;
   public int CurrentRotateAngle;
   public CellView CellView;
   public int MainEntity;
   public CarPartStuffView MainCellPart;
}