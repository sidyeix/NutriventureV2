using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debug helper: when added to a scene and enabled, it forces a set of enerlings
/// to be in an active healing state with a specified remaining time (default 15 minutes).
/// </summary>
public class DebugEnerlingHealingSimulator : MonoBehaviour
{
  [Header("References")]
  public IngredientDatabase ingredientDatabase; // Optional: will use PersistentDataManager.ingredientDatabase if null

  [Header("Simulation Settings")]
  [Tooltip("How many enerlings to mark as healing.")]
  public int enerlingsToSimulate = 5;

  [Tooltip("How many minutes should remain on the regen timer.")]
  public float remainingMinutes = 15f;

  [Tooltip("Reduce life by this fraction of base life when starting regen (0.2 = 20% missing).")]
  [Range(0.05f, 0.5f)]
  public float missingLifeFraction = 0.2f;

  [Tooltip("Run automatically in Start(). If false, call SimulateNow() manually.")]
  public bool runOnStart = true;

  [Tooltip("Log what was simulated to the console.")]
  public bool logDetails = true;

  void Start()
  {
    if (runOnStart)
    {
      SimulateNow();
    }
  }

  [ContextMenu("Simulate Healing Now")]
  public void SimulateNow()
  {
    var pdm = PersistentDataManager.Instance;
    if (pdm == null)
    {
      Debug.LogWarning("DebugEnerlingHealingSimulator: PersistentDataManager not found.");
      return;
    }

    var db = ingredientDatabase != null ? ingredientDatabase : pdm.ingredientDatabase;
    if (db == null)
    {
      Debug.LogWarning("DebugEnerlingHealingSimulator: IngredientDatabase not assigned and not found on PDM.");
      return;
    }

    List<IngredientDatabase.IngredientInfo> unlocked = db.GetUnlockedIngredients();
    if (unlocked.Count == 0)
    {
      Debug.LogWarning("DebugEnerlingHealingSimulator: No unlocked enerlings to simulate.");
      return;
    }

    int count = Mathf.Min(enerlingsToSimulate, unlocked.Count);
    float desiredRemainingSeconds = Mathf.Max(1f, remainingMinutes * 60f);

    for (int i = 0; i < count; i++)
    {
      var info = unlocked[i];
      if (info == null) continue;

      int missing = Mathf.Max(1, Mathf.CeilToInt(info.baseLife * missingLifeFraction));
      int newLife = Mathf.Max(1, info.baseLife - missing);

      // Save reduced life
      pdm.SaveEnerlingCurrentLife(info.ingredientName, newLife);

      // Compute total regen time for the missing portion
      float fullRegenSec = PersistentDataManager.GetFullRegenMinutes(info.rarity) * 60f;
      float totalRegenSec = (fullRegenSec * (info.baseLife - newLife)) / Mathf.Max(1, info.baseLife);

      // Backdate start time so remaining time is the desired value (if possible)
      float backdateSeconds = Mathf.Max(0f, totalRegenSec - desiredRemainingSeconds);
      DateTime startTime = DateTime.Now - TimeSpan.FromSeconds(backdateSeconds);

      PlayerPrefs.SetString(info.ingredientName + "_RegenStartTime", startTime.ToString("o"));
      PlayerPrefs.SetInt(info.ingredientName + "_RegenStartLife", newLife);
      PlayerPrefs.Save();

      if (logDetails)
      {
        Debug.Log($"[HealingSim] {info.ingredientName}: life {newLife}/{info.baseLife}, remaining ~{desiredRemainingSeconds / 60f:F1} min");
      }
    }

    // Force-process once to sync any UI that might read immediately
    pdm.ProcessAllEnerlingHealthRegen();
  }
}
