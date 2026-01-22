using UnityEngine;
using UnityEngine.UI;

public class K3_Phase1Functions : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Assign the PlayerArmature GameObject with 'Player' tag")]
    public GameObject playerArmature;
    
    [Header("GEM Objects - Assign in Inspector")]
    [Tooltip("The GEM object with 'K3_OxidantGEM' tag")]
    public GameObject oxidantGEM;
    [Tooltip("The GEM object with 'K3_MicrobeGEM' tag")]
    public GameObject microbeGEM;
    
    [Header("GEM Child Renderers - Assign in Inspector")]
    [Tooltip("Renderer of GemPackOxidant child (for material switching)")]
    public Renderer oxidantGemRenderer;
    [Tooltip("Renderer of GemPackMicrobe child (for material switching)")]
    public Renderer microbeGemRenderer;
    
    [Header("GEM Materials - Assign in Inspector")]
    [Tooltip("Initial dormant material for both GEMs")]
    public Material dormantMat;
    [Tooltip("Active material for oxidant GEM")]
    public Material antiOxidantMat;
    [Tooltip("Active material for microbe GEM")]
    public Material antiMicrobeMat;
    
    [Header("Info Panels - Initially Disabled")]
    [Tooltip("The Antioxidant info panel GameObject")]
    public GameObject antioxidantInfo;
    [Tooltip("The Antimicrobes info panel GameObject")]
    public GameObject antimicrobeInfo;
    
    [Header("VFX Objects - Initially Disabled")]
    [Tooltip("Oxidant GEM VFX GameObject (activate when panel closes)")]
    public GameObject oxidantVFX;
    [Tooltip("Microbe GEM VFX GameObject (activate when panel closes)")]
    public GameObject microbeVFX;
    
    [Header("Particle Systems - Initially Disabled")]
    [Tooltip("Particle system for Oxidant GEM (assign from oxidantGEM child)")]
    public ParticleSystem oxidantParticles;
    [Tooltip("Particle system for Microbe GEM (assign from microbeGEM child)")]
    public ParticleSystem microbeParticles;
    
    [Header("Close Buttons - Assign from Info Panels")]
    [Tooltip("Close button in Antioxidant info panel")]
    public Button closeAntioxidantButton;
    [Tooltip("Close button in Antimicrobe info panel")]
    public Button closeAntimicrobeButton;
    
    [Header("Respawn System")]
    [Tooltip("Death plane script reference for changing respawn point")]
    public K3_DeathplaneFall deathPlaneScript;
    
    [Tooltip("New respawn point when microbe GEM is activated")]
    public GameObject newRespawnPoint;
    
    [Tooltip("Particle system for new respawn point")]
    public ParticleSystem newRespawnParticles;
    
    [Header("New Respawn Particle Settings")]
    [Tooltip("Duration for new respawn particle effect")]
    public float newParticleDuration = 2f;
    
    [Tooltip("Particle outro animation duration")]
    public float newParticleOutroDuration = 0.5f;
    
    [Header("Audio Settings")]
    [Tooltip("Audio source for playing SFX (will create if not assigned)")]
    public AudioSource audioSource;
    
    [Tooltip("Sound when opening info panel")]
    public AudioClip panelOpenSFX;
    
    [Tooltip("Sound when closing info panel")]
    public AudioClip panelCloseSFX;
    
    [Tooltip("Volume for panel open/close sounds")]
    [Range(0f, 1f)]
    public float panelSoundVolume = 0.7f;
    
    [Tooltip("Sound when activating GEM particle system")]
    public AudioClip particleActivateSFX;
    
    [Tooltip("Sound when switching GEM material")]
    public AudioClip materialSwitchSFX;
    
    [Tooltip("Volume for particle activation sound")]
    [Range(0f, 1f)]
    public float particleSoundVolume = 0.5f;
    
    [Header("Settings")]
    [Tooltip("Collection range for GEMs")]
    public float collectionRange = 2f;
    
    [Tooltip("Cooldown between GEM interactions (seconds)")]
    public float interactionCooldown = 1f;
    
    [Header("Panel Settings")]
    [Tooltip("Prevent panel from reopening while player is in range")]
    public bool preventPanelSpam = true;
    
    [Tooltip("Cooldown after closing panel before it can reopen (seconds)")]
    public float panelReopenCooldown = 2f;
    
    [Header("Debug")]
    public bool showDebugMessages = true;
    
    private bool oxidantActivated = false;
    private bool microbeActivated = false;
    private float lastOxidantInteractionTime = -10f;
    private float lastMicrobeInteractionTime = -10f;
    private bool newRespawnPointSet = false;
    
    // Panel state tracking
    private bool oxidantPanelOpen = false;
    private bool microbePanelOpen = false;
    private float oxidantPanelCloseTime = -10f;
    private float microbePanelCloseTime = -10f;
    
    // Material state tracking
    private bool oxidantMaterialSwitched = false;
    private bool microbeMaterialSwitched = false;
    
    // Particle system instances
    private ParticleSystem activeNewRespawnParticles;
    
    private void Start()
    {
        InitializeAudio();
        InitializeSystem();
        SetupButtonListeners();
        InitializeMaterials();
        
        if (showDebugMessages)
        {
            Debug.Log("K3_Phase1Functions initialized");
            Debug.Log($"Player: {playerArmature?.name ?? "Not assigned"}");
            Debug.Log($"Oxidant GEM: {oxidantGEM?.name ?? "Not assigned"}");
            Debug.Log($"Microbe GEM: {microbeGEM?.name ?? "Not assigned"}");
        }
    }
    
    private void Update()
    {
        // Check for GEM interaction
        if (oxidantGEM != null && playerArmature != null && CanOpenPanel(true))
        {
            CheckGEMInteraction(oxidantGEM, true);
        }
        
        if (microbeGEM != null && playerArmature != null && CanOpenPanel(false))
        {
            CheckGEMInteraction(microbeGEM, false);
        }
    }
    
    private void InitializeAudio()
    {
        // Get or create AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound for UI
                
                if (showDebugMessages) Debug.Log("Created AudioSource component");
            }
        }
    }
    
    private void InitializeMaterials()
    {
        // Set initial dormant materials
        if (oxidantGemRenderer != null && dormantMat != null)
        {
            oxidantGemRenderer.material = dormantMat;
            if (showDebugMessages) Debug.Log("Set oxidant GEM to dormant material");
        }
        
        if (microbeGemRenderer != null && dormantMat != null)
        {
            microbeGemRenderer.material = dormantMat;
            if (showDebugMessages) Debug.Log("Set microbe GEM to dormant material");
        }
    }
    
    private void InitializeSystem()
    {
        // Ensure info panels are disabled at start
        if (antioxidantInfo != null && antioxidantInfo.activeSelf)
            antioxidantInfo.SetActive(false);
            
        if (antimicrobeInfo != null && antimicrobeInfo.activeSelf)
            antimicrobeInfo.SetActive(false);
        
        // Ensure VFX are disabled at start
        if (oxidantVFX != null && oxidantVFX.activeSelf)
            oxidantVFX.SetActive(false);
            
        if (microbeVFX != null && microbeVFX.activeSelf)
            microbeVFX.SetActive(false);
        
        // Ensure particle systems are disabled at start
        if (oxidantParticles != null && oxidantParticles.gameObject.activeSelf)
            oxidantParticles.gameObject.SetActive(false);
            
        if (microbeParticles != null && microbeParticles.gameObject.activeSelf)
            microbeParticles.gameObject.SetActive(false);
        
        // Ensure GEMs are active
        if (oxidantGEM != null && !oxidantGEM.activeSelf)
            oxidantGEM.SetActive(true);
            
        if (microbeGEM != null && !microbeGEM.activeSelf)
            microbeGEM.SetActive(true);
        
        // Initialize new respawn particles if assigned
        if (newRespawnParticles != null && newRespawnParticles.gameObject.activeSelf)
            newRespawnParticles.gameObject.SetActive(false);
    }
    
    private void SetupButtonListeners()
    {
        // Setup close button for antioxidant info
        if (closeAntioxidantButton != null)
        {
            closeAntioxidantButton.onClick.RemoveAllListeners();
            closeAntioxidantButton.onClick.AddListener(() => CloseAntioxidantInfo(true));
            
            if (showDebugMessages) Debug.Log("Antioxidant close button listener set");
        }
        else if (showDebugMessages)
        {
            Debug.LogWarning("Close Antioxidant button not assigned!");
        }
        
        // Setup close button for antimicrobe info
        if (closeAntimicrobeButton != null)
        {
            closeAntimicrobeButton.onClick.RemoveAllListeners();
            closeAntimicrobeButton.onClick.AddListener(() => CloseAntimicrobeInfo(true));
            
            if (showDebugMessages) Debug.Log("Antimicrobe close button listener set");
        }
        else if (showDebugMessages)
        {
            Debug.LogWarning("Close Antimicrobe button not assigned!");
        }
    }
    
    private bool CanOpenPanel(bool isOxidant)
    {
        if (!preventPanelSpam) return true;
        
        if (isOxidant)
        {
            // Don't open if panel is already open
            if (oxidantPanelOpen) return false;
            
            // Check cooldown after closing
            if (Time.time < oxidantPanelCloseTime + panelReopenCooldown) return false;
        }
        else
        {
            // Don't open if panel is already open
            if (microbePanelOpen) return false;
            
            // Check cooldown after closing
            if (Time.time < microbePanelCloseTime + panelReopenCooldown) return false;
        }
        
        return true;
    }
    
    private void CheckGEMInteraction(GameObject gem, bool isOxidant)
    {
        float distance = Vector3.Distance(playerArmature.transform.position, gem.transform.position);
        
        if (distance <= collectionRange)
        {
            InteractWithGEM(gem, isOxidant);
        }
    }
    
    private void InteractWithGEM(GameObject gem, bool isOxidant)
    {
        if (showDebugMessages) Debug.Log($"Interacted with {(isOxidant ? "Oxidant" : "Microbe")} GEM: {gem.name}");
        
        // Mark as activated if first time
        if (isOxidant && !oxidantActivated)
        {
            oxidantActivated = true;
        }
        else if (!isOxidant && !microbeActivated)
        {
            microbeActivated = true;
            // Set new respawn point when microbe GEM is activated for the first time
            if (!newRespawnPointSet && deathPlaneScript != null && newRespawnPoint != null)
            {
                SetNewRespawnPoint();
            }
        }
        
        // Show the appropriate info panel
        if (isOxidant)
        {
            ShowAntioxidantInfo();
        }
        else
        {
            ShowAntimicrobeInfo();
        }
    }
    
    private void ShowAntioxidantInfo()
    {
        if (antioxidantInfo != null && !antioxidantInfo.activeSelf)
        {
            antioxidantInfo.SetActive(true);
            oxidantPanelOpen = true;
            
            // Play panel open SFX
            PlayPanelOpenSound();
            
            if (showDebugMessages) Debug.Log("Antioxidant info panel opened");
        }
    }
    
    private void ShowAntimicrobeInfo()
    {
        if (antimicrobeInfo != null && !antimicrobeInfo.activeSelf)
        {
            antimicrobeInfo.SetActive(true);
            microbePanelOpen = true;
            
            // Play panel open SFX
            PlayPanelOpenSound();
            
            if (showDebugMessages) Debug.Log("Antimicrobe info panel opened");
        }
    }
    
    public void CloseAntioxidantInfo(bool fromButton = false)
    {
        if (antioxidantInfo != null && antioxidantInfo.activeSelf)
        {
            // Play panel close SFX if closed by button
            if (fromButton)
            {
                PlayPanelCloseSound();
            }
            
            antioxidantInfo.SetActive(false);
            oxidantPanelOpen = false;
            oxidantPanelCloseTime = Time.time;
            
            if (showDebugMessages) Debug.Log("Antioxidant info panel closed");
            
            // Switch material if not already switched
            if (fromButton && !oxidantMaterialSwitched)
            {
                SwitchOxidantMaterial();
            }
            
            // Enable VFX if not already active (only when closed by button)
            if (fromButton && oxidantVFX != null && !oxidantVFX.activeSelf)
            {
                oxidantVFX.SetActive(true);
                
                if (showDebugMessages) Debug.Log("Oxidant VFX activated");
            }
            
            // Enable particle system if not already active (only when closed by button)
            if (fromButton && oxidantParticles != null && !oxidantParticles.gameObject.activeSelf)
            {
                oxidantParticles.gameObject.SetActive(true);
                oxidantParticles.Play();
                
                // Play particle activation SFX
                PlayParticleActivateSound();
                
                if (showDebugMessages) Debug.Log("Oxidant particles activated");
            }
        }
    }
    
    public void CloseAntimicrobeInfo(bool fromButton = false)
    {
        if (antimicrobeInfo != null && antimicrobeInfo.activeSelf)
        {
            // Play panel close SFX if closed by button
            if (fromButton)
            {
                PlayPanelCloseSound();
            }
            
            antimicrobeInfo.SetActive(false);
            microbePanelOpen = false;
            microbePanelCloseTime = Time.time;
            
            if (showDebugMessages) Debug.Log("Antimicrobe info panel closed");
            
            // Switch material if not already switched
            if (fromButton && !microbeMaterialSwitched)
            {
                SwitchMicrobeMaterial();
            }
            
            // Enable VFX if not already active (only when closed by button)
            if (fromButton && microbeVFX != null && !microbeVFX.activeSelf)
            {
                microbeVFX.SetActive(true);
                
                if (showDebugMessages) Debug.Log("Microbe VFX activated");
            }
            
            // Enable particle system if not already active (only when closed by button)
            if (fromButton && microbeParticles != null && !microbeParticles.gameObject.activeSelf)
            {
                microbeParticles.gameObject.SetActive(true);
                microbeParticles.Play();
                
                // Play particle activation SFX
                PlayParticleActivateSound();
                
                if (showDebugMessages) Debug.Log("Microbe particles activated");
            }
        }
    }
    
    private void SwitchOxidantMaterial()
    {
        if (oxidantGemRenderer != null && antiOxidantMat != null)
        {
            oxidantGemRenderer.material = antiOxidantMat;
            oxidantMaterialSwitched = true;
            
            // Play material switch SFX
            PlayMaterialSwitchSound();
            
            if (showDebugMessages) Debug.Log("Switched oxidant GEM to AntiOxidant material");
        }
        else if (showDebugMessages)
        {
            if (oxidantGemRenderer == null) Debug.LogWarning("Oxidant renderer not assigned!");
            if (antiOxidantMat == null) Debug.LogWarning("AntiOxidant material not assigned!");
        }
    }
    
    private void SwitchMicrobeMaterial()
    {
        if (microbeGemRenderer != null && antiMicrobeMat != null)
        {
            microbeGemRenderer.material = antiMicrobeMat;
            microbeMaterialSwitched = true;
            
            // Play material switch SFX
            PlayMaterialSwitchSound();
            
            if (showDebugMessages) Debug.Log("Switched microbe GEM to AntiMicrobe material");
        }
        else if (showDebugMessages)
        {
            if (microbeGemRenderer == null) Debug.LogWarning("Microbe renderer not assigned!");
            if (antiMicrobeMat == null) Debug.LogWarning("AntiMicrobe material not assigned!");
        }
    }
    
    private void SetNewRespawnPoint()
    {
        if (deathPlaneScript != null && newRespawnPoint != null)
        {
            // Use the SetNewRespawnPoint method from K3_DeathplaneFall
            deathPlaneScript.SetNewRespawnPoint(newRespawnPoint);
            newRespawnPointSet = true;
            
            // Activate new respawn particles if assigned
            if (newRespawnParticles != null)
            {
                PlayNewRespawnParticles();
            }
            
            if (showDebugMessages) 
            {
                Debug.Log($"Respawn point updated to: {newRespawnPoint.name}");
                Debug.Log($"Position: {newRespawnPoint.transform.position}");
            }
        }
        else if (showDebugMessages)
        {
            if (deathPlaneScript == null) Debug.LogWarning("Death plane script not assigned!");
            if (newRespawnPoint == null) Debug.LogWarning("New respawn point not assigned!");
        }
    }
    
    private void PlayNewRespawnParticles()
    {
        if (newRespawnParticles != null && newRespawnPoint != null)
        {
            // Create a copy of the particle system at new respawn point
            activeNewRespawnParticles = Instantiate(newRespawnParticles, newRespawnPoint.transform.position, Quaternion.identity);
            activeNewRespawnParticles.gameObject.SetActive(true);
            
            // Play the particle system
            activeNewRespawnParticles.Play();
            
            // Start outro animation after main duration
            StartCoroutine(StartNewParticleOutro());
            
            // Destroy after complete duration
            Destroy(activeNewRespawnParticles.gameObject, newParticleDuration + 0.1f);
            
            if (showDebugMessages) 
            {
                Debug.Log($"New respawn particles started for {newParticleDuration} seconds");
            }
        }
        else if (showDebugMessages)
        {
            Debug.LogWarning("Cannot play new respawn particles - system not assigned or no respawn point");
        }
    }
    
    private System.Collections.IEnumerator StartNewParticleOutro()
    {
        yield return new WaitForSeconds(newParticleDuration - newParticleOutroDuration);
        
        if (activeNewRespawnParticles != null)
        {
            // Stop emitting new particles (start outro)
            var emission = activeNewRespawnParticles.emission;
            emission.enabled = false;
            
            if (showDebugMessages) 
            {
                Debug.Log("New respawn particle outro animation started");
            }
        }
    }
    
    private void PlayPanelOpenSound()
    {
        if (panelOpenSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(panelOpenSFX, panelSoundVolume);
            
            if (showDebugMessages) Debug.Log("Played panel open sound");
        }
        else if (showDebugMessages)
        {
            if (panelOpenSFX == null) Debug.LogWarning("Panel open SFX not assigned!");
            if (audioSource == null) Debug.LogWarning("Audio source not available!");
        }
    }
    
    private void PlayPanelCloseSound()
    {
        if (panelCloseSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(panelCloseSFX, panelSoundVolume);
            
            if (showDebugMessages) Debug.Log("Played panel close sound");
        }
        else if (showDebugMessages)
        {
            if (panelCloseSFX == null) Debug.LogWarning("Panel close SFX not assigned!");
            if (audioSource == null) Debug.LogWarning("Audio source not available!");
        }
    }
    
    private void PlayParticleActivateSound()
    {
        if (particleActivateSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(particleActivateSFX, particleSoundVolume);
            
            if (showDebugMessages) Debug.Log("Played particle activation sound");
        }
        else if (showDebugMessages)
        {
            if (particleActivateSFX == null) Debug.LogWarning("Particle activate SFX not assigned!");
            if (audioSource == null) Debug.LogWarning("Audio source not available!");
        }
    }
    
    private void PlayMaterialSwitchSound()
    {
        if (materialSwitchSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(materialSwitchSFX, particleSoundVolume);
            
            if (showDebugMessages) Debug.Log("Played material switch sound");
        }
        else if (showDebugMessages)
        {
            if (materialSwitchSFX == null) Debug.LogWarning("Material switch SFX not assigned!");
            if (audioSource == null) Debug.LogWarning("Audio source not available!");
        }
    }
    
    [ContextMenu("Test Oxidant Interaction")]
    public void TestOxidantInteraction()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        if (oxidantGEM != null)
        {
            Debug.Log("=== TESTING OXIDANT GEM INTERACTION ===");
            InteractWithGEM(oxidantGEM, true);
        }
        else
        {
            Debug.LogError("Cannot test: Oxidant GEM not assigned!");
        }
    }
    
    [ContextMenu("Test Microbe Interaction")]
    public void TestMicrobeInteraction()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        if (microbeGEM != null)
        {
            Debug.Log("=== TESTING MICROBE GEM INTERACTION ===");
            InteractWithGEM(microbeGEM, false);
        }
        else
        {
            Debug.LogError("Cannot test: Microbe GEM not assigned!");
        }
    }
    
    [ContextMenu("Test Switch Oxidant Material")]
    public void TestSwitchOxidantMaterial()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        Debug.Log("=== TESTING SWITCH OXIDANT MATERIAL ===");
        SwitchOxidantMaterial();
    }
    
    [ContextMenu("Test Switch Microbe Material")]
    public void TestSwitchMicrobeMaterial()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        Debug.Log("=== TESTING SWITCH MICROBE MATERIAL ===");
        SwitchMicrobeMaterial();
    }
    
    [ContextMenu("Test Close Antioxidant Info")]
    public void TestCloseAntioxidantInfo()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        Debug.Log("=== TESTING CLOSE ANTIOXIDANT INFO ===");
        CloseAntioxidantInfo(true);
    }
    
    [ContextMenu("Test Close Antimicrobe Info")]
    public void TestCloseAntimicrobeInfo()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        Debug.Log("=== TESTING CLOSE ANTIMICROBE INFO ===");
        CloseAntimicrobeInfo(true);
    }
    
    [ContextMenu("Test Set New Respawn Point")]
    public void TestSetNewRespawnPoint()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        Debug.Log("=== TESTING SET NEW RESPAWN POINT ===");
        SetNewRespawnPoint();
    }
    
    [ContextMenu("Test New Respawn Particles")]
    public void TestNewRespawnParticles()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        Debug.Log("=== TESTING NEW RESPAWN PARTICLES ===");
        PlayNewRespawnParticles();
    }
    
    [ContextMenu("Reset All Systems")]
    public void ResetAllSystems()
    {
        oxidantActivated = false;
        microbeActivated = false;
        newRespawnPointSet = false;
        
        oxidantPanelOpen = false;
        microbePanelOpen = false;
        oxidantPanelCloseTime = -10f;
        microbePanelCloseTime = -10f;
        
        oxidantMaterialSwitched = false;
        microbeMaterialSwitched = false;
        
        // Ensure GEMs are active
        if (oxidantGEM != null)
        {
            oxidantGEM.SetActive(true);
        }
        
        if (microbeGEM != null)
        {
            microbeGEM.SetActive(true);
        }
        
        // Reset materials to dormant
        if (oxidantGemRenderer != null && dormantMat != null)
        {
            oxidantGemRenderer.material = dormantMat;
        }
        
        if (microbeGemRenderer != null && dormantMat != null)
        {
            microbeGemRenderer.material = dormantMat;
        }
        
        // Close info panels
        if (antioxidantInfo != null)
        {
            antioxidantInfo.SetActive(false);
        }
        
        if (antimicrobeInfo != null)
        {
            antimicrobeInfo.SetActive(false);
        }
        
        // Disable VFX
        if (oxidantVFX != null)
        {
            oxidantVFX.SetActive(false);
        }
        
        if (microbeVFX != null)
        {
            microbeVFX.SetActive(false);
        }
        
        // Stop and disable particle systems
        if (oxidantParticles != null)
        {
            oxidantParticles.gameObject.SetActive(false);
            oxidantParticles.Stop();
        }
        
        if (microbeParticles != null)
        {
            microbeParticles.gameObject.SetActive(false);
            microbeParticles.Stop();
        }
        
        // Destroy active new respawn particles
        if (activeNewRespawnParticles != null)
        {
            Destroy(activeNewRespawnParticles.gameObject);
            activeNewRespawnParticles = null;
        }
        
        Debug.Log("All systems reset to initial state");
    }
    
    [ContextMenu("Debug System Status")]
    public void DebugSystemStatus()
    {
        Debug.Log("=== GEM INTERACTION SYSTEM STATUS ===");
        Debug.Log($"Player Armature: {playerArmature?.name ?? "Not assigned"}");
        Debug.Log($"");
        Debug.Log($"OXIDANT GEM:");
        Debug.Log($"- Object: {oxidantGEM?.name ?? "Not assigned"}");
        Debug.Log($"- Active: {oxidantGEM?.activeSelf ?? false}");
        Debug.Log($"- Activated (first time): {oxidantActivated}");
        Debug.Log($"- Panel Open: {oxidantPanelOpen}");
        Debug.Log($"- Panel Close Time: {(Time.time - oxidantPanelCloseTime):F1}s ago");
        Debug.Log($"- Material Switched: {oxidantMaterialSwitched}");
        Debug.Log($"- Current Material: {oxidantGemRenderer?.material?.name ?? "No renderer/material"}");
        Debug.Log($"- VFX: {oxidantVFX?.name ?? "Not assigned"}");
        Debug.Log($"- VFX Active: {oxidantVFX?.activeSelf ?? false}");
        Debug.Log($"- Particles: {oxidantParticles?.name ?? "Not assigned"}");
        Debug.Log($"- Particles Active: {oxidantParticles?.gameObject.activeInHierarchy ?? false}");
        Debug.Log($"");
        Debug.Log($"MICROBE GEM:");
        Debug.Log($"- Object: {microbeGEM?.name ?? "Not assigned"}");
        Debug.Log($"- Active: {microbeGEM?.activeSelf ?? false}");
        Debug.Log($"- Activated (first time): {microbeActivated}");
        Debug.Log($"- Panel Open: {microbePanelOpen}");
        Debug.Log($"- Panel Close Time: {(Time.time - microbePanelCloseTime):F1}s ago");
        Debug.Log($"- Material Switched: {microbeMaterialSwitched}");
        Debug.Log($"- Current Material: {microbeGemRenderer?.material?.name ?? "No renderer/material"}");
        Debug.Log($"- VFX: {microbeVFX?.name ?? "Not assigned"}");
        Debug.Log($"- VFX Active: {microbeVFX?.activeSelf ?? false}");
        Debug.Log($"- Particles: {microbeParticles?.name ?? "Not assigned"}");
        Debug.Log($"- Particles Active: {microbeParticles?.gameObject.activeInHierarchy ?? false}");
        Debug.Log($"");
        Debug.Log($"MATERIALS:");
        Debug.Log($"- Dormant Material: {dormantMat?.name ?? "Not assigned"}");
        Debug.Log($"- AntiOxidant Material: {antiOxidantMat?.name ?? "Not assigned"}");
        Debug.Log($"- AntiMicrobe Material: {antiMicrobeMat?.name ?? "Not assigned"}");
        Debug.Log($"");
        Debug.Log($"INFO PANELS:");
        Debug.Log($"- Antioxidant Info: {antioxidantInfo?.name ?? "Not assigned"}");
        Debug.Log($"- Antioxidant Active: {antioxidantInfo?.activeSelf ?? false}");
        Debug.Log($"- Antimicrobe Info: {antimicrobeInfo?.name ?? "Not assigned"}");
        Debug.Log($"- Antimicrobe Active: {antimicrobeInfo?.activeSelf ?? false}");
        Debug.Log($"");
        Debug.Log($"RESPAWN SYSTEM:");
        Debug.Log($"- Death Plane Script: {deathPlaneScript?.name ?? "Not assigned"}");
        Debug.Log($"- New Respawn Point: {newRespawnPoint?.name ?? "Not assigned"}");
        Debug.Log($"- New Respawn Point Set: {newRespawnPointSet}");
        Debug.Log($"- New Respawn Particles: {newRespawnParticles?.name ?? "Not assigned"}");
        Debug.Log($"- Active New Particles: {activeNewRespawnParticles != null}");
        Debug.Log($"- Particle Duration: {newParticleDuration}s");
        Debug.Log($"- Particle Outro Duration: {newParticleOutroDuration}s");
        Debug.Log($"");
        Debug.Log($"PANEL SETTINGS:");
        Debug.Log($"- Prevent Panel Spam: {preventPanelSpam}");
        Debug.Log($"- Panel Reopen Cooldown: {panelReopenCooldown}s");
        Debug.Log($"");
        Debug.Log($"AUDIO:");
        Debug.Log($"- Audio Source: {audioSource != null}");
        Debug.Log($"- Panel Open SFX: {panelOpenSFX?.name ?? "Not assigned"}");
        Debug.Log($"- Panel Close SFX: {panelCloseSFX?.name ?? "Not assigned"}");
        Debug.Log($"- Particle Activate SFX: {particleActivateSFX?.name ?? "Not assigned"}");
        Debug.Log($"- Material Switch SFX: {materialSwitchSFX?.name ?? "Not assigned"}");
        Debug.Log($"");
        Debug.Log($"SETTINGS:");
        Debug.Log($"- Collection Range: {collectionRange}m");
        Debug.Log($"- Interaction Cooldown: {interactionCooldown}s");
        Debug.Log($"================================");
    }
    
    // Visualize collection range in editor
    private void OnDrawGizmosSelected()
    {
        if (playerArmature != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerArmature.transform.position, collectionRange);
        }
        
        if (oxidantGEM != null)
        {
            Gizmos.color = oxidantActivated ? new Color(1f, 1f, 0f, 0.3f) : Color.yellow;
            Gizmos.DrawWireSphere(oxidantGEM.transform.position, 0.5f);
            Gizmos.DrawWireSphere(oxidantGEM.transform.position, collectionRange);
        }
        
        if (microbeGEM != null)
        {
            Gizmos.color = microbeActivated ? new Color(0f, 1f, 0f, 0.3f) : Color.green;
            Gizmos.DrawWireSphere(microbeGEM.transform.position, 0.5f);
            Gizmos.DrawWireSphere(microbeGEM.transform.position, collectionRange);
        }
        
        if (newRespawnPoint != null)
        {
            Gizmos.color = newRespawnPointSet ? Color.blue : Color.magenta;
            Gizmos.DrawSphere(newRespawnPoint.transform.position, 0.5f);
            Gizmos.DrawWireSphere(newRespawnPoint.transform.position, 1f);
        }
    }
}