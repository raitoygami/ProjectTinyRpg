using UnityEngine;
using UnityEngine.UI;

public static class Utils
{
    /// <summary>
    /// 在 UI 上创建源 GameObject 的镜像，保留所有子节点的层级和相对变换（位置、旋转、缩放）。
    /// 所有 SpriteRenderer 替换为 Image，并正确映射 Sprite 的 pivot。
    /// </summary>
    /// <param name="sourceRoot">源 GameObject（本身为空节点，所有 SpriteRenderer 在子节点中）</param>
    /// <param name="parentUI">UI 父节点（如 Canvas 或 Panel）</param>
    /// <returns>镜像的根节点（RectTransform），你可以手动调整它的 anchoredPosition</returns>
    public static RectTransform CreateUIMirror(GameObject sourceRoot, Transform parentUI)
    {
        if (sourceRoot == null || parentUI == null)
        {
            Debug.LogError("源对象或父 UI 为空！");
            return null;
        }

        // 创建镜像根节点
        var mirrorRootObj = new GameObject(sourceRoot.name + "_Mirror");
        mirrorRootObj.transform.SetParent(parentUI, false);
        var mirrorRootRect = mirrorRootObj.AddComponent<RectTransform>();
        mirrorRootRect.anchorMin = new Vector2(0.5f, 0);
        mirrorRootRect.anchorMax = new Vector2(0.5f, 0);
        mirrorRootRect.pivot = new Vector2(0.5f, 0.0f);
        
        // 根节点的位置由用户后续手动设置，此处置零
        mirrorRootRect.anchoredPosition = Vector2.zero;
        // 根节点的尺寸由子节点撑起，无需设置
        mirrorRootRect.sizeDelta = Vector2.zero;

        // 递归遍历源根节点的所有子节点（不复制根节点本身，因为根节点无 SpriteRenderer）
        foreach (Transform child in sourceRoot.transform)
        {
            CreateMirrorNode(child, mirrorRootRect, 0.0f, new Vector2(0.5f,0.0f), Vector2.zero, 0);
        }

        mirrorRootObj.transform.localScale = Vector3.one * 5;
        
        return mirrorRootRect;
    }

    private static void CreateMirrorNode(Transform sourceChild, Transform parentUI, float pivotParent, Vector2 anchorParent, Vector2 parentSize, float offsetParent)
    {
        // 创建 UI 节点
        var uiObj = new GameObject(sourceChild.name);
        uiObj.transform.SetParent(parentUI, false);
        var rect = uiObj.AddComponent<RectTransform>();

        // 直接使用源子节点的局部位置（相对于其父节点）作为 UI 中的 anchoredPosition
        // 因为 UI 的坐标系与局部坐标系的 X/Y 轴一致
        var localPos = sourceChild.localPosition;
        rect.anchoredPosition = new Vector2(localPos.x * 24, localPos.y * 24);
        // 复制旋转和缩放
        rect.localRotation = sourceChild.localRotation;
        rect.localScale = Vector3.one;
        /*rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);*/
        
        // 处理 SpriteRenderer → Image
        var renderer = sourceChild.GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.sprite != null)
        {
            var image = uiObj.AddComponent<Image>();
            image.sprite = renderer.sprite;
            image.raycastTarget = false; // 默认不阻挡点击
            image.color = renderer.color;

            // 设置 Image 的 pivot 为 Sprite 自身的 pivot（归一化坐标）
            var sprite = renderer.sprite;
            var pivotNormalized = new Vector2(
                sprite.pivot.x / sprite.rect.width,
                sprite.pivot.y / sprite.rect.height
            );

            var localPosY = localPos.y * sprite.pixelsPerUnit;
            
            rect.anchorMin = new Vector2(0.5f, 0.5f + anchorParent.y - pivotNormalized.y);
            rect.anchorMax = new Vector2(0.5f, 0.5f + anchorParent.y - pivotNormalized.y);

            var pivotReal = pivotNormalized.y;
            if (parentSize.y > 0 && Mathf.Abs(pivotParent) > 0.0f)
            {
                pivotReal = 0.5f + (parentSize.y * pivotParent - pivotNormalized.y * sprite.rect.height) / sprite.rect.height;    
            }
            
            rect.pivot = new Vector2(pivotNormalized.x, pivotReal);
            
            rect.localScale = Vector3.one;
            // 设置 Image 尺寸为 Sprite 原始像素尺寸（保证 1:1 像素对应）
            rect.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            rect.anchoredPosition = new Vector2(localPos.x * sprite.pixelsPerUnit, localPosY);
            offsetParent = localPosY;
            anchorParent = pivotNormalized;
            pivotParent = pivotNormalized.y;
            parentSize = rect.sizeDelta;
        }
        else
        {
            // 没有 SpriteRenderer 的空节点，设置尺寸为 0（只作为层级容器）
            rect.sizeDelta = Vector2.zero;
        }

        // 保持与源节点相同的兄弟索引（确保 UI 的渲染顺序正确）
        uiObj.transform.SetSiblingIndex(sourceChild.GetSiblingIndex());

        // 递归处理所有子节点
        foreach (Transform child in sourceChild)
        {
            CreateMirrorNode(child, uiObj.transform, pivotParent, anchorParent, parentSize, offsetParent);
        }
    }
}

