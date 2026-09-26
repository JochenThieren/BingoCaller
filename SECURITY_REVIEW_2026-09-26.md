# BingoCaller — Code Security Review (2026-09-26)

**Reviewed by:** Claude Code, at the author's request, reading the actual source (not assuming compliance).
**Scope:** all 8 hand-written source files under `BingoCaller/BingoCaller/` (about 2,450 lines including designer stubs)
at version 1.0.0: `Program.cs`, `Mainform.cs`, `DisplayForm.cs`, `BackdropPanel.cs`, `InkText.cs`, `OptionsForm.cs`,
`ScreenPickerForm.cs`, `DisplaySettings.cs`. Audit and report only — no source was changed as a result of this review.

## What was checked, and the result

| Check | Result |
|-------|--------|
| Network access (`HttpClient`, `WebClient`, sockets) | **None.** No network API is referenced anywhere. |
| Process / shell launch (`Process.Start`, `ShellExecute`) | **None.** |
| Registry, environment variables | **None used.** |
| Cryptography, key material | **None used** — nothing to misconfigure; nothing is encrypted or signed. |
| Hardcoded secrets, keys, passwords, salts | **None found.** |
| SQL / database | **None.** |
| Deserialisation of untrusted data | Only `System.Text.Json` into one typed class (`DisplaySettings`); no polymorphic/`object`/BinaryFormatter use. |
| Logging of sensitive data | **No logging at all** (no log files, no `Console`/`Debug`/`Trace`). |
| Debug/dev features in release builds | **None** — there are no `#if DEBUG` paths. |
| Clipboard, `SendKeys`, dynamic assembly loading | **None.** |
| Third-party dependencies | **None** (`dotnet list package --include-transitive`: no packages; `--vulnerable`: no vulnerable packages). |
| Native interop | 6 `DllImport`s (`gdi32`, `msimg32`) for `BitBlt`/`AlphaBlend`/DC and bitmap handling. Fixed signatures, no user-controlled arguments; GDI handles are released (`DeleteDC` after use; `DeleteObject` in `ReleaseFrame`, `ReleaseTickerStrip`, `Dispose`). |

## Trust boundaries

1. **Image files chosen by the operator** (slideshow, banner, logos): opened read-only with `FileStream`
   (`FileShare.ReadWrite`), decoded by GDI+ (`Image.FromStream`), immediately re-drawn into an in-memory `Bitmap`
   (down-scaled to at most 2560 px / 1600 px / 1200 px on the longest side), and the file is closed. All in a
   `try/catch` that skips unreadable files.
2. **`BingoCaller.settings.json`** next to the executable: read once at start-up (`DisplaySettings.Load`), typed JSON,
   then `Normalize()` clamps every numeric value and replaces missing strings/lists. A corrupt file → defaults.
3. **Operator input** in the UI (clicks, slider values, file dialogs) — trusted (it is the person running the event).

## Findings

### [Low] Settings-file tampering can redirect image paths (including UNC paths)
- **File:** `DisplaySettings.cs` (`Load`, `Normalize`), `BackdropPanel.cs` (`LoadSafe`).
- Paths in the settings file are not validated. Someone able to write that file could point the slideshow/logo at a
  `\\server\share\image.png` path; opening it makes Windows attempt an SMB connection (an NTLM-authentication exposure
  vector, CWE-522/CWE-610 class). Requires **local write access** to the program folder, which is the operator's own
  per-user folder by default.
- **Status: accepted and documented** (SECURITY.md "Known limitations"). Possible hardening: warn on / refuse UNC paths
  found in the settings file (kept working when the operator picks a network path in a file dialog).

### [Low] Very large images can exhaust memory
- **File:** `BackdropPanel.cs`, `LoadSafe`.
- The image is decoded at full size before being down-scaled; an image of tens of thousands of pixels per side needs
  gigabytes. Only operator-chosen files are opened, and a failure is caught (`catch`), so the impact is at worst a
  slow or failed load for the file the operator chose (self-inflicted DoS, CWE-400).
- **Status: accepted.** Possible hardening: check `Width × Height` after opening and skip images above a pixel cap.

### [Low] Image data validation is switched off for speed
- **File:** `BackdropPanel.cs:747` — `Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false)`.
- Decoding of malformed images is left to GDI+ (a Windows component, patched via Windows Update); any exception
  thrown while drawing is caught by the surrounding `try/catch`. Parser vulnerabilities in GDI+ would have to be
  fixed by Microsoft; the app never fetches images from anywhere but the local file system / a path the operator chose.
- **Status: accepted.** Possible hardening: `validateImageData: true` (small cost on load).

### [Low] Not code-signed
- Releases are unsigned (no certificate). Mitigation: published SHA256 for every installer (`Build-Release.ps1`);
  building from source is documented. Tracked in the CRA gap checklist.

### [Info] Settings are written non-atomically
- `File.WriteAllText` directly to the settings file; a crash mid-write could leave a truncated file. `Load` treats
  any parse failure as "use defaults", so the effect is a lost configuration, not a fault.

### [Info] Default WinForms unhandled-exception dialog
- An unexpected exception shows the standard .NET dialog (with a stack trace) — local, contains no secrets.

## Structural notes (why the attack surface is small)

- **No listening sockets, no IPC, no plug-ins, no scripting, no auto-update.** Updates are a manual re-install.
- **Installer runs per-user** (`PrivilegesRequired=lowest`): no elevation, and no need to grant *Users* write access to a
  protected folder — avoiding the classic "writable Program Files folder → DLL planting" privilege-escalation pattern.
- The application never writes outside its own folder and never deletes anything except through its own uninstaller.

## Summary

No Critical, High or Medium findings. Four Low findings (accepted, with concrete hardening options above) and two
Informational notes. Recommended hardening, in order: pixel cap on image load → refuse UNC paths from the settings file →
`validateImageData: true` → code signing.
