using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

public class ChestManager : MonoBehaviour
{
    [Header("Chest Prefabs")]
    public GameObject[] chestPrefabs;

    [Header("Spawn Point")]
    public Transform chestSpawnPoint;

    [Header("Camera Settings")]
    public Cinemachine.CinemachineVirtualCamera chestCamera;

    [Header("Canvas References")]
    public GameObject menuCanvas;
    public GameObject chestCanvas;

    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public VideoClip[] chestVideoClips;
    public RenderTexture videoRenderTexture;

    [Header("Fade Settings")]
    public float fadeDuration = 1f;

    private Queue<GameObject> chestQueue = new Queue<GameObject>();
    private Chest currentChest;
    private ChestUIHandler chestUIHandler;
    private CanvasGroup chestCanvasGroup;
    private int currentChestIndex = 0;

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
        foreach (GameObject chestPrefab in chestPrefabs)
        {
            if (chestPrefab != null)
            {
                chestQueue.Enqueue(chestPrefab);
            }
        }
    }

    void SpawnNextChest()
    {
        if (chestQueue.Count == 0) return;

        GameObject nextChestPrefab = chestQueue.Dequeue();
        if (nextChestPrefab == null || chestSpawnPoint == null) return;

        GameObject chestObj = Instantiate(nextChestPrefab, chestSpawnPoint);
        chestObj.transform.localPosition = Vector3.zero;
        chestObj.transform.localRotation = Quaternion.identity;

        currentChest = chestObj.GetComponent<Chest>();
        if (currentChest != null)
        {
            currentChest.Initialize();
            currentChest.chestOrder = currentChestIndex;
            currentChestIndex++;
        }
    }

    public void FocusOnChest(Chest chest)
    {
        if (chest != currentChest || chestCamera == null) return;

        ChangeBackgroundVideo(chest.chestOrder);

        chestCamera.Priority = 20;
        chestCamera.LookAt = chest.transform;
        chestCamera.Follow = chest.transform;

        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
        }

        if (chestCanvas != null)
        {
            chestCanvas.SetActive(true);
            StartCoroutine(FadeCanvas(chestCanvasGroup, 0f, 1f, fadeDuration));
        }

        chest.OpenChest();

        if (chestUIHandler != null)
        {
            chestUIHandler.SetCurrentChest(chest);
        }
    }

    private void ChangeBackgroundVideo(int chestOrder)
    {
        if (videoPlayer == null) return;

        if (chestVideoClips != null && chestOrder < chestVideoClips.Length)
        {
            VideoClip targetVideo = chestVideoClips[chestOrder];
            if (targetVideo != null)
            {
                StartCoroutine(ChangeVideoRoutine(targetVideo, chestOrder));
            }
            else
            {
                StopVideo();
            }
        }
        else
        {
            StopVideo();
        }
    }

    private IEnumerator ChangeVideoRoutine(VideoClip newClip, int chestOrder)
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

    private void StopVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
    }

    public void OnChestClaimed()
    {
        if (chestUIHandler != null)
        {
            chestUIHandler.OnChestUIClosed();
        }

        StopVideo();

        if (currentChest != null)
        {
            Destroy(currentChest.gameObject);
            currentChest = null;
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

    private IEnumerator FadeAndHideCanvas()
    {
        yield return StartCoroutine(FadeCanvas(chestCanvasGroup, 1f, 0f, fadeDuration / 2f));
        SwitchBackToMenu();
        StartCoroutine(SpawnNextChestAfterDelay(1f));
    }

    private void SwitchBackToMenu()
    {
        if (chestCamera != null)
        {
            chestCamera.Priority = 0;
            chestCamera.LookAt = null;
            chestCamera.Follow = null;
        }

        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
        }

        if (chestCanvas != null)
        {
            chestCanvas.SetActive(false);
        }
    }

    private IEnumerator FadeCanvas(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
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

    void Update()
    {
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            DebugChestStatus();
        }

        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            if (currentChest != null)
            {
                Destroy(currentChest.gameObject);
                currentChest = null;
            }
            SpawnNextChest();
        }

        if (Keyboard.current.cKey.wasPressedThisFrame && currentChest != null)
        {
            currentChest.MakeChestClaimable();
        }
    }

    public void DebugChestStatus()
    {
        Debug.Log("===== CHEST MANAGER STATUS =====");
        Debug.Log("   - Current Chest: " + (currentChest != null ? currentChest.ChestName : "NULL"));
        Debug.Log("   - Chest Order: " + (currentChest != null ? currentChest.chestOrder.ToString() : "NULL"));
        Debug.Log("   - Chests in queue: " + chestQueue.Count);
        Debug.Log("   - Video Player Playing: " + (videoPlayer != null ? videoPlayer.isPlaying.ToString() : "NULL"));
        Debug.Log("   - Video Player Prepared: " + (videoPlayer != null ? videoPlayer.isPrepared.ToString() : "NULL"));
        Debug.Log("   - Video Clip: " + (videoPlayer != null && videoPlayer.clip != null ? videoPlayer.clip.name : "NULL"));
        Debug.Log("===== END STATUS =====");
    }
}