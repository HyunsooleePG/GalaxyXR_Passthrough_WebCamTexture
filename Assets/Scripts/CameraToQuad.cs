using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using System.Collections;

/// <summary>
/// Displays XRCpuImage camera data on a Quad in real-time for Meta Quest 3
/// </summary>
public class CameraToQuad : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ARCameraManager component")]
    public ARCameraManager arCameraManager;

    [Tooltip("Quad GameObject to display camera feed")]
    public GameObject targetQuad;

    [Header("Settings")]
    [Tooltip("Output texture resolution (0 = use camera resolution)")]
    public Vector2Int outputResolution = Vector2Int.zero;

    [Tooltip("Apply mirror transformation")]
    public bool mirrorX = true;

    [Tooltip("Update every N frames (1 = every frame, 2 = every other frame)")]
    [Range(1, 10)]
    public int updateInterval = 1;

    private Texture2D m_CameraTexture;
    private Material m_QuadMaterial;
    private bool m_IsProcessing = false;
    private int m_FrameCounter = 0;

    void Start()
    {
        // Validate references
        if (arCameraManager == null)
        {
            Debug.LogError("[CameraToQuad] ARCameraManager is not assigned!");
            enabled = false;
            return;
        }

        if (targetQuad == null)
        {
            Debug.LogError("[CameraToQuad] Target Quad is not assigned!");
            enabled = false;
            return;
        }

        // Check if camera image is supported
        var loader = LoaderUtility.GetActiveLoader();
        if (loader == null)
        {
            Debug.LogError("[CameraToQuad] LoaderUtility.GetActiveLoader() is null");
            enabled = false;
            return;
        }

        var cameraSubsystem = loader.GetLoadedSubsystem<XRCameraSubsystem>();
        if (cameraSubsystem == null)
        {
            Debug.LogError("[CameraToQuad] XRCameraSubsystem is not supported on this platform");
        }

        if (cameraSubsystem != null && !cameraSubsystem.subsystemDescriptor.supportsCameraImage)
        {
            Debug.LogError("[CameraToQuad] XRCameraSubsystem does not support camera image");
        }

        if (arCameraManager.descriptor == null || !arCameraManager.descriptor.supportsCameraImage)
        {
            Debug.LogError("[CameraToQuad] ARCameraManager does not support camera image");
        }

        // Setup quad material
        SetupQuadMaterial();

        Debug.Log("[CameraToQuad] Initialized successfully");
    }

    void OnEnable()
    {
        if (arCameraManager != null)
        {
            arCameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        m_IsProcessing = false;

        if (arCameraManager != null)
        {
            arCameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    void OnDestroy()
    {
        // Stop any running coroutines
        StopAllCoroutines();

        // Cleanup texture (material is not destroyed as it's the original asset)
        if (m_CameraTexture != null)
        {
            Destroy(m_CameraTexture);
            m_CameraTexture = null;
        }
    }

    private void SetupQuadMaterial()
    {
        var renderer = targetQuad.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("[CameraToQuad] Target Quad has no Renderer component!");
            enabled = false;
            return;
        }

        // Use existing material on the Quad
        m_QuadMaterial = renderer.material;
        if (m_QuadMaterial == null)
        {
            Debug.LogError("[CameraToQuad] Target Quad has no Material assigned!");
            enabled = false;
            return;
        }
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        // Skip frames based on update interval
        m_FrameCounter++;
        if (m_FrameCounter % updateInterval != 0)
            return;

        // Skip if already processing
        if (m_IsProcessing)
            return;

        // Try to acquire the latest camera image
        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            // Only log occasionally to avoid spam
            if (m_FrameCounter % 60 == 0)
                Debug.LogWarning("[CameraToQuad] Failed to acquire CPU image");
            return;
        }

        Debug.Log($"[CameraToQuad] Acquired image: {image.width}x{image.height}, format: {image.format}");

        // Start async conversion
        StartCoroutine(ProcessCameraImageAsync(image));
    }

    IEnumerator ProcessCameraImageAsync(XRCpuImage image)
    {
        m_IsProcessing = true;

        // Determine output dimensions
        Vector2Int dimensions = outputResolution;
        if (dimensions.x <= 0 || dimensions.y <= 0)
        {
            dimensions = new Vector2Int(image.width, image.height);
        }

        // Setup conversion parameters
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = dimensions,
            outputFormat = TextureFormat.RGBA32,
            transformation = mirrorX ? XRCpuImage.Transformation.MirrorX : XRCpuImage.Transformation.None
        };

        // Create async conversion request
        var request = image.ConvertAsync(conversionParams);

        // Dispose the original image (we have the request now)
        image.Dispose();

        // Wait for conversion to complete
        while (!request.status.IsDone())
            yield return null;

        // Check conversion status
        if (request.status != XRCpuImage.AsyncConversionStatus.Ready)
        {
            Debug.LogError($"[CameraToQuad] Conversion failed with status: {request.status}");
            request.Dispose();
            m_IsProcessing = false;
            yield break;
        }

        Debug.Log($"[CameraToQuad] Conversion complete, output: {dimensions.x}x{dimensions.y}");

        // Get the converted data
        var rawData = request.GetData<byte>();

        // Create or update texture
        if (m_CameraTexture == null ||
            m_CameraTexture.width != dimensions.x ||
            m_CameraTexture.height != dimensions.y)
        {
            if (m_CameraTexture != null)
                Destroy(m_CameraTexture);

            m_CameraTexture = new Texture2D(
                dimensions.x,
                dimensions.y,
                TextureFormat.RGBA32,
                false);

            m_CameraTexture.filterMode = FilterMode.Bilinear;
            m_CameraTexture.wrapMode = TextureWrapMode.Clamp;

            Debug.Log($"[CameraToQuad] Created new texture: {dimensions.x}x{dimensions.y}");
        }

        // Load raw texture data
        m_CameraTexture.LoadRawTextureData(rawData);
        m_CameraTexture.Apply();

        // Apply to material
        if (m_QuadMaterial != null)
        {
            m_QuadMaterial.mainTexture = m_CameraTexture;
            Debug.Log($"[CameraToQuad] Applied texture to material: {m_QuadMaterial.name}");
        }
        else
        {
            Debug.LogError("[CameraToQuad] m_QuadMaterial is null!");
        }

        // Cleanup
        request.Dispose();
        m_IsProcessing = false;
    }

    // Public methods for runtime control
    public void SetMirrorX(bool mirror)
    {
        mirrorX = mirror;
    }

    public void SetUpdateInterval(int interval)
    {
        updateInterval = Mathf.Clamp(interval, 1, 10);
    }

    public void SetOutputResolution(int width, int height)
    {
        outputResolution = new Vector2Int(width, height);
    }

    public Texture2D GetCurrentTexture()
    {
        return m_CameraTexture;
    }
}
