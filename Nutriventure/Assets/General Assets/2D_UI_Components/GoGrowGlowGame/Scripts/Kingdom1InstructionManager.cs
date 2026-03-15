using UnityEngine;
using UnityEngine.Playables;
using StarterAssets;
using System.Collections;

/// <summary>
/// Manages the Kingdom 1 first-visit instruction timeline.
/// On the player's first visit, plays the instruction PlayableDirector (timeline).
/// On subsequent visits, teleports the player directly to the lobby spawn point.
/// Attach this to a GameObject in the Kingdom 1 scene.
/// </summary>
public class Kingdom1InstructionManager : MonoBehaviour
{
  [Header("Timeline")]
  [Tooltip("The PlayableDirector that holds the instruction/cutscene timeline")]
  public PlayableDirector instructionDirector;

  [Header("Spawn Points")]
  [Tooltip("Where the player spawns on subsequent visits (lobby point)")]
  public Transform lobbySpawnPoint;

  [Header("Player References")]
  [Tooltip("The player's root Transform (Player Armature)")]
  public Transform playerTransform;
  [Tooltip("CharacterController on the player — needed to teleport properly")]
  public CharacterController characterController;
  [Tooltip("ThirdPersonController — disabled during the timeline")]
  public ThirdPersonController playerController;

  [Header("UI to Hide During Timeline")]
  [Tooltip("GameObjects to disable while the instruction timeline plays (e.g. joystick, HUD)")]
  public GameObject[] uiToHideDuringTimeline;

  [Header("Objects to Manage")]
  [Tooltip("GameObjects that should only be active during the instruction timeline (e.g. NPC actors, props)")]
  public GameObject[] instructionOnlyObjects;
  [Tooltip("GameObjects that should only be active during normal gameplay (e.g. lobby NPCs)")]
  public GameObject[] gameplayOnlyObjects;

  private bool isPlayingTimeline = false;

  void Start()
  {
    StartCoroutine(InitializeAfterFrame());
  }

  private IEnumerator InitializeAfterFrame()
  {
    // Wait one frame so GameDataManager and other singletons are ready
    yield return null;

    if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
    {
      #if UNITY_EDITOR
      Debug.LogError("Kingdom1InstructionManager: GameDataManager not available!");
      #endif
      // Fallback — skip timeline, spawn at lobby
      SpawnAtLobby();
      yield break;
    }

    bool hasPlayed = GameDataManager.Instance.CurrentGameData.hasPlayedK1Instruction;

    if (!hasPlayed)
    {
      #if UNITY_EDITOR
      Debug.Log("Kingdom1InstructionManager: First visit — playing instruction timeline");
      #endif
      PlayInstructionTimeline();
    }
    else
    {
      #if UNITY_EDITOR
      Debug.Log("Kingdom1InstructionManager: Returning visit — spawning at lobby");
      #endif
      SkipTimeline();
      SpawnAtLobby();
    }
  }

  private void PlayInstructionTimeline()
  {
    isPlayingTimeline = true;

    // Activate instruction-only objects
    SetObjectsActive(instructionOnlyObjects, true);
    SetObjectsActive(gameplayOnlyObjects, false);

    // Hide gameplay UI during the timeline
    SetObjectsActive(uiToHideDuringTimeline, false);

    // Disable player movement during the timeline
    if (playerController != null)
      playerController.enabled = false;

    // Play the timeline
    if (instructionDirector != null)
    {
      instructionDirector.gameObject.SetActive(true);
      instructionDirector.stopped += OnTimelineFinished;
      instructionDirector.Play();
      #if UNITY_EDITOR
      Debug.Log("Kingdom1InstructionManager: Timeline started");
      #endif
    }
    else
    {
      #if UNITY_EDITOR
      Debug.LogError("Kingdom1InstructionManager: No PlayableDirector assigned! Completing immediately.");
      #endif
      OnInstructionComplete();
    }
  }

  private void OnTimelineFinished(PlayableDirector director)
  {
    director.stopped -= OnTimelineFinished;
    #if UNITY_EDITOR
    Debug.Log("Kingdom1InstructionManager: Timeline finished");
    #endif
    OnInstructionComplete();
  }

  private void OnInstructionComplete()
  {
    isPlayingTimeline = false;

    // Mark as played and save immediately
    if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
    {
      GameDataManager.Instance.CurrentGameData.hasPlayedK1Instruction = true;
      GameDataManager.Instance.SaveGameData();
      #if UNITY_EDITOR
      Debug.Log("Kingdom1InstructionManager: Instruction flag saved");
      #endif
    }

    // Deactivate instruction-only objects
    SetObjectsActive(instructionOnlyObjects, false);
    SetObjectsActive(gameplayOnlyObjects, true);

    // Re-enable gameplay UI
    SetObjectsActive(uiToHideDuringTimeline, true);

    // Teleport player to lobby spawn after the timeline
    SpawnAtLobby();

    // Re-enable player movement
    if (playerController != null)
      playerController.enabled = true;
  }

  private void SkipTimeline()
  {
    // Make sure the timeline director is stopped and inactive
    if (instructionDirector != null)
    {
      instructionDirector.Stop();
      instructionDirector.gameObject.SetActive(false);
    }

    // Deactivate instruction-only objects, activate gameplay objects
    SetObjectsActive(instructionOnlyObjects, false);
    SetObjectsActive(gameplayOnlyObjects, true);
  }

  private void SpawnAtLobby()
  {
    if (lobbySpawnPoint == null || playerTransform == null)
    {
      #if UNITY_EDITOR
      Debug.LogWarning("Kingdom1InstructionManager: Missing lobbySpawnPoint or playerTransform!");
      #endif
      return;
    }

    // Disable CharacterController so we can set position directly
    if (characterController != null)
      characterController.enabled = false;

    playerTransform.position = lobbySpawnPoint.position;
    playerTransform.rotation = lobbySpawnPoint.rotation;

    if (characterController != null)
      characterController.enabled = true;

    #if UNITY_EDITOR
    Debug.Log($"Kingdom1InstructionManager: Player spawned at lobby ({lobbySpawnPoint.position})");
    #endif
  }

  private void SetObjectsActive(GameObject[] objects, bool active)
  {
    if (objects == null) return;
    foreach (var obj in objects)
    {
      if (obj != null)
        obj.SetActive(active);
    }
  }

  /// <summary>
  /// Call this from a UI skip button if you want to let the player skip the instruction.
  /// </summary>
  public void SkipInstructionTimeline()
  {
    if (!isPlayingTimeline) return;

    #if UNITY_EDITOR
    Debug.Log("Kingdom1InstructionManager: Player skipped instruction timeline");
    #endif

    if (instructionDirector != null)
    {
      instructionDirector.stopped -= OnTimelineFinished;
      instructionDirector.Stop();
    }

    OnInstructionComplete();
  }

  /// <summary>
  /// Debug/editor utility: resets the instruction flag so it plays again on next visit.
  /// </summary>
  public void ResetInstructionFlag()
  {
    if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
    {
      GameDataManager.Instance.CurrentGameData.hasPlayedK1Instruction = false;
      GameDataManager.Instance.SaveGameData();
      #if UNITY_EDITOR
      Debug.Log("Kingdom1InstructionManager: Instruction flag reset — will play again on next visit");
      #endif
    }
  }
}
