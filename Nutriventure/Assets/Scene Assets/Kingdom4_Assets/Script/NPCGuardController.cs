using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using System.Collections;
using Cinemachine;


public class NPCGuardController : MonoBehaviour
{
    [Header("Quest Mark")]
[SerializeField] private GameObject questMark;

    [Header("Interaction Delay")]
[SerializeField] private float interactionDelay = 5f;

private bool interactionLocked = false;

    [Header("Cameras")]
[SerializeField] private CinemachineVirtualCamera playerCamera;
[SerializeField] private CinemachineVirtualCamera dollyCamera;

    [Header("Cutscene Root")]
[SerializeField] private GameObject cutsceneRoot; // Parent of Timeline & cameras

    [Header("Timeline")]
    [SerializeField] private PlayableDirector timeline;

    [Header("Player")]
[SerializeField] private GameObject player;
[SerializeField] private GameObject playerUIRoot;

private CharacterController characterController;
private MonoBehaviour playerMovementScript;


    [Header("Subtitle UI")]
[SerializeField] private GameObject narratorSubtitleRoot; // Narrator Subtitle
[SerializeField] private K2_SubtitleController subtitleController;

    [SerializeField] private float typingSpeed = 0.04f;

    [TextArea(2, 4)]
    [SerializeField] private string[] narrationLines =
    {
        "Traveler... our kingdom is in danger, not from blades or beasts—",
        "but from what our people eat without knowing.",
        "",
        "This scroll you see is the Scroll of Allergenia.",
        "It holds knowledge of the Big Nine Allergens—",
        "ingredients that bring harm to many.",
        "",
        "Your task is simple, yet vital.",
        "",
        "Study the scroll.",
        "Find each allergen.",
        "Learn to avoid them.",
        "",
        "When your knowledge is complete, the wagon gate will open.",
        "",
        "Drive carefully.",
        "The road will test what you've learned.",
        "",
        "At the end, cross the moving stones and bring the scroll to the Queen.",
        "",
        "Only then can Allerthria be protected."
    };

    [Header("UI")]
    [SerializeField] private GameObject talkUI;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    [Header("Quest Objects")]
    [SerializeField] private GameObject kingdomGate;
    [SerializeField] private GameObject npcModel;

    [Header("Settings")]
    [SerializeField] private float retriggerDelay = 5f;

    private Coroutine narrationRoutine;
    private bool cutscenePlaying;
    private bool playerInRange;
    private bool decisionShown;
private bool questAccepted = false;


    void Start()
{
    timeline.playOnAwake = false;
    timeline.stopped += OnTimelineFinished;

    talkUI.SetActive(false);
    skipButton.gameObject.SetActive(false);
    acceptButton.gameObject.SetActive(false);
    declineButton.gameObject.SetActive(false);

    skipButton.onClick.AddListener(SkipCutscene);
    acceptButton.onClick.AddListener(AcceptQuest);
    declineButton.onClick.AddListener(DeclineQuest);
}


void OnTimelineFinished(PlayableDirector director)
{
    if (!cutscenePlaying || decisionShown)
        return;

    ShowDecision();
}



    // ================= PLAYER INTERACTION =================

    public void OnPlayerEnter()
{
    playerInRange = true;

    if (!cutscenePlaying && !questAccepted && !interactionLocked)
        talkUI.SetActive(true);
}


    public void OnPlayerExit()
    {
        playerInRange = false;
        talkUI.SetActive(false);
    }

    public void StartCutscene()
{
    dollyCamera.gameObject.SetActive(true);
    dollyCamera.Priority = 20;   // takes control
playerCamera.Priority = 10;
    if (cutscenePlaying || questAccepted || interactionLocked)
        return;

    cutscenePlaying = true;

    talkUI.SetActive(false);
    // 🔹 ACTIVATE CUTSCENE (Timeline was inactive)
    cutsceneRoot.SetActive(true);

    // 🔹 ACTIVATE SUBTITLES
    narratorSubtitleRoot.SetActive(true);
    subtitleController.ClearSubtitle();

    // 🔹 HIDE PLAYER UI
    playerUIRoot.SetActive(false);

    // 🔹 FREEZE PLAYER
    FreezePlayer();

    talkUI.SetActive(false);
    skipButton.gameObject.SetActive(true);

    timeline.time = 0;
    timeline.Play();

    narrationRoutine = StartCoroutine(PlayNarration());
}


IEnumerator InteractionCooldown()
{
    interactionLocked = true;
    talkUI.SetActive(false);

    yield return new WaitForSeconds(interactionDelay);

    interactionLocked = false;

    // If player is still nearby, allow talking again
    if (playerInRange && !questAccepted)
        talkUI.SetActive(true);
}


    // ================= NARRATION =================

    IEnumerator PlayNarration()
    {
        foreach (string line in narrationLines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                yield return new WaitForSeconds(0.6f);
                continue;
            }

            subtitleController.ShowSubtitle(line, typingSpeed);

            float waitTime = Mathf.Max(2f, line.Length * typingSpeed);
            yield return new WaitForSeconds(waitTime);

            subtitleController.ClearSubtitle();
        }
    }

    // ================= SKIP =================

    void SkipCutscene()
{
    if (!cutscenePlaying) return;

    if (narrationRoutine != null)
        StopCoroutine(narrationRoutine);

    subtitleController.ClearSubtitle();

    decisionShown = false;

    timeline.time = timeline.duration;
    timeline.Evaluate();
    timeline.Stop(); // triggers OnTimelineFinished
    dollyCamera.gameObject.SetActive(false);

}




void FreezePlayer()
{
    if (characterController != null)
        characterController.enabled = false;

    if (playerMovementScript != null)
        playerMovementScript.enabled = false;
}

void RestorePlayer()
{
    if (characterController != null)
        characterController.enabled = true;

    if (playerMovementScript != null)
        playerMovementScript.enabled = true;
}



    // ================= DECISION =================

   void ShowDecision()
{
    if (decisionShown) return;

    decisionShown = true;

    skipButton.gameObject.SetActive(false);
    acceptButton.gameObject.SetActive(true);
    declineButton.gameObject.SetActive(true);
}



    void AcceptQuest()
{
    questAccepted = true;

    acceptButton.gameObject.SetActive(false);
    declineButton.gameObject.SetActive(false);

    // Hide gate
    kingdomGate.SetActive(false);

    // Hide quest mark
    questMark.SetActive(false);

    EndCutscene();

    dollyCamera.Priority = 0;
    playerCamera.Priority = 10;
    dollyCamera.gameObject.SetActive(false);
}



    void DeclineQuest()
{
    // Quest mark stays visible
    questMark.SetActive(true);

    acceptButton.gameObject.SetActive(false);
    declineButton.gameObject.SetActive(false);

    EndCutscene();

    dollyCamera.Priority = 0;
    playerCamera.Priority = 10;
    dollyCamera.gameObject.SetActive(false);
}




    // ================= CLEANUP =================

    void EndCutscene()
{
    cutscenePlaying = false;
    decisionShown = false;

    subtitleController.ClearSubtitle();
    narratorSubtitleRoot.SetActive(false);

    RestorePlayer();
    playerUIRoot.SetActive(true);

    // 🔒 Lock interaction temporarily
    StartCoroutine(InteractionCooldown());
}

}
