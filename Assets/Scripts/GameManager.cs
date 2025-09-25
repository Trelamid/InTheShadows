using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference clickActionRef; // ЛКМ
    [SerializeField] private InputActionReference mouseDeltaRef;  // Движение мыши
    [SerializeField] private InputActionReference rotateModeRef;  // переключение в режим вращения
    [SerializeField] private InputActionReference moveModeRef;    // переключение в режим движения
    [SerializeField] private GameObject finshUI;

    private Camera _mainCamera;
    private LevelData _currentLevelData;

    private GameObject _spawnPlace;
    private GameObject _activeFigure;
    private LevelData.WinCondition _activeCondition;

    private InputAction _clickAction;
    private InputAction _mouseDeltaAction;
    private InputAction _rotateModeAction;
    private InputAction _moveModeAction;
    private string SavePath => Path.Combine(Application.persistentDataPath, "playerProgress.json");

    private bool _isDragging;
    private bool _positionMode; // false = rotate, true = move
    private int _rotationHorVerMode = 0;
    private bool _isFinish;

    private void Awake()
    {
        _currentLevelData = LevelLoader.CurrentLevel;
        if (_currentLevelData == null)
        {
            Debug.LogError("LevelData is null! Убедись, что LevelLoader.CurrentLevel заполнен.");
            return;
        }

        _clickAction      = clickActionRef.action;
        _mouseDeltaAction = mouseDeltaRef.action;
        _rotateModeAction = rotateModeRef.action;
        _moveModeAction   = moveModeRef.action;
        
        SpawnEnv();

        _mainCamera = Camera.main;
    }

    private void Init()
    {
        _clickAction.performed   += OnClick;
        _clickAction.canceled    += OnRelease;
        _mouseDeltaAction.performed += OnMouseDrag;

        _rotateModeAction.performed += _ => SetMode(false);
        _moveModeAction.performed   += _ => SetMode(true);

        _clickAction.Enable();
        _mouseDeltaAction.Enable();
        _rotateModeAction.Enable();
        _moveModeAction.Enable();
    }

    public void OnExit()
    {
        _clickAction.performed   -= OnClick;
        _clickAction.canceled    -= OnRelease;
        _mouseDeltaAction.performed -= OnMouseDrag;

        _rotateModeAction.performed -= _ => SetMode(false);
        _moveModeAction.performed   -= _ => SetMode(true);
        
        SceneManager.LoadScene(0);
    }

    public void NextLevel()
    {
        if (File.Exists(SavePath) && !LevelLoader.TestMode)
        {
            string json = File.ReadAllText(SavePath);
            var progress = JsonUtility.FromJson<PlayerProgress>(json);
            progress = new PlayerProgress { currentLevel = progress.currentLevel+1 };
            SavePlayerLevel(progress);
        }

        _clickAction.performed   -= OnClick;
        _clickAction.canceled    -= OnRelease;
        _mouseDeltaAction.performed -= OnMouseDrag;

        _rotateModeAction.performed -= _ => SetMode(false);
        _moveModeAction.performed   -= _ => SetMode(true);
        
        if (LevelLoader.CurrentLevel.NextLevelData != null)
        {
            LevelLoader.CurrentLevel = LevelLoader.CurrentLevel.NextLevelData;
            SceneManager.LoadScene(1, LoadSceneMode.Single);
        }
        else
            SceneManager.LoadScene(0);

    }
    
    public void SavePlayerLevel(PlayerProgress progress)
    {
        string json = JsonUtility.ToJson(progress, true);
        File.WriteAllText(SavePath, json);
    }

    private void OnClick(InputAction.CallbackContext ctx)
    {
        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out var hit))
        {
            foreach (var cond in _currentLevelData.winConditions)
            {
                if (hit.collider != null && hit.collider.gameObject.name.StartsWith(cond.prefab.name))
                {
                    _activeFigure    = hit.collider.gameObject;
                    _activeCondition = cond;
                    _isDragging      = true;

                    Debug.Log($"Выбрана фигура {_activeFigure.name}");
                    return;
                }
            }
        }
    }

    private void OnRelease(InputAction.CallbackContext ctx)
    {
        _isDragging = false;
        _activeFigure = null;
        _activeCondition = null;
        _rotationHorVerMode = 0;
    }

    // ======================= Движение мыши =======================
    private void OnMouseDrag(InputAction.CallbackContext ctx)
    {
        if (!_isDragging || _activeFigure == null || _activeCondition == null) return;

        Vector2 delta = ctx.ReadValue<Vector2>();

        if (_rotationHorVerMode == 0 && !_positionMode)
        {
            if (delta.x > delta.y)
                _rotationHorVerMode = 1;
            else
                _rotationHorVerMode = 2;
        }

        if (_positionMode)
            ApplyTranslation(delta);
        else
            ApplyRotation(delta);

        CheckWinCondition();
    }

    private void ApplyRotation(Vector2 delta)
    {
        float speed = 100f * Time.deltaTime;

        if (_activeCondition.allowedMoves.HasFlag(LevelData.AllowedMoves.Horizontal)
            // && _rotationHorVerMode == 1
            )
            _activeFigure.transform.Rotate(Vector3.down, delta.x * speed, Space.World);
        
        if (_activeCondition.allowedMoves.HasFlag(LevelData.AllowedMoves.Vertical)
            // && _rotationHorVerMode == 2
            )
            _activeFigure.transform.Rotate(Vector3.left, -delta.y * speed, Space.World);
        
        // Debug.Log(_activeFigure.transform.rotation+" AAA1 Qat");
    }
    
    private void CheckWinCondition()
    {
        if (_activeFigure == null || _activeCondition == null) return;

        // Конвертируем targetEuler -> Quaternion
        Quaternion targetQ = Quaternion.Euler(_activeCondition.targetRotation);

        // Считаем угол между текущим поворотом и целевым
        float angle = Quaternion.Angle(_activeFigure.transform.localRotation, targetQ);
        bool rotationOk = angle <= _activeCondition.toleranceRotation;

        // Debug.Log($"Q: {_activeFigure.transform.rotation} | targetQ: {targetQ} | Angle {angle}");

        // Проверка позиции (если нужна)
        bool positionOk = true;
        if (_activeCondition.checkPosition)
        {
            float dist = Vector3.Distance(_activeFigure.transform.localPosition, _activeCondition.targetPosition);
            positionOk = dist <= _activeCondition.tolerancePosition;
        }

        if (rotationOk && positionOk && !_isFinish)
        {
            _activeFigure.GetComponent<Collider>().enabled = false;
            _isFinish = true;
            Debug.Log(_currentLevelData.winConditions.Length);

            StartCoroutine(SnapToTarget(_activeFigure, _activeCondition.targetRotation, targetQ, _activeCondition,
                _currentLevelData.winConditions.Length <= 1));

            if (_currentLevelData.winConditions.Length > 1)
                RemoveCondition(_activeCondition);

            _activeFigure = null;
            _activeCondition = null;
        }
    }

    private IEnumerator SnapToTarget(GameObject obj, Vector3 activeConditionTargetRotation, Quaternion targetRot,
        LevelData.WinCondition cond, bool last)
    {
        float duration = 1.5f; // время доведения
        float elapsed = 0f;

        Vector3 startPos = obj.transform.localPosition;
        Quaternion startRot = obj.transform.localRotation;

        Vector3 targetPos = cond.checkPosition ? cond.targetPosition : obj.transform.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Плавная интерполяция (Slerp для вращения)
            obj.transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            obj.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        // Зафиксировать точно
        Debug.Log("AAA : "+activeConditionTargetRotation);
        // obj.transform.rotation = Quaternion.Euler(activeConditionTargetRotation);
        obj.transform.localRotation = Quaternion.Euler(activeConditionTargetRotation);
        obj.transform.localPosition = targetPos;

        Debug.Log("🎉 Level Completed (с доводкой)!");

        yield return new WaitForSeconds(1f);

        if (last)
            OnLevelCompleted();
        else
            _isFinish = false;
    }

    public void RemoveCondition(LevelData.WinCondition conditionToRemove)
    {
        _currentLevelData.winConditions = _currentLevelData.winConditions
            .Where(c => c != conditionToRemove)
            .ToArray();
    }    
    
    private void ApplyTranslation(Vector2 delta)
    {
        if (!_activeCondition.allowedMoves.HasFlag(LevelData.AllowedMoves.Translation)) return;
    
        float speed = 5f * Time.deltaTime;
        Vector3 move = new Vector3(0, delta.y, -delta.x) * speed;
    
        _activeFigure.transform.Translate(move, Space.World);
    }

    private void SetMode(bool positionMode)
    {
        _positionMode = positionMode;
        Debug.Log(_positionMode ? "Режим: перемещение" : "Режим: вращение");
    }

    // ======================= Загрузка окружения/фигур =======================
    private void SpawnEnv()
    {
        StartCoroutine(SpawnEnvRoutine());
    }

    private System.Collections.IEnumerator SpawnEnvRoutine()
    {
        // Грузим сцену (синхронно)
        SceneManager.LoadScene(_currentLevelData.envSceneNumber, LoadSceneMode.Additive);

        // Ждём хотя бы один кадр, пока Unity "приклеит" сцену
        yield return null;

        // Теперь ищем
        _spawnPlace = GameObject.FindWithTag("Player");
        if (_spawnPlace == null)
            Debug.LogError("❌ Не найден объект с тегом Player в env-сцене!");

        _mainCamera = Camera.main;
        if (_mainCamera == null)
            Debug.LogError("❌ Не найдена MainCamera в env-сцене!");

        // Спавним фигуры
        SpawnFigures();
        Init();
    }

    private void SpawnFigures()
    {
        float i = 0;
        foreach (var cond in _currentLevelData.winConditions)
        {
            // var obj = Instantiate(cond.prefab, new Vector3(Vector3.zero.x+i,Vector3.zero.y,Vector3.zero.z), Quaternion.identity, _spawnPlace.transform);
            var obj = Instantiate(cond.prefab, Vector3.zero, Quaternion.identity, _spawnPlace.transform);
            obj.transform.localPosition = new Vector3(Vector3.zero.x,Vector3.zero.y+i,Vector3.zero.z);
            obj.name = cond.prefab.name;
            i += -2f;
        }
    }

    private void OnLevelCompleted()
    {
        finshUI.SetActive(true);
    }
}
