using System;
using UnityEngine;

namespace Raitoygami.Tool {
    public sealed class PresetGradient {
        public Gradient Value;
        
        [NonSerialized]
        public bool Enabled;
        [NonSerialized]
        public readonly int Index; 
        public PresetGradient(int index, bool enabled) {
            Index = index;
            Enabled = enabled;
            Value = new Gradient();
        }

        public void UpdateGradient(Gradient gradient) {
            Value = gradient;
        }
        
        public Texture2D GetTexture(int width, int height) {
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            for (int h = 0; h < height; h++) {
                for (int w = 0; w < width; w++) {
                    texture.SetPixel(w, h, Value.Evaluate((float)w / width));
                }
            }
            texture.Apply();
            
            return texture;
        }
        
    }
}