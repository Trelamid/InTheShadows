using System;
using System.Collections.Generic;
using UnityEngine;

public class UILevelManager : MonoBehaviour
{
    [SerializeField]private GameObject levelCellPrefab;
    [SerializeField]private GameObject horizontal1;
    [SerializeField]private GameObject horizontal2;
    [SerializeField]private GameObject horizontal3;
    [SerializeField]private List<LevelData> levelDatas;

    private int _levelNow;

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
        
    }

    public void OnChoose(LevelData levelData)
    {
        //StartLevel();
    }

    private void GetPlayerLevel()
    {
        _levelNow = 5;
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
            cell.Init(level, 
                (level.levelNumber <= _levelNow ? 
                    (level.levelNumber == _levelNow ? LevelStatus.Next:LevelStatus.Open) 
                    : LevelStatus.Close)
                , this); //TODO наприсать подтягивание уровня из json
        }
    }
}
