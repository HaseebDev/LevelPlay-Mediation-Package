#if LEVELPLAY_INSTALLED
using System;
using UnityEngine;
using Unity.Services.LevelPlay;
using LevelPlaySdk = Unity.Services.LevelPlay.LevelPlay;

namespace Autech.LevelPlay
{
    /// <summary>
    /// Bridges consent into the LevelPlay mediation layer — the LevelPlay
    /// counterpart to the AdMob package's <c>MediationConsentManager</c>.
    ///
    /// GDPR is handled through the IAB TC string the InMobi CMP writes: LevelPlay
    /// and its network adapters read the <c>IABTCF_*</c> values automatically, so
    /// we deliberately do NOT call <c>SetGDPRConsents</c> (that manual API is for
    /// non-CMP flows and would fight the CMP's TCF decision). We DO apply the
    /// explicit CCPA and COPPA flags, which live outside IAB TCF.
    ///
    /// Call AFTER the CMP flow and BEFORE <c>LevelPlay.Init</c>.
    /// </summary>
    public class MediationConsentManager
    {
        private readonly AdConfiguration config;
        private readonly ConsentManager consent;
        private readonly LocalConsentManager localConsent;

        public MediationConsentManager(AdConfiguration config, ConsentManager consent, LocalConsentManager localConsent)
        {
            this.config = config;
            this.consent = consent;
            this.localConsent = localConsent;
        }

        public void Apply()
        {
            try
            {
                // US Privacy / CCPA "do not sell or share" opt-out (config toggle; the
                // CMP also offers a US-privacy form via ConsentManager.ShowCcpaForm()).
                LevelPlayPrivacySettings.SetCCPA(config.CcpaOptOut);

                // COPPA: child-directed treatment (separate from GDPR and CCPA).
                LevelPlayPrivacySettings.SetCOPPA(config.TagForChildDirectedTreatment);

                if (config.UseLocalConsent)
                {
                    ApplyLocalGdprConsent();
                    return;
                }

                Debug.Log("[Autech.LevelPlay] Mediation consent applied: " +
                          $"gdprApplies={consent.GdprApplies()} consentType={consent.GetConsentType()} " +
                          $"(GDPR via IAB TCF) ccpaOptOut={config.CcpaOptOut} coppa={config.TagForChildDirectedTreatment}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Autech.LevelPlay] Failed to apply mediation consent: {e.Message}");
            }
        }

        /// <summary>
        /// Local-consent path only: hand the user's personalized-ads decision to
        /// LevelPlay, which forwards it to every mediated adapter (the adapters
        /// document their GDPR support in terms of this single global consent
        /// flag), so no per-network SDK calls are needed.
        ///
        /// DO NOT switch this to <c>SetGDPRConsents(Dictionary)</c>. That
        /// per-network map was the 9.4.0 API and 9.5.0 deprecated it again in
        /// favour of the boolean below, so it has a shorter remaining life than
        /// the call it was meant to replace. It also replaces every stored entry
        /// on each call, silently revoking consent for any network missing from
        /// the map, which a package shipped to unknown consumers cannot enumerate.
        ///
        /// From 9.5.0 the correct call is <c>LevelPlayPrivacySettings.SetGDPRConsent(bool)</c>.
        /// On 9.4.x that does not exist yet, so the older global
        /// <c>LevelPlay.SetConsent(bool)</c> is used instead. Both reach the same
        /// native entry point, so the behaviour is identical either way.
        /// </summary>
        private void ApplyLocalGdprConsent()
        {
            // No answer collected means no signal, NOT a refusal. The consent flow
            // may be switched off, or the form may have failed to load. Pushing
            // false here would turn "do not ask" into "the user said no" for every
            // user, and would do it to non-GDPR regions too. The InMobi path leaves
            // the GDPR flag untouched in the same situation, so match it.
            if (!localConsent.HasStoredConsent)
            {
                Debug.Log("[Autech.LevelPlay] No local consent answer stored, leaving the GDPR flag untouched " +
                          $"(ccpaOptOut={config.CcpaOptOut} coppa={config.TagForChildDirectedTreatment} still applied).");
                return;
            }

            bool adsGranted = localConsent.AdsGranted;

#if LEVELPLAY_9_5_OR_NEWER
            LevelPlayPrivacySettings.SetGDPRConsent(adsGranted);
#else
#pragma warning disable CS0618
            LevelPlaySdk.SetConsent(adsGranted);
#pragma warning restore CS0618
#endif

            Debug.Log("[Autech.LevelPlay] Mediation consent applied: " +
                      $"consentType={localConsent.GetConsentType()} (GDPR via local consent, forwarded to all adapters) " +
                      $"ccpaOptOut={config.CcpaOptOut} coppa={config.TagForChildDirectedTreatment}");
        }
    }
}
#endif // LEVELPLAY_INSTALLED
