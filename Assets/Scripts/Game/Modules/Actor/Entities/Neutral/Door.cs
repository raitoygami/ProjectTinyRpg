using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JSAM;
using Unity.VisualScripting;
using UnityEngine;

public class Door : Entity
{
    [Header("存档唯一ID（必须全局唯一！）")]
    public string uniqueID;
    
    [SerializeField] private GameObject _doorOpened;
    [SerializeField] private GameObject _doorClosed;
    [SerializeField] private SoundFileObject _doorOpenedSound;
    [SerializeField] private List<SpriteRenderer> _FogRelateds;

#if UNITY_EDITOR
    [ContextMenu("Generate UID")]
    private void GenerateUID()
    {
        // 使用 GUID 确保绝对唯一，且不受位置/名称变化影响
        uniqueID = $"{gameObject.scene.name}_{gameObject.name}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        UnityEditor.EditorUtility.SetDirty(this); // 标记修改
    }
#endif
    
    protected void Awake()
    {
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
        if (_doorOpenedSound != null)
            AudioManager.PlaySound(_doorOpenedSound);
        
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
    
    public override void InitAfterLevelLoad()
    {
        var gridPosition = transform.position.SnapToGrid();
        transform.position = gridPosition.GridToWorld();

        X = (int)gridPosition.x;
        Y = (int)gridPosition.y;
        if (GridSizeX < 1) GridSizeX = 1;
        if (GridSizeZ < 1) GridSizeZ = 1;

        Layer = 1 << gameObject.layer;
        PathFinder.Instance.UpdateCell(X, Y, this);
    }
}
