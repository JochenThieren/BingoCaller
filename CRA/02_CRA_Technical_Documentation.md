# CRA Technical Documentation (voluntary) — BingoCaller

Structure follows CRA **Annex VII** (technical documentation) and **Annex II** (information and instructions to the
user). Because the product is out of scope as distributed (see [01](01_CRA_Scope_Statement.md)), this file is
**voluntary good practice**, not a conformity claim. English only.

Legend: ✅ present · ◑ partial · ☐ open · n/a not applicable (with reason)

## Product identity

- **Name / type:** BingoCaller — desktop application for calling bingo numbers with a big-screen display
- **Version:** 1.0.1 (single source: `<Version>` in `BingoCaller/BingoCaller.csproj`; synced by hand with `MyAppVersion` in
  `BingoCaller_INNO.iss` and `CHANGELOG.md`; shown in the main window title)
- **Platform:** Windows 10/11 x64, .NET 8 Desktop Runtime (framework-dependent, single-file publish)
- **Manufacturer / author:** Jochen Thieren — jochen.thieren@gmail.com
- **Intended purpose:** run a bingo game: the operator clicks called numbers; audiences see them on a projector/TV.
  Offline use on a PC the operator controls.

## Part A — Technical documentation (Annex VII)

### 1. General description
- ✅ Intended purpose, versions, user information — [README.md](../README.md)
- n/a Hardware — software only

### 2. Design, development
- ✅ Architecture (see [docs/DEVELOPMENT_NOTES.md](../docs/DEVELOPMENT_NOTES.md)):
  `Program` → `MainForm` (caller window, game state) → one or more `DisplayForm` (big screens) → `BackdropPanel`
  (slideshow, banner ticker, logos; GDI drawing) + `InkText` (outline-centred text). `OptionsForm` and
  `ScreenPickerForm` are modal/modeless dialogs; `DisplaySettings` is the single settings object persisted to
  `BingoCaller.settings.json`.
- ✅ No third-party code, no plug-in or scripting interface, no network code.
- ✅ Secure development practices: source review ([SECURITY_REVIEW_2026-09-26.md](../SECURITY_REVIEW_2026-09-26.md)),
  randomised stress testing during development, version control.

### 3. Vulnerability handling
- ✅ Coordinated vulnerability disclosure policy and contact — [SECURITY.md](../SECURITY.md)
- ✅ SBOM — [sbom.json](../sbom.json) (CycloneDX 1.6; no third-party packages)
- ✅ Timelines: acknowledge 5 · assess 10 · fix/mitigate 30 business days (best effort)
- ◑ Secure update distribution — installers carry a published SHA256; **not code-signed** ([04](04_GapClosure_Checklist.md))
- ☐ Automated dependency/CVE monitoring — nothing to monitor except the .NET runtime (Windows Update / .NET servicing)

### 4. Cybersecurity risk assessment
- ✅ [03_Threat_Model.md](03_Threat_Model.md). Summary: offline, no network, no elevated privileges, no secrets;
  the only untrusted inputs are image files chosen by the operator and one local settings file. Highest residual
  risks are **Low** (see the review).

### 5. Support period
- ◑ 1.0.x receives security fixes (best effort). The runtime target **.NET 8 leaves Microsoft support in November 2026**;
  a move to a supported LTS is the first item on the gap list. A dated support-end will be published in SECURITY.md
  with each release.

### 6. Specifications / standards applied
- No harmonised standard is applied. Practices used: least privilege (per-user install), attack-surface minimisation,
  input range-checking, secure defaults, minimal dependencies.

### 7. Essential requirements — Annex I, Part I (2)

