
   public struct SlotMachineComponent {
        public SlotValues[] CurrentSlotValues;
        public float[] CurrentPositionValues;
        public int[] CurrentPositionItemIndexValues;
        public bool IsStarted;
        public float SwitchDelay;
        public float CurrentRollTime;
        public int TryCount;
        public int CurrentBet;
   }
    public struct SlotValues {
        public int[] ItemsId;
        public float RollSpeed;

        public SlotValues(int[] itemsId, float rollSpeed) {
            ItemsId = itemsId;
            RollSpeed = rollSpeed;
        }
    }
    
