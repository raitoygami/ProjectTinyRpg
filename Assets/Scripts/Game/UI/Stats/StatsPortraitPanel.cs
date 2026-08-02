using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class StatsPortraitPanel : MonoBehaviour
{
    [SerializeField] private Image _AvatarPlayer;

    private void Awake()
    {
        this.SubscribeGlobal<Context.AvatarChangedEvt>(OnAvatarChanged);
    }

    // 初始化的时候更新面板
    private void Start()
    {
        RefreshAvatar();
    }
    
    private UniTask OnAvatarChanged(Context.AvatarChangedEvt arg)
    {
        RefreshAvatar();
        return UniTask.CompletedTask;
    }
    
    private void RefreshAvatar()
    {
        // 在这里更新
        if (!Context.HasInstance() || Context.Instance.PlayerInst == null) return;
        var customization = Context.Instance.PlayerInst.GetComponent<AgentCustomization>();
        if (customization == null) return;
        _AvatarPlayer.sprite = customization.GetCombinedSprite();
    }
}
