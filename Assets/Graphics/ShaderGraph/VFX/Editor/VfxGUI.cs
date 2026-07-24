#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEngine;

namespace Raitoygami {
    public class VfxGUI : BaseShaderGUI {
        private MaterialProperty[] _Properties;
        private MaterialProperty EnableCustomData;
        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] props) {
            base.FindProperties(props);
            // save off the list of all properties for shadergraph
            _Properties = props;
            EnableCustomData = FindProperty("_CUSTOMDATA", _Properties);
        }

        public override void DrawSurfaceOptions(Material material) {
            base.DrawSurfaceOptions(material);
            SetMaterialKeywords(material);
        }

        public override void DrawSurfaceInputs(Material material) {
            EditorGUILayout.LabelField("Vfx | All in 1", EditorStyles.boldLabel);
            if (GUILayout.Button("User Guide")) Application.OpenURL("https://www.google.com/");
            EditorGUILayout.Space();
            
            DrawBasic();

            // 扭曲
            DrawDistortion();

            // 溶解
            DrawDissolve();

            // Mask
            DrawMask();

            // 叠加颜色
            DrawColorAdjustment();

            // 叠加贴图
            DrawTextureAddition();

            // 菲涅尔
            DrawFresnel();
            
            // UV 动画
            DrawUVAnimation();
        }

        private const float FoldoutHeight = 32.0f;

        private static bool showBasic = true;

        private void DrawBasic() {
            var rect = GUILayoutUtility.GetRect(0f, 0);
            rect.height = showBasic ? EditorGUIUtility.singleLineHeight * 7f + FoldoutHeight : FoldoutHeight;
            EditorGUI.HelpBox(rect, "", MessageType.None);

            showBasic = VfxGuiUtil.DrawHeaderFoldout("Base", showBasic, FoldoutHeight, true);
            if (showBasic) {
                EditorGUI.indentLevel++;
                var mainTex = FindProperty("_MainTex", _Properties);
                materialEditor.TextureProperty(mainTex, "MainTex", true);

                var ClipThreshold = FindProperty("_ClipThreshold", _Properties);
                materialEditor.RangeProperty(ClipThreshold, "Clip Threshold");
                var CustomData = FindProperty("_CUSTOMDATA", _Properties);
                materialEditor.ShaderProperty(CustomData, "Enable Custom Data");
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static bool showDistortion = false;

        private void DrawDistortion() {
            var Distortion = FindProperty("_DISTORTION", _Properties);
            if (Distortion == null) {
                return;
            }

            var rect = GUILayoutUtility.GetRect(0f, 0);
            rect.height = showDistortion ? EditorGUIUtility.singleLineHeight * 10f + FoldoutHeight : FoldoutHeight;
            EditorGUI.HelpBox(rect, "", MessageType.None);

            showDistortion =
                VfxGuiUtil.DrawHeaderFoldout("扭曲", showDistortion, FoldoutHeight, Distortion.floatValue > 0);

            if (showDistortion) {
                EditorGUI.indentLevel++;

                materialEditor.ShaderProperty(Distortion, "Enable Distortion Effect");

                var DistortionTex = FindProperty("_DistortionTex", _Properties);
                materialEditor.TextureProperty(DistortionTex, "Distortion Tex");

                var DistortionIntensity = FindProperty("_DistortionIntensity", _Properties);
                materialEditor.RangeProperty(DistortionIntensity, "Distortion Intensity");

                var DistortionVelocity = FindProperty("_DistortionVelocity", _Properties);
                materialEditor.ShaderProperty(DistortionVelocity, "Distortion Velocity");

                if (EnableCustomData.floatValue > 0) {
                    var _DistortionVelocityBinding = FindProperty("_DistortionVelocityBinding", _Properties);
                    DoPopup(DistortionVelocityBinding, _DistortionVelocityBinding, VfxGuiUtil.CustomDataVector4);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static bool showDissolve = false;

        private void DrawDissolve() {
            var Dissolve = FindProperty("_DISSOLVE", _Properties);
            if (Dissolve == null) {
                return;
            }

            var rect = GUILayoutUtility.GetRect(0f, 0);
            
            rect.height = showDissolve ? EditorGUIUtility.singleLineHeight * 10f + FoldoutHeight : FoldoutHeight;
            EditorGUI.HelpBox(rect, "", MessageType.None);

            showDissolve = VfxGuiUtil.DrawHeaderFoldout("溶解", showDissolve, FoldoutHeight, Dissolve.floatValue > 0);

            if (showDissolve) {
                EditorGUI.indentLevel++;

                materialEditor.ShaderProperty(Dissolve, "Enable Dissolve Effect");

                var DissolveTex = FindProperty("_DissolveTex", _Properties);
                materialEditor.TextureProperty(DissolveTex, "Dissolve Tex");

                // 溶解阈值
                var DissolveClip = FindProperty("_DissolveClip", _Properties);
                materialEditor.RangeProperty(DissolveClip, "Dissolve Clip");
// 溶解阈值
                             
                if (EnableCustomData.floatValue > 0) {
                    var _DissolveClipBinding = FindProperty("_DissolveClipBinding", _Properties);
                    DoPopup(DissolveClipBinding, _DissolveClipBinding, VfxGuiUtil.CustomDataFloat);
                }
                
                // 溶解边缘
                var DissolveEdgeWidth = FindProperty("_DissolveEdgeWidth", _Properties);
                materialEditor.RangeProperty(DissolveEdgeWidth, "Dissolve Edge Width");

                // 溶解边缘颜色
                var DissolveEdgeColor = FindProperty("_DissolveEdgeColor", _Properties);
                materialEditor.ColorProperty(DissolveEdgeColor, "Dissolve Edge Color");

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static bool showMask = false;

        private void DrawMask() {
            var Mask = FindProperty("_MASK", _Properties);
            if (Mask == null) {
                return;
            }

            var rect = GUILayoutUtility.GetRect(0f, 0);
            rect.height = showMask ? EditorGUIUtility.singleLineHeight * 10f + FoldoutHeight : FoldoutHeight;
            EditorGUI.HelpBox(rect, "", MessageType.None);

            showMask = VfxGuiUtil.DrawHeaderFoldout("遮罩", showMask, FoldoutHeight, Mask.floatValue > 0);

            if (showMask) {
                EditorGUI.indentLevel++;

                materialEditor.ShaderProperty(Mask, "Enable Mask Effect");

                var MaskTex = FindProperty("_MaskTex", _Properties);
                materialEditor.TextureProperty(MaskTex, "Mask Tex");

                var _MaskIntensity = FindProperty("_MaskIntensity", _Properties);
                materialEditor.RangeProperty(_MaskIntensity, "Mask Intensity");
                
                var _MaskAnimation = FindProperty("_MaskAnimation", _Properties);
                materialEditor.VectorProperty(_MaskAnimation, "Velocity");
                
                if (EnableCustomData.floatValue > 0) {
                    var _MaskAnimationBinding = FindProperty("_MaskAnimationBinding", _Properties);
                    // materialEditor.VectorProperty(_UVVelocity, "Velocity");
                    DoPopup(MaskAnimationBinding, _MaskAnimationBinding, VfxGuiUtil.CustomDataVector4);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static bool showColorAdjustment = false;

        private void DrawColorAdjustment() {
            var ColorAdjustment = FindProperty("_COLORADJUSTMENT", _Properties);
            if (ColorAdjustment == null) {
                return;
            }

            var rect = GUILayoutUtility.GetRect(0f, 0);
            rect.height = showColorAdjustment ? EditorGUIUtility.singleLineHeight * 4f + FoldoutHeight : FoldoutHeight;
            EditorGUI.HelpBox(rect, "", MessageType.None);

            showColorAdjustment = VfxGuiUtil.DrawHeaderFoldout("叠加颜色", showColorAdjustment, FoldoutHeight,
                ColorAdjustment.floatValue > 0);

            if (showColorAdjustment) {
                EditorGUI.indentLevel++;

                materialEditor.ShaderProperty(ColorAdjustment, "Enable Color Additional");

                // 叠加颜色
                var ColorAddition = FindProperty("_ColorAddition", _Properties);
                materialEditor.ColorProperty(ColorAddition, "Color Additional");

                var ColorBrightness = FindProperty("_ColorBrightness", _Properties);
                materialEditor.RangeProperty(ColorBrightness, "Color Brightness");

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        //
        private static bool showTextureAddition = false;

        private void DrawTextureAddition() {
            var TextureAddition = FindProperty("_TEXTUREADDITION", _Properties);
            if (TextureAddition == null) {
                return;
            }

            var rect = GUILayoutUtility.GetRect(0f, 0);
            
            rect.height = showTextureAddition ? EditorGUIUtility.singleLineHeight * 10f
                                                + FoldoutHeight : FoldoutHeight;
            EditorGUI.HelpBox(rect, "", MessageType.None);

            showTextureAddition = VfxGuiUtil.DrawHeaderFoldout("叠加贴图", showTextureAddition, FoldoutHeight,
                TextureAddition.floatValue > 0);

            if (showTextureAddition) {
                EditorGUI.indentLevel++;

                materialEditor.ShaderProperty(TextureAddition, "Enable Color Additional");

                // 混合模式
                var BlendMode = FindProperty("_ADDITIONBLENDMODE", _Properties);
                materialEditor.ShaderProperty(BlendMode, "Blend Mode");

                var AdditionTex = FindProperty("_AdditionTex", _Properties);
                materialEditor.TextureProperty(AdditionTex, "Addition Tex");
                var AdditionTexColor = FindProperty("_AdditionTexColor", _Properties);
                materialEditor.ColorProperty(AdditionTexColor, "Addition Color");

                var AdditionTexIntensity = FindProperty("_AdditionTexIntensity", _Properties);
                materialEditor.RangeProperty(AdditionTexIntensity, "Intensity");

             
                if (EnableCustomData.floatValue > 0) {
                    var _AdditionTexIntensityBinding = FindProperty("_AdditionTexIntensityBinding", _Properties);
                    DoPopup(AdditionTexIntensityBinding, _AdditionTexIntensityBinding, VfxGuiUtil.CustomDataFloat);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static bool showFresnel = false;

        private void DrawFresnel() {
            var Fresnel = FindProperty("_FRESNEL", _Properties);
            if (Fresnel == null) {
                return;
            }

            var rect = GUILayoutUtility.GetRect(0f, 0);
            rect.height = showFresnel ? EditorGUIUtility.singleLineHeight * 5f + FoldoutHeight : FoldoutHeight;
            EditorGUI.HelpBox(rect, "", MessageType.None);

            showFresnel = VfxGuiUtil.DrawHeaderFoldout("菲涅尔", showFresnel, FoldoutHeight, Fresnel.floatValue > 0);

            if (showFresnel) {
                EditorGUI.indentLevel++;

                materialEditor.ShaderProperty(Fresnel, "Enable Fresnel");

                // 混合模式
                var FresnelRange = FindProperty("_FresnelRange", _Properties);
                materialEditor.RangeProperty(FresnelRange, "Fresnel Range");

                var FresnelIntensity = FindProperty("_FresnelIntensity", _Properties);
                materialEditor.RangeProperty(FresnelIntensity, "Fresnel Intensity");

                var FresnelTexColor = FindProperty("_FresnelColor", _Properties);
                materialEditor.ColorProperty(FresnelTexColor, "Fresnel Color");

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static bool showUVAnimation = false;

        private void DrawUVAnimation() {

            var UVAnimation = FindProperty("_UVANIMATION", _Properties);
            if (UVAnimation == null) {
                return;
            }

            var offset = EnableCustomData.floatValue > 0 ? 1f : 0;
            var rect = GUILayoutUtility.GetRect(0f, 0);
            rect.height = showUVAnimation ? 
                EditorGUIUtility.singleLineHeight * 5f 
                + FoldoutHeight 
                + offset
                : FoldoutHeight;
            EditorGUI.HelpBox(rect, "", MessageType.None);

            showUVAnimation = VfxGuiUtil.DrawHeaderFoldout("UV滚动", showUVAnimation, FoldoutHeight, UVAnimation.floatValue > 0);

            if (showUVAnimation) {
                EditorGUI.indentLevel++;

                materialEditor.ShaderProperty(UVAnimation, "Enable UVAnimation");

                var _UVVelocity = FindProperty("_UVVelocity", _Properties);
                materialEditor.VectorProperty(_UVVelocity, "Velocity");
                if (EnableCustomData.floatValue > 0) {
                    var _UVVelocityBinding = FindProperty("_UVVelocityBinding", _Properties);
                    // materialEditor.VectorProperty(_UVVelocity, "Velocity");
                    DoPopup(UVVelocityBinding, _UVVelocityBinding, VfxGuiUtil.CustomDataVector4);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        public static readonly GUIContent MaskAnimationBinding = EditorGUIUtility.TrTextContent("Velocity Binding",
            "Select a binding channel of particle custom data.");
        public static readonly GUIContent UVVelocityBinding = EditorGUIUtility.TrTextContent("Velocity Binding",
            "Select a binding channel of particle custom data.");
        
        public static readonly GUIContent AdditionTexIntensityBinding = EditorGUIUtility.TrTextContent("Intensity Binding",
            "Select a binding channel of particle custom data.");
        public static readonly GUIContent DissolveClipBinding = EditorGUIUtility.TrTextContent("Dissolve Clip Binding",
                    "Select a binding channel of particle custom data.");
        
        public static readonly GUIContent DistortionVelocityBinding = EditorGUIUtility.TrTextContent("Distortion Velocity Binding",
            "Select a binding channel of particle custom data.");
        // 
        // _FRESNEL
    }
}

#endif