using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Test_EnerlingUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject viewEnerlingButton; // The button that appears when near an Enerling
    public GameObject enerlingInfoPanel;  // The panel that shows Enerling info
    
    // Basic Information Header
    [Header("Basic Information")]
    public TextMeshProUGUI enerlingNameText; // Text to display Enerling name
    public Image enerlingSpriteImage; // Image for Enerling sprite
    public TextMeshProUGUI rarityText; // Text for rarity
    public TextMeshProUGUI kingdomText; // Text for kingdom origin
    public TextMeshProUGUI enerlingStoryText; // Text for Enerling story
    
    // NEW: Banner and Rarity Images
    public Image kingdomBannerImage; // Image for kingdom banner
    public Image rarityImage; // Image for rarity icon
    
    // NEW: Sprite Assignments
    [Header("Rarity Icons")]
    public Sprite commonIcon;
    public Sprite rareIcon;
    public Sprite ultraRareIcon;
    
    [Header("Kingdom Banners")]
    public Sprite nutriBanner;
    public Sprite sugariabanner;
    public Sprite preserviaBanner;
    public Sprite allerthiaBanner;
    
    // Battle Information Header
    [Header("Battle Information")]
    public TextMeshProUGUI baseLifeText; // Text for base life
    public TextMeshProUGUI armorPercentageText; // Text for armor percentage
    public TextMeshProUGUI baseDamageText; // Text for base damage
    
    // Skills Visuals
    [Header("Skill Visuals")]
    public Image skill1Image; // Image for skill 1 sprite
    public Image skill2Image; // Image for skill 2 sprite
    public Image skill3Image; // Image for skill 3 sprite
    public Image skill4Image; // Image for skill 4 sprite
    
    // NEW: Organ Display
    [Header("Organ Display")]
    public TextMeshProUGUI organsTxt; // Text for "Beneficial Organs" or "Target Organs"
    public GameObject organsToShow; // Grid Layout Group container for organ images
    public GameObject organImagePrefab; // Prefab for organ image UI element
    
    [Header("UI Buttons")]
    public Button closeButton; // Button to close the panel
    public Button textToSpeechButton; // Button for Text-to-Speech
    
    [Header("Camera Reference")]
    public EnerlingCameraController cameraController; // Camera controller
    
    [Header("Database Reference")]
    public IngredientDatabase ingredientDatabase; // Assign this in inspector!
    
    [Header("Detection Settings")]
    public float detectionRange = 3f; // How close player needs to be
    public float checkInterval = 0.2f; // How often to check for nearby Enerlings
    
    [Header("Button Animation Settings")]
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float slideDistance = 100f;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private bool enableAnimation = true;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource; // AudioSource for playing Enerling audio
    
    [Header("Components to Disable During Audio")]
    [Tooltip("GameObjects that will be disabled while audio is playing")]
    public List<GameObject> disableDuringAudio = new List<GameObject>();
    
    private Test_EnerlingController currentNearbyEnerling;
    private GameObject player;
    private bool isPanelOpen = false;
    private float timeSinceLastCheck = 0f;
    private Coroutine buttonAnimationCoroutine;
    private Vector3 buttonOriginalPosition;
    private CanvasGroup buttonCanvasGroup;
    private bool isAnimating = false;
    private bool buttonWasVisible = false;
    private AudioClip currentEnerlingAudioClip;
    
    // Buffer to prevent immediate hiding
    private float lastDetectionTime = 0f;
    private const float DETECTION_BUFFER_TIME = 0.5f;
    
    // Cache for organ image instances
    private List<GameObject> organImageInstances = new List<GameObject>();
    
    // Track original active states of disabled components
    private Dictionary<GameObject, bool> originalComponentStates = new Dictionary<GameObject, bool>();
    
    void Start()
    {
        // Find player by tag
        player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null)
        {
            Debug.LogError("No GameObject with 'Player' tag found!");
        }
        
        // Initialize button animation components
        InitializeButtonAnimation();
        
        // Setup button listeners
        if (viewEnerlingButton != null)
        {
            Button viewButton = viewEnerlingButton.GetComponent<Button>();
            if (viewButton != null)
            {
                viewButton.onClick.AddListener(OpenEnerlingInfoPanel);
                Debug.Log("View button listener added");
            }
            else
            {
                Debug.LogError("View button has no Button component!");
            }
        }
        else
        {
            Debug.LogError("View Enerling Button is not assigned!");
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseEnerlingInfoPanel);
        }
        
        // Text-to-Speech button listener
        if (textToSpeechButton != null)
        {
            textToSpeechButton.onClick.AddListener(PlayEnerlingAudio);
        }
        else
        {
            Debug.LogWarning("Text-to-Speech button not assigned!");
        }
        
        // Setup AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                Debug.Log("Added AudioSource component to Enerling UI Manager");
            }
        }
        
        // Hide UI elements initially
        if (viewEnerlingButton != null)
        {
            if (enableAnimation)
            {
                viewEnerlingButton.SetActive(true); // Keep active for animation
                SetButtonInitialState();
            }
            else
            {
                viewEnerlingButton.SetActive(false);
            }
        }
        
        if (enerlingInfoPanel != null)
            enerlingInfoPanel.SetActive(false);
        
        // Clear organ display initially
        ClearOrganDisplay();
        
        // Log for debugging
        Debug.Log("Enerling UI Manager initialized with animation: " + enableAnimation);
        
        // Validate sprite assignments
        ValidateSpriteAssignments();
        
        // Initialize component states
        InitializeComponentStates();
    }
    
    private void InitializeComponentStates()
    {
        foreach (GameObject obj in disableDuringAudio)
        {
            if (obj != null)
            {
                originalComponentStates[obj] = obj.activeSelf;
            }
        }
    }
    
    private void ValidateSpriteAssignments()
    {
        // Log warnings for missing sprite assignments
        if (commonIcon == null) Debug.LogWarning("CommonIcon sprite is not assigned!");
        if (rareIcon == null) Debug.LogWarning("RareIcon sprite is not assigned!");
        if (ultraRareIcon == null) Debug.LogWarning("UltraRareIcon sprite is not assigned!");
        
        if (nutriBanner == null) Debug.LogWarning("NutriBanner sprite is not assigned!");
        if (sugariabanner == null) Debug.LogWarning("SugariaBanner sprite is not assigned!");
        if (preserviaBanner == null) Debug.LogWarning("PreserviaBanner sprite is not assigned!");
        if (allerthiaBanner == null) Debug.LogWarning("AllerthiaBanner sprite is not assigned!");
        
        // Validate UI references
        if (kingdomBannerImage == null) Debug.LogWarning("KingdomBanner Image component is not assigned!");
        if (rarityImage == null) Debug.LogWarning("RarityImage Image component is not assigned!");
        
        // Validate organ display references
        if (organsTxt == null) Debug.LogWarning("OrgansTxt TextMeshPro component is not assigned!");
        if (organsToShow == null) Debug.LogWarning("OrgansToShow GameObject is not assigned!");
        if (organImagePrefab == null) Debug.LogWarning("OrganImagePrefab is not assigned!");
        
        // Validate skill image references
        if (skill1Image == null) Debug.LogWarning("skill1Image Image component is not assigned!");
        if (skill2Image == null) Debug.LogWarning("skill2Image Image component is not assigned!");
        if (skill3Image == null) Debug.LogWarning("skill3Image Image component is not assigned!");
        if (skill4Image == null) Debug.LogWarning("skill4Image Image component is not assigned!");
        
        // Validate text-to-speech button
        if (textToSpeechButton == null) Debug.LogWarning("TextToSpeechButton is not assigned!");
    }
    
    private void InitializeButtonAnimation()
    {
        if (viewEnerlingButton == null || !enableAnimation) return;
        
        // Store original position
        buttonOriginalPosition = viewEnerlingButton.transform.localPosition;
        
        // Add or get CanvasGroup for fade animation
        buttonCanvasGroup = viewEnerlingButton.GetComponent<CanvasGroup>();
        if (buttonCanvasGroup == null)
        {
            buttonCanvasGroup = viewEnerlingButton.AddComponent<CanvasGroup>();
        }
        
        // Set initial state for animation
        SetButtonInitialState();
    }
    
    private void SetButtonInitialState()
    {
        if (viewEnerlingButton == null || !enableAnimation) return;
        
        // Start hidden (slid out and invisible)
        viewEnerlingButton.transform.localPosition = buttonOriginalPosition - new Vector3(slideDistance, 0, 0);
        buttonCanvasGroup.alpha = 0f;
        buttonCanvasGroup.interactable = false;
        buttonCanvasGroup.blocksRaycasts = false;
    }
    
    void Update()
    {
        if (player == null) 
        {
            // Try to find player again if null
            player = GameObject.FindGameObjectWithTag("Player");
            return;
        }
        
        // Check for nearby Enerlings at intervals
        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck >= checkInterval)
        {
            bool foundEnerling = FindNearestEnerling();
            
            // Only handle button visibility if panel is not open
            if (!isPanelOpen)
            {
                UpdateButtonVisibility(foundEnerling);
            }
            
            timeSinceLastCheck = 0f;
        }
        
        // If panel is open, keep button hidden
        if (isPanelOpen && viewEnerlingButton != null && buttonWasVisible)
        {
            HideButtonWithAnimation();
            buttonWasVisible = false;
        }
    }
    
    private bool FindNearestEnerling()
    {
        Test_EnerlingController previousEnerling = currentNearbyEnerling;
        currentNearbyEnerling = null;
        float closestDistance = float.MaxValue;
        
        // Find all Enerlings in the scene
        Test_EnerlingController[] allEnerlings = FindObjectsOfType<Test_EnerlingController>();
        
        if (allEnerlings.Length == 0)
        {
            return false;
        }
        
        foreach (var enerling in allEnerlings)
        {
            if (enerling == null || enerling.IsInteracting()) continue;
            
            float distance = Vector3.Distance(player.transform.position, enerling.transform.position);
            
            if (distance < detectionRange && distance < closestDistance)
            {
                closestDistance = distance;
                currentNearbyEnerling = enerling;
            }
        }
        
        // Update detection time
        if (currentNearbyEnerling != null)
        {
            lastDetectionTime = Time.time;
        }
        
        return currentNearbyEnerling != null;
    }
    
    private void UpdateButtonVisibility(bool shouldShow)
    {
        if (viewEnerlingButton == null) return;
        
        // Check if state changed
        bool stateChanged = (shouldShow != buttonWasVisible);
        
        // Always hide button if no Enerling is detected and enough time has passed
        if (!shouldShow && buttonWasVisible && (Time.time - lastDetectionTime) > DETECTION_BUFFER_TIME)
        {
            stateChanged = true;
        }
        
        if (!stateChanged) return; // No change, no need to animate
        
        buttonWasVisible = shouldShow;
        
        if (shouldShow)
        {
            ShowButtonWithAnimation();
        }
        else
        {
            HideButtonWithAnimation();
        }
    }
    
    private void ShowButtonWithAnimation()
    {
        if (viewEnerlingButton == null || isAnimating || isPanelOpen) return;
        
        if (buttonAnimationCoroutine != null)
            StopCoroutine(buttonAnimationCoroutine);
        
        buttonAnimationCoroutine = StartCoroutine(AnimateButton(true));
    }
    
    private void HideButtonWithAnimation()
    {
        if (viewEnerlingButton == null || isAnimating) return;
        
        if (buttonAnimationCoroutine != null)
            StopCoroutine(buttonAnimationCoroutine);
        
        buttonAnimationCoroutine = StartCoroutine(AnimateButton(false));
    }
    
    private IEnumerator AnimateButton(bool show)
    {
        isAnimating = true;
        
        // Ensure button is active for animation
        if (show && !viewEnerlingButton.activeSelf)
            viewEnerlingButton.SetActive(true);
        
        float elapsedTime = 0f;
        Vector3 startPosition = viewEnerlingButton.transform.localPosition;
        Vector3 targetPosition = show ? buttonOriginalPosition : 
            buttonOriginalPosition - new Vector3(slideDistance, 0, 0);
        
        float startAlpha = buttonCanvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;
        
        // Enable interaction when showing
        if (show)
        {
            buttonCanvasGroup.interactable = true;
            buttonCanvasGroup.blocksRaycasts = true;
        }
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            
            // Use animation curves
            float slideT = slideCurve.Evaluate(t);
            float fadeT = fadeCurve.Evaluate(t);
            
            // Position animation
            viewEnerlingButton.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, slideT);
            
            // Alpha animation
            buttonCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, fadeT);
            
            yield return null;
        }
        
        // Final state
        viewEnerlingButton.transform.localPosition = targetPosition;
        buttonCanvasGroup.alpha = targetAlpha;
        
        // Disable interaction when hiding
        if (!show)
        {
            buttonCanvasGroup.interactable = false;
            buttonCanvasGroup.blocksRaycasts = false;
            viewEnerlingButton.SetActive(false); // Fully deactivate when hidden
        }
        
        isAnimating = false;
    }
    
    public void OpenEnerlingInfoPanel()
    {
        Debug.Log("OpenEnerlingInfoPanel called");
        
        if (currentNearbyEnerling == null)
        {
            Debug.LogWarning("No nearby Enerling to show!");
            return;
        }
        
        if (enerlingInfoPanel == null)
        {
            Debug.LogError("Enerling Info Panel is not assigned!");
            return;
        }
        
        if (cameraController == null)
        {
            Debug.LogError("Camera controller is not assigned!");
            return;
        }
        
        // Switch camera to view Enerling using the specific virtual camera
        cameraController.StartViewingEnerling(currentNearbyEnerling);
        
        // Hide button with animation before opening panel
        HideButtonWithAnimation();
        
        // Get the Enerling's ingredient info
        var ingredientInfo = GetIngredientInfoFromEnerling(currentNearbyEnerling);
        
        if (ingredientInfo != null)
        {
            // Store the audio clip for Text-to-Speech
            currentEnerlingAudioClip = ingredientInfo.audioClip;
            
            // Enable/disable Text-to-Speech button based on audio availability
            if (textToSpeechButton != null)
            {
                textToSpeechButton.interactable = (currentEnerlingAudioClip != null);
            }
            
            // Update UI with Enerling info
            UpdateEnerlingInfoUI(ingredientInfo);
            
            // Show the panel
            enerlingInfoPanel.SetActive(true);
            isPanelOpen = true;
                
            Debug.Log($"Opened info panel for {ingredientInfo.ingredientName}");
        }
        else
        {
            Debug.LogWarning("Could not get ingredient info for nearby Enerling");
            
            // Fallback: Show panel with just the name
            if (enerlingNameText != null)
            {
                string displayName = currentNearbyEnerling.gameObject.name
                    .Replace("_Enerling", "")
                    .Replace("_Enerling_Fallback", "")
                    .Replace("_Enerling", "")
                    .Replace("(Clone)", "");
                enerlingNameText.text = displayName;
            }
            
            // Disable Text-to-Speech button for fallback
            if (textToSpeechButton != null)
            {
                textToSpeechButton.interactable = false;
            }
            
            enerlingInfoPanel.SetActive(true);
            isPanelOpen = true;
        }
    }
    
    private IngredientDatabase.IngredientInfo GetIngredientInfoFromEnerling(Test_EnerlingController enerling)
    {
        if (enerling == null) return null;
        
        var method = enerling.GetType().GetMethod("GetIngredientInfo");
        if (method != null)
        {
            return method.Invoke(enerling, null) as IngredientDatabase.IngredientInfo;
        }
        
        if (ingredientDatabase != null)
        {
            string enerlingName = enerling.gameObject.name;
            string ingredientName = ExtractIngredientName(enerlingName);
            return ingredientDatabase.GetIngredientInfo(ingredientName);
        }
        
        return null;
    }
    
    private string ExtractIngredientName(string enerlingName)
    {
        string name = enerlingName;
        name = name.Replace("_Enerling", "");
        name = name.Replace("_Enerling_Fallback", "");
        name = name.Replace("(Clone)", "");
        name = name.Replace("[Unlocked]", "").Replace("[LOCKED]", "").Trim();
        return name;
    }
    
    private void UpdateEnerlingInfoUI(IngredientDatabase.IngredientInfo info)
    {
        if (info == null) return;
        
        if (enerlingNameText != null)
        {
            enerlingNameText.text = info.ingredientName;
        }
        
        if (enerlingSpriteImage != null && info.enerlingSprite != null)
        {
            enerlingSpriteImage.sprite = info.enerlingSprite;
            enerlingSpriteImage.preserveAspect = true;
        }
        
        // Update rarity text and image
        if (rarityText != null)
        {
            rarityText.text = info.rarity.ToString();
            switch (info.rarity)
            {
                case IngredientDatabase.Rarity.Common:
                    rarityText.color = Color.white;
                    break;
                case IngredientDatabase.Rarity.Rare:
                    rarityText.color = Color.blue;
                    break;
                case IngredientDatabase.Rarity.UltraRare:
                    rarityText.color = Color.yellow;
                    break;
            }
        }
        
        // Update rarity image
        if (rarityImage != null)
        {
            switch (info.rarity)
            {
                case IngredientDatabase.Rarity.Common:
                    if (commonIcon != null)
                    {
                        rarityImage.sprite = commonIcon;
                        rarityImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        rarityImage.gameObject.SetActive(false);
                        Debug.LogWarning("CommonIcon sprite is missing!");
                    }
                    break;
                    
                case IngredientDatabase.Rarity.Rare:
                    if (rareIcon != null)
                    {
                        rarityImage.sprite = rareIcon;
                        rarityImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        rarityImage.gameObject.SetActive(false);
                        Debug.LogWarning("RareIcon sprite is missing!");
                    }
                    break;
                    
                case IngredientDatabase.Rarity.UltraRare:
                    if (ultraRareIcon != null)
                    {
                        rarityImage.sprite = ultraRareIcon;
                        rarityImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        rarityImage.gameObject.SetActive(false);
                        Debug.LogWarning("UltraRareIcon sprite is missing!");
                    }
                    break;
            }
        }
        
        // Update kingdom text and banner
        if (kingdomText != null)
        {
            kingdomText.text = info.kingdom.ToString();
        }
        
        // Update kingdom banner
        if (kingdomBannerImage != null)
        {
            switch (info.kingdom)
            {
                case IngredientDatabase.KingdomOrigin.NutriKingdom:
                    if (nutriBanner != null)
                    {
                        kingdomBannerImage.sprite = nutriBanner;
                        kingdomBannerImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        kingdomBannerImage.gameObject.SetActive(false);
                        Debug.LogWarning("NutriBanner sprite is missing!");
                    }
                    break;
                    
                case IngredientDatabase.KingdomOrigin.Sugaria:
                    if (sugariabanner != null)
                    {
                        kingdomBannerImage.sprite = sugariabanner;
                        kingdomBannerImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        kingdomBannerImage.gameObject.SetActive(false);
                        Debug.LogWarning("SugariaBanner sprite is missing!");
                    }
                    break;
                    
                case IngredientDatabase.KingdomOrigin.Preservia:
                    if (preserviaBanner != null)
                    {
                        kingdomBannerImage.sprite = preserviaBanner;
                        kingdomBannerImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        kingdomBannerImage.gameObject.SetActive(false);
                        Debug.LogWarning("PreserviaBanner sprite is missing!");
                    }
                    break;
                    
                case IngredientDatabase.KingdomOrigin.Alerthia:
                    if (allerthiaBanner != null)
                    {
                        kingdomBannerImage.sprite = allerthiaBanner;
                        kingdomBannerImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        kingdomBannerImage.gameObject.SetActive(false);
                        Debug.LogWarning("AllerthiaBanner sprite is missing!");
                    }
                    break;
            }
        }
        
        if (enerlingStoryText != null)
        {
            if (string.IsNullOrEmpty(info.enerlingStory) || 
                string.IsNullOrWhiteSpace(info.enerlingStory))
            {
                enerlingStoryText.text = "There's no enerling story available...";
                enerlingStoryText.fontStyle = FontStyles.Italic;
            }
            else
            {
                enerlingStoryText.text = info.enerlingStory;
            }
        }
        
        if (baseLifeText != null)
        {
            baseLifeText.text = $"{info.baseLife}";
        }
        
        if (armorPercentageText != null)
        {
            armorPercentageText.text = $"{info.armorPercent}%";
        }
        
        if (baseDamageText != null)
        {
            baseDamageText.text = $"{info.baseDamage}";
        }
        
        // Update Skill Visuals - Fixed: Set preserveAspect to false to use Image GameObject size
        UpdateSkillImage(skill1Image, info.skill1);
        UpdateSkillImage(skill2Image, info.skill2);
        UpdateSkillImage(skill3Image, info.skill3);
        UpdateSkillImage(skill4Image, info.skill4);
        
        // Update Organ Display
        UpdateOrganDisplay(info);
        
        Debug.Log($"Updated UI with: {info.ingredientName}");
    }
    
    private void UpdateSkillImage(Image skillImage, IngredientDatabase.SkillInfo skillInfo)
    {
        if (skillImage != null)
        {
            if (skillInfo != null && skillInfo.skillCircleIcon != null)
            {
                skillImage.sprite = skillInfo.skillCircleIcon;
                skillImage.preserveAspect = false; // Set to false to use the Image GameObject's size
                skillImage.gameObject.SetActive(true);
                
                // Log for debugging
                Debug.Log($"Set skill sprite to {skillInfo.skillCircleIcon.name} with preserveAspect: {skillImage.preserveAspect}");
            }
            else
            {
                skillImage.gameObject.SetActive(false);
            }
        }
    }
    
    private void UpdateOrganDisplay(IngredientDatabase.IngredientInfo info)
    {
        // Clear previous organ display
        ClearOrganDisplay();
        
        if (organsTxt == null || organsToShow == null || organImagePrefab == null)
        {
            Debug.LogWarning("Organ display components not properly assigned!");
            return;
        }
        
        // Determine which organ list to display
        List<string> organsToDisplay = null;
        string organLabel = "No Special Organs";
        
        if (info.beneficialOrgans != null && info.beneficialOrgans.Count > 0)
        {
            organsToDisplay = info.beneficialOrgans;
            organLabel = "Beneficial Organs";
        }
        else if (info.targetOrgans != null && info.targetOrgans.Count > 0)
        {
            organsToDisplay = info.targetOrgans;
            organLabel = "Target Organs";
        }
        
        // Update the organ text label
        organsTxt.text = organLabel;
        
        // If there are no organs, hide the container or show a message
        if (organsToDisplay == null || organsToDisplay.Count == 0)
        {
            // Optional: Show a placeholder or disable the whole section
            Debug.Log($"{info.ingredientName} has no special organs");
            return;
        }
        
        // Ensure organsToShow is active
        organsToShow.SetActive(true);
        
        // Create organ images for each organ
        foreach (string organName in organsToDisplay)
        {
            // Get the organ sprite from the database
            Sprite organSprite = ingredientDatabase.GetOrganSprite(organName);
            
            if (organSprite != null)
            {
                // Instantiate a new organ image
                GameObject organImageGO = Instantiate(organImagePrefab, organsToShow.transform);
                organImageInstances.Add(organImageGO);
                
                // Set up the image component
                Image organImage = organImageGO.GetComponent<Image>();
                if (organImage != null)
                {
                    organImage.sprite = organSprite;
                    organImage.preserveAspect = true;
                    
                    // Add a tooltip or label if needed
                    Tooltip tooltip = organImageGO.GetComponent<Tooltip>();
                    if (tooltip == null)
                    {
                        tooltip = organImageGO.AddComponent<Tooltip>();
                    }
                    tooltip.tooltipText = organName;
                }
                else
                {
                    Debug.LogWarning($"Organ image prefab doesn't have an Image component!");
                }
            }
            else
            {
                Debug.LogWarning($"No sprite found for organ: {organName}");
            }
        }
        
        Debug.Log($"Displayed {organsToDisplay.Count} organs for {info.ingredientName}");
    }
    
    private void ClearOrganDisplay()
    {
        // Destroy all existing organ image instances
        foreach (GameObject organImage in organImageInstances)
        {
            if (organImage != null)
            {
                Destroy(organImage);
            }
        }
        organImageInstances.Clear();
        
        // Optional: Disable or reset the organs container
        if (organsToShow != null)
        {
            // Reset to default state if needed
        }
    }
    
    // PlayEnerlingAudio method with component disabling
    public void PlayEnerlingAudio()
    {
        if (audioSource == null || currentEnerlingAudioClip == null) return;
        
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            ReenableComponents(); // Re-enable if we're stopping
        }
        else
        {
            // Store current states before disabling
            StoreComponentStates();
            
            // Disable specified components
            DisableComponents();
            
            // Play the audio
            audioSource.clip = currentEnerlingAudioClip;
            audioSource.Play();
            
            // Start coroutine to re-enable when audio finishes
            StartCoroutine(ReenableComponentsAfterAudio());
            
            Debug.Log($"Playing audio for: {currentEnerlingAudioClip.name}");
        }
    }
    
    private void StoreComponentStates()
    {
        foreach (GameObject obj in disableDuringAudio)
        {
            if (obj != null)
            {
                originalComponentStates[obj] = obj.activeSelf;
            }
        }
    }
    
    private void DisableComponents()
    {
        foreach (GameObject obj in disableDuringAudio)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
    
    private void ReenableComponents()
    {
        foreach (GameObject obj in disableDuringAudio)
        {
            if (obj != null && originalComponentStates.ContainsKey(obj))
            {
                obj.SetActive(originalComponentStates[obj]);
            }
        }
    }
    
    private IEnumerator ReenableComponentsAfterAudio()
    {
        // Wait for the audio to finish playing
        yield return new WaitWhile(() => audioSource.isPlaying);
        
        // Re-enable the components
        ReenableComponents();
        
        Debug.Log("Audio finished, components re-enabled");
    }
    
    public void CloseEnerlingInfoPanel()
    {
        if (enerlingInfoPanel == null) return;
        
        // Stop any audio and re-enable components
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            ReenableComponents();
        }
        
        // Clear organ display before closing
        ClearOrganDisplay();
        
        enerlingInfoPanel.SetActive(false);
        isPanelOpen = false;
        
        // Switch camera back to player and re-enable other enerlings
        if (cameraController != null)
        {
            cameraController.StopViewingEnerling();
        }
        
        Debug.Log("Closed info panel");
        
        // Check distance and decide whether to show button again
        if (currentNearbyEnerling != null && player != null)
        {
            float distance = Vector3.Distance(player.transform.position, currentNearbyEnerling.transform.position);
            if (distance <= detectionRange)
            {
                ShowButtonWithAnimation();
            }
            else
            {
                currentNearbyEnerling = null;
                buttonWasVisible = false;
            }
        }
        else
        {
            currentNearbyEnerling = null;
            buttonWasVisible = false;
        }
    }
    
    void OnDestroy()
    {
        // Clean up listeners
        if (viewEnerlingButton != null)
        {
            Button viewButton = viewEnerlingButton.GetComponent<Button>();
            if (viewButton != null)
            {
                viewButton.onClick.RemoveListener(OpenEnerlingInfoPanel);
            }
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseEnerlingInfoPanel);
        }
        
        // Clean up text-to-speech button listener
        if (textToSpeechButton != null)
        {
            textToSpeechButton.onClick.RemoveListener(PlayEnerlingAudio);
        }
        
        // Stop any running coroutines
        if (buttonAnimationCoroutine != null)
        {
            StopCoroutine(buttonAnimationCoroutine);
        }
        
        // Stop any playing audio and re-enable components
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            ReenableComponents();
        }
        
        // Clear organ display
        ClearOrganDisplay();
    }
    
    // Public method to test UI updates with a specific ingredient
    public void TestUIWithIngredient(string ingredientName)
    {
        if (ingredientDatabase != null)
        {
            var ingredientInfo = ingredientDatabase.GetIngredientInfo(ingredientName);
            if (ingredientInfo != null)
            {
                UpdateEnerlingInfoUI(ingredientInfo);
                Debug.Log($"Tested UI with ingredient: {ingredientName}");
            }
            else
            {
                Debug.LogWarning($"Ingredient not found: {ingredientName}");
            }
        }
        else
        {
            Debug.LogError("Ingredient Database is not assigned!");
        }
    }
    
    // Public method to manually set the current Enerling for testing
    public void SetCurrentEnerlingForTesting(Test_EnerlingController testEnerling)
    {
        currentNearbyEnerling = testEnerling;
        if (testEnerling != null)
        {
            var ingredientInfo = GetIngredientInfoFromEnerling(testEnerling);
            if (ingredientInfo != null)
            {
                UpdateEnerlingInfoUI(ingredientInfo);
            }
        }
    }
    
    // Method to add a GameObject to the disable list dynamically
    public void AddComponentToDisableList(GameObject component)
    {
        if (component != null && !disableDuringAudio.Contains(component))
        {
            disableDuringAudio.Add(component);
            originalComponentStates[component] = component.activeSelf;
            Debug.Log($"Added {component.name} to disable during audio list");
        }
    }
    
    // Method to remove a GameObject from the disable list
    public void RemoveComponentFromDisableList(GameObject component)
    {
        if (disableDuringAudio.Contains(component))
        {
            disableDuringAudio.Remove(component);
            if (originalComponentStates.ContainsKey(component))
            {
                originalComponentStates.Remove(component);
            }
            Debug.Log($"Removed {component.name} from disable during audio list");
        }
    }
}

// Simple Tooltip component for organ images (optional)
public class Tooltip : MonoBehaviour
{
    public string tooltipText = "";
    private GameObject tooltipObject;
    
    void Start()
    {
        // You can implement tooltip display logic here
        // For example, show on hover using EventTrigger component
    }
}