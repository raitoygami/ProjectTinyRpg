using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JSAM;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : Entity
{
    [Header("存档唯一ID（不同sceneName可以重复）最终id为 uniqueID_posx_posy")]
    public string uniqueID;
    
    [SerializeField] private GameObject _doorOpened;
    [SerializeField] private GameObject _doorClosed;
    [SerializeField] private SoundFileObject _doorOpenedSound;
    [SerializeField] private List<SpriteRenderer> _FogRelateds;
    private bool _isOpen;
    protected void Awake()
    {
        Faction = EntityFaction.Neutral;
        this.Subscribe<AgentInteractive.InteractionEvent>(OnInteraction);
        this.AddComponent<AgentInteractive>();
    }

    protected override bool IsBlockVision()
    {
        return !_isOpen;
    }

    private async UniTask OnInteraction(AgentInteractive.InteractionEvent arg)
    {
        var sceneName = SceneManager.GetActiveScene().name;
        var entityName = $"{uniqueID}_{transform.position.x}_{transform.position.y}_{transform.position.z}";
        if (!MapManager.Instance.SetEntityStatData(sceneName, entityName, new EntityStatDoor { IsOpen = true }))
            return;
        _isOpen = true;
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
        foreach (var fog in _FogRelateds)
        {
            fog.gameObject.SetActive(false);
        }
      
        PathFinder.Instance.ClearLogical(this);
        await UniTask.CompletedTask;
    }
    
    public override void InitAfterLevelLoad()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        
        var entityName = $"{uniqueID}_{transform.position.x}_{transform.position.y}_{transform.position.z}";
        var entityStatData = MapManager.Instance.GetEntityStatData(sceneName, entityName);
        
        var isOpen = (entityStatData as EntityStatDoor)?.IsOpen ?? false;
        _isOpen = isOpen;
        if (!_isOpen)
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
        // 更新门的状态
        _doorClosed.SetActive(!_isOpen);
        _doorOpened.SetActive(_isOpen);
        foreach (var fog in _FogRelateds)
        {
            fog.gameObject.SetActive(!_isOpen);
        }
    }
}
