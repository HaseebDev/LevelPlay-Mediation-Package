using System.Diagnostics.CodeAnalysis;
using UnityEngine;

/// <summary>
/// Bridge between the Choice Unity Instance-wide API and Android implementation.
/// </summary>
/// <para>
/// Publishers integrating with Choice should make all calls through the <see cref="ChoiceCMP"/> class, and handle any
/// desired ChoiceCMP Events from the <see cref="ChoiceCMPManager"/> class.
/// </para>
/// <para>
/// For other platform-specific implementations, see <see cref="ChoiceCMPUnityEditor"/> and <see cref="ChoiceCMPiOS"/>.
/// </para>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal class ChoiceCMPAndroid : ChoiceCMPPlatformApi
{
    private static readonly AndroidJavaClass PluginClass = new AndroidJavaClass("com.inmobi.cmp.unityplugin.InMobiChoiceCmp");

    #region SdkSetup

    internal override void ForceDisplayUI()
    {
        PluginClass.CallStatic("forceDisplayUI");
    }

    internal override void GetGDPRData()
    {
       PluginClass.CallStatic("getGDPRData");
    }

    internal override void ShowCCPA()
    {
        PluginClass.CallStatic("showCCPAScreen");
    }

    internal override void ShowUSRegulations()
    {
        PluginClass.CallStatic("showUSRegulationScreen");
    }

    internal override void StartChoice(string pCode, ChoiceStyle choiceStyle, bool shouldDisplayIDFA)
    {
        string choiceStyleJson = ChoiceUtils.EncodeChoiceStyleResource(choiceStyle);
        PluginClass.CallStatic("startChoice", new object[2] { pCode, choiceStyleJson });
    }

    internal override void ShowGoogleBasicConsent()
    {
        PluginClass.CallStatic("showGoogleBasicConsentScreen");
    }

    internal override void SetUserLoginOrSubscriptionStatus(bool status)
    {
        PluginClass.CallStatic("setUserLoginOrSubscriptionStatus", new object[1] { status });
    }

    internal override string GetSDKVersion()
    {
        return PluginClass.CallStatic<string>("getSDKVersion");
    }

    #endregion SdkSetup
}
