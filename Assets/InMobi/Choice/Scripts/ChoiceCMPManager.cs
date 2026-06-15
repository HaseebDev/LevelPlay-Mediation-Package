using System;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Handler for Choice integration across publisher apps and Unity Editor.
/// </summary>
/// <para>
/// Publishers integrating with InMobiChoice should make all calls through the <see cref="ChoiceCMP"/> class, and handle any
/// desired ChoiceCMP Events from this class.
/// </para>

public class ChoiceCMPManager : MonoBehaviour
{
    #region ChoiceCMPEvents


    // Fired when the SDK has finished loading
    public static event Action<PingResult> CMPDidLoadEvent;

    // Fired when SDK fails to load
    public static event Action<string> CMPDidErrorEvent;

    // Fired when on receiving the IAB Vendor Consent
    public static event Action<GDPRData> CMPDidReceiveIABVendorConsentEvent;

    // Fired when on receiving the NON IAB Vendor Consent
    public static event Action<NonIABData> CMPDidReceiveNonIABVendorConsentEvent;

    // Fired when on receiving the Additional Consent
    public static event Action<ACData> CMPDidReceiveAdditionalConsentEvent;

    // Fired when on receiving the Additional Consent
    public static event Action<string> CMPDidReceiveCCPAConsentEvent;

    //fired when on receiving the Google Basic Consent
    public static event Action<GoogleBasicConsents> CMPDidReceiveGoogleBasicConsentEvent;

    // Fired when on receiving US Regulations Consent
    public static event Action<USRegulationData> CMPDidReceiveUSRegulationsConsent;

    // Fired when on user moved to other state regards to US regulations
    public static event Action CMPUserDidMoveToOtherState;

    // Fired in response to getGDPRData API call
    public static event Action<GDPRData> CMPGDPRDataCallback;

    // Fired when UI state changes
    public static event Action<DisplayInfo> CMPUIStatusChangedEvent;

    // Fired when consent or pay is enabled and user taps one of the action buttons
    public static event Action<ActionButtons> CMPDidReceiveActionButtonTapEvent;

    #endregion ChoiceCMPEvents


    // Singleton.
    public static ChoiceCMPManager Instance { get; protected set; }

    #region ChoiceCMPManagerPrefab

   
  
    // API to make calls to the platform-specific Choice SDK.
    internal static ChoiceCMPPlatformApi ChoiceCMPPlatformApi { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (ChoiceCMPPlatformApi == null)
            ChoiceCMPPlatformApi = new
#if UNITY_EDITOR
                ChoiceCMPUnityEditor
#elif UNITY_ANDROID
                ChoiceCMPAndroid
#else
                ChoiceCMPiOS
#endif
                ();

        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);

    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


    #endregion ChoiceCMPManagerPrefab


    #region PlatformCallbacks

    public void EmitCMPDidLoadEvent(string pingReturnJson)
    {
        PingResult pingResult;
        try
        {
            pingResult = JsonUtility.FromJson<PingResult>(pingReturnJson);
        } catch (Exception)
        {
            pingResult = new PingResult();
            ChoiceCMPLog.Log("EmitCMPDidLoadEvent", "Error decoding ping result");
        }

        ChoiceCMPLog.Log("EmitCMPDidLoadEvent", ChoiceCMPLog.SdkLogEvent.InitFinished);
        ChoiceCMPLog.Log("EmitCMPDidLoadEvent", "LoadEvent: " + pingReturnJson);
        var evt = CMPDidLoadEvent;
        if (evt != null) evt(pingResult);
    }


    public void EmitCMPDidErrorEvent(string error)
    {
        ChoiceCMPLog.Log("EmitCMPDidErrorEvent", ChoiceCMPLog.AdLogEvent.LoadFailed, error);
        var evt = CMPDidErrorEvent;
        if (evt != null) evt(error);
    }


    public void EmitCMPDidReceiveIABVendorConsentEvent(string gdprDataJson)
    {
        GDPRData gdprData;
        try
        {
            gdprData = JsonConvert.DeserializeObject<GDPRData>(gdprDataJson);
        } catch (Exception)
        {
            gdprData = new GDPRData();
            ChoiceCMPLog.Log("EmitCMPDidReceiveIABVendorConsentEvent", "Error decoding GPDRData");

        }

        ChoiceCMPLog.Log("EmitCMPDidReceiveIABVendorConsentEvent", ChoiceCMPLog.AdLogEvent.DidReceiveIABVendorConsent);
        var evt = CMPDidReceiveIABVendorConsentEvent;
        if (evt != null) evt(gdprData);
    }

    public void EmitCMPDidReceiveGDPRDataEvent(string gdprDataJson)
    {
        GDPRData gdprData;
        try
        {
            gdprData = JsonConvert.DeserializeObject<GDPRData>(gdprDataJson);
        }
        catch (Exception)
        {
            gdprData = new GDPRData();
            ChoiceCMPLog.Log("EmitCMPDidReceiveGDPRDataEvent", "Error decoding GPDRData");

        }

        ChoiceCMPLog.Log("EmitCMPDidReceiveGDPRDataEvent", ChoiceCMPLog.AdLogEvent.DidReceiveGDPRData);
        var evt = CMPGDPRDataCallback;
        if (evt != null) evt(gdprData);
    }

