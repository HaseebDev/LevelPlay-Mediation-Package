using System;
using System.Collections.Generic;

[Serializable]
public class GDPRData
{
    public string tcString;
    public string gppString;
    public ConsentAndLI purpose { get; set; }
    public ConsentAndLI vendor { get; set; }
    public Publisher publisher { get; set; }
    public Dictionary<string,bool> specialFeatureOptins { get; set; }
    public bool isServiceSpecific;
    public bool useNonStandardStacks;
    public string publisherCC;
    public bool purposeOneTreatment;
    public int cmpId;
    public int cmpVersion;
    public bool gdprApplies;
    public int tcfPolicyVersion;
    public PrivacyEncodingMode privacyEncodingMode;
}
