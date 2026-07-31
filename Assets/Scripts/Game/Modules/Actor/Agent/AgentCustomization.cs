using cfg;
using UnityEngine;

public class AgentCustomization : MonoBehaviour
{
    // 默认的sprite
    [SerializeField] private Sprite _defaultSprite;
    [SerializeField] private CustomizationTable _customizationTable;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private Sprite _CombineSprite;


    private int _ItemIdHelmet;
    private int _ItemIdChestplate;

    private Sprite _SpriteHelmet;
    private Sprite _SpriteChestplate;

    public void RefreshCustomization()
    {
        var helmet = PlayerManager.Instance.GetArmor(EquipType.Helmet);
        var chestplate = PlayerManager.Instance.GetArmor(EquipType.Chestplate);

        // 装备相同外观的armor
        if (SameType(_ItemIdHelmet, helmet) && SameType(_ItemIdChestplate, chestplate))
            return;

        _ItemIdHelmet = helmet?.ItemId ?? 0;
        _ItemIdChestplate = chestplate?.ItemId ?? 0;

        _SpriteHelmet = _customizationTable.GetSprite(_ItemIdHelmet);
        _SpriteChestplate = _customizationTable.GetSprite(_ItemIdChestplate);
        // 合成sprite

        CombineAll();
    }

    private void CombineAll()
    {
        // 获取底层纹理（必须存在）
        var defaultTex = _defaultSprite.texture;
        var width = defaultTex.width;
        var height = defaultTex.height;

// 获取各层像素数据（若为 null 则视为全透明）
        var defaultPixels = defaultTex.GetPixels();
        var helmetPixels = _SpriteHelmet?.texture.GetPixels();
        var chestPixels = _SpriteChestplate?.texture.GetPixels();
// 合成像素
        var combinedPixels = new Color[defaultPixels.Length];
        for (var i = 0; i < combinedPixels.Length; i++)
        {
            var final = defaultPixels[i];

            // 混合胸甲（中间层）
            if (chestPixels != null)
            {
                var chest = chestPixels[i];
                final = chest * chest.a + final * (1 - chest.a);
            }

            // 混合头盔（最上层）
            if (helmetPixels != null)
            {
                var helmet = helmetPixels[i];
                final = helmet * helmet.a + final * (1 - helmet.a);
            }

            combinedPixels[i] = final;
        }

// 创建合成纹理
        var combinedTex = new Texture2D(width, height, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point, // 像素风，不插值
            wrapMode = TextureWrapMode.Clamp // 防止边缘溢出
        };
        combinedTex.SetPixels(combinedPixels);
        combinedTex.Apply();

        if (_CombineSprite != null)
        {
            Destroy(_CombineSprite);
            _CombineSprite = null;
        }
        
// 生成 Sprite（pivot 设为中心）
        _CombineSprite = Sprite.Create(combinedTex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.0f), 24);
    }

    private bool SameType(int itemId, ItemStack itemStack)
    {
        if (itemStack == null && itemId == 0)
            return true;

        if (itemStack != null && itemId == itemStack.ItemId) return true;

        return false;
    }


    public Sprite GetCombinedSprite()
    {
        return _CombineSprite != null ? _CombineSprite : _defaultSprite;
    }
}