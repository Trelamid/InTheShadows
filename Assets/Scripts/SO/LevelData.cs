using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
	[Header("Основная информация")]
	public Sprite levelIcon;       // Иконка уровня
	public Sprite levelShadowIcon;       // Иконка тени уровня
	public string levelName;       // Название уровня
	public int levelNumber;        // Номер уровня
	public int envSceneNumber;
	public LevelData NextLevelData;
	
	[System.Flags]
	public enum AllowedMoves
	{
		None = 0,
		Horizontal = 1 << 0,
		Vertical = 1 << 1,
		Translation = 1 << 2
	}

	[System.Serializable]
	public class WinCondition
	{
		// public string figureName;      // Имя или ID фигуры
		public GameObject prefab; // Префаб фигуры
		public AllowedMoves allowedMoves;
		public Vector3 targetRotation; // Целевая позиция для победы
		public Quaternion targetRotationQ; // Целевая позиция для победы
		public float toleranceRotation = 20f; // Допустимая погрешность
		
		public bool checkPosition; //Нужно ли проверять позицию у фигуры
		public Vector3 targetPosition; // Целевая позиция для победы
		public float tolerancePosition = 1f; // Допустимая погрешность
	}

	[Header("Условия победы")]
	public WinCondition[] winConditions;
}