using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

public class ChestManager : MonoBehaviour
{
    [Header("Chest Database")]
    public ChestDatabase chestDatabase; // Single ScriptableObject with list

    [Header("Fallback Settings (Use if no Database)")]
    public GameObject[] chestPrefabs;
    public VideoClip[] chestVideoClips;
    public AudioClip[] chestBackgroundMusic;
    public float[] chestRewardDelays = new float[] { 2f, 3f, 4f, 5f };

    [Header("Spawn Point")]
    public Transform chestSpawnPoint;

    [Header("Camera Settings")]
    public Cinemachine.CinemachineVirtualCamera chestCamera;

    [Header("Canvas References")]
    public GameObject menuCanvas;
    public GameObject chestCanvas;

    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public RenderTexture videoRenderTexture;

    [Header("Fade Settings")]
    public float fadeDuration = 1f;

    [Header("Player Settings")]
    public GameObject playerObject;

    private Queue<int> chestQueue = new Queue<int>();
    private Chest currentChest;
    private ChestUIHandler chestUIHandler;
    private CanvasGroup chestCanvasGroup;
    private int currentChestIndex = 0;
    private bool isPlayingChestMusic = false;
    private Coroutine waitForDelayCoroutine;

    public static ChestManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (chestCanvas != null)
        {
            chestUIHandler = chestCanvas.GetComponent<ChestUIHandler>();
            chestCanvasGroup = chestCanvas.GetComponent<CanvasGroup>();

            if (chestCanvasGroup == null)
            {
                chestCanvasGroup = chestCanvas.AddComponent<CanvasGroup>();
            }

            chestCanvasGroup.alpha = 0f;
            chestCanvasGroup.interactable = false;
            chestCanvasGroup.blocksRaycasts = false;
        }

