using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AgentInteractive : MonoBehaviour
{
    
    public class InteractionEvent : EventArgs
    {
        public GameObject gameObject;
    }
    
    public bool Interactable(PathNode t_Target)
    {
        var cell = PathFinder.Instance.GetCell(t_Target.X, t_Target.Y);

        if (cell?.Logical == null) return false;
        
        var go = (cell.Logical as Component)?.gameObject;
        if (go == null) return false;

        return go.GetComponent<AgentInteractive>() != null;

    }

    public async UniTask Interact(PathNode t_Target)
    {
        var cell = PathFinder.Instance.GetCell(t_Target.X, t_Target.Y);

        if (cell?.Logical == null) return;
        
        var go = (cell.Logical as Component)?.gameObject;
        if (go == null) return;

        var interactable = go.GetComponent<AgentInteractive>();
        
        _Interactable = false;
        await interactable.ReceiveInteraction(gameObject);
        _Interactable = true;

        //await UniTask.DelayFrame(1);
        
    }

    private bool _Interactable = true;
    
    public async UniTask ReceiveInteraction(GameObject t_Publisher)
    {
        if (_Interactable)
        {
            _Interactable = false;
            await this.Publish(new InteractionEvent
            {
                gameObject = t_Publisher
            }, sequential: true);
            _Interactable = true;
        }
    }
    
}