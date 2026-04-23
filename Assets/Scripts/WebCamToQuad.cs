using UnityEngine;
using System.Collections;
using System.Linq;

/// <summary>
/// Displays WebCamTexture camera data on a Quad for Android/Quest devices
/// </summary>
public class WebCamToQuad : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Quad GameObject to display camera feed")]
    public GameObject targetQuad;

    [Header("Camera Settings")]
    [Tooltip("Requested camera resolution width (0 = device default)")]
    public int requestedWidth = 1280;

    [Tooltip("Requested camera resolution height (0 = device default)")]
    public int requestedHeight = 720;

    [Tooltip("Requested FPS (0 = device default)")]
    public int requestedFPS = 30;

    [Tooltip("Select specific camera device (leave empty for default front camera)")]
    public string specificDeviceName = "";

    [Header("Options")]
    [Tooltip("Auto-start camera on Start()")]
    public bool autoStart = true;

    [Tooltip("Mirror the camera horizontally")]
    public bool mirrorHorizontally = false;

    private WebCamTexture m_WebCamTexture;
    private Material m_QuadMaterial;
    private Renderer m_QuadRenderer;
    private int m_CachedRotation = -1;
    private bool m_CachedVerticallyMirrored;
    private bool m_CachedMirrorHorizontally;

    void Start()
    {
        // Validate references
        if (targetQuad == null)
        {
            Debug.LogError("[WebCamToQuad] Target Quad is not assigned!");
            enabled = false;
            return;
        }

        // Setup quad
        m_QuadRenderer = targetQuad.GetComponent<Renderer>();
        if (m_QuadRenderer == null)
        {
            Debug.LogError("[WebCamToQuad] Target Quad has no Renderer component!");
            enabled = false;
            return;
        }

        m_QuadMaterial = m_QuadRenderer.material;
        if (m_QuadMaterial == null)
        {
            Debug.LogError("[WebCamToQuad] Target Quad has no Material assigned!");
            enabled = false;
            return;
        }

        // List available cameras
        ListAvailableCameras();

        // Auto-start if enabled
        if (autoStart)
        {
            StartCamera();
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (m_WebCamTexture != null && m_WebCamTexture.isPlaying)
        {
            m_WebCamTexture.Stop();
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        StopCamera();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // App paused - stop camera
            if (m_WebCamTexture != null && m_WebCamTexture.isPlaying)
            {
                m_WebCamTexture.Pause();
            }
        }
        else
        {
            // App resumed - restart camera
            if (m_WebCamTexture != null && !m_WebCamTexture.isPlaying)
            {
                m_WebCamTexture.Play();
            }
        }
    }

    void Update()
    {
        // Update texture rotation and scale if needed
        if (m_WebCamTexture != null && m_WebCamTexture.isPlaying)
        {
            UpdateQuadTransform();
        }
    }

    /// <summary>
    /// List all available camera devices
    /// </summary>
    private void ListAvailableCameras()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogWarning("[WebCamToQuad] No camera devices found!");
            return;
        }

        Debug.Log($"[WebCamToQuad] ========== Available Cameras ({devices.Length}) ==========");
        for (int i = 0; i < devices.Length; i++)
        {
            var device = devices[i];
            string facing = device.isFrontFacing ? "Front" : "Back";
            string kind = device.kind.ToString();

            Debug.Log($"[WebCamToQuad] [{i}] {device.name}");
            Debug.Log($"[WebCamToQuad]     - Facing: {facing}, Kind: {kind}");

            // List available resolutions if supported
            if (device.availableResolutions != null && device.availableResolutions.Length > 0)
            {
                var resolutions = device.availableResolutions;
                Debug.Log($"[WebCamToQuad]     - Resolutions ({resolutions.Length}):");
                foreach (var res in resolutions)
                {
                    Debug.Log($"[WebCamToQuad]       {res.width}x{res.height} @ {res.refreshRateRatio}Hz");
                }
            }
            else
            {
                Debug.Log($"[WebCamToQuad]     - Resolutions: (default)");
            }
        }
        Debug.Log($"[WebCamToQuad] ================================================");
    }

    /// <summary>
    /// Start the camera
    /// </summary>
    public void StartCamera()
    {
        if (m_WebCamTexture != null && m_WebCamTexture.isPlaying)
        {
            Debug.LogWarning("[WebCamToQuad] Camera is already running!");
            return;
        }

        StartCoroutine(StartCameraCoroutine());
    }

    private IEnumerator StartCameraCoroutine()
    {
        // Request camera permission on Android
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            Debug.Log("[WebCamToQuad] Requesting camera permission...");
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);

            // Poll for permission with timeout
            float timeout = 30f;
            float elapsed = 0f;
            while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
                if (elapsed >= timeout)
                {
                    Debug.LogError("[WebCamToQuad] Camera permission request timed out!");
                    yield break;
                }
            }
            Debug.Log("[WebCamToQuad] Camera permission granted");
        }
        #endif

        // Wait a frame to ensure permission is granted
        yield return null;

        // Select camera device
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("[WebCamToQuad] No camera devices available!");
            yield break;
        }

        string deviceName = "";

        // Use specific device if specified
        if (!string.IsNullOrEmpty(specificDeviceName))
        {
            var device = devices.FirstOrDefault(d => d.name == specificDeviceName);
            if (!string.IsNullOrEmpty(device.name))
            {
                deviceName = device.name;
                Debug.Log($"[WebCamToQuad] Using specific camera: {deviceName}");
            }
            else
            {
                Debug.LogWarning($"[WebCamToQuad] Specified camera '{specificDeviceName}' not found, using default");
            }
        }
        
        if (string.IsNullOrEmpty(deviceName))
        {
            // If no device is selected, use camera index 2, because cameras 0 and 2 are the front-facing cameras on the GalaxyXR
            deviceName = devices[2].name;
            Debug.Log($"[WebCamToQuad] Using camera: {deviceName}");
        }

        // Create WebCamTexture
        if (requestedWidth > 0 && requestedHeight > 0 && requestedFPS > 0)
        {
            m_WebCamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFPS);
        }
        else if (requestedWidth > 0 && requestedHeight > 0)
        {
            m_WebCamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight);
        }
        else
        {
            m_WebCamTexture = new WebCamTexture(deviceName);
        }

        // Apply texture to material
        m_QuadMaterial.mainTexture = m_WebCamTexture;
        
        // Start playing
        m_WebCamTexture.Play();

        // Wait for camera to actually start and report correct resolution
        yield return new WaitUntil(() => m_WebCamTexture.width > 16 && m_WebCamTexture.height > 16);

        Debug.Log($"[WebCamToQuad] Camera started: {m_WebCamTexture.width}x{m_WebCamTexture.height} @ {m_WebCamTexture.requestedFPS}fps");

        // Force initial transform update
        m_CachedRotation = -1;
        UpdateQuadTransform();
    }

    /// <summary>
    /// Stop the camera
    /// </summary>
    public void StopCamera()
    {
        if (m_WebCamTexture != null)
        {
            if (m_WebCamTexture.isPlaying)
            {
                m_WebCamTexture.Stop();
            }
            Destroy(m_WebCamTexture);
            m_WebCamTexture = null;
            Debug.Log("[WebCamToQuad] Camera stopped");
        }
    }

    /// <summary>
    /// Update quad transform based on camera rotation (only when values change)
    /// </summary>
    private void UpdateQuadTransform()
    {
        // Get camera video rotation angle
        int rotation = m_WebCamTexture.videoRotationAngle;
        bool verticallyMirrored = m_WebCamTexture.videoVerticallyMirrored;

        // Skip if nothing changed
        if (rotation == m_CachedRotation &&
            verticallyMirrored == m_CachedVerticallyMirrored &&
            mirrorHorizontally == m_CachedMirrorHorizontally)
        {
            return;
        }

        // Cache current values
        m_CachedRotation = rotation;
        m_CachedVerticallyMirrored = verticallyMirrored;
        m_CachedMirrorHorizontally = mirrorHorizontally;

        // Apply rotation to texture
        float scaleX = mirrorHorizontally ? -1f : 1f;
        float scaleY = verticallyMirrored ? -1f : 1f;

        m_QuadMaterial.mainTextureScale = new Vector2(scaleX, scaleY);
        m_QuadMaterial.mainTextureOffset = new Vector2(
            scaleX < 0 ? 1f : 0f,
            scaleY < 0 ? 1f : 0f
        );

        Debug.Log($"[WebCamToQuad] Transform updated - Rotation: {rotation}, VertMirror: {verticallyMirrored}, HorizMirror: {mirrorHorizontally}");
    }

    // Public control methods
    public void SetMirrorHorizontally(bool mirror)
    {
        mirrorHorizontally = mirror;
    }

    public bool IsPlaying()
    {
        return m_WebCamTexture != null && m_WebCamTexture.isPlaying;
    }

    public WebCamTexture GetWebCamTexture()
    {
        return m_WebCamTexture;
    }

    public void SwitchCamera(string deviceName)
    {
        StopCamera();
        specificDeviceName = deviceName;
        StartCamera();
    }

    /// <summary>
    /// Get all available camera device names
    /// </summary>
    public string[] GetAvailableCameraNames()
    {
        return WebCamTexture.devices.Select(d => d.name).ToArray();
    }
}
