

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ScreenResolutionManager : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown; // Ссылка на UI Dropdown
    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;
    public Button isFullScreenButton;
    [SerializeField]
    private RenderTexture _pixelEffectsLayerTexture;
    [SerializeField]
    private RenderTexture _pixelEffectsUnderCarLayerTexture;
    [SerializeField]
    private Slider _fpsLockSlider;
    [SerializeField]
    private TMP_Text _fpsLockText;
 
private bool isFullscreen = true;
    void Start()
    {
        LoadResolutions();
    }

    void LoadResolutions()
    {
        isFullScreenButton.transform.GetChild(0).gameObject.SetActive(DataPersistenceManagerSystem._gameData.IsFullScreen);
        isFullScreenButton.onClick.AddListener(SetFullScreen);
      
        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();
        
        resolutionDropdown.ClearOptions();
        
        // 2. (Опционально) Фильтруем разрешения по частоте обновления текущего разрешения
        RefreshRate currentRefreshRate = Screen.currentResolution.refreshRateRatio;
        foreach (Resolution res in resolutions)
        {
            if (res.refreshRateRatio.value == currentRefreshRate.value)
                filteredResolutions.Add(res);
            
        }
        
        // 3. Создаем список строк для отображения в Dropdown
        List<string> options = new List<string>();
        for (int i = 0; i < filteredResolutions.Count; i++) {
            string option = filteredResolutions[i].width + " x " + filteredResolutions[i].height;
            options.Add(option);

            // Запоминаем индекс текущего разрешения, чтобы установить его в Dropdown по умолчанию
            if (filteredResolutions[i].width == Screen.width && filteredResolutions[i].height == Screen.height &&
                DataPersistenceManagerSystem._gameData.ResolutionIndex == -1)

                DataPersistenceManagerSystem._gameData.ResolutionIndex = i;
        }
        
        // 4. Добавляем опции в Dropdown и устанавливаем текущее значение
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = DataPersistenceManagerSystem._gameData.ResolutionIndex ;
        resolutionDropdown.RefreshShownValue();
        // Добавляем слушатель события изменения выбора в Dropdown
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        
        _pixelEffectsLayerTexture.Release();
        _pixelEffectsLayerTexture.width = Screen.width;
        _pixelEffectsLayerTexture.height = Screen.height;
        _pixelEffectsLayerTexture.Create();
        _pixelEffectsUnderCarLayerTexture.Release();
        _pixelEffectsUnderCarLayerTexture.width = Screen.width;
        _pixelEffectsUnderCarLayerTexture.height = Screen.height;
        _pixelEffectsUnderCarLayerTexture.Create();
        SetFpsLockSlider();
    }
    
    // Метод, вызываемый при выборе новой опции в Dropdown
    public void SetResolution(int resolutionIndex) {
        DataPersistenceManagerSystem._gameData.ResolutionIndex = resolutionIndex;
        Resolution selectedResolution = filteredResolutions[resolutionIndex];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, DataPersistenceManagerSystem._gameData.IsFullScreen);
        _pixelEffectsLayerTexture.width = selectedResolution.width;
        _pixelEffectsLayerTexture.height = selectedResolution.height;
        _pixelEffectsUnderCarLayerTexture.width = selectedResolution.width;
        _pixelEffectsUnderCarLayerTexture.height = selectedResolution.height;
        
        _pixelEffectsLayerTexture.Release();
        _pixelEffectsLayerTexture.width = selectedResolution.width;
        _pixelEffectsLayerTexture.height = selectedResolution.height;
        _pixelEffectsLayerTexture.Create();
        _pixelEffectsUnderCarLayerTexture.Release();
        _pixelEffectsUnderCarLayerTexture.width = selectedResolution.width;
        _pixelEffectsUnderCarLayerTexture.height = selectedResolution.height;
        _pixelEffectsUnderCarLayerTexture.Create();
    }

    private void SetFpsLockSlider() {
        _fpsLockSlider.minValue = 15;
        
        _fpsLockSlider.maxValue =  GetMonitorRefreshRate() + 1;

        _fpsLockSlider.value = DataPersistenceManagerSystem._gameData.FpsCountLock == -1? _fpsLockSlider.maxValue:DataPersistenceManagerSystem._gameData.FpsCountLock;
        
        _fpsLockSlider.onValueChanged.AddListener(SetTargetFps);

        SetTargetFpsText();
    }
    private void SetTargetFps(float value) {
        DataPersistenceManagerSystem._gameData.FpsCountLock = (int)value;
       
            Application.targetFrameRate = (int)_fpsLockSlider.value == (int)_fpsLockSlider.maxValue? -1:(int)value;
        
        SetTargetFpsText();
    }
    private void SetTargetFpsText() {
        _fpsLockText.text = "FPS lock:";
        _fpsLockText.text +=(int)_fpsLockSlider.value==(int)_fpsLockSlider.maxValue? BetweenScenesService.Instance.CsvLocalization.GetLocalizedValue("Unlimited"): _fpsLockSlider.value.ToString();
    }
    
    private int GetMonitorRefreshRate()
    {
        // Для обычных приложений
        int refreshRate = (int)Screen.currentResolution.refreshRateRatio.value;
        
        if (refreshRate <= 0)
        {
            Resolution[] resolutions = Screen.resolutions;
            if (resolutions.Length > 0)
                refreshRate = (int)resolutions[^1].refreshRateRatio.value;
            
        }
        
        return Mathf.Max(60, Mathf.RoundToInt(refreshRate));
    }
    // Переключение полноэкранного / оконного режимов
    public void SetFullScreen() {
        DataPersistenceManagerSystem._gameData.IsFullScreen= !DataPersistenceManagerSystem._gameData.IsFullScreen;
        Screen.fullScreen = DataPersistenceManagerSystem._gameData.IsFullScreen;
        isFullScreenButton.transform.GetChild(0).gameObject.SetActive(DataPersistenceManagerSystem._gameData.IsFullScreen);
    }
}
