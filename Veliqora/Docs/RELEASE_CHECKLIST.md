# Release Checklist (Google Play)

This is a working checklist, **not a compliance statement**. Requirements change. Verify each item against the current
Google Play policies, the documentation of the SDKs you actually integrate, and local law with qualified advice.

## Identity & store
- [ ] Package name chosen and owned (placeholder: `com.animatix.veliqora`).
- [ ] App name / title checked for trademark conflicts in target markets (*Veliqora: Color Sort Puzzle* is a working title).
- [ ] Final icon (adaptive foreground/background), feature graphic, and screenshots made from real builds (not mock-ups or the concept board).
- [ ] Store description is accurate: no "hundreds of levels" unless true (currently 200), and no claims about events or features that don't exist.
- [ ] Content rating questionnaire (IARC) completed honestly.

## Privacy & data
- [ ] Privacy policy written, hosted, and linked in Play Console **and** in-app (`CommonOverlays.PrivacyPolicyUrl`).
- [ ] **Data safety form** completed. List what each integrated SDK collects or shares. With no ad or analytics SDK the game itself stores
      progress only locally on the device (`persistentDataPath`), and sends no network requests.
- [ ] Review the data practices of each SDK (ads, analytics, crash reporting) and whether they appear in Google Play's SDK Index.
- [ ] Data deletion: in-app "Reset progress" deletes local data. If you add accounts or cloud saves, provide the account-deletion path Play requires.

## Ads & consent (only if a real ad SDK is integrated)
- [ ] Implement the production adapter against the SDK's **current** documentation. Grant rewards only from the SDK's completed or rewarded callback (keep `RewardedAdGate`).
- [ ] Consent: collect consent where required **by geography** before ad requests (e.g. GDPR/UK GDPR → an IAB TCF-compatible CMP; US state privacy laws → opt-out
      signals as applicable). Wire the "Ad privacy choices" entry in Settings to the CMP's privacy options.
- [ ] Age & audience: decide the target age groups in Play Console. If children are in the audience, follow the **Families policy** (Families-certified ad
      SDKs only, child-directed treatment, restrictions on ad content/formats, `childDirected` in `ads.json`) or exclude children.
- [ ] Ads must be clearly distinguishable from game UI. No ads over gameplay, controls or system navigation (the banner is limited to Home/Map/Shop; no interstitials).
- [ ] Remove or replace every `REPLACE_WITH_*` placeholder. Confirm mock ads never appear in release (enforced by `AdsFactory`).
- [ ] Declare ads in Play Console ("Contains ads").

## Technical
- [ ] Target API level meets the current Play requirement. Min SDK reviewed.
- [ ] IL2CPP + ARM64 release, signed with the upload key, Play App Signing enabled.
- [ ] `bundleVersionCode` incremented.
- [ ] Unity EditMode **and** PlayMode suites pass. `Veliqora → Validate Project` is clean. Batch level validation reports 200/200.
- [ ] Profiled on at least one low-end and one midrange device (FPS, GC allocations during pours, startup time, memory).
- [ ] Tested on notch/cutout phones, gesture navigation, a tablet aspect, background/foreground, low storage (save failure path), offline.
- [ ] Accessibility pass: color-blind symbols, reduced motion, touch targets, text contrast.
- [ ] Placeholder art and audio replaced, or knowingly shipped as "procedural art" (it's original, but labelled placeholder in this repo).

## Monetization ethics (project rule)
- [ ] No pay-to-win pressure, no forced interstitials, no real-money purchases unless separately configured with Play Billing and disclosed.
