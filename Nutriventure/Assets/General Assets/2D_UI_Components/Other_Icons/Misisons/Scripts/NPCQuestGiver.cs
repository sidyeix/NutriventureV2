using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using Cinemachine;
using System.Linq;

public class NPCQuestGiver : MonoBehaviour
{
    [Header("Kingdom")]
    [SerializeField] private string kingdomID;

    [Header("UI")]
    [SerializeField] private GameObject questButton;
    [SerializeField] private GameObject missionCanvas;

    [Header("Camera")]
    [SerializeField] private CinemachineVirtualCamera npcCamera;

    [Header("Timeline")]
    [SerializeField] private PlayableDirector playableDirector;

    private Quest currentQuest;

    private bool waitingForTimeline = false;


    private void Start()
    {
        questButton.SetActive(false);
        missionCanvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        FindAvailableQuest();

        questButton.SetActive(currentQuest != null);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        questButton.SetActive(false);
    }

    private void FindAvailableQuest()
    {
        currentQuest = null;

        var quests = QuestManager.Instance.GetQuestsByKingdom(kingdomID);

        currentQuest = quests.FirstOrDefault(q =>
            q.status == QuestStatus.NotStarted &&
            (q.category == QuestCategory.Tutorial ||
             q.category == QuestCategory.MainStory));
    }

    // BUTTON EVENT
    public void OnQuestButtonClicked()
    {
        if (currentQuest == null) return;

        // CASE 1: Quest has a timeline
        if (currentQuest.timelineAsset != null && playableDirector != null)
        {
            waitingForTimeline = true;

            playableDirector.playableAsset = currentQuest.timelineAsset;
            playableDirector.stopped += OnTimelineFinished;
            playableDirector.Play();
        }
        // CASE 2: No timeline ? show immediately
        else
        {
            ShowMissionCanvas();
        }
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        if (!waitingForTimeline) return;

        waitingForTimeline = false;
        playableDirector.stopped -= OnTimelineFinished;

        ShowMissionCanvas();
    }

    private void ShowMissionCanvas()
    {
        missionCanvas.SetActive(true);

        npcCamera.Priority = 30;

        missionCanvas
            .GetComponent<QuestUIController>()
            .DisplayQuest(currentQuest);
    }


    public Quest GetCurrentQuest()
    {
        return currentQuest;
    }
}
