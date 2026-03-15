using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnerlingHealingIncubatorManager : MonoBehaviour
{
  [Header("References")]
  public IngredientDatabase ingredientDatabase;
  [Tooltip("Incubators placed in Nutri Kingdom (KingdomOrigin.NutriKingdom)")]
  public List<EnerlingHealingIncubatorSlot> nutriIncubators = new List<EnerlingHealingIncubatorSlot>();

  [Tooltip("Incubators placed in Alerthia (KingdomOrigin.Alerthia)")]
  public List<EnerlingHealingIncubatorSlot> alerthiaIncubators = new List<EnerlingHealingIncubatorSlot>();

  [Tooltip("Incubators placed in Sugaria (KingdomOrigin.Sugaria)")]
  public List<EnerlingHealingIncubatorSlot> sugariaIncubators = new List<EnerlingHealingIncubatorSlot>();

  [Tooltip("Incubators placed in Preservia (KingdomOrigin.Preservia)")]
  public List<EnerlingHealingIncubatorSlot> preserviaIncubators = new List<EnerlingHealingIncubatorSlot>();

  [Header("Refresh Settings")]
  public float refreshInterval = 1f;

  private Coroutine refreshRoutine;

  void Start()
  {
    if (PersistentDataManager.Instance != null)
    {
      PersistentDataManager.Instance.EnsureAllDamagedEnerlingsRegenerating();
    }

    if (refreshRoutine != null)
      StopCoroutine(refreshRoutine);

    refreshRoutine = StartCoroutine(RefreshLoop());
  }

  IEnumerator RefreshLoop()
  {
    while (true)
    {
      RefreshIncubators();
      yield return new WaitForSeconds(refreshInterval);
    }
  }

  private void RefreshIncubators()
  {
    if (ingredientDatabase == null || PersistentDataManager.Instance == null)
      return;

    PersistentDataManager.Instance.ProcessAllEnerlingHealthRegen();

    List<string> nutri = new List<string>();
    List<string> alerthia = new List<string>();
    List<string> sugaria = new List<string>();
    List<string> preservia = new List<string>();

    foreach (var info in ingredientDatabase.ingredients)
    {
      if (info == null || !info.isUnlocked)
        continue;

      if (!PersistentDataManager.Instance.IsEnerlingRegenerating(info.ingredientName))
        continue;

      switch (info.kingdom)
      {
        case IngredientDatabase.KingdomOrigin.NutriKingdom:
          nutri.Add(info.ingredientName);
          break;
        case IngredientDatabase.KingdomOrigin.Alerthia:
          alerthia.Add(info.ingredientName);
          break;
        case IngredientDatabase.KingdomOrigin.Sugaria:
          sugaria.Add(info.ingredientName);
          break;
        case IngredientDatabase.KingdomOrigin.Preservia:
          preservia.Add(info.ingredientName);
          break;
      }
    }

    RefreshIncubatorGroup(nutriIncubators, nutri);
    RefreshIncubatorGroup(alerthiaIncubators, alerthia);
    RefreshIncubatorGroup(sugariaIncubators, sugaria);
    RefreshIncubatorGroup(preserviaIncubators, preservia);
  }

  private void RefreshIncubatorGroup(List<EnerlingHealingIncubatorSlot> slots, List<string> regeneratingNames)
  {
    if (slots == null)
      return;

    int maxSlots = slots.Count;
    if (regeneratingNames.Count > maxSlots)
    {
      regeneratingNames = regeneratingNames.GetRange(0, maxSlots);
    }

    HashSet<string> assigned = new HashSet<string>();

    foreach (var slot in slots)
    {
      if (slot == null)
        continue;

      string currentName = slot.AssignedEnerlingName;
      if (!string.IsNullOrEmpty(currentName))
      {
        if (!regeneratingNames.Contains(currentName))
        {
          slot.Clear();
        }
        else
        {
          assigned.Add(currentName);
          var info = ingredientDatabase.GetIngredientInfo(currentName);
          float remaining = PersistentDataManager.Instance.GetEnerlingRegenRemainingSeconds(currentName);
          slot.UpdateTimer(remaining);
          if (info != null)
            slot.UpdateLifeUI(info, true);
        }
      }
    }

    foreach (var slot in slots)
    {
      if (slot == null || slot.IsOccupied)
        continue;

      string nextName = GetNextUnassignedName(regeneratingNames, assigned);
      if (!string.IsNullOrEmpty(nextName))
      {
        var info = ingredientDatabase.GetIngredientInfo(nextName);
        if (info != null)
        {
          slot.AssignEnerling(info);
          assigned.Add(nextName);
          float remaining = PersistentDataManager.Instance.GetEnerlingRegenRemainingSeconds(nextName);
          slot.UpdateTimer(remaining);
          slot.UpdateLifeUI(info, true);
        }
      }
      else
      {
        slot.Clear();
      }
    }
  }

  private string GetNextUnassignedName(List<string> regeneratingNames, HashSet<string> assigned)
  {
    for (int i = 0; i < regeneratingNames.Count; i++)
    {
      string name = regeneratingNames[i];
      if (!assigned.Contains(name))
        return name;
    }

    return "";
  }
}
