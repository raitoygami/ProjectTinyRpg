using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using UnityEditor;
using UnityEngine;


public class GradientTool : MonoBehaviour
{
#if UNITY_EDITOR
    public Gradient ShadowColor1= new();
    public Gradient ShadowColor2= new();
    public Gradient ShadowColor3= new();
    public Gradient ShadowColor4= new();
    public Gradient ShadowColor5= new();
    
    public Gradient RimColor1= new();
    public Gradient RimColor2= new();
    public Gradient RimColor3= new();
    public Gradient RimColor4= new();
    public Gradient RimColor5 = new();
    
    private Texture2D _GradientTexture;

    public Material _TargetMaterial;
    private static readonly int ShadowRampTex = Shader.PropertyToID("_ShadowRampTex");

    private void OnValidate() {
        UpdateTexture();
    }

    public void UpdateTexture() {
        if (_GradientTexture == null) {
            _GradientTexture = new Texture2D(256, 20, TextureFormat.RGB24, false);
        }

        for (int w = 0; w < 256; w++) {
            for (int h = 0; h < 2; h++) {
                _GradientTexture.SetPixel(w,h, RimColor5.Evaluate((float)w / 256));
            }
            for (int h = 2; h < 4; h++) {
                _GradientTexture.SetPixel(w,h, RimColor4.Evaluate((float)w / 256));
            }
            for (int h = 4; h < 6; h++) {
                _GradientTexture.SetPixel(w,h, RimColor3.Evaluate((float)w / 256));
            }
            for (int h = 6; h < 8; h++) {
                _GradientTexture.SetPixel(w,h, RimColor2.Evaluate((float)w / 256));
            }
            for (int h = 8; h < 10; h++) {
                _GradientTexture.SetPixel(w,h, RimColor1.Evaluate((float)w / 256));
            }
            
            for (int h = 10; h < 12; h++) {
                _GradientTexture.SetPixel(w,h, ShadowColor5.Evaluate((float)w / 256));
            }
            for (int h = 12; h < 14; h++) {
                _GradientTexture.SetPixel(w,h, ShadowColor4.Evaluate((float)w / 256));
            }
            for (int h = 14; h < 16; h++) {
                _GradientTexture.SetPixel(w,h, ShadowColor3.Evaluate((float)w / 256));
            }
            for (int h = 16; h < 18; h++) {
                _GradientTexture.SetPixel(w,h, ShadowColor2.Evaluate((float)w / 256));
            }
            for (int h = 18; h < 20; h++) {
                _GradientTexture.SetPixel(w,h, ShadowColor1.Evaluate((float)w / 256));
            }
            
        }

        
        
        _GradientTexture.Apply();
        _GradientTexture.filterMode = FilterMode.Bilinear;
        _GradientTexture.wrapMode = TextureWrapMode.Clamp;
        _GradientTexture.requestedMipmapLevel = 1;
        UpdateMaterial();
    }

    public void UpdateMaterial() {
        if (_TargetMaterial) {
            _TargetMaterial.SetTexture(ShadowRampTex, _GradientTexture);    
        }
    }

    private string _LastSavePath = Application.dataPath;
    public void GenerateRampTexture() {
        var path = EditorUtility.SaveFilePanel("Save", _LastSavePath, "","png");
        if (!GetRelativePath(ref path)) return;
        if (_GradientTexture == null) return;
        
        var bytes = _GradientTexture.EncodeToPNG();
        if (bytes != null) {
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.requestedMipmapLevel = 0;

        if (_TargetMaterial) {
            _TargetMaterial.SetTexture(ShadowRampTex, texture);
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
#endif

}

