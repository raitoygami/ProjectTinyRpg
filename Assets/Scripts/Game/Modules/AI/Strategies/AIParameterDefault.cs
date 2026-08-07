using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "AIParameterDefault", menuName = "Config/AI Parameter/Default")]
public class AIParameterDefault : AIParameter
{
    public int VisionRange;
    public int ThreatTime;
}
