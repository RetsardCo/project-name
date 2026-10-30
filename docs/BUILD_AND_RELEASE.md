# Peerlight — Build and Release Engineering Guide

**Version:** 1.0.0  
**Target Runtimes:** Windows 11 (`win-x64`), Android (API 28-34)  

---

## 1. Prerequisites & Toolchain

| Component | Minimum Version | Verified Local Installation |
| :--- | :--- | :--- |
| **.NET SDK** | 8.0.400+ | Microsoft .NET 8.0.400 SDK (`C:\Program Files\dotnet\sdk`) |
| **Windows Desktop Runtime** | .NET 8.0 WindowsDesktop | Microsoft.WindowsDesktop.App 8.0.8 |
| **Java Development Kit** | OpenJDK 17 LTS | Microsoft 17.0.20.1+1-LTS |
| **Android Build Tools** | AGP 8.5.0, Gradle 8.5 | Compile SDK 34, Min SDK 28, Target SDK 34 |

---

## 2. Windows Desktop Application Build

### 2.1 Standard Build & Unit Tests
```powershell
Set-Location "C:\Users\Kurados\.gemini\antigravity\scratch\project-name"
dotnet restore ProjectName.sln
dotnet test ProjectName.sln --configuration Release
```

### 2.2 Framework-Dependent Release Output
```powershell
dotnet publish src/windows/ProjectName.Windows/ProjectName.Windows.csproj `
    --configuration Release `
    --output publish/windows-app/
```

### 2.3 Standalone Self-Contained Single-File Executable
Generates an executable embedding the .NET 8 runtime, Windows Forms GUI, and embedded Peerlight icon:
```powershell
dotnet publish src/windows/ProjectName.Windows/ProjectName.Windows.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output publish/standalone/windows-desktop/
```
Output Binary: `publish/standalone/windows-desktop/ProjectName.Windows.exe` (~75 MB)

---

## 3. Google TV Remote Console Build

### 3.1 Standalone Single-File Executable
```powershell
dotnet publish src/tv/ProjectName.Tv/ProjectName.Tv.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    --output publish/standalone/tv-remote/
```
Output Binary: `publish/standalone/tv-remote/ProjectName.Tv.exe` (~65 MB)

---

## 4. Android Mobile Application Build

### 4.1 Unit Tests & Debug APK
```powershell
Set-Location "C:\Users\Kurados\.gemini\antigravity\scratch\project-name\src\android"
.\gradlew.bat testDebugUnitTest assembleDebug
```
Output Binary: `src/android/app/build/outputs/apk/debug/app-debug.apk` (~4.5 MB)

### 4.2 Packaging Verification
- Application ID: `com.projectname.android` (Preserved for signing and upgrade continuity)
- Application Label: `Peerlight` (defined in `strings.xml`)
- Launcher Icon: `ic_launcher` (Generated from official Peerlight glowing node asset across mdpi to xxxhdpi)
- Minimum SDK: 28 (Android 9.0 Pie)
- Target SDK: 34 (Android 14)

---

## 5. Continuous Integration (CI) Workflow

The GitHub Actions workflow at `.github/workflows/ci.yml` validates:
1. `.NET 8.0` SDK setup.
2. NuGet dependency restoration.
3. Solution compilation in `Release` configuration.
4. Comprehensive execution of all unit tests (Tv, Shared, Windows).
