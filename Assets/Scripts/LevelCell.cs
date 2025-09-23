using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LevelStatus
{
    Open = 0,
    Close = 1,
    Next = 2
}

public class LevelCell : MonoBehaviour
{
    [SerializeField] private GameObject close;
    [SerializeField] private GameObject open;
    [SerializeField] private Image levelIcon;
    [SerializeField] private Image levelShadowIcon;
    [SerializeField] private GameObject next;
    [SerializeField] private GameObject text;
    [SerializeField] private Button button;

    private LevelData _levelData;
    private LevelStatus _status;
    private UILevelManager _levelManager;

    public void Init(LevelData levelData, LevelStatus newStatus, UILevelManager levelManager)
    {
        _levelData = levelData;
        _levelManager = levelManager;
        
        levelIcon.sprite = _levelData.levelIcon;
        levelShadowIcon.sprite = _levelData.levelShadowIcon;
        text.GetComponent<TextMeshProUGUI>().text = _levelData.levelName;
        
        SetStatus(newStatus);
    }

    public void OnCellClick()
    {
        _levelManager.OnChoose(_levelData);
    }

    private void SetStatus(LevelStatus newStatus)
    {
        switch (newStatus)
        {
            case LevelStatus.Open :
                open.SetActive(true);
                text.SetActive(true);
                break;
            case LevelStatus.Close :
                close.SetActive(true);
                button.interactable = false;
                break;
            case LevelStatus.Next :
                next.SetActive(true);
                text.SetActive(true);
                break;
            default:
                break;
        }

        _status = newStatus;
    }    
}
