using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] private Button rotationB;
    [SerializeField] private Button positionB;
    [SerializeField] private Button window1920;
    [SerializeField] private Button window2560;
    [SerializeField] private Slider soundSlider;

    [Header("Texts (для отображения текущих кнопок)")]
    [SerializeField] private TMP_Text rotationText;
    [SerializeField] private TMP_Text positionText;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference rotationAction;
    [SerializeField] private InputActionReference positionAction;
    [SerializeField] private GameObject pressWindow;

    private void Start()
    {
        // Настройки звука
        soundSlider.onValueChanged.AddListener(delegate { ChangeSoundSliderValue(); });

        // Настройки разрешения
        window1920.onClick.AddListener(() => ChangeWindowSize(1920, 1080));
        window2560.onClick.AddListener(() => ChangeWindowSize(2560, 1440));

        // Ребинд кнопок
        rotationB.onClick.AddListener(() => StartRebind(rotationAction, rotationText));
        positionB.onClick.AddListener(() => StartRebind(positionAction, positionText));

        // Загружаем бинды из PlayerPrefs
        LoadRebind(rotationAction, rotationText);
        LoadRebind(positionAction, positionText);

        // Загружаем звук
        soundSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
        AudioListener.volume = soundSlider.value;
    }

    // ================= ЗВУК =================
    private void ChangeSoundSliderValue()
    {
        float volume = soundSlider.value;
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("Volume", volume);
        PlayerPrefs.Save();
        Debug.Log($"Volume set to {volume}");
    }

    // ================= ОКНА =================
    private void ChangeWindowSize(int width, int height)
    {
        Screen.SetResolution(width, height, FullScreenMode.Windowed);
        Debug.Log($"Resolution changed to {width}x{height}");
    }

    // ================= РЕБИНДЫ =================
    private void StartRebind(InputActionReference actionRef, TMP_Text label)
    {
        pressWindow.SetActive(true);
        var action = actionRef.action;
        action.Disable();

        Debug.Log($"Rebinding {action.name}... Нажмите кнопку!");

        action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse/Position") // чтобы не словил движение мыши
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(operation =>
            {
                action.Enable();
                operation.Dispose();

                string newBind = action.bindings[0].effectivePath;
                Debug.Log($"{action.name} rebound to: {newBind}");

                // обновляем текст в UI
                if (label != null) label.text = GetReadableBinding(newBind);

                // сохраняем
                PlayerPrefs.SetString(action.id.ToString(), newBind);
                PlayerPrefs.Save();
                
                pressWindow.SetActive(false);
            })
            .Start();
    }

    private void LoadRebind(InputActionReference actionRef, TMP_Text label)
    {
        var action = actionRef.action;
        string key = action.id.ToString();

        if (PlayerPrefs.HasKey(key))
        {
            string savedBinding = PlayerPrefs.GetString(key);
            action.ApplyBindingOverride(0, savedBinding);
            if (label != null) label.text = GetReadableBinding(savedBinding);
            Debug.Log($"Loaded binding for {action.name}: {savedBinding}");
        }
        else
        {
            if (label != null) label.text = GetReadableBinding(action.bindings[0].effectivePath);
        }
    }
    
    private string GetReadableBinding(string bindingPath)
    {
        if (string.IsNullOrEmpty(bindingPath)) return "";

        int slashIndex = bindingPath.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < bindingPath.Length - 1)
        {
            return bindingPath.Substring(slashIndex + 1);
        }
        return bindingPath;
    }
}
