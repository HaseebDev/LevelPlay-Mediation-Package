using System;

[Serializable]
public class PingResult
{
    public bool gdprApplies;

    public bool cmpLoaded;

    public string cmpStatus;

    public string displayStatus;

    public string apiVersion;

    public string cmpVersion;

    public int cmpId;

    public int gvlVersion;

    public int tcfPolicyVersion;

    public bool usRegulationApplies;
}

