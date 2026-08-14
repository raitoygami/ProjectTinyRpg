using System;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class DungeonEntry : Entity
{
    [SerializeField] private string dungeonName;
    [SerializeField] private Vector3Int EntryPoint;
    public void Awake()
    {
        Faction = EntityFaction.Env;
        this.Subscribe<AgentInteractive.InteractionEvent>(OnInteraction);
        this.AddComponent<AgentInteractive>();
    }

    private async UniTask OnInteraction(AgentInteractive.InteractionEvent arg)
    {
        var mapInfo = MapManager.Instance.GetMapInfo(dungeonName);
        if (mapInfo == null)
            return;
        
        await UIRoot.Instance.FadeIn(0.1f);
        // 设置玩家模板id 和出生位置
        PlayerManager.Instance.SetCurrentMap(dungeonName);
        PlayerManager.Instance.SetCurrentLocation(EntryPoint);
        
        // 加载场景
        await MapLoader.Instance.Load(PlayerManager.Instance.GetCurrentMap());
        
        await UniTask.DelayFrame(1);
        await this.PublishGlobal(new MapLoader.MapChangedEvt());
        UIRoot.Instance.OpenMainUI();
        await UIRoot.Instance.FadeOut(0.1f);

    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var spawnerWorld = transform.position;
        Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.35f);
        Gizmos.DrawCube(spawnerWorld, Vector3.one);
    }
#endif    
}
