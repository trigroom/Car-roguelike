using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class EvemiesCarBuilderView : MonoBehaviour {
    public Button CreateEnemyConfigButton;
    public TMP_InputField EnemyConfigNameInputField;
    public Button AdminPanelSwitch;
    public TMP_InputField MoneyInputField;
    public Button GetMoneyButton;
    public TMP_InputField GetCarPartIdInputField;
    public Button GetCarPartIdButton;
    public Transform AdminPanelContainer;
    private string _filePath = "Assets/Configs/EnemyCarsConfigs";
    private void Start() {
        AdminPanelSwitch.onClick.AddListener(SwitchAdminPanelMode);
    }

    private void SwitchAdminPanelMode() {
        AdminPanelContainer.gameObject.SetActive(!AdminPanelContainer.gameObject.activeInHierarchy);
    }

    public void SaveEnemyCarConfig(int carId,List<CarPartTypeRow> inputGridDownLevel, List<CarPartTypeRow> inputGridUpLevel, Vector2Int gridSize) {
        if (EnemyConfigNameInputField.text == "") return;
        EnemyCarPartsMaps config = ScriptableObject.CreateInstance<EnemyCarPartsMaps>();
        config.CreateGrid(carId,inputGridDownLevel, inputGridUpLevel, gridSize);

        string assetPath = _filePath + "/" + EnemyConfigNameInputField.text + ".asset";

        EnemyCarPartsMaps existingConfig = AssetDatabase.LoadAssetAtPath<EnemyCarPartsMaps>(assetPath);

        if (existingConfig != null) {
            existingConfig.CreateGrid(carId,inputGridDownLevel, inputGridUpLevel, gridSize);
            EditorUtility.SetDirty(existingConfig);
        } else
            AssetDatabase.CreateAsset(config, assetPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
