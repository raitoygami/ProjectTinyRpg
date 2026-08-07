using UnityEngine;

[CreateAssetMenu(fileName = "AIParameterDefault", menuName = "Config/AI Parameter/Summon")]
public class AIParameterSummon : AIParameter
{
    public int VisionRange;
    public int FollowDistance;
}
