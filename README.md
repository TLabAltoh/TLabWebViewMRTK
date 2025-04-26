# TLabWebViewMRTK
Sample project for using [```TLabWebView```](https://github.com/TLabAltoh/TLabWebView) (3D web browser / 3D WebView plugin) with [```Mixed Reality Toolkit```](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/?view=mrtkunity-2022-05)

[Document is here](https://tlabgames.gitbook.io/tlabwebview)  
[Snippets is here](https://gist.github.com/TLabAltoh/e0512b3367c25d3e1ec28ddbe95da497#file-tlabwebview-snippets-md)

> [!NOTE]
> This is a project created for Oculus Quest 2 using MRTK2, but it will not work with HoloLens. This is because the WebView used is an Oculus (or Android) dependent system.

> [!NOTE]
> This project is configured by default not to open http sites (a blank screen is displayed); if you want to open http sites, set clearartextTrafficPermitted to true in network_sec_config.xml.

> [!WARNING]
> When upgrading a project to Unity 2022.3.x, a problem was found where the build and web page display succeeded, but the controller and hand tracking were not enabled. Since the cause is not yet known, I recommend that this project be built with Unity 2021.3.x.

> [!WARNING]
> This project uses `HardwareBuffer` as the default `CaptureMode` for WebView. This `HardwareBuffer` option is confirmed to work with Oculus Quest, but may not work with some other devices (e.g., the WebView screen may go blank). In that case, change the `CaptureMode` from `HardwareBuffer` to `ByteBuffer`.
>
> <details>
> <img src="Media/image.0.png" width="256"></img><br>
> <img src="Media/image.1.png" width="256"></img>
> </details>

## ScreenShot
<img src="Media/tlab_webview_mrtk_feature.gif" width="512"></img>

## Operating Environment
- Oculus Quest 2
- Qualcomm Adreno650

## Requirements
- Unity 2021.3 LTS
- [Mixed Reality Toolkit](https://learn.microsoft.com/ja-jp/windows/mixed-reality/mrtk-unity/mrtk2/?view=mrtkunity-2022-05)
- [Oculus Integration](https://assetstore.unity.com/packages/tools/integration/oculus-integration-deprecated-82022?locale=ja-JP)
- [TLabVKeyborad](https://github.com/TLabAltoh/TLabVKeyborad.git)
- [TLabWebView](https://github.com/TLabAltoh/TLabWebView.git)

## Get Started

### Installing

Clone the repository with the following command

```
git clone https://github.com/TLabAltoh/TLabWebViewMRTK.git

cd TLabWebViewMRTK

git submodule update --init
```

### Set Up
Please see the setup section [```here```](https://github.com/TLabAltoh/TLabWebView?tab=readme-ov-file#set-up)

### Sample Scene
```
/Assets/TLab/TLabWebViewMRTK/Scene/TLabWebView MRTK Sample.unity
```
