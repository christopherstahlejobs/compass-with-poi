using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: Loads gameplay scene
/// </summary>
public sealed class GameplayInitializer : MonoBehaviour
{
	[Header("Scene Settings")]
	[SerializeField] private int _gameplaySceneIndex;

	private void Start()
	{
		if (_gameplaySceneIndex < 0 || _gameplaySceneIndex >= SceneManager.sceneCountInBuildSettings)
		{
			Debug.LogError($"GameplayInitializer: Invalid scene index {_gameplaySceneIndex}. Must be between 0 and {SceneManager.sceneCountInBuildSettings - 1}", this);
			return;
		}

		SceneManager.LoadScene(_gameplaySceneIndex, LoadSceneMode.Single);
	}
}