| Req. | Topic | Status | How |
|------|-------|--------|-----|
| (a) | No known exploitable vulnerabilities at release | ✅ | Source review found none above Low; no third-party packages (`dotnet list package --vulnerable`: none) |
| (b) | Secure-by-default configuration; ability to reset | ✅ | Defaults load nothing from disk; **Options → Reset all to defaults** |
| (c) | Security updates, automatic where applicable | ◑ | No auto-update mechanism (deliberate: no network). Updates are a re-install; changelog announces fixes |
| (d) | Protection against unauthorised access | n/a | Single-user desktop tool; no accounts, no data to protect from other users |
| (e) | Confidentiality of data | n/a | Processes no personal or secret data; nothing stored except display settings |
| (f) | Integrity of data | ✅ | Settings are type-checked and range-clamped on load; corrupt file → defaults |
| (g) | Data minimisation | ✅ | Stores only display settings (fonts, sizes, file paths, chosen monitors); no telemetry |
| (h) | Availability / resilience | ✅ | Unreadable images are skipped; the game continues without slideshow/logos |
| (i) | Minimise impact on other services | ✅ | No network use; modest CPU (GDI blits, ~22 fps ticker only when enabled) |
| (j) | Limited attack surface | ✅ | No sockets, IPC, plug-ins, scripting, auto-update or child processes |
| (k) | Exploitation mitigation | ◑ | Managed code (no unsafe code); GDI+ image decoding is a Windows component |
| (l) | Security logging/monitoring | n/a | No security-relevant events to record (no authentication, no network) |
| (m) | Secure removal of data/settings | ✅ | Uninstaller deletes `BingoCaller.settings.json`; no data elsewhere |

### 7b. Annex I, Part II — vulnerability handling

| Req. | Topic | Status |
|------|-------|--------|
| (1) | Identify/document vulnerabilities and components (SBOM) | ✅ sbom.json, SECURITY_REVIEW |
| (2) | Address vulnerabilities without delay; provide security updates | ✅ policy in SECURITY.md |
| (3) | Regular, effective tests and reviews | ◑ review done for 1.0.0, with an addendum for the 1.0.1 features; repeat each release (checklist in 04) |
| (4) | Publicly disclose fixed vulnerabilities | ✅ via CHANGELOG / GitHub advisories |
| (5) | Coordinated vulnerability disclosure policy | ✅ SECURITY.md |
| (6) | Facilitate sharing of vulnerability information (contact) | ✅ GitHub private reporting + e-mail |
| (7) | Mechanisms to distribute updates securely | ◑ GitHub Releases + SHA256; unsigned |
| (8) | Patches disseminated without delay, free of charge | ✅ free by nature |

### 8. Test / check reports
- ✅ Source review 2026-09-26; ✅ dependency check (no packages); ◑ automated static analysis (CodeQL) — see checklist.
- ✅ Functional/robustness testing during development: randomised 60–80-round stress runs of the display with changes to
  fonts, sizes, monitors, logos, banner, opacity and pause, checking for exceptions and disposed-resource faults.

### 9. Useful-life / support-period methodology — ◑ see §5.
### 10. EU Declaration of Conformity — n/a while out of scope (would be self-declared, Module A, if that ever changes).

## Part B — Information and instructions for the user (Annex II)

| # | Item | Status |
|---|------|--------|
| 1 | Manufacturer name and contact | ✅ Jochen Thieren, jochen.thieren@gmail.com |
| 2 | Point of contact for vulnerability reports | ✅ SECURITY.md |
| 3 | Product identification | ✅ name + version (window title, installer, changelog) |
| 4 | Intended use, including security environment | ✅ README (offline, operator-controlled PC); keep the program folder writable only by the operator |
| 5 | Foreseeable misuse / residual risk | ✅ SECURITY.md "Known limitations" (untrusted images, network paths, shared PCs) |
| 6 | Where the EU declaration can be found | n/a |
| 7 | Type of security support and end date | ◑ see §5 |
| 8 | Instructions: first use, updates, decommissioning, removal | ✅ README (install, use, reset, uninstall) |
| 9 | Where to obtain the SBOM | ✅ `sbom.json` in the repository |
