using System.Collections;
using UnityEngine;
using StarterAssets; // for StarterAssetsInputs

[RequireComponent(typeof(CharacterController))]
public class PlayerClimbController : MonoBehaviour
{
    [Header("References")]
    public Transform ladderStart;   // assign LadderStart transform in inspector when starting climb
    public Transform ladderTop;     // assign LadderTop transform in inspector
    public Animator animator;
    public CharacterController controller;
    public StarterAssetsInputs starterInputs;
    public MonoBehaviour thirdPersonController; // reference to your ThirdPersonController component (so we can disable it)

    [Header("Settings")]
    public float snapDuration = 0.3f;      // time to move to ladder start smoothly
    public float climbSpeed = 2.0f;        // world units per second while climbing at input=1
    public float approachDistance = 0.1f;  // distance considered arrived at start/top
    public float topThreshold = 0.25f;     // how close to top to trigger ClimbToTop
    public bool allowClimbFromTop = false; // optional: allow initiating climb from top position

    // internal
    private bool isClimbing = false;
    private bool isApproaching = false;
    private Vector3 approachTarget;
    private float snapElapsed = 0f;
    private float snapStartTime = 0f;
    private Vector3 snapStartPos;
    private Transform activeLadder;
    private float savedAnimatorSpeed = 1f;

    private void Reset()
    {
        // auto-fill common components
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        starterInputs = GetComponent<StarterAssetsInputs>();
        // thirdPersonController leave for you to assign in inspector (drag the ThirdPersonController component)
    }

    private void Update()
    {
        if (isApproaching)
        {
            // Smoothly move player to approachTarget
            snapElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(snapElapsed / snapDuration);
            Vector3 newPos = Vector3.Lerp(snapStartPos, approachTarget, t);
            // use CharacterController to preserve collisions
            Vector3 diff = newPos - transform.position;
            controller.Move(diff);
            if (t >= 1f)
            {
                isApproaching = false;
                EnterClimbMode();
            }
            return;
        }

        if (isClimbing)
        {
            HandleClimbing();
        }
    }

    // Call this from your UI button OnClick (set ladder transforms before calling)
    public void OnClimbPressed(Transform ladderStartTransform, Transform ladderTopTransform)
    {
        if (isClimbing || isApproaching) return;

        // set ladder references
        ladderStart = ladderStartTransform;
        ladderTop = ladderTopTransform;
        activeLadder = ladderStartTransform.parent; // optional reference

        // Decide whether we go to bottom or top depending on distance — here we go to ladderStart by default
        StartApproachTo(ladderStart.position);
    }

    public void CancelClimb()
    {
        if (!isClimbing && !isApproaching) return;
        ExitClimbMode();
    }

    private void StartApproachTo(Vector3 targetPosition)
    {
        isApproaching = true;
        snapElapsed = 0f;
        snapStartTime = Time.time;
        snapStartPos = transform.position;
        approachTarget = targetPosition;
        // optionally align rotation to ladder (face it)
        Vector3 lookDir = (targetPosition - transform.position);
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDir);
    }

    private void EnterClimbMode()
    {
        isClimbing = true;

        // Disable the ThirdPersonController so it doesn't fight our movement
        if (thirdPersonController != null) thirdPersonController.enabled = false;

        // disable gravity vertical velocity by resetting controller vertical (charactercontroller handles collisions)
        // start climbing animation
        if (animator != null)
        {
            animator.SetTrigger("StartClimb"); // fire StartClimb
            animator.SetBool("IsClimbing", true);
            // save animator speed
            savedAnimatorSpeed = animator.speed;
            animator.speed = 1f; // ensure playing
        }
    }

    private void ExitClimbMode()
    {
        isClimbing = false;
        isApproaching = false;
        activeLadder = null;

        // restore animator
        if (animator != null)
        {
            animator.SetBool("IsClimbing", false);
            animator.speed = savedAnimatorSpeed;
        }

        // re-enable the ThirdPersonController
        if (thirdPersonController != null) thirdPersonController.enabled = true;
    }

    private void HandleClimbing()
    {
        // read joystick vertical (forward) from starter inputs
        float verticalInput = 0f;
        if (starterInputs != null)
        {
            // On typical StarterAssets, forward is move.y
            verticalInput = starterInputs.move.y;
        }
        // also allow keyboard up/down (if needed), but above is fine

        // cancel climb on jump
        if (starterInputs != null && starterInputs.jump)
        {
            // optional: play jump animation or just exit climb
            ExitClimbMode();
            return;
        }

        // If no ladder top or start assigned, exit
        if (ladderTop == null || ladderStart == null)
        {
            ExitClimbMode();
            return;
        }

        // Determine movement amount
        Vector3 moveVec = Vector3.zero;
        // climb along the ladder's up direction (assume ladder local up = global up; else use ladder transform)
        Vector3 ladderUp = (ladderTop.position - ladderStart.position).normalized;
        moveVec = ladderUp * (verticalInput * climbSpeed * Time.deltaTime);

        // perform movement using CharacterController to keep collisions
        controller.Move(moveVec);

        // drive animator parameter
        if (animator != null)
        {
            animator.SetFloat("ClimbSpeed", verticalInput); // positive = up, negative = down
            // pause animator when player is not moving (verticalInput approx 0)
            if (Mathf.Abs(verticalInput) < 0.05f)
            {
                animator.speed = 0f; // pause
            }
            else
            {
                animator.speed = 1f; // resume
            }
        }

        // check if we reached top
        float distToTop = Vector3.Distance(transform.position, ladderTop.position);
        if (distToTop <= topThreshold && verticalInput > 0.1f)
        {
            // snap to top and play ClimbToTop
            StartCoroutine(FinishClimbToTop());
        }

        // check if we reached bottom (optional: if you want special exit when reach bottom)
        float distToBottom = Vector3.Distance(transform.position, ladderStart.position);
        if (distToBottom <= approachDistance && verticalInput < -0.1f)
        {
            // If player descends to bottom and pressed down maybe exit climb
            // For now do nothing — adjust if you want to dismount at bottom
        }
    }

    private IEnumerator FinishClimbToTop()
    {
        // Prevent repeated triggers
        isClimbing = false;

        // Trigger the top animation
        if (animator != null)
        {
            animator.SetTrigger("ClimbToTop");
            // keep animator playing to let the top animation run
            animator.speed = 1f;
        }

        // Optionally wait for the length of the ClimbToTop animation clip
        float waitTime = 0.9f; // default; set properly:
        // If you know exact animation length, set waitTime to that, or fetch it (simple approach below)
        if (animator != null)
        {
            AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfos != null && clipInfos.Length > 0)
            {
                // This may not always return the ClimbToTop clip if animator just transitioned; adjust as needed.
                waitTime = clipInfos[0].clip.length;
            }
        }

        // Wait roughly for the animation to finish (you can use animation events for precision)
        yield return new WaitForSeconds(waitTime);

        // Snap player to the ladder top position (or an "exit" anchor slightly offset)
        Vector3 finalPos = ladderTop.position;
        controller.enabled = false; // temporarily disable CC to snap precisely
        transform.position = finalPos;
        controller.enabled = true;

        // restore animator and third person control
        if (animator != null)
        {
            animator.SetBool("IsClimbing", false);
            animator.speed = savedAnimatorSpeed;
        }
        if (thirdPersonController != null) thirdPersonController.enabled = true;
    }
}
