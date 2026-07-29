using UnityEngine;
using UnityEngine.UI;

public static class PhysicsUtil
{
    public static void SetRaycastTargetRecursively(GameObject obj, bool enable)
    {
        if (obj == null) return;
        // 第二个参数 true 表示包含非激活的子物体
        var graphics = obj.GetComponentsInChildren<Graphic>(true);
        foreach (var graphic in graphics)
        {
            graphic.raycastTarget = enable;
        }
    }
    
}
