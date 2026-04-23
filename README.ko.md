# GalaxyXR Passthrough WebCamTexture

[English](README.md) | [한국어](README.ko.md) | [日本語](README.ja.md)

GalaxyXR 디바이스에서 Passthrough 카메라 이미지에 접근하여 Unity에서 활용하는 프로젝트입니다.

## 프로젝트 개요

이 프로젝트는 삼성 GalaxyXR 디바이스에서 패스스루(Passthrough) 카메라 이미지를 획득하여 Unity의 3D 오브젝트(Quad)에 실시간으로 표시하는 방법을 구현합니다.

https://github.com/user-attachments/assets/92cea140-9805-4eeb-b625-59e5281345c5

## 중요 사항: GalaxyXR의 제약사항

**GalaxyXR에서는 ARFoundation의 XRCpuImage 방식으로 Passthrough 이미지에 접근할 수 없습니다.**

- `CameraToQuad.cs` (XRCpuImage 방식): ❌ **GalaxyXR에서 작동하지 않음**
- `WebCamToQuad.cs` (WebCamTexture 방식): ✅ **GalaxyXR에서 정상 작동**

따라서 GalaxyXR에서는 반드시 **WebCamTexture** 방식을 사용해야 합니다.

## 기술 스택

- **Unity 버전**: 6000.3.2f1
- **렌더 파이프라인**: Universal Render Pipeline (URP)
- **타겟 플랫폼**: AndroidXR
- **XR 프레임워크**:
  - Unity OpenXR Android XR

## 주요 스크립트

### 1. WebCamToQuad.cs

GalaxyXR에서 사용 가능한 방식입니다.

**기능**:
- Unity의 `WebCamTexture` API를 사용하여 카메라 접근
- Android 카메라 권한 자동 요청
- GalaxyXR 전용: 카메라 인덱스 0 또는 2 사용 (전면 카메라)
- 사용 가능한 모든 카메라 나열 및 해상도 정보 출력

**설정 가능한 옵션**:
- `requestedWidth/Height`: 요청 해상도 (기본값: 1280x720)
- `requestedFPS`: 요청 프레임레이트 (기본값: 30)
- `autoStart`: 자동 시작 여부

### 2. CameraToQuad.cs ⚠️ (참고용)

ARFoundation의 XRCpuImage를 사용하는 방식으로, **GalaxyXR에서는 작동하지 않습니다.**

다른 OpenXR 호환 디바이스(Meta Quest 등)에서는 작동할 수 있으나, GalaxyXR에서는 Passthrough 이미지 접근이 제한되어 있습니다.

**기능 (참고용)**:
- ARFoundation의 `XRCpuImage` 사용
- 비동기 이미지 변환 처리 (RGBA32)

## 사용 방법

### 1. 씬 설정

1. `Assets/Scenes/WebCamScene.unity` 씬을 엽니다
2. Hierarchy에 Quad 오브젝트를 생성합니다

### 2. WebCamToQuad 컴포넌트 추가

1. Quad에 `WebCamToQuad` 스크립트를 추가합니다
2. Inspector에서 다음 항목을 설정합니다:
   - `Target Quad`: 카메라 피드를 표시할 Quad 오브젝트
   - `Requested Width/Height`: 원하는 해상도 (예: 1280x720)
   - `Requested FPS`: 원하는 프레임레이트 (예: 30)
   - `Auto Start`: 체크 (자동 시작)

### 3. Permission Manager

1. Scene에 있는 Permission Manager의 Android XR 부분에 android.permission.CAMERA를 추가

### 4. Custom AndroidManifest.xml

1. Custom AndroidManifest.xml을 추가
2. uses-permission으로 <uses-permission android:name="android.permission.CAMERA" />를 추가

### 5. 빌드 및 실행

1. **Build Settings**에서 AndroidXR 플랫폼 선택
2. GalaxyXR 디바이스에 빌드 및 설치
3. 앱 실행 시 카메라 권한 허용

### 권한 처리

Android 빌드에서는 런타임에 자동으로 카메라 권한을 요청합니다. 사용자가 권한을 거부하면 카메라가 시작되지 않습니다.

## 디버깅

### 카메라 목록 확인

앱 실행 시 Logcat에서 다음과 같은 로그를 확인할 수 있습니다:

```
[WebCamToQuad] ========== Available Cameras (N) ==========
[WebCamToQuad] [0] Camera Name
[WebCamToQuad]     - Facing: Front, Kind: ColorAndDepth
[WebCamToQuad]     - Resolutions (X):
[WebCamToQuad]       1920x1080 @ 30Hz
...
[WebCamToQuad] ================================================
```

## 참고사항

- GalaxyXR에서는 ARFoundation의 XRCpuImage API를 통한 Passthrough 이미지 접근이 제한되어 있습니다
- WebCamTexture 방식이 현재 GalaxyXR에서 Passthrough 이미지에 접근하는 유일한 방법입니다
- 다른 OpenXR 디바이스에서는 XRCpuImage 방식이 작동할 수 있습니다
- **실제 발매를 위한 프로젝트에는 해당 방법에 문제가 없나 플랫포머 측과 확인이 필요합니다**
