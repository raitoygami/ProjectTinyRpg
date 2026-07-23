using System.Collections.Generic;

public class DialogueLines : DialogueEntry
{
#if UNITY_EDITOR
    public override List<string> GetStyleClasses() {
        var ret = new List<string> {"content"};
        return ret;
    }
#endif
}
