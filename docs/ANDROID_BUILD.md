# Android Build Guide — Two Ladles (`docs/ANDROID_BUILD.md`)

> [!IMPORTANT]
> The competition APK (`TwoLadles.apk`) is built only with **Unity 2022.3.62f3** from `unity/TwoLadles/Two Ladles/`. The old Godot export and its GitHub Actions workflow are archived on the `godot` branch and are not used.

## 1. Prerequisites
- Unity `2022.3.62f3` with the **Android Build Support** module, including **Android SDK & NDK Tools** and **OpenJDK** (add them in Unity Hub → Installs → ⚙ → Add modules).
- An Android phone running **Android 7.0 (API 24) or newer** with a 64-bit (ARM64) processor.

## 2. Build `TwoLadles.apk`
1. Open `unity/TwoLadles/Two Ladles` in Unity Hub.
2. In the Unity menu bar choose **Two Ladles → Build Android Standalone APK**.
3. Wait for the Console message `ANDROID BUILD SUCCESSFUL! APK generated at: Builds/Android/TwoLadles.apk`.

Output: `unity/TwoLadles/Two Ladles/Builds/Android/TwoLadles.apk`.

### What the build script sets
[`BuildScript.BuildAndroid()`](../unity/TwoLadles/Two%20Ladles/Assets/Editor/BuildScript.cs) regenerates the main scene, then applies:

| Setting | Value |
| --- | --- |
| Product name / company | `Two Ladles` / `Two Ladles Team` |
| Orientation | Landscape Left |
| Minimum SDK | API 24 |
| Target SDK | API 34 |
| Scripting backend | IL2CPP |
| Architecture | ARM64 |

If the build fails, the script logs `ANDROID BUILD FAILED!` with the error count and exits with code `1` (useful for batch builds).

### Batch build (optional)
```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -quit -projectPath "unity/TwoLadles/Two Ladles" -executeMethod BuildScript.BuildAndroid -logFile android-build.log
```

## 3. Install on a phone
Connect the phone by USB with **USB debugging** turned on (Settings → Developer options), then run in PowerShell from `unity/TwoLadles/Two Ladles`:
```powershell
adb install -r "Builds/Android/TwoLadles.apk"
```
`-r` replaces an existing install and keeps saved progress. You can also copy the APK to the phone and open it (allow "install unknown apps" when asked).

## 4. On the phone
- On-screen touch controls (joystick plus INTERACT, CHOP and THROW buttons) appear automatically on mobile (`Application.isMobilePlatform`).
- Developer-only buttons such as **2-BOT TEST RUN** are hidden on mobile.
- To play co-op with a laptop, follow the [LAN crossplay test checklist](NETWORKING.md#lan-crossplay-test-checklist).

> [!CAUTION]
> Never commit `.keystore` or `.jks` signing files to Git. `.gitignore` blocks them.
