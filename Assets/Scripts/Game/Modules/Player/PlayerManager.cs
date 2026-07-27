public partial class PlayerManager : Singleton<PlayerManager>
{
    //  根据存档数据 构建信息
    public void RebuildPersist()
    {
        RebuildInventory();
    }


    
}
