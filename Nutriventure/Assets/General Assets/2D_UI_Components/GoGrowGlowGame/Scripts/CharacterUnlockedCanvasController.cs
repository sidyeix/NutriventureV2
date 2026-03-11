using UnityEngine;

/// <summary>
/// Handles the "Character Unlocked" animated canvas.
/// Attach to the canvas or a manager object. Wire the Continue button to OnContinueClicked().
/// </summary>
public class CharacterUnlockedCanvasController : MonoBehaviour
{
  [Header("Character To Unlock")]
  [Tooltip("The character ID from CharacterDatabase to unlock")]
  public int characterIDToUnlock;

  [Header("Canvas Reference")]
  [Tooltip("The character-unlocked canvas GameObject to close")]
  public GameObject characterUnlockedCanvas;

  /// <summary>
  /// Hook this to the Continue button's OnClick event.
  /// </summary>
  public void OnContinueClicked()
  {
    UnlockCharacter();
    CloseCanvas();
  }

  private void UnlockCharacter()
  {
    if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
    {
#if UNITY_EDITOR
      Debug.LogError("CharacterUnlockedCanvasController: GameDataManager not available!");
#endif
      return;
    }

    var gameData = GameDataManager.Instance.CurrentGameData;

    if (!gameData.unlockedCharacterIDs.Contains(characterIDToUnlock))
    {
      gameData.unlockedCharacterIDs.Add(characterIDToUnlock);
      GameDataManager.Instance.SaveGameData();
    }
  }

  private void CloseCanvas()
  {
    if (characterUnlockedCanvas != null)
    {
      characterUnlockedCanvas.SetActive(false);
    }
  }
}
