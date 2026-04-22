# GalaxyXR Passthrough WebCamTexture

[English](README.md) | [한국어](README.ko.md) | [日本語](README.ja.md)

GalaxyXRデバイスでPassthroughカメラ画像にアクセスし、Unityで活用するプロジェクトです。

## プロジェクト概要

このプロジェクトは、Samsung GalaxyXRデバイスからPassthroughカメラ画像を取得し、Unity上でリアルタイム表示する方法を実装してます。

## 重要事項：GalaxyXRの制約

**GalaxyXRではARFoundationのXRCpuImage方式でPassthrough画像にアクセスできません。**

- `CameraToQuad.cs` (XRCpuImage方式): ❌ **GalaxyXRでは動作しません**
- `WebCamToQuad.cs` (WebCamTexture方式): ✅ **GalaxyXRで正常動作**

したがって、GalaxyXRでは必ず**WebCamTexture**方式を使用する必要があります。

## 技術スタック

- **Unityバージョン**: 6000.3.2f1
- **レンダーパイプライン**: Universal Render Pipeline (URP)
- **ターゲットプラットフォーム**: AndroidXR
- **XRフレームワーク**:
  - Unity OpenXR Android XR

## 主要スクリプト

### 1. WebCamToQuad.cs

GalaxyXRで使用可能な方式です。

**機能**:
- Unityの`WebCamTexture` APIを使用したカメラアクセス
- Androidカメラ権限の自動リクエスト
- GalaxyXR専用：カメラインデックス0または2を使用（前面カメラ）
- 使用可能なすべてのカメラのリストと解像度情報の出力

**設定可能なオプション**:
- `requestedWidth/Height`: リクエスト解像度（デフォルト: 1280x720）
- `requestedFPS`: リクエストフレームレート（デフォルト: 30）
- `autoStart`: 自動開始オプション

### 2. CameraToQuad.cs ⚠️ (参考用)

ARFoundationのXRCpuImageを使用する方式で、**GalaxyXRでは動作しません。**

他のOpenXR互換デバイス（Meta Questなど）では動作する可能性がありますが、GalaxyXRではPassthrough画像へのアクセスが制限されています。

**機能（参考用）**:
- ARFoundationの`XRCpuImage`を使用
- 非同期画像変換処理（RGBA32）

## 使用方法

### 1. シーン設定

1. `Assets/Scenes/WebCamScene.unity`シーンを開きます
2. HierarchyにQuadオブジェクトを作成します

### 2. WebCamToQuadコンポーネントの追加

1. Quadに`WebCamToQuad`スクリプトを追加します
2. Inspectorで以下の項目を設定します：
   - `Target Quad`: カメラフィードを表示するQuadオブジェクト
   - `Requested Width/Height`: 希望する解像度（例: 1280x720）
   - `Requested FPS`: 希望するフレームレート（例: 30）
   - `Auto Start`: 自動開始のためにチェック

### 3. Permission Manager

1. シーン内のPermission ManagerのAndroid XRセクションにandroid.permission.CAMERAを追加

### 4. Custom AndroidManifest.xml

1. カスタムAndroidManifest.xmlを追加
2. カメラ権限を追加: <uses-permission android:name="android.permission.CAMERA" />

### 5. ビルドと実行

1. **Build Settings**でAndroidXRプラットフォームを選択
2. GalaxyXRデバイスにビルドしてインストール
3. アプリ起動時にカメラ権限を許可

### 権限処理

Androidビルドでは、実行時に自動的にカメラ権限がリクエストされます。ユーザーが権限を拒否すると、カメラは起動しません。

## デバッグ

### カメラリストの確認

アプリ実行時、Logcatで以下のようなログを確認できます：

```
[WebCamToQuad] ========== Available Cameras (N) ==========
[WebCamToQuad] [0] Camera Name
[WebCamToQuad]     - Facing: Front, Kind: ColorAndDepth
[WebCamToQuad]     - Resolutions (X):
[WebCamToQuad]       1920x1080 @ 30Hz
...
[WebCamToQuad] ================================================
```

## 注意事項

- GalaxyXRではARFoundationのXRCpuImage APIを通じたPassthrough画像へのアクセスが制限されています
- WebCamTexture方式が現在GalaxyXRでPassthrough画像にアクセスする唯一の方法です
- XRCpuImage方式は他のOpenXRデバイスでは動作する可能性があります
- **実際のリリース用プロジェクトでは、この方法に問題がないかプラットフォーム側と確認が必要です**
