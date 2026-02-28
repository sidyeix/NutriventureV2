// KeyCollectionEvents.cs
using UnityEngine;
using System;

public static class KeyCollectionEvents
{
    public static event Action<string> OnKeyCollected;
    
    public static void TriggerKeyCollected(string keyName)
    {
        Debug.Log($"Key Collection Event Triggered: {keyName}");
        OnKeyCollected?.Invoke(keyName);
    }
}