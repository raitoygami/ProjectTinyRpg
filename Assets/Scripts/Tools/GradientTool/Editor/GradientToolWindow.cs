using System;

using System.Collections.Generic;

using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;

using Application = UnityEngine.Application;

namespace Raitoygami.Tool {

    public class GradientToolWindow : EditorWindow {
        private List<PresetGradient> _Gradients;
        private ESaveMode _SaveMode;
        private string _LastSavePath;
        private PresetGradientLibrary _presetLibrary;
        public enum ESaveMode {
            Split =  1,
            Pack = 2,
            PackSelected = 3,
        }
        
        [MenuItem("Raitoygami/Tools/Gradient Tool(色阶图生成器)")]
        public static void GradientTool() {
            GetWindow(typeof(GradientToolWindow), true, "Gradient Tool", false).Show();
        }

        private void OnEnable() {
            minSize = new Vector2(400, 280);
            maxSize = new Vector2(400, 280);
            
            _Gradients = new List<PresetGradient>();
            _presetLibrary = CreateInstance<PresetGradientLibrary>();
            
            for (int i = 0; i < 10; i++) {
                var gradient = new PresetGradient(i, false);
                _Gradients.Add(gradient);
                _presetLibrary.Add(gradient.Value);
            }

            _SaveMode = ESaveMode.Split;
        }

        private void OnGUI() {
            GUILayout.Box(new GUIContent(), GUILayout.ExpandWidth(true), GUILayout.Height(2));

            EditorGUILayout.BeginVertical();

            foreach (var gradient in _Gradients) {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.GradientField(gradient.Value);

                gradient.Enabled = GUILayout.Toggle(gradient.Enabled, gradient.Enabled ? "o" : "-");

                if (GUILayout.Button("Save")) {
                    SaveSplit(gradient);
                }

                EditorGUILayout.EndHorizontal();
            }

            
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Select All")) {
                    foreach (var gradient in _Gradients) {
                        gradient.Enabled = true;
                    }
                }

                if (GUILayout.Button("DeSelect All")) {
                    foreach (var gradient in _Gradients) {
                        gradient.Enabled = false;
                    }
                }
                
                if (GUILayout.Button("Load Preset")) {
                    LoadPreset();
                }

                if (GUILayout.Button("New Preset")) {
                    NewPreset();
                }
                
                _SaveMode = (ESaveMode) EditorGUILayout.EnumPopup(_SaveMode);
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Save", GUILayout.Height(20))) {
                SaveGradients();
            }
            EditorGUILayout.EndVertical();
            GUILayout.Box(new GUIContent(), GUILayout.ExpandWidth(true), GUILayout.Height(2));
        }

        private void SaveGradients() {
            switch (_SaveMode) {
                case ESaveMode.Split:
                    SaveGradientSplit();
                    break;
                case ESaveMode.Pack:
                    SaveGradientPacked(true);
                    break;
                case ESaveMode.PackSelected:
                    SaveGradientPacked(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SaveGradientSplit() {
            var path = EditorUtility.SaveFilePanel("Save", _LastSavePath, "","png");
            if (!string.IsNullOrEmpty(path)) {
                _LastSavePath = Path.GetDirectoryName(path);
                var fileName = Path.GetFileNameWithoutExtension(path);
                foreach (var gradient in _Gradients.Where(gradient => gradient.Enabled)) {
                    Save(gradient, _LastSavePath + "/" +fileName + gradient.Index + ".png");
                }
            }
        }
        
        private void SaveSplit(PresetGradient gradient) {
            if (gradient.Enabled) {
                var path = EditorUtility.SaveFilePanel("Save", _LastSavePath, "","png");
                Save(gradient, path);
            }
        }

        private void Save(PresetGradient gradient, string path) {
            if (!string.IsNullOrEmpty(path)) {
                var texture = gradient.GetTexture(256, 1);
                var bytes = texture.EncodeToPNG();
                if (bytes != null) {
                    File.WriteAllBytes(path, bytes);
                    AssetDatabase.Refresh();
                }
                
                _LastSavePath = Path.GetDirectoryName(path);
            }
        }
        
        private void SaveGradientPacked(bool bPackAll) {
            var path = EditorUtility.SaveFilePanel("Save", _LastSavePath, "","png");
            if (!GetRelativePath(ref path)) return;
            
            int height = _Gradients.Count(gradient => (bPackAll || gradient.Enabled) ) * 2;
            var texture = new Texture2D(256 , height, TextureFormat.RGB24, false);
                
            int index = 1;
            foreach (var gradient in _Gradients.Where(gradient => (bPackAll || gradient.Enabled) )) {
                    
                var t = gradient.GetTexture(256, 1);
                for (int i = 0; i < 256; i++) {
                    //(height - 1) 19 - 
                    texture.SetPixel(i, height - index * 2, t.GetPixel(i, 1));
                    texture.SetPixel(i, height - index * 2 + 1, t.GetPixel(i, 1));
                }

                index++;
            }
            
            texture.Apply();
            
            var bytes = texture.EncodeToPNG();
            if (bytes != null) {
                File.WriteAllBytes(path, bytes);
                AssetDatabase.Refresh();
            }
            
            _LastSavePath = Path.GetDirectoryName(path);
        }

        private bool GetRelativePath(ref string path) {
            if (string.IsNullOrEmpty(path)) return false;
            
            if (!path.StartsWith(Application.dataPath)) 
                return false;
            
            path = "Assets" + path[Application.dataPath.Length..];
            return true;
        }
        
        private void LoadPreset() {
            var path = EditorUtility.OpenFilePanel("Open", _LastSavePath, "asset");
            if (!GetRelativePath(ref path)) return;

            _presetLibrary = AssetDatabase.LoadAssetAtPath<PresetGradientLibrary>(path);
            for (int i = 0; i < _Gradients.Count; i++) {
                _Gradients[i].UpdateGradient(_presetLibrary.Get(i));
            }
            
            titleContent = new GUIContent("Gradient Tool:" + path);

        }

        private void NewPreset() {
            var path = EditorUtility.SaveFilePanel("Save", _LastSavePath, "","asset");
            
            if (!GetRelativePath(ref path)) return;
            
            _presetLibrary = CreateInstance<PresetGradientLibrary>();
            
            foreach (var gradient in _Gradients) {
                _presetLibrary.Add(gradient.Value);
            }
            
            AssetDatabase.CreateAsset(_presetLibrary, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = _presetLibrary;
            // 重新加载
            for (int i = 0; i < _Gradients.Count; i++) {
                _Gradients[i].UpdateGradient(_presetLibrary.Get(i));
            }
            
            titleContent = new GUIContent("Gradient Tool:" + path);
        }
    }
}