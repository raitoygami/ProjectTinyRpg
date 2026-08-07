using UnityEngine;

[CreateAssetMenu(menuName = "Config/Scriptable Container", fileName = "ScriptableContainer", order = 0)]
public class ScriptableContainer : ScriptableObject
{
    public CustomizationTable CustomizationTable;
    public AIParameterTable AIParameterTable;
}
