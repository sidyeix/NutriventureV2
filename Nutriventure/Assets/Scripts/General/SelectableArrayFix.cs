using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fixes IndexOutOfRangeException in Selectable.OnEnable() caused by
/// Timeline ActivationMixerPlayable rapidly enabling UI GameObjects.
/// Unity's internal s_Selectables array can overflow — this pre-allocates it.
/// </summary>
public static class SelectableArrayFix
{
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void ExpandSelectableCapacity()
  {
    // Access the internal static array via reflection
    var field = typeof(Selectable).GetField("s_Selectables",
        BindingFlags.Static | BindingFlags.NonPublic);

    if (field == null)
    {
      // Fallback: try the alternative field name used in some ugui versions
      field = typeof(Selectable).GetField("s_SelectableArray",
          BindingFlags.Static | BindingFlags.NonPublic);
    }

    if (field != null && field.FieldType == typeof(Selectable[]))
    {
      var currentArray = (Selectable[])field.GetValue(null);
      int currentCapacity = currentArray != null ? currentArray.Length : 0;
      int targetCapacity = 256;

      if (currentCapacity < targetCapacity)
      {
        var newArray = new Selectable[targetCapacity];
        if (currentArray != null)
        {
          System.Array.Copy(currentArray, newArray, currentArray.Length);
        }
        field.SetValue(null, newArray);
        Debug.Log($"SelectableArrayFix: Expanded capacity from {currentCapacity} to {targetCapacity}");
      }
    }
    else
    {
      Debug.LogWarning("SelectableArrayFix: Could not find s_Selectables field — ugui version may not need this fix.");
    }
  }
}
