using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MainUI : MonoBehaviour
{
    public class RollEvt : EventArgs
    {
        public Vector3 Direction;
        public float Duration;
    }
    
    [SerializeField]
    private List<Transform> _slots = new();
    
    [SerializeField]
    private List<Transform> _equips = new();

    private void Awake()
    {
        /*this.SubscribeGlobal<Context.PlayerInitEvt>(OnPlayerInit);
        this.SubscribeGlobal<Context.PlayerMoveFinishEvt>(OnPlayerMoveFinish);*/
    }

    /*private string[] dir = new[]
    {
        "右",
        "下",
        "后",
        "前",
        "上",
        "左",
    };*/

    private UniTask OnPlayerInit(Context.PlayerInitEvt arg)
    {
        UpdateEquips().Forget();
        return UniTask.CompletedTask;
    }
    
    private async UniTask OnPlayerMoveFinish(Context.PlayerMoveFinishEvt arg)
    {
        await UpdateEquips();
    }

    private async UniTask UpdateEquips()
    {
        var player = Context.Instance.PlayerInst;
        var agentAnimations = player.GetComponent<AgentAnimations>();
        for (var i = 0; i <= 5; i++)
        {
            _equips[agentAnimations.GetIndex(i)].SetParent(_slots[i], false);
        }

        /*
        Debug.Log(agentAnimations.GetUpFaceAfterMove(Vector3.forward) + 1);
        Debug.Log(agentAnimations.GetUpFaceAfterMove(Vector3.back) + 1);
        Debug.Log(agentAnimations.GetUpFaceAfterMove(Vector3.left) + 1);
        Debug.Log(agentAnimations.GetUpFaceAfterMove(Vector3.right) + 1);
        */
        
        
        await UniTask.CompletedTask;
    }
    

    private void OnEnable()
    {
        if (!this.HasSubscription<RollEvt>())
        {
            this.SubscribeGlobal<RollEvt>(OnRollStart);
        }

    }

    private UniTask OnRollStart(RollEvt arg)
    {
        return UniTask.CompletedTask;
    }

    public void OnBtnClickTitle()
    {
        Game.Instance.ExitToTitle().Forget();
    }
    
}
