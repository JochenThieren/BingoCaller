# Changelog — BingoCaller

All notable changes are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

The version lives in `BingoCaller/BingoCaller.csproj` (`<Version>`) and must match `MyAppVersion` in
`BingoCaller_INNO.iss`. `Build-Release.ps1` prints the SHA256 line to paste under each release.

---

## [1.0.1] — 2026-09-26

SHA256 (setup_BingoCaller_1.0.1.exe): _added after the release build_

### Added

- **Random draw** (optional backup, off by default; *Options → Random draw*): a button picks one of the not-yet-called numbers with
  the operating system's cryptographic random generator and reveals it with a ~3-second rolling animation on all big screens;
  the number is then called automatically. Randomly drawn numbers get a small dot in the numbers strip. Controls are locked while
  the animation runs. Optional **sound** (on by default, switchable, with a *Play sample* button): ticks that slow down with the
  roll, then a chime. It is synthesised in code, so no audio files are shipped.
- Application icon: on the exe (Explorer, taskbar), on every window and dialog, and on the installer wizard.
- Screenshots and dummy sponsor logos/pictures for the documentation (`docs/screenshots/`, `assets/`).

### Fixed

- Caller window: the status text ("Last call … (N of 75 called)") wrapped onto two lines; the default window is now wider.
- Options window: the "Numbers strip opacity" label and the note on the Banner tab were clipped.

## [1.0.0] — 2026-09-26

First public release.

SHA256 (setup_BingoCaller_1.0.0.exe): `D0554BAE2A85653D06C75B6CCD5AEDE5E4E8B46BADD59A91C9104F6A3D1DE2B1`

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
