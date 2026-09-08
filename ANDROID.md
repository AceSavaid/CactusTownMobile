# Android / Google Play build

Cactus Town is Godot 4.4.1 **mono/.NET (C#)**. C# Android export is marked
*experimental* by Godot 4.4 but works.

## One-time toolchain (this machine)

Already in place:

| Tool | Location |
|---|---|
| JDK 17 | `C:\Program Files\Eclipse Adoptium\jdk-17.0.19.10-hotspot` |
| Android SDK | `C:\Users\alann\AppData\Local\Android\Sdk` — cmdline-tools, platform-tools, build-tools 35.0.0/36.1.0, platforms;android-34/35/36, **NDK 23.2.8568313** |
| Godot .NET export templates | `%APPDATA%\Godot\export_templates\4.4.1.stable.mono\` |
| Android build template | `android/build/` (gradle project — gitignored, regenerate via **Project → Install Android Build Template**) |
| Godot editor settings | `export/android/android_sdk_path`, `java_sdk_path` set |
| `global.json` | pins .NET SDK 8.0.x (Godot 4.4.1 targets net8.0) |

Still required — run once, in an **Administrator** PowerShell (MSI packs need elevation):

```powershell
dotnet workload install android
```

## Release signing (do once, keep secret)

Create an **upload keystore** — pick a strong password, answer the name prompts:

```
& "C:\Program Files\Eclipse Adoptium\jdk-17.0.19.10-hotspot\bin\keytool.exe" -genkeypair -v -keystore "C:\Users\alann\keys\cactustown-upload.jks" -alias cactustown -keyalg RSA -keysize 2048 -validity 10000
```

Then fill these in `export_presets.cfg` under `[preset.0.options]` (the file is
gitignored — the password stays local). The keys already exist there empty:

```
keystore/release="C:/Users/alann/keys/cactustown-upload.jks"
keystore/release_user="cactustown"
keystore/release_password="<your password>"
```

Back the `.jks` up somewhere safe. If you lose it you can't ship updates under
Play App Signing's upload key (recoverable, but a hassle).

## Target API 36 patch (Google Play requirement)

Play requires new/updated apps to target **API level 36**. Godot 4.4.1's Android
build template ships pinned to compile/target SDK 34 (AGP 8.2.0). The template
lives in `android/build/` — **gitignored and regenerated** by *Project → Install
Android Build Template*, so this patch must be re-applied after any regenerate.

1. `android/build/config.gradle` — in `ext.versions`:
   ```
   compileSdk : 36      // was 34
   targetSdk  : 36      // was 34
   buildTools : '35.0.0' // was '34.0.0'
   ```
   (AGP stays `8.2.0`, Gradle wrapper stays `8.2` — no bump needed.)
2. `android/build/gradle.properties` — add:
   ```
   android.suppressUnsupportedCompileSdk=36
   ```
   (AGP 8.2 refuses an unknown compileSdk without this flag.)
3. `export_presets.cfg` (both presets) already set `gradle_build/target_sdk="36"`.

Verify after export — the merged manifest must show `targetSdkVersion="36"`:
```
grep -A2 uses-sdk android/build/build/intermediates/merged_manifest/monoRelease/AndroidManifest.xml
```

Bump `version/code` in `export_presets.cfg` for every Play upload (currently `2`).

## Build

```
godot --headless --path . --export-debug "Android APK (test)" build/CactusTownMobile.apk   # for device testing
godot --headless --path . --export-release "Android"           build/CactusTownMobile.aab   # for Play
```

`godot` = `C:\Users\alann\OneDrive\Desktop\Games\Sources\Godot_v4.4.1-stable_mono_win64\Godot_v4.4.1-stable_mono_win64.exe`

- **APK preset** — arm64 + x86_64, format APK, for `adb install` / emulator.
- **Android** preset — arm64 only, **AAB**, signed, for Play. `version/code` must
  increase on every upload; `version/name` is the human string.

## Test on a device

USB debugging on, phone plugged in:

```
adb install -r build/CactusTownMobile.apk
```

## Google Play

1. Play Console account — one-time $25 (play.google.com/console).
2. Create app → "Cactus Town", app/game, free.
3. **Internal testing** track → create release → upload the `.aab`.
4. Play App Signing: accept the generated app signing key (Play re-signs; your
   `.jks` is only the upload key).
5. Complete the required forms before the release can go live: content rating
   questionnaire, Data safety (the game collects nothing — all local save data),
   target audience & content, ads declaration (none), privacy policy URL
   (only strictly required if targeting children or declaring certain data — a
   simple hosted page is fine).
6. Add internal testers by email, roll out, install via the opt-in link.

`package/unique_name` (`acesavaid.cactustown`) is **permanent** once the app
is created on Play — change it in `export_presets.cfg` first if you want
something else.
