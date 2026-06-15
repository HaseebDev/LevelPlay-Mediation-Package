using System;
using System.Collections.Generic;

[Serializable]
public class ConsentAndLI
{
    public Dictionary<string,bool> consents { get; set; }
    public Dictionary<string,bool> legitimateInterests { get; set; }
}

