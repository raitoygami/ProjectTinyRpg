#if UNITY_EDITOR


using UnityEditor;
using UnityEngine;

namespace Raitoygami {

    public class VfxHeatDistortionGUI : BaseShaderGUI {
        private MaterialProperty[] _Properties;
        
        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] props) {
            base.FindProperties(props);
            // save off the list of all properties for shadergraph
            _Properties = props;
            
        }

        public override void DrawSurfaceOptions(Material material) {
            base.DrawSurfaceOptions(material);
            SetMaterialKeywords(material);
        }

        public override void DrawSurfaceInputs(Material material) {
            var CustomData = FindProperty("_CUSTOMDATA", _Properties);
            materialEditor.ShaderProperty(CustomData, "Enable Custom Data");
            
            var _DistortionTex = FindProperty("_DistortionTex", _Properties);
            materialEditor.TextureProperty(_DistortionTex, "Distortion Tex", true);

            var _DistortionVelocity = FindProperty("_DistortionVelocity", _Properties);
            materialEditor.VectorProperty(_DistortionVelocity, "Distortion Velocity");

            var _DistortionScale = FindProperty("_DistortionScale", _Properties);
            materialEditor.RangeProperty(_DistortionScale, "Distortion Scale");
            
            var _DistortionIntensity = FindProperty("_DistortionIntensity", _Properties);
            materialEditor.RangeProperty(_DistortionIntensity, "Distortion Intensity");

            if (CustomData.floatValue > 0) {
                var DistortionIntensityCustom = FindProperty("_DistortionIntensityCustom", _Properties);
                DoPopup(DistortionIntensityBind, DistortionIntensityCustom, VfxGuiUtil.CustomDataFloat);    
            }

        }
        
        public static readonly GUIContent DistortionIntensityBind = EditorGUIUtility.TrTextContent("Distortion Intensity Binding",
            "Select a binding channel of particle custom data.");
    }
}

#endif
