using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LogoManager : MonoBehaviour
{
    public float logoDisplayTime = 3f; // Show logo for 3 seconds

    void Start()
    {
        // Tell LoadingScreen what to load next
        PlayerPrefs.SetString("NextScene", "PlayerProfile");

        // Wait 3 seconds then load LoadingScreen
        Invoke("LoadLoadingScreen", logoDisplayTime);
    }

    void LoadLoadingScreen()
    {
        // Use the EXACT scene name: "LoadingScreen"
        SceneManager.LoadScene("LoadingScreen");
    }

    // Input System compatible skip
    void Update()
    {
        // Check for ANY input using Input System
        bool anyInput = false;

        // Check keyboard
        if (Keyboard.current != null)
            anyInput |= Keyboard.current.anyKey.wasPressedThisFrame;

        // Check mouse click
        if (Mouse.current != null)
            anyInput |= Mouse.current.leftButton.wasPressedThisFrame;

        // Check touch
        if (Touchscreen.current != null)
            anyInput |= Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        // Skip if any input
        if (anyInput)
        {
            LoadLoadingScreen();
        }
    }
}