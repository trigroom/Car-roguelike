using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class StartFieldCustomRedactor : MonoBehaviour {
    [SerializeField] private Vector2 spacing = new Vector2(10, 10); // Отступы между кнопками
    [SerializeField] private float padding = 20; 
    
    private RedactorFieldButton[,] _fieldButtons;
    
    private CarPartType[,] _fieldCellTypes;
    
    public Button ButtonPrefab;
    
    public RedactorFieldButton FieldButtonPrefab;
    public int CarWidth;
    public int CarHeight;
    
    public CarPartType[] CellTypesToRedactor;
    
    public CarPartType CurrentCellType;
    
    public TMP_Text CurrentCellTypeText;
    public Transform ChooseCellTypeButtonsContainer;
    public RectTransform FieldRect;
    public TMP_InputField FieldInput;
    private string _filePath = "Assets/Configs/EnemyCarsConfigs";

    public void Start() {
        SetRedactButtons();
        SetFieldButtons();
    }

    private void SetRedactButtons()
    {
        foreach (Transform child in ChooseCellTypeButtonsContainer)
        {
            Destroy(child.gameObject);
        }

            foreach (var curCellType in CellTypesToRedactor)
            {
                var buttonPrefab = Instantiate(ButtonPrefab, ChooseCellTypeButtonsContainer);
                buttonPrefab.onClick.AddListener(() => ChangeCurrentCellType(curCellType));
                buttonPrefab.GetComponentInChildren<TMP_Text>().text = curCellType.ToString();
            }
    }

    public void ChangeFieldRedactorState(bool isOn)
    {
        SetRedactButtons();
        SetFieldButtons();
    }
    
    public void SetFieldButtons()
    {
        
        foreach (Transform child in FieldRect)
        {
            Destroy(child.gameObject);
        }


        SetButtonsPosition();
    }

    private void SetButtonsPosition()
    {
        
        _fieldButtons = new RedactorFieldButton[CarHeight, CarWidth];
        _fieldCellTypes = new CarPartType[CarHeight, CarWidth];
        
        float panelWidth = FieldRect.rect.width - padding * 2;
        float panelHeight = FieldRect.rect.height - padding * 2;

        float buttonWidth = (panelWidth - spacing.x * (CarWidth - 1)) / CarWidth;
        float buttonHeight = (panelHeight - spacing.y * (CarHeight - 1)) / CarHeight;
        for (int i = 0; i < CarHeight; i++) {
            for (int j = 0; j < CarWidth; j++) {
                float posX =- FieldRect.rect.width/2 + padding + j * (buttonWidth + spacing.x) + buttonWidth / 2;
                float posY =  FieldRect.rect.height/2 + -padding - i * (buttonHeight + spacing.y) - buttonHeight / 2;
                Vector2 butPos = new Vector2(posX,posY);

                    SetGameFieldButton(butPos, i, j, buttonWidth, buttonHeight);
            }
        }
    }

    private void SetGameFieldButton(Vector2 butPos, int i, int j, float sizeX,float sizeY)
    { 
        var button = Instantiate(FieldButtonPrefab, FieldRect);
        button.transform.localPosition = butPos;
        button.SetData(new Vector2Int(i, j), ChangeCellCellType, new Vector2(sizeX, sizeY));
        button.SetType(CarPartType.None);
        _fieldButtons[i, j] = button;
    }

    public void ChangeCurrentCellType(CarPartType cellType) {
        CurrentCellType = cellType;
        CurrentCellTypeText.text = cellType.ToString();
    }

    public void ChangeCellCellType(Vector2Int coord) {
     
        _fieldCellTypes[coord.x, coord.y] = CurrentCellType;
        _fieldButtons[coord.x, coord.y].SetType(_fieldCellTypes[coord.x, coord.y]);
    }

    public void ClearField() {
        for (int i = 0; i < CarWidth; i++) {
            for (int j = 0; j < CarWidth; j++) {
                _fieldCellTypes[i, j] = CarPartType.None;
                _fieldButtons[i, j].SetType(CarPartType.None);
            }
        }
    }

#if UNITY_EDITOR

    public void SaveConfigButton()
    {
            SaveStartGameFieldToConfig();
    }
    
    private void SaveStartGameFieldToConfig() {
        if (FieldInput.text == "") return;
        EnemyCarPartsMaps config = ScriptableObject.CreateInstance<EnemyCarPartsMaps>();
        config.CreateGrid(_fieldCellTypes);

        string assetPath = _filePath + "/" + FieldInput.text + ".asset";

        EnemyCarPartsMaps existingConfig = AssetDatabase.LoadAssetAtPath<EnemyCarPartsMaps>(assetPath);

        if (existingConfig != null) {
            existingConfig.CreateGrid(_fieldCellTypes);
            EditorUtility.SetDirty(existingConfig);
        } else
            AssetDatabase.CreateAsset(config, assetPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
#endif
}