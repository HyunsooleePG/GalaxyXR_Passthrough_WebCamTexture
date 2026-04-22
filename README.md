# GalaxyXR Passthrough WebCamTexture

[English](README.md) | [한국어](README.ko.md) | [日本語](README.ja.md)

A Unity project that demonstrates how to access and utilize Passthrough camera images on GalaxyXR devices.

## Project Overview

This project implements a method to capture Passthrough camera images from Samsung GalaxyXR devices and display them in real-time on Unity.

## Important: GalaxyXR Limitations

**GalaxyXR does not support accessing Passthrough images via ARFoundation's XRCpuImage method.**

- `CameraToQuad.cs` (XRCpuImage method): ❌ **Does not work on GalaxyXR**
- `WebCamToQuad.cs` (WebCamTexture method): ✅ **Works properly on GalaxyXR**

Therefore, you must use the **WebCamTexture** method on GalaxyXR.

## Tech Stack

- **Unity Version**: 6000.3.2f1
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Target Platform**: AndroidXR
- **XR Frameworks**:
  - Unity OpenXR Android XR

## Key Scripts

### 1. WebCamToQuad.cs

This method is available on GalaxyXR.

**Features**:
- Uses Unity's `WebCamTexture` API for camera access
- Automatic Android camera permission requests
- GalaxyXR-specific: Uses camera index 0 or 2 (front-facing camera)
- Lists all available cameras and outputs resolution information

**Configurable Options**:
- `requestedWidth/Height`: Requested resolution (default: 1280x720)
- `requestedFPS`: Requested frame rate (default: 30)
- `autoStart`: Auto-start option

### 2. CameraToQuad.cs ⚠️ (Reference Only)

This method uses ARFoundation's XRCpuImage and **does not work on GalaxyXR.**

While it may work on other OpenXR-compatible devices (e.g., Meta Quest), GalaxyXR restricts access to Passthrough images.

**Features (Reference Only)**:
- Uses ARFoundation's `XRCpuImage`
- Asynchronous image conversion processing (RGBA32)

## Usage

### 1. Scene Setup

1. Open the `Assets/Scenes/WebCamScene.unity` scene
2. Create a Quad object in the Hierarchy

### 2. Add WebCamToQuad Component

1. Add the `WebCamToQuad` script to the Quad
2. Configure the following in the Inspector:
   - `Target Quad`: Quad object to display the camera feed
   - `Requested Width/Height`: Desired resolution (e.g., 1280x720)
   - `Requested FPS`: Desired frame rate (e.g., 30)
   - `Auto Start`: Check for automatic start

### 3. Permission Manager

1. Add "android.permission.CAMERA" to the Android XR section of the Permission Manager in the scene

### 4. Custom AndroidManifest.xml

1. Add a custom AndroidManifest.xml
2. Add the camera permission: <uses-permission android:name="android.permission.CAMERA" />

### 5. Build and Run

1. Select AndroidXR platform in **Build Settings**
2. Build and install on GalaxyXR device
3. Grant camera permission when the app launches

### Permission Handling

In Android builds, camera permission is automatically requested at runtime. If the user denies permission, the camera will not start.

## Debugging

### Check Camera List

When the app runs, you can check the following logs in Logcat:

```
[WebCamToQuad] ========== Available Cameras (N) ==========
[WebCamToQuad] [0] Camera Name
[WebCamToQuad]     - Facing: Front, Kind: ColorAndDepth
[WebCamToQuad]     - Resolutions (X):
[WebCamToQuad]       1920x1080 @ 30Hz
...
[WebCamToQuad] ================================================
```

## Notes

- GalaxyXR restricts access to Passthrough images via ARFoundation's XRCpuImage API
- The WebCamTexture method is currently the only way to access Passthrough images on GalaxyXR
- The XRCpuImage method may work on other OpenXR devices
- **For production releases, it is necessary to confirm with the platform provider that this method is acceptable**
