
    using TMPro;

    public struct TaskComponent {
        public TaskType TaskType;
        public int NeedCount;
        public int CurrentCount;
        public TMP_Text TaskText;
        public int MoneyReward;
    }

    public enum TaskType {
        Kills,
        BrokenParts
    }
    
