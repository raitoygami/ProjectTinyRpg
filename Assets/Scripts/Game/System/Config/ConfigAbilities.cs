using cfg;

public partial class ConfigManager
{
    public t_Ability GetAbility(int id)
    {
        return Tables?.DataAbility.GetOrDefault(id);
    }
}
