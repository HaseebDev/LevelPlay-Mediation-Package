using System;
using System.Collections.Generic;

public class USRegulationData
{
    public int version;
    public string gppString;
    public int sharingNotice;
    public int saleOptOutNotice;
    public int sharingOptOutNotice;
    public int targetedAdvertisingOptOutNotice;
    public int sensitiveDataProcessingOptOutNotice;
    public int sensitiveDataLimitUseNotice;
    public int saleOptOut;
    public int sharingOptOut;
    public int targetedAdvertisingOptOut;
    public List<int> sensitiveDataProcessing = new List<int>();
    public List<int> knownChildSensitiveDataConsents = new List<int>();
    public int personalDataConsents;
    public int mspaCoveredTransaction;
    public int mspaOptOutOptionMode;
    public int mspaServiceProviderMode;
}
