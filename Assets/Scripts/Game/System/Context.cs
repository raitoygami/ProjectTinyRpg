using System;

public class Context : Singleton<Context>
{
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