/// <summary>
/// 单种伤害类型的减伤计算，便于扩展新类型（真实伤害、元素、毒等）。
/// </summary>
public interface IDamageReduction
{
    /// <summary>
    /// 根据攻击方与防守方属性，计算减伤后的伤害（未含暴击）。
    /// </summary>
    /// <param name="attacker">攻击方 AgentStats（穿甲/穿抗在此）</param>
    /// <param name="defender">防守方 AgentStats（护甲/魔抗在此）</param>
    /// <param name="rawDamage">原始伤害</param>
    /// <returns>减伤后的伤害值，至少为 1（可后续改为 0 或可配置）</returns>
    int Reduce(AgentStats attacker, AgentStats defender, int rawDamage);
}
