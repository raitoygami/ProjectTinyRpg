using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AIParameter", menuName = "Config/AI Parameter Table")]
public class AIParameterTable : ScriptableObject
{
    [Serializable]
    public class AIParameterData
    {
        public int ID;
        public AIPattern Pattern;
        [SerializeField]
        public AIParameter Parameter;
    }
    
    public List<AIParameterData> Parameters = new();
    
    private Dictionary<int, AIParameterData> _data;

    // 在游戏启动时调用一次（比如在 GameManager 里）
    public void Initialize()
    {
        _data = new Dictionary<int, AIParameterData>();
        foreach (var parameter in Parameters)
        {
            _data.Add(parameter.ID, parameter);
        }
    }

    public AIParameterData GetData(int id)
    {
        if (_data == null)
            Initialize();
        
        return _data != null && _data.TryGetValue(id, out var parameter) ? parameter : null;        
    }
    
}
