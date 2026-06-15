/// <summary>
/// Bridge between the InMobiChoice Unity Instance-wide API and platform-specific implementations.
/// </summary>
/// <para>
/// Publishers integrating with InMobiChoice should make all calls through the <see cref="ChoiceCMP"/> class, and handle any
/// desired ChoiceCMP Events from the <see cref="ChoiceCMPManager"/> class.
/// </para>
/// <para>
/// For platform-specific implementations, see
/// <see cref="ChoiceCMPUnityEditor"/>, <see cref="ChoiceCMPAndroid"/>, and <see cref="ChoiceCMPiOS"/>.
/// </para>
internal abstract class ChoiceCMPPlatformApi
{

    #region SdkSetup

    internal abstract void StartChoice(string pCode, ChoiceStyle choiceStyle, bool shouldDisplayIDFA);

    internal abstract void ShowCCPA();

    internal abstract void ShowUSRegulations();

    internal abstract void ForceDisplayUI();

    internal abstract void GetGDPRData();

    internal abstract void ShowGoogleBasicConsent();

    internal abstract void SetUserLoginOrSubscriptionStatus(bool status);

    internal abstract string GetSDKVersion();

    #endregion SdkSetup

}
