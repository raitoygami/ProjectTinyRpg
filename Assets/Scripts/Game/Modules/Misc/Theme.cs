using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Theme 
{
#if UNITY_EDITOR
    public static class EditorUtil
    {
        public static readonly GUIStyle SectionNameStyle = new() {
            fontStyle = FontStyle.Bold,
            // fontSize = 40,
            normal = {
                textColor = Color.green,
                background = Texture2D.redTexture,
            },
            alignment = TextAnchor.MiddleLeft,
        };
        public static readonly GUIStyle PropertyNameStyle = new() {
            fontStyle = FontStyle.Bold,
            // fontSize = 40,
            /*normal = {
                textColor = Color.green,
                background = Texture2D.redTexture,
            },*/
            alignment = TextAnchor.MiddleLeft,
        };
    }
#endif
}
