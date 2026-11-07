# Android Build Guide — Two Ladles (`docs/ANDROID_BUILD.md`)

> [!IMPORTANT]
> **Active Engine Notice:** Godot Android export is officially **stopped and archived**. The official competition Android APK (`TwoLadles.apk`) is built exclusively in **Unity 2022.3.62f3** located at `unity/TwoLadles/`.

## 1. Official Unity Android Build Pipeline (`BuildScript.cs`)

### Prerequisites in Unity
- Unity `2022.3.62f3` with **Android Build Support** (including Android SDK, NDK, and OpenJDK modules installed via Unity Hub).
- Scripting Backend: `IL2CPP` or `Mono` (Target Architecture: `ARM64` / `ARMv7`).

### How to Build `TwoLadles.apk` in Unity
1. Open the project at `unity/TwoLadles` in Unity Editor.
2. In the top Unity menu, select **File -> Build Settings...** and switch platform to **Android**.
3. Click **Build** and choose output path `Builds/Android/TwoLadles.apk`.
4. Alternatively, use the automated build pipeline in [`Assets/Editor/BuildScript.cs`](../unity/TwoLadles/Assets/Editor/BuildScript.cs):
   - Invoke `BuildScript.BuildAndroid()` to generate a clean production APK.

---

## 2. Installing APK onto Android Test Devices
Connect your Android phone via USB with USB Debugging enabled, or transfer via Wi-Fi:
```powershell
adb install -r "Builds/Android/TwoLadles.apk"
```

---

## 3. Archived Godot Android Setup (Historical Reference)

To avoid downloading gigabytes of Android SDK/NDK toolchains locally during early development, `.github/workflows/android-build.yml` compiles the signed debug Android APK (`two-ladles-debug.apk`) in GitHub Actions.

### How to Trigger a Cloud Android APK Build
1. **Manual Dispatch:** Open your GitHub repository → **Actions** tab → **Two Ladles Android Debug APK Build** → click **Run workflow**.
2. **Version Tag:** Push any tag starting with `v` (for example `git tag v0.2.0 && git push origin v0.2.0`).
3. Download the resulting `two-ladles-android-debug-apk-<sha>` zip artifact from the completed workflow run and install `two-ladles-debug.apk` on your Android phone.

---

## 2. Optional Local Android SDK Setup (For Future Local APK Exports)

If you later want to export Android APKs directly from your Windows PC without using GitHub Actions:

1. **Install OpenJDK 17:**
   ```powershell
   winget install EclipseAdoptium.Temurin.17.JDK
   ```
2. **Install Android SDK Command-Line Tools** (via Android Studio or `sdkmanager`) into `%LOCALAPPDATA%\Android\Sdk` with:
   - `platform-tools` (`adb`)
   - `build-tools;34.0.0`
   - `platforms;android-34`
3. **Generate a Local Debug Keystore:**
   ```powershell
   keytool -genkeypair -v -keystore "$env:USERPROFILE\.android\debug.keystore" -storepass android -alias androiddebugkey -keypass android -keyalg RSA -keysize 2048 -validity 10000 -dname "CN=Android Debug,O=Android,C=US"
   ```
4. **Point Godot Editor Settings** (`Editor -> Editor Settings -> Export -> Android`) to your Android SDK path and debug keystore, then run:
   ```powershell
   & "$env:LOCALAPPDATA\Programs\two-ladles-tools\godot\Godot_v4.7.2-stable_win64_console.exe" --headless --export-debug "Android" "build/android/two-ladles-debug.apk"
   ```

> **Security Rule:** Never commit `.keystore` or `.jks` files to Git. `.gitignore` blocks them automatically.
