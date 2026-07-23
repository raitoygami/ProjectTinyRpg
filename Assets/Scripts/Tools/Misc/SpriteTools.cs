#if UNITY_EDITOR


using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class SpriteTools : EditorWindow {
    private static readonly Vector2Int size = new Vector2Int(250, 100);
    private string childrenPrefix;
    private int startIndex;

    [MenuItem("Tools/Auto Convert to Sliced Sprites")]
    public static void ShowWindow() {
        EditorWindow window = GetWindow<SpriteTools>();
        window.minSize = size;
        window.maxSize = size;
    }

    private void OnGUI() {
        if (GUILayout.Button("Generate Animation Clips")) {
            var textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
            foreach (var texture in textures) {
                GenerateAnimations(texture);
            }
        }

        if (GUILayout.Button("Convert Texture2D To Sprites")) {
            var sos = Selection.assetGUIDs;

            foreach (var obj in sos) {
                var path = AssetDatabase.GUIDToAssetPath(obj);
                var importer = (TextureImporter) AssetImporter.GetAtPath(
                    path);
                importer.textureType = TextureImporterType.Sprite; // added <---
                importer.isReadable = true;
                importer.filterMode = FilterMode.Point;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.allowAlphaSplitting = false;
                AssetDatabase.ImportAsset(path,
                    ImportAssetOptions.ForceUpdate);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        if (!GUILayout.Button("Slice Sprites")) return;
        var selectedObjects = Selection.assetGUIDs;
        foreach (var obj in selectedObjects) {
            var path = AssetDatabase.GUIDToAssetPath(obj);
            var importer = (TextureImporter) AssetImporter.GetAtPath(
                path);
            importer.textureType = TextureImporterType.Sprite; // added <---
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.allowAlphaSplitting = false;

            var sourceTexture = (Texture2D) AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
            // sourceTexture.isReadable = true;
            var spriteMetaDatas = new List<SpriteMetaData>();
            const int sizeY = 200;
            const int sizeX = 300;
            int frameNumber = 0;
            for (int j = sourceTexture.height; j > 0; j -= sizeY) {
                for (int i = 0; i < sourceTexture.width; i += sizeX) {
                    var rect = new Rect(i, j - sizeY, sizeX, sizeY);
                    var spriteMetaData = new SpriteMetaData {
                        name = sourceTexture.name + "_" + frameNumber,
                        rect = rect,
                        alignment = (int) SpriteAlignment.BottomCenter,
                        border = Vector4.zero,
                    };

                    int xMin = (int) rect.xMax;
                    int xMax = (int) rect.xMin;
                    int yMin = (int) rect.yMax;
                    int yMax = (int) rect.yMin;

                    for (int y = (int) rect.yMin; y < (int) rect.yMax; y++) {
                        for (int x = (int) rect.xMin; x < (int) rect.xMax; x++) {
                            if (PixelHasAlpha(x, y, sourceTexture)) {
                                xMin = Mathf.Min(xMin, x);
                                xMax = Mathf.Max(xMax, x);
                                yMin = Mathf.Min(yMin, y);
                                yMax = Mathf.Max(yMax, y);
                            }
                        }
                    }

                    // Case 582309: Return an empty rectangle if no pixel has an alpha
                    if (xMin > xMax || yMin > yMax)
                        continue;
                    spriteMetaDatas.Add(spriteMetaData);
                    frameNumber++;
                }
            }

            m_AlphaPixelCache = null;
            // importer.isReadable = false;
#pragma warning disable CS0618
            importer.spritesheet = spriteMetaDatas.ToArray();
#pragma warning restore CS0618
            AssetDatabase.ImportAsset(path,
                ImportAssetOptions.ForceUpdate);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private bool[] m_AlphaPixelCache = null;

    private bool PixelHasAlpha(int x, int y, Texture2D texture) {
        if (m_AlphaPixelCache == null) {
            m_AlphaPixelCache = new bool[texture.width * texture.height];

            Color32[] pixels = texture.GetPixels32();

            for (int i = 0; i < pixels.Length; i++)
                m_AlphaPixelCache[i] = pixels[i].a != 0;
        }

        int index = y * (int) texture.width + x;
        return m_AlphaPixelCache[index];
    }

    private static void GenerateAnimations(Object texture) {
        //Create an Array of all sprites in the selected texture
        string path = AssetDatabase.GetAssetPath(texture);
        var allSprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        string newPath = Path.GetDirectoryName(path) + "\\" + texture.name + ".anim";
        var outputAnimClip = AssetDatabase.LoadMainAssetAtPath(newPath) as AnimationClip;

        var clip = new AnimationClip {
            frameRate = 10f
        };

        //Create the CurveBinding
        var spriteBinding = new EditorCurveBinding {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        //Create the KeyFrames
        // var spriteKeyFrames = new ObjectReferenceKeyframe[allSprites.Length];

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding,
            allSprites.Select((t, j) => new ObjectReferenceKeyframe {time = j / clip.frameRate, value = t}).ToArray());

        //Set Loop Time to True
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (outputAnimClip != null) {
            outputAnimClip.name = texture.name + "_Anim";
            EditorUtility.CopySerialized(clip, outputAnimClip);
            AssetDatabase.SetMainObject(outputAnimClip, newPath);
        }
        else {
            AssetDatabase.CreateAsset(clip, newPath);
            AssetDatabase.SaveAssets();
        }

        AssetDatabase.Refresh();
        //Save the clip
    }
}
#endif