using System.Runtime.InteropServices;

/// <summary>
/// Bridge between the Choice Unity Instance-wide API and iOS implementation.
/// </summary>
/// <para>
/// Publishers integrating with Choice should make all calls through the <see cref="ChoiceCMP"/> class, and handle any
/// desired ChoiceCMP Events from the <see cref="ChoiceCMPManager"/> class.
/// </para>
/// <para>
/// For other platform-specific implementations, see <see cref="ChoiceCMPUnityEditor"/> and <see cref="ChoiceCMPAndroid"/>.
/// </para>

internal class ChoiceCMPiOS : ChoiceCMPPlatformApi
{
    #region SdkSetup

    internal override void StartChoice(string pCode, ChoiceStyle choiceStyle, bool shouldDisplayIDFA)
    {
        string choiceStyleJson = ChoiceUtils.EncodeChoiceStyleResource(choiceStyle);
        _StartChoice(pCode, choiceStyleJson, shouldDisplayIDFA);
    }

    internal override void ShowCCPA()
    {
        _ShowCCPA();
    }

    internal override void ShowUSRegulations()
    {
        _ShowUSRegulation();
    }

    internal override void ForceDisplayUI()
    {
        _ForceDisplayUI();
    }

    internal override void GetGDPRData()
    {
        _GetGDPRData();
    }

    internal override void ShowGoogleBasicConsent()
    {
        _ShowGoogleBasicConsent();
    }

    internal override void SetUserLoginOrSubscriptionStatus(bool status)
    {
        _SetUserLoginOrSubscriptionStatus(status);
    }

    internal override string GetSDKVersion()
    {
        return _GetSDKVersion();
    }


    #endregion SdkSetup

    #region DllImports
#if ENABLE_IL2CPP && UNITY_ANDROID
    // IL2CPP on Android scrubs DllImports, so we need to provide stubs to unblock compilation
    private static void _StartChoice(string pCode,string choiceStyleJson, bool shouldDisplayIDFA) {}
    private static void _ShowCCPA() {}
    private static void _ShowUSRegulation() {}
    private static void _GetGDPRData() {}
    private static void _ShowGoogleBasicConsent() {}
    private static void _ForceDisplayUI() {}
    private static void _SetUserLoginOrSubscriptionStatus(bool status) {}
    private static string _GetSDKVersion() { return ""; }
#else
    [DllImport("__Internal")]
    private static extern void _StartChoice(string pCode, string choiceStyle, bool shouldDisplayIDFA);

    [DllImport("__Internal")]
    private static extern void _ShowCCPA();

    [DllImport("__Internal")]
    private static extern void _ShowUSRegulation();

    [DllImport("__Internal")]
    private static extern void _ForceDisplayUI();

    [DllImport("__Internal")]
    private static extern void _GetGDPRData();

    [DllImport("__Internal")]
    private static extern void _ShowGoogleBasicConsent();

    [DllImport("__Internal")]
    private static extern void _SetUserLoginOrSubscriptionStatus(bool status);

    [DllImport("__Internal")]
    private static extern string _GetSDKVersion();

#endif
    #endregion DllImports
}
