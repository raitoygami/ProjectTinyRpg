using System.Collections.Generic;
using UnityEngine;

// 防具换装表
[CreateAssetMenu(fileName = "CustomizationTable", menuName = "Config/Customization Armor Config")]
public class CustomizationTable : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public int itemId;
        public Sprite sprite;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    // 运行时字典（缓存）
    private Dictionary<int, Sprite> _dict;

    // 在游戏启动时调用一次（比如在 GameManager 里）
    public void Initialize()
    {
        _dict = new Dictionary<int, Sprite>();
        foreach (var entry in entries)
        {
            if (entry.sprite != null && !_dict.ContainsKey(entry.itemId))
                _dict.Add(entry.itemId, entry.sprite);
        }
    }

    // 根据 ItemId 获取精灵，若不存在则返回 null
    public Sprite GetSprite(int itemId)
    {
        if (_dict == null) Initialize(); // 防御性初始化
        return _dict != null && _dict.TryGetValue(itemId, out var sprite) ? sprite : null;
    }
}
