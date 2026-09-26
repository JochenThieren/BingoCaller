# Gap-Closure Checklist — BingoCaller

State: **2026-09-26**, version 1.0.0. Priority: **P1** = do soon · **P2** = worthwhile · **P3** = nice to have.

## A. Open items

| # | Pri | Gap | Action |
|---|-----|-----|--------|
| 1 | **P1** | **.NET 8 leaves Microsoft support in November 2026** (verify the exact date) | Retarget `BingoCaller.csproj` to the current LTS (`net10.0-windows`), rebuild, re-run the manual/stress checks, update README requirements and the installer's runtime check (`Microsoft.WindowsDesktop.App 10.`); then set a dated support end in SECURITY.md |
| 2 | P1 | Releases are unsigned | Apply for free open-source code signing (e.g. SignPath Foundation, if eligible) or buy a certificate; until then keep the published SHA256 and say so in the release notes |
| 3 | P1 | GitHub features not enabled yet | Turn on **private vulnerability reporting**, **Dependabot alerts**, and **CodeQL** code scanning in the repository settings (free for public repositories) |
| 4 | P2 | Image loading has no size guard | Skip images above a pixel cap after opening (e.g. 100 MP) — see review finding 2 |
| 5 | P2 | UNC paths accepted from the settings file | Ignore/warn on `\\server\share` paths read from the settings file — review finding 1 |
| 6 | P2 | `validateImageData: false` on image decode | Switch to `true` and measure the load-time cost — review finding 3 |
| 7 | P2 | No automated tests / CI | Add a GitHub Actions workflow (build on `windows-latest`) and a few unit tests for `DisplaySettings` (load/normalise/reset) |
| 8 | P3 | SBOM is hand-maintained | Generate with the CycloneDX .NET tool in `Build-Release.ps1` once any package is added |
| 9 | P3 | No app icon | Add an `.ico` (`ApplicationIcon` + installer `SetupIconFile`) |

## B. Done

- ✅ MIT `LICENSE` with the author's note; README; CHANGELOG
- ✅ `SECURITY.md` with reporting channels, timelines, supported versions, known limitations
- ✅ `sbom.json` (CycloneDX 1.6) and a source-level security review
- ✅ CRA scope statement, technical documentation, threat model (this folder)
- ✅ Version in one place (`.csproj`), shown in the window title, mirrored in the installer and changelog
- ✅ Per-user installer (no elevation, no writable-Program-Files pattern); SHA256 written by `Build-Release.ps1`
- ✅ Secure defaults and a one-click **Reset all to defaults**; uninstaller removes the settings file
- ✅ No third-party dependencies, no network access, no secrets

## C. Per-release routine

1. Bump `<Version>` in the `.csproj`, `MyAppVersion` in `BingoCaller_INNO.iss`, and add the `CHANGELOG.md` entry.
2. `dotnet list package --include-transitive --vulnerable` (expect: none); update `sbom.json` version/timestamp.
3. Skim the diff for new APIs on the review's list (network, process launch, registry, file writes, native interop).
4. `.\Build-Release.ps1`; paste the printed SHA256 line into `CHANGELOG.md`; attach installer + `.sha256` to the GitHub release.
5. Re-date this checklist.
