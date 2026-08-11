using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/Entity/Entity Template Table", fileName = "EntityTemplateTable", order = 0)]
public class EntityTemplateTable : ScriptableObject
{
    [Serializable]
    public class EntityTemplate
    {
        [SerializeField] public int id;
        [SerializeField] public string DefaultWeapon;    
    }
    
    [SerializeField]
    private List<EntityTemplate> _entityTemplates = new();
    
    // 运行时字典（缓存）
    private Dictionary<int, EntityTemplate> _dict;

    // 在游戏启动时调用一次（比如在 GameManager 里）
    public void Initialize()
    {
        _dict = new Dictionary<int, EntityTemplate>();
        foreach (var template in _entityTemplates)
        {
            _dict.TryAdd(template.id, template);
        }
    }

    // 根据 ItemId 获取精灵，若不存在则返回 null
    public EntityTemplate GetTemplate(int entityId)
    {
        if (_dict == null) Initialize(); // 防御性初始化
        return _dict?.GetValueOrDefault(entityId);
    }
    
}
