using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class UILevelManager : MonoBehaviour
{
    [SerializeField]private GameObject levelCellPrefab;
    [SerializeField]private GameObject horizontal1;
    [SerializeField]private GameObject horizontal2;
    [SerializeField]private GameObject horizontal3;
    [SerializeField]private List<LevelData> levelDatas;

    private int _levelNow;
    private List<GameObject> _cells = new List<GameObject>();
    private string SavePath => Path.Combine(Application.persistentDataPath, "playerProgress.json");

    public void OnOpen(bool test)
    {
        if (test)
            _levelNow = 9;
        else
            GetPlayerLevel();
        
        GenerateLevelCells();
    }

    public void OnClose()
    {
        DeleteCells();
    }

    public void OnChoose(LevelData levelData)
    {
        //StartLevel();
    }

    private void GetPlayerLevel()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            var progress = JsonUtility.FromJson<PlayerProgress>(json);
            _levelNow = progress.currentLevel;
        }
        else
        {
            var progress = new PlayerProgress { currentLevel = 0 };
            SavePlayerLevel(progress);
            _levelNow = progress.currentLevel;
        }
    }
    
    public void SavePlayerLevel(PlayerProgress progress)
    {
        string json = JsonUtility.ToJson(progress, true);
        File.WriteAllText(SavePath, json);
    }

    private void GenerateLevelCells()
    {
        foreach (var level in levelDatas)
        {
            var parent = horizontal1;
            switch (level.levelNumber)
            {
                case <= 2:
                    parent = horizontal1;
                    break;
                case <= 5:
                    parent = horizontal2;
                    break;
                case <= 8:
                    parent = horizontal3;
                    break;
            }

            var cell = Instantiate(levelCellPrefab, parent.transform).GetComponent<LevelCell>();
            _cells.Add(cell.gameObject);
            cell.Init(level, 
                (level.levelNumber <= _levelNow ? 
                    (level.levelNumber == _levelNow ? LevelStatus.Next:LevelStatus.Open) 
                    : LevelStatus.Close)
                , this);
        }
    }

    private void DeleteCells()
    {
        foreach (var cell in _cells)
        {
            GameObject.Destroy(cell);
        }
        _cells.Clear();
    }
}
