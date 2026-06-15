using System;
using System.Collections.Generic;

[Serializable]
public class Publisher
{
	public int vendorId;
    public Dictionary<string,bool> consents { get; set; }
    public Dictionary<string,bool> legitimateInterests { get; set; }
    public ConsentAndLI customPurpose { get; set; }
}
