using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class SetGlobalBoolEvent : EventArgs
{
    public string Name;
    public bool Value;
}

public class SetGlobalFloatEvent : EventArgs
{
    public string Name;
    public float Value;
}

public class SetGlobalIntEvent : EventArgs
{
    public string Name;
    public int Value;
}

public class GlobalParameterContext
{
    private readonly Dictionary<string, DialogueParameter> _parameters = new();

    public DialogueParameter GetParameter(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName)) return null;
        _parameters.TryGetValue(parameterName, out var p);
        return p;
    }

    private DialogueParameter GetOrCreate(string parameterName, DialogueParameterType type)
    {
        if (_parameters.TryGetValue(parameterName, out var p)) return p;
        p = new DialogueParameter { Name = parameterName, Type = type };
        _parameters[parameterName] = p;
        return p;
    }

    public void SetBool(string parameterName, bool value)
    {
        GetOrCreate(parameterName, DialogueParameterType.Bool).defaultBool = value;
    }

    public void SetFloat(string parameterName, float value)
    {
        GetOrCreate(parameterName, DialogueParameterType.Float).defaultFloat = value;
    }

    public void SetInt(string parameterName, int value)
    {
        GetOrCreate(parameterName, DialogueParameterType.Int).defaultInt = value;
    }

    public void BindPubSub(PubSub bus)
    {
        if (bus == null) return;
        bus.Subscribe<SetGlobalBoolEvent>(e =>
        {
            SetBool(e.Name, e.Value);
            return UniTask.CompletedTask;
        });
        bus.Subscribe<SetGlobalFloatEvent>(e =>
        {
            SetFloat(e.Name, e.Value);
            return UniTask.CompletedTask;
        });
        bus.Subscribe<SetGlobalIntEvent>(e =>
        {
            SetInt(e.Name, e.Value);
            return UniTask.CompletedTask;
        });
    }
}
