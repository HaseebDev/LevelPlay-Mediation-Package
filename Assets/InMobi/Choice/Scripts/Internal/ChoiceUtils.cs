using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class ChoiceUtils
{

    public static string EncodeChoiceStyleResource(ChoiceStyle choiceStyle)
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(choiceStyle);
    }

    public static string InvariantCultureToString(object obj)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}", obj);
    }
}