using UnityEngine;

namespace Raitoygami.Tool {
    using UnityEditor;
    
    [CustomEditor(typeof(GradientTool))]
 
    public class GradientToolEditor : Editor {
 
        public override void OnInspectorGUI() {
 
            DrawDefaultInspector();
 
            var gradientTool = (GradientTool)target;
 
            if(GUILayout.Button("生成Ramp贴图")) {
 
                gradientTool.GenerateRampTexture();
            }
        }
    }
}

