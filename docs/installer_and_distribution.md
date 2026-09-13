# 📦 Lexora Installer & Distribution Documentation

---

## 1. Packaging Strategy & System Requirements

Lexora uses **Inno Setup 6** to produce an enterprise-ready, single-file Windows installer (`Setup.exe`).

### 1.1. Why Unpackaged Self-Contained?
- Lexora is built with WinUI 3 (Windows App SDK) configured with `<WindowsPackageType>None</WindowsPackageType>`.
- By compiling as **Self-contained (`win-x64`)** with `-p:WindowsAppSDKSelfContained=true`, the output binary bundles:
  - .NET 10.0 CoreCLR runtime & base class libraries.
  - Windows App SDK / WinUI 3 native components (MRT Core, DWriteCore, WinUI XAML engines).
  - SQLite native dynamic link libraries (`e_sqlite3.dll`).
  - High-resolution brand assets and default seed templates.
- **End-user experience**: The user simply double-clicks `Lexora-Setup-v1.0.0.exe`, completes the wizard, and runs the app. **Zero external runtime downloads are required.**

---

## 2. Automated Release Pipeline (`build-installer.ps1`)

The repository includes a PowerShell automation script `build-installer.ps1` that orchestrates the entire release cycle from verification to packaging:

```powershell
# Standard usage:
powershell -ExecutionPolicy Bypass -File ./build-installer.ps1 -Version "1.0.0"

# Optional parameters:
powershell -ExecutionPolicy Bypass -File ./build-installer.ps1 `
    -Version "1.0.0" `
    -Configuration "Release" `
    -Runtime "win-x64" `
    -SkipTests `
    -SkipPublish
```

### 2.1. Execution Stages
1. **Compiler Detection**: Automatically locates `ISCC.exe` from `PATH`, `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`, or `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`.
2. **Quality Gate**: Executes `dotnet test Lexora.slnx --configuration Release` to ensure no broken builds are ever packaged (all 64 tests must pass).
3. **Self-Contained Publish**: Runs `dotnet publish src/Lexora.Presentation/Lexora.Presentation.csproj -c Release -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true -o publish/win-x64`.
4. **Inno Setup Compilation**: Invokes `ISCC.exe` with dynamic arguments, applying `lzma2/ultra64` solid compression.
5. **Output Verification**: Verifies the final binary in `publish/installer/Lexora-Setup-v1.0.0.exe` and reports file size (~73 MB).

---

## 3. Inno Setup Specification (`installer/LexoraSetup.iss`)

### 3.1. Key Directives
- **`AppId`**: `{{4E7A7D6C-8451-409B-89E2-7D6195E95E4A}` (Immutable GUID ensuring clean upgrades).
- **`ArchitecturesAllowed` & `ArchitecturesInstallIn64BitMode`**: Set to `x64compatible` to enforce 64-bit Program Files deployment.
- **`PrivilegesRequiredOverridesAllowed`**: `commandline dialog`, allowing standard non-admin users to install per-user to `{userpf}\Lexora` or administrators to install system-wide to `C:\Program Files\Lexora`.
- **`CloseApplications`**: `yes`, automatically detecting if `Lexora.Presentation.exe` is running and prompting the user to close it before overwriting files.
- **`WizardStyle`**: `modern`, matching Windows 11 aesthetics.

### 3.2. Data Retention Safety (Uninstall Routine)
When uninstalling, user vocabulary, spaced repetition logs, and streak history stored in `%LocalAppData%\Lexora\data\lexora.db` are **preserved by default**. The uninstaller explicitly asks the user:

> *"Do you want to delete your study data, vocabulary, and streak history stored in ...?"*
> - Default answer: **NO**.
> - Study data is only purged if the user explicitly selects **YES**.

---

## 4. Silent & Enterprise Deployment

The generated installer supports standard Inno Setup command-line switches:

| Command Switch | Function |
| :--- | :--- |
| `/SILENT` | Installs with wizard background progress display; suppresses prompt dialogs. |
| `/VERYSILENT` | Installs completely silently with zero UI. |
| `/DIR="C:\Path"` | Overrides default installation directory. |
| `/ALLUSERS` | Forces system-wide administrative installation. |
| `/CURRENTUSER` | Forces non-elevated per-user installation. |
| `/TASKS="desktopicon"` | Automatically creates a desktop shortcut. |
| `/NORESTART` | Prevents automatic reboot if any system file is locked. |

---

## 5. Clean Upgrade Procedures

When upgrading from version `X` to `Y`:
1. The installer detects the existing installation via `AppId`.
2. Existing program binaries in `{app}` are replaced with new self-contained binaries.
3. User data at `%LocalAppData%\Lexora\data\lexora.db` is untouched.
4. On first launch, `StartupOrchestrator` runs EF Core migrations to update the database schema if needed without data loss.
