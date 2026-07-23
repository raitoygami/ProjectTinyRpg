using System.Collections.Generic;
using UnityEngine;

public class DialogueRoot : DialogueEntry {

#if UNITY_EDITOR
    public override List<string> GetStyleClasses() {
        var ret = new List<string> {"root"};
        return ret;
    }
#endif
}