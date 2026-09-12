using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Autech.LevelPlay
{
    /// <summary>
    /// Behaviour on the authored local consent form prefab
    /// (<c>Runtime/Resources/AutechLocalConsentForm.prefab</c>), shown by
    /// <see cref="LocalConsentManager"/> when VerifyLevelPlay has
    /// <c>useLocalConsent</c> ticked.
    ///
    /// Every rect, sprite, colour and label lives on the prefab. This script only
    /// reads the two toggles and reports the user's choice back exactly once.
    /// </summary>
    public class LocalConsentForm : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField] private Toggle adsToggle;
        [SerializeField] private Toggle analyticsToggle;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;
        [Tooltip("Opens the privacy policy. Hidden automatically when no URL is configured.")]
        [SerializeField] private Button policyButton;

        private Action<bool, bool> onSubmitted;
        private bool submitted;
        private GameObject createdEventSystem;
        private string policyUrl;

        /// <summary>
        /// True when the prefab has at least one button wired. A form with neither
        /// can never be answered, so the caller must not wait on one.
        /// </summary>
        public bool IsAnswerable => acceptButton != null || declineButton != null;

        /// <summary>
        /// True when the form's labels will actually render. The prefab's text is
        /// TextMeshPro, which is a separate package on Unity 2021.3 and 2022.x. In a
        /// project without it every label deserializes as a missing script and the
        /// form draws its card, toggles and buttons with no readable text at all,
        /// leaving the user to tap Accept or Decline blind. That is worse than not
        /// asking, so <see cref="LocalConsentManager"/> refuses to show it.
        ///
        /// Resolved by reflection so the package never hard-depends on TextMeshPro.
        /// </summary>
        public bool HasReadableText
        {
            get
            {
                var tmpType = FindTextMeshProType();
                if (tmpType == null)
                {
                    return false;
                }

                var labels = GetComponentsInChildren(tmpType, true);
                return labels != null && labels.Length > 0;
            }
        }

        private static Type cachedTmpType;

        private static Type FindTextMeshProType()
        {
            if (cachedTmpType != null)
            {
                return cachedTmpType;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var found = assembly.GetType("TMPro.TextMeshProUGUI");
                    if (found != null)
                    {
                        cachedTmpType = found;
                        return cachedTmpType;
                    }
                }
                catch
                {
                    // Dynamic or reflection-only assemblies cannot be queried; skip.
                }
            }

            return null;
        }

        /// <summary>
        /// Seed the toggles with the current choice and start listening. The
        /// callback fires exactly once, with the ads and analytics decisions.
        /// </summary>
        public void Present(bool adsGranted, bool analyticsGranted, Action<bool, bool> onChoiceMade)
        {
            Present(adsGranted, analyticsGranted, null, onChoiceMade);
        }

        /// <summary>
        /// Seed the toggles, wire the policy link, and start listening. GDPR requires
        /// consent to be informed, so the policy link is how the user can actually
        /// find out who receives their data before deciding.
        /// </summary>
        public void Present(bool adsGranted, bool analyticsGranted, string privacyPolicyUrl,
                            Action<bool, bool> onChoiceMade)
        {
            policyUrl = privacyPolicyUrl;
            onSubmitted = onChoiceMade;
            submitted = false;

            if (policyButton != null)
            {
                // Hide rather than show a dead link when no URL is configured.
                policyButton.gameObject.SetActive(!string.IsNullOrEmpty(policyUrl));
            }

            if (adsToggle != null)
            {
                adsToggle.isOn = adsGranted;
            }

            if (analyticsToggle != null)
            {
                analyticsToggle.isOn = analyticsGranted;
            }

            EnsureEventSystem();
            gameObject.SetActive(true);

            // Subscribe last, so a problem above cannot fire completion early.
            if (acceptButton != null)
            {
                acceptButton.onClick.AddListener(HandleAccept);
            }

            if (declineButton != null)
            {
                declineButton.onClick.AddListener(HandleDecline);
            }

            if (policyButton != null)
            {
                policyButton.onClick.AddListener(HandlePolicy);
            }
        }

        /// <summary>Open the publisher's privacy policy. Does not answer the form.</summary>
        private void HandlePolicy()
        {
            if (string.IsNullOrEmpty(policyUrl))
            {
                return;
            }

            Application.OpenURL(policyUrl);
        }

        private void OnDisable()
        {
            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveListener(HandleAccept);
            }

            if (declineButton != null)
            {
                declineButton.onClick.RemoveListener(HandleDecline);
            }

            if (policyButton != null)
            {
                policyButton.onClick.RemoveListener(HandlePolicy);
            }
        }

        /// <summary>
        /// Last-resort cleanup. Submit tears the EventSystem down on the normal path,
        /// but the form can also be destroyed without ever being answered (the
        /// manager's failure path, or a game destroying it). Leaving the EventSystem
        /// behind would be worse than a plain leak: EventSystem.current is the FIRST
        /// registered instance, so an orphan stays current for the life of the app and
        /// the game's own EventSystem never processes input again.
        /// </summary>
        private void OnDestroy()
        {
            DestroyCreatedEventSystem();
        }

        private void DestroyCreatedEventSystem()
        {
            if (createdEventSystem == null)
            {
                return;
            }

            Destroy(createdEventSystem);
            createdEventSystem = null;
        }

        /// <summary>
        /// The scene that owned the EventSystem can be unloaded while this form is
        /// still up, because the form is DontDestroyOnLoad and the consent flow
        /// overlaps ordinary startup. If that happens the form is left visible and
        /// permanently unclickable, so re-check every frame and adopt one when the
        /// previous owner disappears.
        /// </summary>
        private void Update()
        {
            if (EventSystem.current == null)
            {
                EnsureEventSystem();
            }
        }

        /// <summary>
        /// A Canvas receives no taps without an EventSystem, and a game with no
        /// uGUI of its own may not have one, which would leave the form visible
        /// but unclickable. Create one matching the active input backend only when
        /// the scene has none, and tear it down again in <see cref="Submit"/> so
        /// the game is never left with a second EventSystem.
        ///
        /// It must be DontDestroyOnLoad for the same reason the form is: the
        /// consent flow is started from VerifyLevelPlay.Start and overlaps ordinary
        /// startup, so the game can load its next scene while the form is still up.
        /// A scene load would otherwise destroy the EventSystem but not the form,
        /// leaving the form permanently unclickable and its task never completing.
        /// </summary>
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            createdEventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && INPUTSYSTEM_PACKAGE
            createdEventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            createdEventSystem.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(createdEventSystem);
        }

        /// <summary>Accept applies whatever the two toggles currently say.</summary>
        private void HandleAccept()
        {
            bool ads = false;
            if (adsToggle != null)
            {
                ads = adsToggle.isOn;
            }

            bool analytics = false;
            if (analyticsToggle != null)
            {
                analytics = analyticsToggle.isOn;
            }

            Submit(ads, analytics);
        }

        /// <summary>Decline refuses both, whatever the toggles show.</summary>
        private void HandleDecline()
        {
            Submit(false, false);
        }

        private void Submit(bool ads, bool analytics)
        {
            // Lock synchronously the moment the choice lands.
            if (submitted)
            {
                return;
            }

            submitted = true;

            var callback = onSubmitted;
            onSubmitted = null;
            gameObject.SetActive(false);
            DestroyCreatedEventSystem();

            if (callback != null)
            {
                callback(ads, analytics);
            }
        }
    }
}
