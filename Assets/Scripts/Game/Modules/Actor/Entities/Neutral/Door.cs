using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class Door : Entity
{
    [Header("存档唯一ID（必须全局唯一！）")]
    public string uniqueID;
    
    [SerializeField] private GameObject _doorOpened;
    [SerializeField] private GameObject _doorClosed;

    [SerializeField] private List<SpriteRenderer> _FogRelateds;
    
    protected void Awake()
    {
        uniqueID = $"{gameObject.scene.name}_{name}_{transform.position.x}_{transform.position.y}_{transform.position.z}";
        Faction = EntityFaction.Neutral;
        this.Subscribe<AgentInteractive.InteractionEvent>(OnInteraction);
        this.AddComponent<AgentInteractive>();
        _doorClosed.SetActive(true);
        _doorOpened.SetActive(false);
    }
    
    private async UniTask OnInteraction(AgentInteractive.InteractionEvent arg)
    {
        _doorClosed.SetActive(false);
        _doorOpened.SetActive(true);
        var  tasks = Enumerable.Select(_FogRelateds, related => 
                DOTween.To(() => related.color, // getter
                    c => related.color = c, // setter
                    Color.clear, // 目标颜色
                    0.5f // 时长
                )
                .SetEase(Ease.OutExpo)
                .ToUniTask())
            .ToList();
        await UniTask.WhenAll(tasks);
        
        PathFinder.Instance.ClearLogical(this);
        await UniTask.CompletedTask;
    }

}
