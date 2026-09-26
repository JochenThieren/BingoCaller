# Development notes

Background for anyone changing the code. Everything lives in `BingoCaller/BingoCaller/`.

## Architecture

| File | Role |
|------|------|
| `Program.cs` | Entry point → `MainForm` |
| `Mainform.cs` | Caller window: 75 buttons, game state (`_called`), Undo/Reset/Pause/Options/Big Screen; owns the list of big-screen windows (`_displays`) |
| `DisplayForm.cs` | One big screen: layout (letter / number / optional sponsor row / numbers strip), chip sizing, fonts |
| `BackdropPanel.cs` | Everything drawn *behind* the numbers: slideshow (cross-fade), scrolling banner, banner, six logos, pause view |
| `InkText.cs` | Text control that centres by the glyph outline; also holds the slider→size mapping (`MapEm`) |
| `OptionsForm.cs` | Modeless options, applies every change live; **Reset all to defaults** rebuilds the tabs |
| `ScreenPickerForm.cs` | Multi-select monitor dialog with **Identify** |
| `DisplaySettings.cs` | The single settings object; JSON in `BingoCaller.settings.json` next to the exe; `SettingsVersion` + `Migrate()` for meaning changes |

The version is in `BingoCaller.csproj`; keep `MyAppVersion` in `BingoCaller_INNO.iss` and `CHANGELOG.md` in step.

## Gotchas learned the hard way

- **`Font.Equals` compares by value, and `Control.Font` ignores a "new" font equal to the current one.** Replacing
  and disposing fonts on controls therefore leaves them holding a disposed font (paint fails with *Parameter is not
  valid*). The chips and big labels use `InkText`, which stores family/size as plain values — no `Font` objects to dispose.
- **`Label.AutoSize` defaults to true.** A docked label that gets a tiny font collapses to its text size and never
  grows back. (`InkText` is not auto-sized.)
- **GDI+ `DrawImage` of a full-screen bitmap costs ~60 ms here; a GDI `BitBlt` from an HBITMAP costs ~2 ms.** Transparent
  child controls repaint the parent's background several times per frame, so the slideshow (including the cross-fade) is
  **composed once into a cached bitmap** (`ComposeFrame`) and painted with `BitBlt`/`AlphaBlend` (`BitBltRect`). Logos and
  the banner are cached pre-scaled (`GetScaled`). Do not go back to per-paint `DrawImage` of large images.
- **Painting through a translated `Graphics`** (transparent children): GDI works in device coordinates, so `BitBltRect`
  adds the graphics' translation offset.
- **Label/size events fire inside layout passes.** Re-fitting fonts there re-enters layout; `ScheduleFonts` defers it
  with `BeginInvoke`, and `LayoutHistory` uses a re-entrancy guard with a re-run flag.
- **Sizes:** slider 100 % = the default; 200 % = the largest size where the widest text ("88" / "BINGO") still fits
  (`InkText.FitEm`, `MapEm`). If a setting's *meaning* changes, bump `SettingsVersion` and convert in `Migrate()`.
- **Settings live next to the exe, on purpose** (portable, no AppData). That is why the installer is per-user — do not
  "fix" a failing settings write by granting *Users* modify rights on `Program Files`.

## Testing

There is no automated UI test suite yet. During development the display was exercised with a throw-away harness that
creates `DisplayForm`/`OptionsForm`, applies random settings (fonts, sizes, logos, banner, opacity, pause, resizes),
pumps the message loop and captures the real screen with `Graphics.CopyFromScreen`. Use `CopyFromScreen`, not
`Control.DrawToBitmap`: they take different paint paths and `DrawToBitmap` hides some bugs. Use only your own,
non-confidential test images.

## Build & release

```powershell
dotnet build BingoCaller.slnx -c Release
.\Build-Release.ps1        # publish (single file, framework-dependent) -> Inno Setup -> .\dist\ + .sha256
```
