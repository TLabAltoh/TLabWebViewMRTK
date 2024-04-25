# TLabWebViewMRTK
Sample project for manipulating TLabWebView from Mixed Reality Toolkit

# Document
[document is here](https://tlabgames.gitbook.io/tlabwebview)

## Note

<details><summary>please see here</summary>

### This Repository only for Oculus Quest 2
This is a project I created for Oculus Quest 2, using the Mixed Reality Toolkit, but it does not work with HoloLens. This is because the WebView used is an Oculus (or Android) dependent system.

### Module Management Policy Modified
The policy has been changed to manage libraries in the repository as submodules after commit ``` 4a7a833 ```. Please run ``` git submodule update --init ``` to adjust the commit of the submodule to the version recommended by the project.

### WebView Input System Updated
Previously the interaction with WebView was based on the pan example, but currently the interaction example is based on uGUI. (2024/4/15)

</details>

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

- Build Settings

| Property      | Value   |
| ------------- | ------- |
| Platform      | Android |

- Project Settings

| Property          | Value     |
| ----------------- | --------- |
| Color Space       | Linear    |
| Graphics          | OpenGLES3 |
| Minimum API Level | 29        |
| Target API Level  | 31        |


- Add the following symbols to Project Settings --> Player --> Other Settings (to be used at build time)

```
UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC
```
```
UNITYWEBVIEW_ANDROID_ENABLE_CAMERA
```
```
UNITYWEBVIEW_ANDROID_ENABLE_MICROPHONE
```

- Create Assets/Plugins/Android/AndroidManifest.xml and copy the following text

<details><summary>manifest example</summary>

```xml
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android" android:installLocation="auto">
  <application android:label="@string/app_name" android:icon="@mipmap/app_icon" android:allowBackup="false">
    <activity android:theme="@android:style/Theme.Black.NoTitleBar.Fullscreen" android:configChanges="locale|fontScale|keyboard|keyboardHidden|mcc|mnc|navigation|orientation|screenLayout|screenSize|smallestScreenSize|touchscreen|uiMode" android:launchMode="singleTask" android:name="com.unity3d.player.UnityPlayerActivity" android:excludeFromRecents="true">
      <intent-filter>
        <action android:name="android.intent.action.MAIN" />
        <category android:name="android.intent.category.LAUNCHER" />
      </intent-filter>
    </activity>
    <meta-data android:name="unityplayer.SkipPermissionsDialog" android:value="false" />
    <meta-data android:name="com.samsung.android.vr.application.mode" android:value="vr_only" />
  </application>
	
    <!-- For Unity-WebView -->
    <application android:allowBackup="true"/>
    <application android:supportsRtl="true"/>
    <application android:hardwareAccelerated="true"/>
    <application android:usesCleartextTraffic="true"/>

    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE"/>
    <uses-permission android:name="android.permission.CAMERA" />
    <uses-permission android:name="android.permission.MICROPHONE" />
    <uses-permission android:name="android.permission.MODIFY_AUDIO_SETTINGS" />
    <uses-permission android:name="android.permission.RECORD_AUDIO" />

    <uses-feature android:name="android.hardware.camera" />
    <uses-feature android:name="android.hardware.microphone" />
    <!-- For Unity-WebView -->
	
  <uses-feature android:name="android.hardware.vr.headtracking" android:version="1" android:required="true" />
</manifest>
```

</details>

### Sample Scene
- Sample scenes are located in the following directories
```
Assets\TLab\TLabWebViewMRTK\Scene\TLabWebView MRTK Sample.unity
```
- Build and run the sample scene to Oculus Quest 2

## Link
- [Source code of the java plugin used](https://github.com/TLabAltoh/TLabWebViewPlugin.git)
