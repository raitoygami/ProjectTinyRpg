using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UIGameplay : MonoBehaviour
{
    [SerializeField] private UIStatBar m_StatBarPreset;
    [SerializeField] private RectTransform m_StatBarRoot;
    [Tooltip("世界空间偏移，如头顶 + (0, 1.5f, 0)")]
    [SerializeField] private Vector3 m_WorldOffset = new Vector3(0f, 0, 0f);

    private readonly Dictionary<Entity, StatBarEntry> m_StatBars = new Dictionary<Entity, StatBarEntry>();

    private void Awake()
    {
        m_StatBarPreset.gameObject.SetActive(false);
    }
    
    private readonly List<EntityFaction> m_FractionMonitors = new(){
        EntityFaction.Enemy, EntityFaction.PlayerSummon, EntityFaction.EnemySummon,
    };
    
    private void OnEnable()
    {
        if (!EntityManager.HasInstance()) return;
        EntityManager.Instance.OnEntityRegistered += AddStatBar;
        EntityManager.Instance.OnEntityUnregistered += RemoveStatBar;

        foreach (var faction in m_FractionMonitors)
        {
            var list = EntityManager.Instance.GetFractionEntities(faction);
            if (list == null) continue;
            foreach (var e in list)
                AddStatBar(e);
        }
    }

    private void OnDisable()
    {
        if (EntityManager.HasInstance())
        {
            EntityManager.Instance.OnEntityRegistered -= AddStatBar;
            EntityManager.Instance.OnEntityUnregistered -= RemoveStatBar;
        }
        foreach (var kv in m_StatBars)
        {
            kv.Value.Unregister?.Invoke();
            if (kv.Value.Root != null)
                Destroy(kv.Value.Root.gameObject);
        }
        m_StatBars.Clear();
    }

    private void Update()
    {
        if (!UIRoot.HasInstance())
            return;
        var cam = Camera.main;
        if (cam == null) return;

        foreach (var (ent, value) in m_StatBars)
        {
            if (ent == null || value.Root == null) continue;
            var worldPos = ent.transform.position + m_WorldOffset;
            var screenPos = cam.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                UIRoot.Instance.GetRoot(),
                screenPos,
                UIRoot.Instance.GetUICamera(), out var point);
            value.Root.anchoredPosition = point;
        }
    }

    private void AddStatBar(Entity entity)
    {
        if (!m_FractionMonitors.Contains(entity.Faction))
            return;
        
        if (entity == null || m_StatBarPreset == null || m_StatBars.ContainsKey(entity)) return;

        var stats = entity.GetComponent<AgentStats>();
        if (stats == null) return;

        var view = Instantiate(m_StatBarPreset, m_StatBarRoot != null ? m_StatBarRoot : transform as RectTransform);
        var rect = view.transform as RectTransform;
        if (rect == null) rect = view.GetComponent<RectTransform>();

        var maxH = stats.MaxHealth;
        var fill = maxH > 0 ? (float)stats.HealthCurrent / maxH : 1f;
        view.SetHpBar(fill);
        view.SetShieldFill(maxH > 0 ? (float)stats.TotalShieldAbsorption / maxH : 0f);
        view.gameObject.SetActive(stats.HealthCurrent < stats.MaxHealth || stats.TotalShieldAbsorption > 0);

        Action unregister = null;
        var pub = entity.GetComponent<PubSubActor>();
        if (pub != null)
        {
            UniTask Visibility(AgentAnimations.VisibilityChangedEvent evt)
            {
                view.SetVisibility(evt.Fade);
                return UniTask.CompletedTask;
            }
            
            UniTask Handler(AgentStats.HealthChangedEvent evt)
            {
                if (evt.Stats != stats) return UniTask.CompletedTask;
                var max = evt.Max > 0 ? evt.Max : 1;
                view.SetHpBar((float)evt.Current / max);
                view.gameObject.SetActive(evt.Current < evt.Max || stats.TotalShieldAbsorption > 0);
                return UniTask.CompletedTask;
            }

            UniTask ShieldHandler(AgentStats.ShieldChangedEvent evt)
            {
                if (evt.Stats != stats) return UniTask.CompletedTask;
                var max = stats.MaxHealth;
                view.SetShieldFill(max > 0 ? (float)evt.Current / max : 0f);
                view.gameObject.SetActive(stats.HealthCurrent < stats.MaxHealth || evt.Current > 0);
                return UniTask.CompletedTask;
            }

            pub.Messager.Subscribe<AgentAnimations.VisibilityChangedEvent>(Visibility);
            pub.Messager.Subscribe<AgentStats.HealthChangedEvent>(Handler);
            pub.Messager.Subscribe<AgentStats.ShieldChangedEvent>(ShieldHandler);
            unregister = () =>
            {
                pub.Messager.Unsubscribe<AgentAnimations.VisibilityChangedEvent>(Visibility);
                pub.Messager.Unsubscribe<AgentStats.HealthChangedEvent>(Handler);
                pub.Messager.Unsubscribe<AgentStats.ShieldChangedEvent>(ShieldHandler);
            };
        }

        m_StatBars[entity] = new StatBarEntry { Entity = entity, Root = rect, View = view, Unregister = unregister };
    }

    private void RemoveStatBar(Entity entity)
    {
        // 已销毁的 Entity 在 Unity 中与 null 相等，若先判 entity==null 会提前 return，字典残留导致 Update 仍访问 transform。
        if (!m_StatBars.TryGetValue(entity, out var entry)) return;
        entry.Unregister?.Invoke();
        if (entry.Root != null)
            Destroy(entry.Root.gameObject);
        m_StatBars.Remove(entity);
    }

    private class StatBarEntry
    {
        public Entity Entity;
        public RectTransform Root;
        public UIStatBar View;
        public Action Unregister;
    }
}
