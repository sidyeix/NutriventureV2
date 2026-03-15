using UnityEngine;
using System;

public static class KeyCollectionEvents
{
    // Event that fires when any key is collected
    public static event Action<string> OnKeyCollected;

    // Call this method when a key is collected
    public static void TriggerKeyCollected(string keyName)
    {
        Debug.Log($"🔥 Key Collection Event Triggered: {keyName}");
        OnKeyCollected?.Invoke(keyName);
    }
}