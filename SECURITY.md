# Security Policy — BingoCaller

## What this is

BingoCaller is a small, **offline** Windows desktop application (.NET 8 WinForms) for calling bingo numbers and
showing them on a big screen. It makes **no network connections**, runs **no other programs**, uses **no
registry, database or cryptography**, and has **no runtime dependencies** beyond the .NET 8 Desktop Runtime and
Windows itself. Its only inputs are the clicks of the operator, image files the operator chooses, and one local
settings file. See [SECURITY_REVIEW_2026-09-26.md](SECURITY_REVIEW_2026-09-26.md) for the source-level review.

## Supported versions

| Version | Supported |
|---------|-----------|
| 1.0.x   | Yes — security fixes for the latest 1.0.x release |

Support follows the underlying runtime: the app targets **.NET 8**, whose support ends in **November 2026**
(see the gap checklist in [CRA/04_GapClosure_Checklist.md](CRA/04_GapClosure_Checklist.md)). A move to a newer
supported .NET is planned; the supported-versions table will be updated with dated end-of-support information.

## Reporting a vulnerability

Please report security issues **privately** — do not open a public issue.

1. Preferably use **GitHub → Security → "Report a vulnerability"** on this repository (private vulnerability reporting), or
2. e-mail **Jochen Thieren — jochen.thieren@gmail.com** with the subject `BingoCaller security`.

Please include the version (shown in the window title), what you did, and what happened. Sample files that trigger
the problem are welcome (please send them privately).

**Response timeline (best effort — this is a personal project):** acknowledgement within 5 business days ·
initial assessment within 10 business days · fix or mitigation plan within 30 business days. Reporters are credited
in the release notes unless they prefer otherwise. Fixed vulnerabilities are described publicly in
[CHANGELOG.md](CHANGELOG.md) once a fix is available.

## Security design

- **No network, no telemetry.** The program never opens a connection or collects data.
- **No code execution from data.** Nothing the program reads is ever executed; it never starts other processes.
- **Untrusted input is limited to:** image files you choose (decoded by the Windows imaging component, GDI+) and
  `BingoCaller.settings.json` next to the program. The settings file is parsed as plain typed JSON and every numeric
  value is range-checked; an unreadable or corrupt file falls back to defaults.
- **Per-user installation.** The installer needs no administrator rights and installs into a folder the user owns,
  so the settings file can be written without weakening permissions on a protected folder such as `Program Files`.
- **Secure by default / reset.** Defaults are the safe, quiet configuration (nothing loaded, nothing shown from disk).
  *Options → Reset all to defaults…* restores them; uninstalling removes the settings file.

## Dependencies / supply chain

No NuGet packages and no bundled third-party binaries. Only the .NET 8 Desktop Runtime (installed separately) and
Windows system libraries (`gdi32`, `msimg32`, GDI+). See [sbom.json](sbom.json).

## Known limitations

- **Not code-signed** (no certificate). Integrity is provided by a published **SHA256** checksum for every release
  installer and by building from source. Windows SmartScreen may warn on first run.
- An image file that is very large (tens of thousands of pixels per side) can use a lot of memory when it is loaded.
  Only choose images you trust.
- If the settings file is edited by someone else, it can point the slideshow/logo paths at other files, including
  network (`\\server\share`) paths, which makes Windows contact that server. Keep the program folder writable only by
  the person running the event.

## Regulatory note (EU Cyber Resilience Act)

BingoCaller is free, open-source software distributed without charge and outside any commercial activity, which is
outside the scope of the CRA. The documents in [CRA/](CRA/) explain that position and record voluntary good practice.
They are informational — not legal advice and not a compliance attestation.
