using System.Collections.Generic;
using UnityEngine;


// ReSharper disable once CheckNamespace
namespace Raitoygami.Tool {
    public class PresetGradientLibrary : ScriptableObject {
        [SerializeField] private List<Gradient> _Presets = new List<Gradient>();

        public void Add(object presetObject, string presetName = "") {
            if (presetObject is not Gradient gradient) {
                Debug.LogError("Wrong type used in GradientPresetLibrary");
                return;
            }

            var copy = new Gradient {
                alphaKeys = gradient.alphaKeys,
                colorKeys = gradient.colorKeys,
                mode = gradient.mode
            };
            _Presets.Add(copy);
        }

        public void Replace(int index, object newPresetObject) {
            if (newPresetObject is not Gradient gradient) {
                Debug.LogError("Wrong type used in GradientPresetLibrary");
                return;
            }

            var copy = new Gradient {
                alphaKeys = gradient.alphaKeys,
                colorKeys = gradient.colorKeys,
                mode = gradient.mode
            };
            _Presets[index] = copy;
        }

        public Gradient Get(int index) {
            return _Presets[index];
        }
    }
}