        InitializeVideoPlayer();
        InitializeChestCamera();
        InitializeChestQueue();
        SpawnNextChest();
    }

    void InitializeVideoPlayer()
    {
        if (videoPlayer != null)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.playOnAwake = false;
            videoPlayer.skipOnDrop = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }
    }

    void InitializeChestCamera()
    {
        if (chestCamera != null)
        {
            chestCamera.Priority = 0;
            chestCamera.LookAt = null;
            chestCamera.Follow = null;
        }
    }

    void InitializeChestQueue()
    {
        // Queue up all chest indices
        int chestCount = GetChestCount();
        for (int i = 0; i < chestCount; i++)
        {
            chestQueue.Enqueue(i);
        }
    }

    int GetChestCount()
    {
        // Use database if available, otherwise use fallback arrays
        if (chestDatabase != null && chestDatabase.chestConfigs != null && chestDatabase.chestConfigs.Count > 0)
        {
            return chestDatabase.chestConfigs.Count;
        }
        else if (chestPrefabs != null && chestPrefabs.Length > 0)
        {
            return chestPrefabs.Length;
        }
        return 0;
    }

    void SpawnNextChest()
    {
        if (chestQueue.Count == 0) return;

        int chestIndex = chestQueue.Dequeue();
        GameObject chestPrefab = GetChestPrefab(chestIndex);

        if (chestPrefab == null || chestSpawnPoint == null) return;

        GameObject chestObj = Instantiate(chestPrefab, chestSpawnPoint);
        chestObj.transform.localPosition = Vector3.zero;
        chestObj.transform.localRotation = Quaternion.identity;

        currentChest = chestObj.GetComponent<Chest>();
        if (currentChest != null)
        {
            currentChest.Initialize();
            currentChest.SetChestIndex(chestIndex);
            currentChestIndex = chestIndex;
        }
    }

    GameObject GetChestPrefab(int index)
    {
        if (chestDatabase != null && chestDatabase.chestConfigs != null &&
            chestDatabase.chestConfigs.Count > index && chestDatabase.chestConfigs[index] != null)
        {
            return chestDatabase.chestConfigs[index].chestPrefab;
        }
        else if (chestPrefabs != null && chestPrefabs.Length > index)
        {
            return chestPrefabs[index];
        }
        return null;
    }

    public void FocusOnChest(Chest chest)
    {
        if (chest != currentChest || chestCamera == null) return;

        // REMOVED: Hide the player/character - Character will remain visible

        // Get the chest index
        int chestIndex = chest.chestOrder;

        // Get the custom delay for this chest type
        float customDelay = GetRewardDelayForChest(chestIndex);

        // Play chest background music
        PlayChestBackgroundMusic(chestIndex);

        // Change background video
        ChangeBackgroundVideo(chestIndex);

        // Set up camera
        chestCamera.Priority = 20;
        chestCamera.LookAt = chest.transform;
        chestCamera.Follow = chest.transform;

        // Hide menu, show chest UI
        if (menuCanvas != null) menuCanvas.SetActive(false);
        if (chestCanvas != null)
        {
            chestCanvas.SetActive(true);
            StartCoroutine(FadeCanvas(chestCanvasGroup, 0f, 1f, fadeDuration));
        }

        // Open the chest
        chest.OpenChest();

        // Set the chest in UI handler (shows "Opening...")
        if (chestUIHandler != null)
        {
            chestUIHandler.SetCurrentChest(chest);
        }

        // Wait for custom delay before showing rewards
        if (waitForDelayCoroutine != null)
            StopCoroutine(waitForDelayCoroutine);

        waitForDelayCoroutine = StartCoroutine(WaitForCustomDelay(chest, customDelay));
    }

    float GetRewardDelayForChest(int chestIndex)
    {
        if (chestDatabase != null && chestDatabase.chestConfigs != null &&
            chestDatabase.chestConfigs.Count > chestIndex && chestDatabase.chestConfigs[chestIndex] != null)
        {
            return chestDatabase.chestConfigs[chestIndex].rewardDelay;
        }
        else if (chestRewardDelays != null && chestRewardDelays.Length > chestIndex)
        {
            return chestRewardDelays[chestIndex];
        }
        return 2f; // Default
    }

    VideoClip GetVideoClipForChest(int chestIndex)
    {
        if (chestDatabase != null && chestDatabase.chestConfigs != null &&
            chestDatabase.chestConfigs.Count > chestIndex && chestDatabase.chestConfigs[chestIndex] != null)
        {
            return chestDatabase.chestConfigs[chestIndex].videoClip;
        }
        else if (chestVideoClips != null && chestVideoClips.Length > chestIndex)
        {
            return chestVideoClips[chestIndex];
        }
        return null;
    }

    AudioClip GetAudioClipForChest(int chestIndex)
    {
        if (chestDatabase != null && chestDatabase.chestConfigs != null &&
            chestDatabase.chestConfigs.Count > chestIndex && chestDatabase.chestConfigs[chestIndex] != null)
        {
            return chestDatabase.chestConfigs[chestIndex].backgroundMusic;
        }
        else if (chestBackgroundMusic != null && chestBackgroundMusic.Length > chestIndex)
        {
            return chestBackgroundMusic[chestIndex];
        }
        return null;
    }

    IEnumerator WaitForCustomDelay(Chest chest, float delay)
    {
        Debug.Log($"Waiting {delay} seconds before showing rewards for {chest.ChestName}");

        yield return new WaitForSeconds(delay);

        Debug.Log($"Delay finished, showing rewards for {chest.ChestName}");

        // Now start revealing rewards
        if (chestUIHandler != null)
        {
            chestUIHandler.StartRevealingRewards(chest);
        }
    }

    void PlayChestBackgroundMusic(int chestIndex)
    {
        if (AudioHandler.Instance != null)
        {
            // Stop main menu music
            AudioHandler.Instance.musicSource.Stop();

            AudioClip chestMusic = GetAudioClipForChest(chestIndex);
            if (chestMusic != null)
            {
                AudioHandler.Instance.musicSource.clip = chestMusic;
                AudioHandler.Instance.musicSource.loop = false;
                AudioHandler.Instance.musicSource.Play();
                isPlayingChestMusic = true;

                StartCoroutine(StopMusicWhenEnds(chestMusic.length));
                Debug.Log($"Playing chest music: {chestMusic.name}");
            }
        }
    }

    void ChangeBackgroundVideo(int chestIndex)
    {
        if (videoPlayer == null) return;

        VideoClip targetVideo = GetVideoClipForChest(chestIndex);
        if (targetVideo != null)
        {
            StartCoroutine(ChangeVideoRoutine(targetVideo));
        }
        else
        {
            StopVideo();
        }
    }

    IEnumerator ChangeVideoRoutine(VideoClip newClip)
    {
        videoPlayer.Stop();

        if (videoRenderTexture != null)
        {
            RenderTexture.active = videoRenderTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
        }

        yield return null;

        videoPlayer.clip = newClip;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        videoPlayer.Play();
    }

    // REMOVED: HidePlayer() method - Character will remain visible

    // REMOVED: ShowPlayer() method - Character will remain visible

    IEnumerator StopMusicWhenEnds(float musicLength)
    {
        yield return new WaitForSeconds(musicLength);

        if (isPlayingChestMusic && chestCanvas != null && chestCanvas.activeInHierarchy)
        {
            StopChestMusic();
        }
    }

    void StopChestMusic()
    {
        if (AudioHandler.Instance != null && AudioHandler.Instance.musicSource != null)
        {
            AudioHandler.Instance.musicSource.Stop();
            isPlayingChestMusic = false;
        }
    }

    void ResumeMainMenuMusic()
    {
        if (AudioHandler.Instance != null)
        {
            StopChestMusic();
            AudioHandler.Instance.PlayMainMenuMusic();
        }
    }

    void StopVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
    }

    public void OnChestClaimed()
    {
        // REMOVED: Show player again - Character remains visible

        if (chestUIHandler != null)
        {
            chestUIHandler.OnChestUIClosed();
        }

        StopVideo();
        ResumeMainMenuMusic();

        if (currentChest != null)
        {
            Destroy(currentChest.gameObject);
            currentChest = null;
        }

        if (waitForDelayCoroutine != null)
        {
            StopCoroutine(waitForDelayCoroutine);
            waitForDelayCoroutine = null;
        }

        if (chestCanvas != null && chestCanvasGroup != null)
        {
            StartCoroutine(FadeAndHideCanvas());
        }
        else
        {
            SwitchBackToMenu();
            StartCoroutine(SpawnNextChestAfterDelay(1f));
        }
    }

    IEnumerator FadeAndHideCanvas()
    {
        yield return StartCoroutine(FadeCanvas(chestCanvasGroup, 1f, 0f, fadeDuration / 2f));
        SwitchBackToMenu();
        StartCoroutine(SpawnNextChestAfterDelay(1f));
    }

    void SwitchBackToMenu()
    {
        if (chestCamera != null)
        {
            chestCamera.Priority = 0;
            chestCamera.LookAt = null;
            chestCamera.Follow = null;
        }

        if (menuCanvas != null) menuCanvas.SetActive(true);
        if (chestCanvas != null) chestCanvas.SetActive(false);
    }

    IEnumerator FadeCanvas(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;
        canvasGroup.alpha = startAlpha;

        if (endAlpha > startAlpha)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            canvasGroup.alpha = currentAlpha;
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    IEnumerator SpawnNextChestAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnNextChest();
    }

    // Helper method to get chest config from database
    public ChestDatabase.ChestConfig GetChestConfig(int index)
    {
        if (chestDatabase != null)
        {
            return chestDatabase.GetChestConfig(index);
        }
        return null;
    }

    // Helper method to get chest config by name
    public ChestDatabase.ChestConfig GetChestConfigByName(string name)
    {
        if (chestDatabase != null)
        {
            return chestDatabase.GetChestConfigByName(name);
        }
        return null;
    }
}