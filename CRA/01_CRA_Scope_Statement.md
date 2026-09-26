# CRA Scope Statement — BingoCaller

**Product:** BingoCaller 1.0.0 — offline Windows desktop application (.NET 8 WinForms)
**Author / maintainer:** Jochen Thieren · **Date:** 2026-09-26
**Regulation:** (EU) 2024/2847, the *Cyber Resilience Act* (CRA)

> This is an informational analysis by the author, prepared with an AI assistant. It is **not legal advice** and not a
> compliance attestation. Dates and article references are given from the author's understanding of the regulation —
> verify them against the Official Journal text before relying on them.

## 1. Conclusion

As distributed today, BingoCaller is **free and open-source software supplied outside any commercial activity**, and the
author's position is that it is therefore **outside the scope of the CRA**. The author nevertheless follows the CRA's
good-practice expectations voluntarily (vulnerability policy, SBOM, changelog, timely fixes, stated support period), so
that the project is in good shape if the situation changes — and to be a good citizen of the software supply chain.

## 2. Facts this conclusion rests on

| Fact | Status |
|------|--------|
| Distributed free of charge (source on GitHub, installer as a release download) | ✅ |
| No price, licence fee, subscription, in-app purchase or paid edition | ✅ |
| No paid technical support, no paid customisation | ✅ |
| No advertising, no telemetry, no personal-data monetisation (the program has no network access at all) | ✅ |
| Not developed or sold on behalf of a company; not bundled into a commercial product by the author | ✅ |
| Donations, if ever accepted, only to cover costs and **without profit intent** | ✅ (none solicited today) |

## 3. What would change the conclusion

Re-assess (and update this file) if **any** of the following happens:

- BingoCaller, or a modified version, is **sold**, or offered together with a **paid service** (paid support, hosted
  version, paid features, sponsored placement, "pay to remove" anything).
- The author starts **monetising** it in another way (advertising, data collection, mandatory sponsorship).
- A **company** takes it on as its product, or ships it inside a commercial product/service. That company then becomes
  the *manufacturer* of *its* product and has to handle BingoCaller as a component (due diligence, SBOM, vulnerability
  reporting). The MIT licence explicitly allows this; the author's documents (SBOM, SECURITY.md, this folder) are
  meant to make that due diligence easy.
- The author's employer, or another organisation, distributes it as part of its own offering.

## 3a. Timeline reminders (verify)

- The CRA entered into force on **10 December 2024**.
- The **vulnerability/incident reporting obligations** for manufacturers start on **11 September 2026**.
- Most other obligations apply from **11 December 2027**.

If BingoCaller ever moves into scope, the reporting obligations would already apply from day one.

## 4. If it were in scope: classification

BingoCaller would fall in the **default category** — it is not an "important" or "critical" product with digital
elements (it is not an identity-management, browser, password-manager, VPN, network, operating-system,
security-tooling or similar product). The default category permits **self-assessment** (Module A) and the manufacturer's
own EU declaration of conformity.

## 5. Related documents

- [02_CRA_Technical_Documentation.md](02_CRA_Technical_Documentation.md) — Annex VII-style technical file (voluntary)
- [03_Threat_Model.md](03_Threat_Model.md) — risk assessment
- [04_GapClosure_Checklist.md](04_GapClosure_Checklist.md) — what is done and what remains
- [../SECURITY.md](../SECURITY.md), [../sbom.json](../sbom.json), [../SECURITY_REVIEW_2026-09-26.md](../SECURITY_REVIEW_2026-09-26.md)
