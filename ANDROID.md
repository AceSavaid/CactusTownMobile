# Android / Google Play build

Cactus Town is Godot 4.4.1 **mono/.NET (C#)**. C# Android export is marked
*experimental* by Godot 4.4 but works.

## One-time toolchain (this machine)

Already in place:

| Tool | Location |
|---|---|
| JDK 17 | `C:\Program Files\Eclipse Adoptium\jdk-17.0.19.10-hotspot` |
| Android SDK | `C:\Users\alann\AppData\Local\Android\Sdk` — cmdline-tools, platform-tools, build-tools 35.0.0, platforms;android-35, **NDK 23.2.8568313** |
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
gitignored — the password stays local):

```
keystore/release="C:/Users/alann/keys/cactustown-upload.jks"
keystore/release_user="cactustown"
keystore/release_password="<your password>"
```

Back the `.jks` up somewhere safe. If you lose it you can't ship updates under
Play App Signing's upload key (recoverable, but a hassle).

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

`package/unique_name` (`com.acesavaid.cactustown`) is **permanent** once the app
is created on Play — change it in `export_presets.cfg` first if you want
something else.
