import os
import sys
import urllib.request
import zipfile
import subprocess
import shutil

def download_with_progress(url, dest_path):
    print(f"Downloading {url} to {dest_path}...")
    def reporthook(block_num, block_size, total_size):
        if total_size > 0:
            percent = (block_num * block_size * 100) / total_size
            if block_num % 1000 == 0 or percent >= 100.0:
                print(f"  {percent:.1f}% ({block_num * block_size // 1048576}MB / {total_size // 1048576}MB)")
    urllib.request.urlretrieve(url, dest_path, reporthook)
    print("Download complete.")

def main():
    tools_dir = os.path.expandvars(r"%LOCALAPPDATA%\Programs\two-ladles-tools")
    os.makedirs(tools_dir, exist_ok=True)

    sdk_dir = os.path.join(tools_dir, "android-sdk")
    jdk_dir = os.path.join(tools_dir, "jdk")
    os.makedirs(sdk_dir, exist_ok=True)

    # 1. Platform-tools (already downloaded or ready)
    platform_tools_dir = os.path.join(sdk_dir, "platform-tools")
    if not os.path.exists(os.path.join(platform_tools_dir, "adb.exe")):
        pt_zip = os.path.join(tools_dir, "platform-tools.zip")
        if not os.path.exists(pt_zip):
            download_with_progress("https://dl.google.com/android/repository/platform-tools-latest-windows.zip", pt_zip)
        print("Extracting platform-tools...")
        with zipfile.ZipFile(pt_zip, 'r') as zip_ref:
            zip_ref.extractall(sdk_dir)
        print("platform-tools ready.")

    # 2. Download OpenJDK 17
    keytool_path = ""
    for root, dirs, files in os.walk(jdk_dir):
        if "keytool.exe" in files:
            keytool_path = os.path.join(root, "keytool.exe")
            break

    if not keytool_path:
        jdk_zip = os.path.join(tools_dir, "microsoft-jdk-17.zip")
        if not os.path.exists(jdk_zip):
            download_with_progress("https://aka.ms/download-jdk/microsoft-jdk-17-windows-x64.zip", jdk_zip)
        print("Extracting OpenJDK 17...")
        with zipfile.ZipFile(jdk_zip, 'r') as zip_ref:
            zip_ref.extractall(jdk_dir)
        for root, dirs, files in os.walk(jdk_dir):
            if "keytool.exe" in files:
                keytool_path = os.path.join(root, "keytool.exe")
                break
        print("OpenJDK 17 ready.")

    jdk_home = os.path.dirname(os.path.dirname(keytool_path)).replace("\\", "/")
    print(f"Java Home: {jdk_home}")

    # Set JAVA_HOME in env for sdkmanager
    os.environ["JAVA_HOME"] = jdk_home
    os.environ["PATH"] = os.path.join(jdk_home, "bin") + os.pathsep + os.environ.get("PATH", "")

    # 3. Download Android Command Line Tools & Install build-tools;34.0.0
    build_tools_dir = os.path.join(sdk_dir, "build-tools")
    has_build_tools = False
    if os.path.exists(build_tools_dir):
        for sub in os.listdir(build_tools_dir):
            if os.path.exists(os.path.join(build_tools_dir, sub, "apksigner.bat")):
                has_build_tools = True
                print(f"Found existing build-tools: {sub}")
                break

    if not has_build_tools:
        clt_zip = os.path.join(tools_dir, "commandlinetools.zip")
        if not os.path.exists(clt_zip):
            download_with_progress("https://dl.google.com/android/repository/commandlinetools-win-11076708_latest.zip", clt_zip)
        cmdline_dir = os.path.join(sdk_dir, "cmdline-tools", "latest")
        if not os.path.exists(cmdline_dir):
            print("Extracting commandlinetools...")
            temp_clt = os.path.join(tools_dir, "temp_clt")
            with zipfile.ZipFile(clt_zip, 'r') as zip_ref:
                zip_ref.extractall(temp_clt)
            extracted = os.path.join(temp_clt, "cmdline-tools")
            os.makedirs(os.path.dirname(cmdline_dir), exist_ok=True)
            shutil.move(extracted, cmdline_dir)
            shutil.rmtree(temp_clt, ignore_errors=True)

        sdkmanager_bat = os.path.join(cmdline_dir, "bin", "sdkmanager.bat")
        print(f"Running sdkmanager to install build-tools;34.0.0 (sdk_root={sdk_dir})...")
        # Accept licenses automatically
        p = subprocess.Popen(
            [sdkmanager_bat, f"--sdk_root={sdk_dir}", "build-tools;34.0.0"],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )
        stdout, stderr = p.communicate(input="y\ny\ny\ny\n")
        print(stdout[-400:] if stdout else "")
        if p.returncode != 0:
            print("sdkmanager error:", stderr)

    # 4. Generate Debug Keystore
    keystores_dir = os.path.expandvars(r"%APPDATA%\Godot\keystores")
    os.makedirs(keystores_dir, exist_ok=True)
    keystore_path = os.path.join(keystores_dir, "debug.keystore")
    if not os.path.exists(keystore_path):
        print(f"Generating debug keystore at {keystore_path}...")
        cmd = [
            keytool_path,
            "-genkeypair", "-v",
            "-keystore", keystore_path,
            "-storepass", "android",
            "-alias", "androiddebugkey",
            "-keypass", "android",
            "-keyalg", "RSA",
            "-keysize", "2048",
            "-validity", "10000",
            "-dname", "CN=Android Debug,O=Android,C=US"
        ]
        res = subprocess.run(cmd, capture_output=True, text=True)
        if res.returncode != 0:
            print("Failed to generate keystore:", res.stderr)
        else:
            print("Debug keystore generated.")

    # 5. Update Godot editor settings
    settings_file = os.path.expandvars(r"%APPDATA%\Godot\editor_settings-4.7.tres")
    if os.path.exists(settings_file):
        with open(settings_file, "r", encoding="utf-8") as f:
            content = f.read()

        sdk_formatted = sdk_dir.replace("\\", "/")
        keystore_formatted = keystore_path.replace("\\", "/")

        def set_or_replace(text, key, val):
            import re
            pattern = re.compile(rf'^{re.escape(key)}\s*=.*$', re.MULTILINE)
            newline = f'{key} = "{val}"'
            if pattern.search(text):
                return pattern.sub(newline, text)
            else:
                return text + f"\n{newline}\n"

        content = set_or_replace(content, "export/android/android_sdk_path", sdk_formatted)
        content = set_or_replace(content, "export/android/java_sdk_path", jdk_home)
        content = set_or_replace(content, "export/android/debug_keystore", keystore_formatted)
        content = set_or_replace(content, "export/android/debug_keystore_user", "androiddebugkey")
        content = set_or_replace(content, "export/android/debug_keystore_pass", "android")

        with open(settings_file, "w", encoding="utf-8") as f:
            f.write(content)
        print("Godot editor settings updated successfully:")
        print(f"  Android SDK: {sdk_formatted}")
        print(f"  Java SDK: {jdk_home}")
        print(f"  Keystore: {keystore_formatted}")

    print("\nAndroid toolchain setup completed successfully!")

if __name__ == "__main__":
    main()
