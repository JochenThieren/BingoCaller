# Changelog — BingoCaller

All notable changes are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

The version lives in `BingoCaller/BingoCaller.csproj` (`<Version>`) and must match `MyAppVersion` in
`BingoCaller_INNO.iss`. `Build-Release.ps1` prints the SHA256 line to paste under each release.

---

## [1.0.0] — 2026-09-26

First public release.

SHA256 (setup_BingoCaller_1.0.0.exe): _added by `Build-Release.ps1` when the release is built_

### Added

- Caller window with 75 number buttons (B-I-N-G-O columns), **Undo** and **Reset**.
- Big-screen display: current number and letter, plus a called-numbers strip in which all 75 numbers always fit
  (no scrolling). `F11` full screen, `Esc` to leave it.
- **Multiple big screens**: choose any set of monitors, with an **Identify** button; the choice is remembered.
- **Pause / Resume**: big screens show only the slideshow, full screen and fitted; picking numbers and Undo are blocked while paused.
- Photo **slideshow** background with cross-fade, shuffle, crop/fit and opacity; see-through numbers strip.
- **Banner** (single image or slideshow images scrolling right to left) with speed, position, height and opacity;
  optionally shown big before the first number is called.
- **Six sponsor logos** (top / middle / bottom, left / right) with height and opacity; bottom logos above or below the strip.
- **Fonts and sizes** for number, letter and strip (0–200 %, where 200 % is the largest size that fits); letters can be hidden.
- **Options** window with live preview and **Reset all to defaults**.
- Settings saved in `BingoCaller.settings.json` next to the program (no registry, no AppData).
- Per-user **Inno Setup** installer (no administrator rights), with a .NET 8 Desktop Runtime check.
- Governance documents: MIT license, security policy, SBOM, source security review, CRA scope/technical documentation.
