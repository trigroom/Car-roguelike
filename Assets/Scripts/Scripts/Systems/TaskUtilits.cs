using System.Collections.Generic;
using UnityEngine;

public class TaskUtilits{
    private static readonly List<TaskType> TaskTypes = new List<TaskType> {
        TaskType.BrokenParts, TaskType.Kills
    };
    
    public static TaskType GetRandomTaskType() {
        return TaskTypes[Random.Range(0, TaskTypes.Count)];
    }

    public static void UpdateTaskText(TaskComponent taskCmp) {
        if (taskCmp.TaskType == TaskType.BrokenParts)
            taskCmp.TaskText.text = $"{taskCmp.CurrentCount}/{taskCmp.NeedCount} {BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue("brokenCarPartsTask")} {taskCmp.MoneyReward} $";
        else if (taskCmp.TaskType == TaskType.Kills)
            taskCmp.TaskText.text = $"{taskCmp.CurrentCount}/{taskCmp.NeedCount} {BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue("brokenCarsTask")} {taskCmp.MoneyReward} $";
        else if (taskCmp.TaskType == TaskType.CarIntegrity)
            taskCmp.TaskText.text = $"{taskCmp.CurrentCount}/{taskCmp.NeedCount} {BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue("endRoundsWithTask")} {taskCmp.MoneyReward} $";
    }
}
