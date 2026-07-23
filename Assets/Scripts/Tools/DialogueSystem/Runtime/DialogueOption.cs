using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueOption : DialogueEntry {
    
    public override string GetDescription() {
        return "Options";
    }
#if UNITY_EDITOR
    public override List<string> GetStyleClasses() {
        var ret = new List<string> {"option"};
        return ret;
    }
    
#endif
}
