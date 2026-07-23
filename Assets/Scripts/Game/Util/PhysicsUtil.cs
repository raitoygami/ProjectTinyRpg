/*
using UnityEngine;

public static class PhysicsUtil
{
    private const int MAX_ALLOC_COUNT = 32;
    public static readonly Collider[] m_Colliders = new Collider[MAX_ALLOC_COUNT];
    
    // public static T[] GetNeighbors<T>(Vector3 position, Vector3 size, Quaternion rotation, LayerMask buildingLayer, 
    //     QueryTriggerInteraction query = QueryTriggerInteraction.UseGlobal)
    // {
    //     var defaultQueriesHitTriggers = Physics.queriesHitTriggers;
    //     Physics.queriesHitTriggers = true;
    //
    //     int colliderCount = Physics.OverlapBoxNonAlloc(position, size, m_Colliders, rotation, buildingLayer, query);
    //
    //     Physics.queriesHitTriggers = defaultQueriesHitTriggers;
    //
    //     var types = new T[colliderCount];
    //
    //     for (int i = 0; i < colliderCount; i++) {
    //         if (m_Colliders[i].isTrigger) continue;
    //         var type = m_Colliders[i].GetComponentInParent<T>();
    //
    //         if (type == null) continue;
    //         types[i] = type;
    //     }
    //     return types;
    // }

    public static int GetNeighbors(Vector3 position, Vector3 size, Quaternion rotation, LayerMask layer,
        QueryTriggerInteraction query = QueryTriggerInteraction.UseGlobal) {
        
        var defaultQueriesHitTriggers = Physics.queriesHitTriggers;
        Physics.queriesHitTriggers = true;

        int colliderCount = Physics.OverlapBoxNonAlloc(position, size, m_Colliders, rotation, layer, query);

        Physics.queriesHitTriggers = defaultQueriesHitTriggers;

        return colliderCount;
    }

    public static Collider GetNeighbor(int index) {
        return m_Colliders[index];
    }
    
    // public static RaycastHit hit
    
    public static int Cast(Vector3 position, Vector3 size, Quaternion rotation, LayerMask layer,
        QueryTriggerInteraction query = QueryTriggerInteraction.UseGlobal) {
        
        var defaultQueriesHitTriggers = Physics.queriesHitTriggers;
        Physics.queriesHitTriggers = true;

        int colliderCount = Physics.OverlapBoxNonAlloc(position, size, m_Colliders, rotation, layer, query);

        Physics.queriesHitTriggers = defaultQueriesHitTriggers;

        return colliderCount;
    }
    
    public static Collider GetTarget(int index) {
        return m_Colliders[index];
    }
    
}
*/
