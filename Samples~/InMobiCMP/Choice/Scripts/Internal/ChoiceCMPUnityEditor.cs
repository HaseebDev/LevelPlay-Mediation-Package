using System.Collections;
#if UNITY_EDITOR
using System;

/// <summary>
/// Bridge between the Choice Unity Instance-wide API and In-Editor implementation.
/// </summary>
/// <para>
/// Publishers integrating with InMobiChoice should make all calls through the <see cref="ChoiceCMP"/> class, and handle any
/// desired ChoiceCMP Events from the <see cref="ChoiceCMPManager"/> class.
/// </para>
/// <para>
/// For other platform-specific implementations, see <see cref="ChoiceCMPAndroid"/> and <see cref="ChoiceCMPiOS"/>.
/// </para>
/// <remarks>
/// Some properties have added public setters in order to facilitate testing in Play mode.
/// </remarks>
internal class ChoiceCMPUnityEditor : ChoiceCMPPlatformApi
{
    #region SdkSetup

    internal override void StartChoice(string pCode, ChoiceStyle choiceStyleResource, bool shouldDisplayIDFA)
    {
        WaitOneFrame(() => {
            ChoiceCMPManager.Instance.EmitCMPDidLoadEvent("");
        });
    }

    #endregion SdkSetup


    #region MockEditor
    private static IEnumerator WaitOneFrameCoroutine(Action action)
    {
        yield return null;
        action();
    }


    public static void WaitOneFrame(Action action)
    {
        ChoiceCMPManager.Instance.StartCoroutine(WaitOneFrameCoroutine(action));
    }

    public static void SimulateApplicationResume()
    {
        WaitOneFrame(() => {
            ChoiceCMPLog.Log("SimulateApplicationResume", "Simulating application resume.");
        });
    }

    internal override void ShowCCPA()
    {
        
    }

    internal override void ShowUSRegulations() 
    {

    }

    internal override void ForceDisplayUI()
    {
        
    }

    internal override void GetGDPRData()
    {
      
    }

    internal override void ShowGoogleBasicConsent()
    {
    
    }

    internal override void SetUserLoginOrSubscriptionStatus(bool status)
    {

    }

    internal override string GetSDKVersion()
    {
        return "";
    }

    #endregion MockEditor
}
#endif
