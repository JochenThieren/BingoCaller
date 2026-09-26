# Threat Model & Risk Assessment — BingoCaller 1.0.0

Method: lightweight STRIDE-style walk through assets, trust boundaries and actors. Companion to the
[source review](../SECURITY_REVIEW_2026-09-26.md).

## 1. System context

```
 Operator ──clicks──▶ MainForm ──numbers──▶ DisplayForm(s) ──▶ projector / TV
                        │                        ▲
   image files ─────────┴──▶ BackdropPanel (GDI+ decode, in memory)
   BingoCaller.settings.json ◀──▶ DisplaySettings (read at start, written on change)
```

No network, no other processes, no registry, no secrets. Everything runs in one process as the logged-on user.

## 2. Assets

| Asset | Why it matters |
|-------|----------------|
| Correct, uninterrupted display during an event | The whole purpose; availability matters most |
| The operator's PC and account | Must not be harmed by opening an image or a settings file |
| `BingoCaller.settings.json` | Only persistent data; low value |
| Installer integrity | Users must get what the author built |

Not assets (nothing to protect): personal data, credentials, keys, financial data. Game fairness (whether the operator
calls honest numbers) is an event-organisation matter, not a security property of the software. The optional *Random draw*
picks a number with the operating system's cryptographic random generator, decides it before the reveal animation starts,
and marks such numbers in the strip; it is a convenience backup, not a certified gaming device (README, Disclaimer).

## 3. Actors

- **Operator** — trusted.
- **Supplier of an image** (sponsor, download, USB stick) — may hand over a malicious or huge image.
- **Local attacker with write access to the program folder** — can edit the settings file or replace files.
- **Distribution attacker** — tampers with the installer/download.
- **Remote attacker** — no reachable surface (no listening ports, no network client).

## 4. Threats and mitigations

| # | Threat (STRIDE) | Mitigation | Residual |
|---|-----------------|------------|----------|
| T1 | Malicious image exploits the image decoder (E) | Decoding is done by GDI+ (Windows Update patched); files are only those the operator picked; exceptions caught | Low — depends on Windows patching; only open trusted images |
| T2 | Oversized image exhausts memory (D) | Decoded once and down-scaled; failures caught | Low — self-inflicted; pixel cap is a planned hardening |
| T3 | Tampered settings file changes paths/values (T) | Typed JSON, numeric values clamped, corrupt → defaults | Low — a UNC path could trigger an SMB connection (documented); requires local write access |
| T4 | Settings tampering used for code execution (E) | Nothing in the file is executed or interpreted as code/commands | None identified |
| T5 | Tampered installer/download (T) | Published SHA256; open source; per-user install | Low–Medium — **unsigned**; user must compare the checksum |
| T6 | Privilege escalation via a writable program folder (E) | Per-user install into a user-owned folder; **no** users-modify ACL added to a protected folder; no elevation | None from the installer |
| T7 | Information disclosure (I) | Nothing sensitive stored or transmitted; no logging; no telemetry | None |
| T8 | Crash/denial during an event (D) | Robust to bad files (skipped); Options changes apply live and can be reset; stress-tested | Low — unhandled exception shows the default .NET dialog |
| T9 | Supply-chain compromise of a dependency (T) | No third-party packages; only the .NET runtime and Windows | Runtime updates come from Microsoft |
| T10 | Spoofing/impersonation of the tool (S) | Source and releases in the author's repository; documented checksum | Low — no signing |

## 5. Basic requirements that do not apply (justification)

- **Authentication / access control, confidentiality, security logging:** the application has no users, accounts,
  sensitive data or security-relevant events.
- **Network-related requirements (secure transport, resilience to DoS, etc.):** no network functionality exists.
- **Automatic security updates:** deliberately omitted — an updater would add the network surface the design avoids.

## 6. Overall risk

**Low.** No Critical/High/Medium issue identified. Most valuable next steps: retarget to a supported .NET, code-sign
releases, add an image pixel cap, warn about UNC paths ([04](04_GapClosure_Checklist.md)).
