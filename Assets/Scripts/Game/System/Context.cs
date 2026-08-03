using System;

public class Context : Singleton<Context>
{
    // avatar更新完毕后， 推送消息更新ui界面
    public class AvatarChangedEvt : EventArgs
    {
        
    }
    public static readonly AvatarChangedEvt AvatarChanged = new();
    
    // 装备发生变化的时候调用
    // UI刷新EquipmentPanel
    // Player.刷新AgentWeapon
    public class EquipmentUpdateEvt : EventArgs
    {
        
    }
    public static readonly EquipmentUpdateEvt  EquipmentUpdate = new();


    public class PlayerStatsChangeEvt : EventArgs
    {
        
    }
    public static readonly PlayerStatsChangeEvt PlayerStatsChange = new();
    
    public class PlayerInitEvt : EventArgs
    {
        
    }
    private PlayerInitEvt _PlayerInitEvt = new();

    public class PlayerMoveFinishEvt : EventArgs
    {
        
    }
    
    public Player PlayerInst { get; private set; }
    public GlobalParameterContext GlobalParameters { get; } = new();

    private void Awake()
    {
        GlobalParameters.BindPubSub(Messager);
    }

    public void SetPlayer(Player t_Player)
    {
        PlayerInst = t_Player;
        if (t_Player != null)
            this.Publish(_PlayerInitEvt);
    }
    
    
}