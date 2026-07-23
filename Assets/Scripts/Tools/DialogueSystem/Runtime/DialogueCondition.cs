using System;
using UnityEngine;

[Serializable]
public enum DialogueConditionMode
{
    True = 1,
    False = 2,
    Greater = 3,
    Less = 4,
    // Equals = 5,
    // NotEqual = 6
}

[Serializable]
public struct DialogueCondition
{
    [SerializeField] private DialogueConditionMode m_ConditionMode;
    [SerializeField] private string m_ConditionEvent;
    [SerializeField] private float m_EventTreshold;
    
    public DialogueConditionMode Mode
    {
        get => m_ConditionMode;
        set => m_ConditionMode = value;
    }
    public string Parameter
    {
        get => m_ConditionEvent;
        set => m_ConditionEvent = value;
    }
    public float Threshold
    {
        get => m_EventTreshold;
        set => m_EventTreshold = value;
    }
}