using UnityEngine;
using UnityEngine.Tilemaps;

public class MapLayers : MonoBehaviour
{
    public Tilemap LayerBlocks;
    public Transform BlockProps;
    public GameObject ActiveAfterLoad;
    public BoxCollider2D CameraBound;
}
