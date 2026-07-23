using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameobjectUtil{
    public const string s_Layer_Player = "Player";
    
    public static void UpdateLayer(GameObject go, string layer){
        foreach (Transform t in go.transform){
            t.gameObject.layer = LayerMask.NameToLayer(layer);
        }
    }
}
