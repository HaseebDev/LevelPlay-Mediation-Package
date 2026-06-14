#if LEVELPLAY_INSTALLED
using System;
using UnityEngine;
using Unity.Services.LevelPlay;

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

        public MediationConsentManager(AdConfiguration config, ConsentManager consent)
        {
            this.config = config;
            this.consent = consent;
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

                Debug.Log("[Autech.LevelPlay] Mediation consent applied — " +
                          $"gdprApplies={consent.GdprApplies()} consentType={consent.GetConsentType()} " +
                          $"(GDPR via IAB TCF) ccpaOptOut={config.CcpaOptOut} coppa={config.TagForChildDirectedTreatment}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Autech.LevelPlay] Failed to apply mediation consent: {e.Message}");
            }
        }
    }
}
#endif // LEVELPLAY_INSTALLED
