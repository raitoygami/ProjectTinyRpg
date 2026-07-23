using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AbilityRoot : AbilityEffect {
    protected override async UniTask OnApply()
    {
        await ApplyChildren();
    }
    
#if UNITY_EDITOR
    public override List<string> GetStyleClasses() {
        var ret = new List<string> {"root"};
        return ret;
    }
#endif
}