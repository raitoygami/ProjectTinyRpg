
using System;
using UnityEngine;

public enum DialogueParameterType
{
    Float = 0,
    Bool = 1,
    Int = 2,
}

[Serializable]
public sealed class DialogueParameter
{
    [SerializeField] private float m_DefaultFloat;
    [SerializeField] private bool m_DefaultBool;
    [SerializeField] private int m_DefaultInt;
    [SerializeField] private string m_Name = string.Empty;
    [SerializeField] private DialogueParameterType m_Type;
    
    public bool defaultBool
    {
        get => m_DefaultBool;
        set => m_DefaultBool = value;
    }

    public float defaultFloat
    {
        get => m_DefaultFloat;
        set => m_DefaultFloat = value;
    }

    public int defaultInt
    {
        get => m_DefaultInt;
        set => m_DefaultInt = value;
    }

    public string Name
    {
        get => m_Name;
        set => m_Name = value;
    }

    public int nameHash => Animator.StringToHash(m_Name);

    public DialogueParameterType Type
    {
        get => m_Type;
        set => m_Type = value;
    }
    
}