    public void EmitCMPDidReceiveNonIABVendorConsentEvent(string nonIABDataJson)
    {
        NonIABData nonIABData;
        try
        {
            nonIABData = JsonConvert.DeserializeObject<NonIABData>(nonIABDataJson);
        } catch (Exception)
        {
            nonIABData = new NonIABData();
            ChoiceCMPLog.Log("EmitCMPDidReceiveNonIABVendorConsentEvent", "Error decoding NonIABData");
        }
        
        ChoiceCMPLog.Log("EmitCMPDidReceiveNonIABVendorConsentEvent", ChoiceCMPLog.AdLogEvent.DidReceiveNONIABVendorConsent);
        ChoiceCMPLog.Log("EmitCMPDidReceiveNonIABVendorConsentEvent", "NonIABVendorConsent: " + nonIABDataJson);
        var evt = CMPDidReceiveNonIABVendorConsentEvent;
        if (evt != null) evt(nonIABData);
    }

    public void EmitCMPDidReceiveAdditionalConsentEvent(string acDataJson)
    {
        ACData acData;
        try
        {
            acData = JsonUtility.FromJson<ACData>(acDataJson);
        } catch (Exception)
        {
            acData = new ACData();
            ChoiceCMPLog.Log("EmitCMPDidReceiveAdditionalConsentEvent", "Error decoding ACData");
        }

        ChoiceCMPLog.Log("EmitCMPDidReceiveAdditionalConsentEvent", ChoiceCMPLog.AdLogEvent.DidReceiveAdditionalConsent);
        var evt = CMPDidReceiveAdditionalConsentEvent;
        if (evt != null) evt(acData);
    }

    public void EmitCMPDidReceiveCCPAConsentEvent(string ccpaConsent)
    {
        ChoiceCMPLog.Log("EmitCMPDidReceiveCCPAConsentEvent", ChoiceCMPLog.AdLogEvent.DidReceiveCCPAConsent);
        var evt = CMPDidReceiveCCPAConsentEvent;
        if (evt != null) evt(ccpaConsent);
    }

    public void EmitCMPDidReceiveGoogleBasicConsentEvent(string googleBasicConsentJson)
    {
        GoogleBasicConsents googleBasicConsents;
        try
        {
            googleBasicConsents = JsonConvert.DeserializeObject<GoogleBasicConsents>(googleBasicConsentJson);
        } catch (Exception)
        {
            googleBasicConsents = new GoogleBasicConsents();
            ChoiceCMPLog.Log("EmitCMPDidReceiveGoogleBasicConsentEvent", "Error decoding GoogleBasicConsents");
        }
        ChoiceCMPLog.Log("EmitCMPDidReceiveGoogleBasicConsentEvent", ChoiceCMPLog.AdLogEvent.DidReceiveGoogleBasicConsentEvent);
        var evt = CMPDidReceiveGoogleBasicConsentEvent;
        if (evt != null) evt(googleBasicConsents);
    }

    public void EmitCMPDidReceiveUSRegulationsConsentEvent(string usRegulationDataJson)
    {
        USRegulationData usRegulationData;
        try
        {
            usRegulationData = JsonConvert.DeserializeObject<USRegulationData>(usRegulationDataJson);
        }
        catch (Exception)
        {
            usRegulationData = new USRegulationData();
            ChoiceCMPLog.Log("EmitCMPDidReceiveUSRegulationsConsentEvent", "Error decoding GoogleBasicConsents");
        }
        ChoiceCMPLog.Log("EmitCMPDidReceiveUSRegulationsConsentEvent", ChoiceCMPLog.AdLogEvent.DidReceiveUSRegulationConsent);
        var evt = CMPDidReceiveUSRegulationsConsent;
        if (evt != null) evt(usRegulationData);
    }

    public void EmitUserDidMoveToOtherStateEvent()
    {
        ChoiceCMPLog.Log("EmitUserDidMoveToOtherStateEvent", ChoiceCMPLog.AdLogEvent.DidUserMovedToOtherState);
        var evt = CMPUserDidMoveToOtherState;
        if (evt != null) evt();
    }

    public void EmitCMPUIStatusChangedEvent(string displayInfoJson)
    {
        DisplayInfo displayInfo;
        try
        {
            displayInfo = JsonUtility.FromJson<DisplayInfo>(displayInfoJson);
        }
        catch (Exception)
        {
            displayInfo = new DisplayInfo();
            ChoiceCMPLog.Log("EmitCMPUIStatusChangedEvent", "Error decoding Display Info");
        }

        ChoiceCMPLog.Log("EmitCMPUIStatusChangedEvent", ChoiceCMPLog.AdLogEvent.CMPUIStatusChanged);
        var evt = CMPUIStatusChangedEvent;
        if (evt != null) evt(displayInfo);
    }

    public void EmitCMPActionButtonTapEvent(string action)
    {
        if (Enum.TryParse(action, out ActionButtons actionButton))
        {
            ChoiceCMPLog.Log("EmitCMPActionButtonTapEvent", ChoiceCMPLog.AdLogEvent.ActionButtonTap, actionButton);
            var evt = CMPDidReceiveActionButtonTapEvent;
            if (evt != null) evt(actionButton);
        }
        else
        {
            ChoiceCMPLog.Log("EmitCMPActionButtonTapEvent", ChoiceCMPLog.AdLogEvent.ActionButtonParseError);
        }

    }

    #endregion PlatformCallbacks
}
