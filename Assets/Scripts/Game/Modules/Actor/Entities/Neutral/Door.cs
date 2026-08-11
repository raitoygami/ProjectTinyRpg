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

    private async UniTask Close()
    {
        var cell = PathFinder.Instance.GetCell(GridPosition.x, GridPosition.y);
        var logical = cell?.Logical;
        if (logical != null)
        {
            AudioManager.PlaySound(GameAudioSounds.Sfx_Common_Denied);
            return;
        }
        
        var sceneName = PlayerManager.GetSceneName();
        var entityName = $"{uniqueID}_{transform.position.x}_{transform.position.y}_{transform.position.z}";
        if (!MapManager.Instance.SetEntityStatData(sceneName, entityName, new EntityStatDoor { IsOpen = false }))
            return;

        _isOpen = false;
        
        _doorClosed.SetActive(true);
        _doorClosed.GetComponent<IInteractable>().OnHoverExit();
        _doorOpened.SetActive(false);
        AudioManager.PlaySound(GameAudioSounds.Sfx_Common_DoorClose);
        
        UpdateCell();
        
        await this.PublishGlobal(Context.FOVDirty);
    }

    
    private async UniTask Open()
    {
        var sceneName = PlayerManager.GetSceneName();
        var entityName = $"{uniqueID}_{transform.position.x}_{transform.position.y}_{transform.position.z}";
        if (!MapManager.Instance.SetEntityStatData(sceneName, entityName, new EntityStatDoor { IsOpen = true }))
            return;
        
        _isOpen = true;
        _doorClosed.SetActive(false);
        _doorOpened.SetActive(true);
        _doorOpened.GetComponent<IInteractable>().OnHoverExit();
        AudioManager.PlaySound(GameAudioSounds.Sfx_Common_DoorOpen);
        
        PathFinder.Instance.ClearLogical(this);

        await this.PublishGlobal(Context.FOVDirty);
        
        await UniTask.CompletedTask;
        
    }
    
    public void Interact(bool open)
    {
        if (open)
            Open().Forget();
        else
            Close().Forget();
    }
    
    private async UniTask OnInteraction(AgentInteractive.InteractionEvent arg)
    {
        await Open();
    }

    private void UpdateCell()
    {
        var gridPosition = transform.position.SnapToGrid();
        transform.position = gridPosition.GridToWorld();
        
        if (GridSizeX < 1) GridSizeX = 1;
        if (GridSizeZ < 1) GridSizeZ = 1;

        X = (int)gridPosition.x;
        Y = (int)gridPosition.y;
        
        Layer = 1 << gameObject.layer;
        if (_isOpen) return;
        PathFinder.Instance.UpdateCell(X, Y, this);
    }
    
    public override void InitAfterLevelLoad()
    {
        var sceneName = PlayerManager.GetSceneName();
        
        var entityName = $"{uniqueID}_{transform.position.x}_{transform.position.y}_{transform.position.z}";
        var entityStatData = MapManager.Instance.GetEntityStatData(sceneName, entityName);
        
        var isOpen = (entityStatData as EntityStatDoor)?.IsOpen ?? false;
        _isOpen = isOpen;
        UpdateCell();
        // 更新门的状态
        _doorClosed.SetActive(!_isOpen);
        _doorOpened.SetActive(_isOpen);
    }
}
