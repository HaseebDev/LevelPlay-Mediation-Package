
using System;
using UnityEngine;

public abstract class ChoiceCMP
{

    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Debug = 2,
    }
    private static string code;

    public static LogLevel ChoiceLogLevel;

    /// <summary>
    /// InMobi Choice Unity plugin version
    /// </summary>
    public const string ChoicePluginVersion = "2.0.1";

    /// <summary>
    /// Get the current SDK version being used
    /// </summary>
    public static string SDKVersion {
        get
        {
            return ChoiceCMPManager.ChoiceCMPPlatformApi.GetSDKVersion();
        }
    }


    static ChoiceCMP()
    {
        if (ChoiceCMPManager.Instance == null)
            new GameObject("ChoiceCMPManager", typeof(ChoiceCMPManager));
    }


    /// <summary>
    /// Start Choice with pCode and optional shouldDisplayIDFA
    /// </summary>
    /// <param name="pCode">PCode for initialising Choice.
    /// <param name="shouldDisplayIDFA">Optional param to show IDFA pop up on iOS.
    public static void StartChoice(string pCode, ChoiceStyle choiceStyle = null, bool shouldDisplayIDFA = false)
    {
        ChoiceCMPLog.Log("StartChoice", ChoiceCMPLog.SdkLogEvent.InitStarted);
        ChoiceCMPManager.ChoiceCMPPlatformApi.StartChoice(pCode, choiceStyle, shouldDisplayIDFA);
        code = pCode;
    }

    /// <summary>
    /// Show CCPA popup
    /// </summary>
    public static void ShowCCPA()
    {
        ChoiceCMPManager.ChoiceCMPPlatformApi.ShowCCPA();
    }

    /// <summary>
    /// Show US Regulation popup
    /// </summary>
    public static void ShowUSRegulations()
    {
        ChoiceCMPManager.ChoiceCMPPlatformApi.ShowUSRegulations();
    }

    /// <summary>
    /// Force show Choice popup
    /// </summary>
    public static void ForceDisplayUI()
    {
        ChoiceCMPManager.ChoiceCMPPlatformApi.ForceDisplayUI();
    }

    /// <summary>
    /// Get the TC String
    /// </summary>
    public static void GetGDPRData(Action<GDPRData> callback)
    {
        ChoiceCMPManager.CMPGDPRDataCallback -= callback; // Remove the old one if there is any
        ChoiceCMPManager.CMPGDPRDataCallback += callback;
        ChoiceCMPManager.ChoiceCMPPlatformApi.GetGDPRData();
    }

    /// <summary>
    /// Show Google Basic Consent
    /// </summary>
    public static void ShowGoogleBasicConsent()
    {
        ChoiceCMPManager.ChoiceCMPPlatformApi.ShowGoogleBasicConsent();
    }

    /// <summary>
    /// Set the user login or subscription status
    /// </summary>
    public static void SetUserLoginOrSubscriptionStatus(bool status)
    {
        ChoiceCMPManager.ChoiceCMPPlatformApi.SetUserLoginOrSubscriptionStatus(status);
    }
}
