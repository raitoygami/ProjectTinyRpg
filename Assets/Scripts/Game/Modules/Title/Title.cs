using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class Title : PubSubActor
{
    public void Awake()
    {
        this.SubscribeInput<InputSystem.MouseClickEvt>(OnMouseClick);
        Debug.Log("OnAwake");
    }
    
    private static UniTask OnMouseClick(InputSystem.MouseClickEvt arg)
    {
        Game.Instance.LoadGame().Forget();
        return UniTask.CompletedTask;
    }


}
