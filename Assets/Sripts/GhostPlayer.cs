using System.Collections.Generic;
using UnityEngine;
using Ashsvp;

public class GhostPlayer : MonoBehaviour
{
    [HideInInspector]
    public bool isPlaying = false;
    [HideInInspector]
    public Vector3 positionOffset = Vector3.zero;
    
    private List<GhostData> ghostPath;
    private int currentIndex = 0;
    private float playbackStartTime = 0f;
    
    void Start()
    {
        SetupGhostVisuals();
    }

    void SetupGhostVisuals()
    {
        // Disable physics for ghost (make it kinematic so it doesn't respond to physics)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Make all colliders triggers so ghost doesn't block player
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.isTrigger = true;
        }
        
        // Disable vehicle controller script if present (ghost shouldn't be controllable)
        SimcadeVehicleController vehicleController = GetComponent<SimcadeVehicleController>();
        if (vehicleController != null)
        {
            vehicleController.enabled = false;
        }
    }
    
    public void ApplyMaterial(Material ghostMaterial)
    {
        if (ghostMaterial != null)
        {
            ApplyCustomMaterial(ghostMaterial);
        }
        else
        {
            CreateAutoMaterial();
        }
    }
    
    void ApplyCustomMaterial(Material material)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }
            renderer.materials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    
    void CreateAutoMaterial()
    {
        // Get all renderers in the ghost vehicle
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            // Create new materials array for this renderer
            Material[] newMaterials = new Material[renderer.materials.Length];
            
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                // Clone the original material to avoid modifying the original
                Material originalMaterial = renderer.materials[i];
                Material ghostMat = new Material(originalMaterial);
                
                // Make it transparent with a cyan/blue tint
                if (ghostMat.HasProperty("_Color"))
                {
                    ghostMat.color = new Color(0.25f, 0.7f, 1.0f, 0.5f); // Cyan with 50% transparency
                }
                
                // Configure transparency settings for Standard shader
                if (ghostMat.shader.name.Contains("Standard"))
                {
                    ghostMat.SetFloat("_Mode", 3); // Set to transparent mode
                    ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    ghostMat.SetInt("_ZWrite", 0); // Disable depth writing for transparency
                    ghostMat.DisableKeyword("_ALPHATEST_ON");
                    ghostMat.EnableKeyword("_ALPHABLEND_ON"); // Enable alpha blending
                    ghostMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    ghostMat.renderQueue = 3000; // Render after opaque objects
                }
                
                newMaterials[i] = ghostMat;
            }
            
            // Apply the new materials and disable shadows
            renderer.materials = newMaterials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    
    public void StartPlayback(List<GhostData> path)
    {
        ghostPath = new List<GhostData>(path);
        currentIndex = 0;
        playbackStartTime = Time.time;
        isPlaying = true;
        
        // Устанавливаем начальную позицию с учетом offset
        if (ghostPath.Count > 0)
        {
            transform.position = ghostPath[0].position + positionOffset;
            transform.rotation = ghostPath[0].rotation;
        }
    }
    
    public void StopPlayback()
    {
        isPlaying = false;
        currentIndex = 0;
    }
    
    void Update()
    {
        if (isPlaying && ghostPath != null && ghostPath.Count > 0)
        {
            UpdateGhostPosition();
        }
    }
    
    void UpdateGhostPosition()
    {
        // Calculate how much time has passed since playback started
        float currentTime = Time.time - playbackStartTime;
        
        // Advance to the next data point if we've passed its timestamp
        while (currentIndex < ghostPath.Count - 1 && ghostPath[currentIndex + 1].timeStamp <= currentTime)
        {
            currentIndex++;
        }
        
        // Check if we've reached the end of the recorded path
        if (currentIndex >= ghostPath.Count - 1)
        {
            // Set to final position and stop playback
            transform.position = ghostPath[ghostPath.Count - 1].position + positionOffset;
            transform.rotation = ghostPath[ghostPath.Count - 1].rotation;
            StopPlayback();
            return;
        }
        
        // Interpolate between current and next data points for smooth movement
        GhostData currentPoint = ghostPath[currentIndex];
        GhostData nextPoint = ghostPath[currentIndex + 1];
        
        // Calculate interpolation factor based on time between points
        float timeDifference = nextPoint.timeStamp - currentPoint.timeStamp;
        float interpolationFactor = 0f;
        
        if (timeDifference > 0)
        {
            interpolationFactor = (currentTime - currentPoint.timeStamp) / timeDifference;
            interpolationFactor = Mathf.Clamp01(interpolationFactor); // Clamp between 0 and 1
        }
        
        // Smoothly interpolate position and rotation, applying position offset
        Vector3 targetPosition = Vector3.Lerp(currentPoint.position, nextPoint.position, interpolationFactor);
        transform.position = targetPosition + positionOffset;
        transform.rotation = Quaternion.Lerp(currentPoint.rotation, nextPoint.rotation, interpolationFactor);
    }
}