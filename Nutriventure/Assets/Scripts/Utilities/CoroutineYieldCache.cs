using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Caches WaitForSeconds and WaitForSecondsRealtime instances to avoid GC allocations
/// in coroutines. Use instead of "new WaitForSeconds(x)" in yield statements.
/// 
/// Usage:
///   yield return CoroutineYieldCache.WaitForSeconds(1f);
///   yield return CoroutineYieldCache.WaitForSecondsRealtime(0.5f);
/// </summary>
public static class CoroutineYieldCache
{
    private static readonly Dictionary<float, WaitForSeconds> _waitForSeconds = new Dictionary<float, WaitForSeconds>();
    private static readonly Dictionary<float, WaitForSecondsRealtime> _waitForSecondsRealtime = new Dictionary<float, WaitForSecondsRealtime>();
    private static readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();
    private static readonly WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();

    public static WaitForSeconds WaitForSeconds(float seconds)
    {
        if (!_waitForSeconds.TryGetValue(seconds, out var wait))
        {
            wait = new WaitForSeconds(seconds);
            _waitForSeconds[seconds] = wait;
        }
        return wait;
    }

    public static WaitForSecondsRealtime WaitForSecondsRealtime(float seconds)
    {
        if (!_waitForSecondsRealtime.TryGetValue(seconds, out var wait))
        {
            wait = new WaitForSecondsRealtime(seconds);
            _waitForSecondsRealtime[seconds] = wait;
        }
        return wait;
    }

    public static WaitForEndOfFrame WaitForEndOfFrame => _waitForEndOfFrame;
    public static WaitForFixedUpdate WaitForFixedUpdate => _waitForFixedUpdate;
}
