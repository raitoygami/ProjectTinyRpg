using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class Title : PubSubActor
{
    public void Awake()
    {
        this.SubscribeInput<InputManager.MouseClickEvt>(OnMouseClick);
        Debug.Log("OnAwake");
    }
    
    private static UniTask OnMouseClick(InputManager.MouseClickEvt arg)
    {
        Game.Instance.LoadGame().Forget();
        return UniTask.CompletedTask;
    }


}
