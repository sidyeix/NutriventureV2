using System;
using UnityEngine;

[Serializable]
public class BattleRuntimeState
{
  public int version = 1;
  public bool hasActiveBattle;
  public string playerEnerlingName;
  public string opponentEnerlingName;
  public BattlePlayerRuntimeState playerState;
  public BattleAIRuntimeState aiState;
  public BattleTurnRuntimeState turnState;
  public string savedAtUtc;
}

[Serializable]
public class BattlePlayerRuntimeState
{
  public int currentLife;
  public int currentArmor;
  public int activeDefend;
  public bool hasDefend;
  public bool defendUsedThisTurn;
  public int skill1Cooldown;
  public int skill2Cooldown;
  public int skill3Cooldown;
  public int skill4Cooldown;
  public int organCooldownTimer;
  public int maxOrganCooldown;
  public bool organCooldownReady;
}

[Serializable]
public class BattleAIRuntimeState
{
  public int currentLife;
  public int currentArmor;
  public int activeDefend;
  public bool hasDefend;
  public int skill1Cooldown;
  public int skill2Cooldown;
  public int skill3Cooldown;
  public int skill4Cooldown;
  public int organCooldownTimer;
  public int maxOrganCooldown;
  public bool organCooldownReady;
}

[Serializable]
public class BattleTurnRuntimeState
{
  public bool isPlayerTurn;
  public bool isTurnActive;
  public float currentTurnTime;
  public int currentRound;
  public float gameTimer;
}

public static class BattleRuntimeStateStore
{
  private const string RuntimeStateKey = "OCR_Battle_RuntimeState";

  public static BattleRuntimeState PendingResumeState { get; private set; }

  public static bool ShouldDeferBattleInitialization
  {
    get
    {
      PreloadFromPrefs();
      return PendingResumeState != null && PendingResumeState.hasActiveBattle;
    }
  }

  public static void PreloadFromPrefs()
  {
    if (PendingResumeState != null)
      return;

    if (!PlayerPrefs.HasKey(RuntimeStateKey))
      return;

    string json = PlayerPrefs.GetString(RuntimeStateKey, string.Empty);
    if (string.IsNullOrWhiteSpace(json))
      return;

    try
    {
      PendingResumeState = JsonUtility.FromJson<BattleRuntimeState>(json);
    }
    catch (Exception ex)
    {
      Debug.LogWarning($"BattleRuntimeStateStore preload failed: {ex.Message}");
      PendingResumeState = null;
      PlayerPrefs.DeleteKey(RuntimeStateKey);
      PlayerPrefs.Save();
    }
  }

  public static BattleRuntimeState GetPendingState()
  {
    PreloadFromPrefs();
    return PendingResumeState;
  }

  public static void SaveState(BattleRuntimeState state)
  {
    if (state == null)
      return;

    state.savedAtUtc = DateTime.UtcNow.ToString("o");
    string json = JsonUtility.ToJson(state);
    PendingResumeState = state;
    PlayerPrefs.SetString(RuntimeStateKey, json);
    PlayerPrefs.Save();
  }

  public static void ClearState()
  {
    PendingResumeState = null;
    PlayerPrefs.DeleteKey(RuntimeStateKey);
    PlayerPrefs.Save();
  }
}
