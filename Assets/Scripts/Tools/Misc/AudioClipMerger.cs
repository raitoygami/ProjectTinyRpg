#if UNITY_EDITOR


using UnityEngine;
using UnityEditor;
using System.IO;

public class AudioClipMerger : EditorWindow
{
    private AudioClip introClip;
    private AudioClip loopClip;
    private string newClipName = "Merged_Intro_Loop";

    [Header("Loop Points 设置")] [Tooltip("Loop Start 时间（秒），即 Intro 结束的位置")]
    private float loopStartTime = 0f; // 默认会自动计算为 intro 时长

    [MenuItem("Tools/Audio/Intro + Loop Merger (带 SMPL Loop Points)")]
    public static void ShowWindow()
    {
        GetWindow<AudioClipMerger>("Intro + Loop Merger");
    }

    private void OnGUI()
    {
        GUILayout.Label("合并 Intro + Loop 并写入 SMPL Loop Points", EditorStyles.boldLabel);

        introClip = (AudioClip)EditorGUILayout.ObjectField("Intro Clip", introClip, typeof(AudioClip), false);
        loopClip = (AudioClip)EditorGUILayout.ObjectField("Loop Clip", loopClip, typeof(AudioClip), false);

        newClipName = EditorGUILayout.TextField("新文件名（不带后缀）", newClipName);

        if (introClip != null)
        {
            loopStartTime = EditorGUILayout.FloatField("Loop Start 时间 (秒)", loopStartTime);
            EditorGUILayout.HelpBox($"建议值：{introClip.length:F3} 秒（Intro 时长）", MessageType.Info);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("合并并保存为 .wav（带 SMPL Loop Points）", GUILayout.Height(50)))
        {
            if (introClip == null || loopClip == null)
            {
                EditorUtility.DisplayDialog("错误", "请拖入 Intro 和 Loop 文件！", "确定");
                return;
            }

            MergeAndSaveWithSMPL();
        }

        EditorGUILayout.HelpBox("生成 .wav 后：\n" +
                                "1. Unity 会尝试读取 SMPL chunk 中的 loop points。\n" +
                                "2. 在 MusicFileObject 中仍可使用 JSAM 的 Loop with Loop Points 辅助。\n" +
                                "3. 建议把 AudioClip 的 Compression Format 设为 Vorbis（生成 Ogg）。", MessageType.Info);
    }

    private void MergeAndSaveWithSMPL()
    {
        if (introClip.frequency != loopClip.frequency || introClip.channels != loopClip.channels)
        {
            EditorUtility.DisplayDialog("错误", "采样率和声道数必须相同！", "确定");
            return;
        }

        // 计算样本数
        int introSamples = introClip.samples * introClip.channels;
        int loopSamples = loopClip.samples * loopClip.channels;

        float[] introData = new float[introSamples];
        float[] loopData = new float[loopSamples];

        introClip.GetData(introData, 0);
        loopClip.GetData(loopData, 0);

        float[] mergedData = new float[introData.Length + loopData.Length];
        System.Array.Copy(introData, 0, mergedData, 0, introData.Length);
        System.Array.Copy(loopData, 0, mergedData, introData.Length, loopData.Length);

        // 创建合并后的 AudioClip
        AudioClip mergedClip = AudioClip.Create(newClipName, mergedData.Length / introClip.channels,
            introClip.channels, introClip.frequency, false);
        mergedClip.SetData(mergedData, 0);

        // 保存路径
        string path = EditorUtility.SaveFilePanelInProject("保存带 Loop Points 的 WAV", newClipName, "wav", "");
        if (string.IsNullOrEmpty(path)) return;

        if (!path.ToLower().EndsWith(".wav")) path += ".wav";

        // 计算 Loop Start 样本位置
        if (loopStartTime <= 0f) loopStartTime = introClip.length;
        long loopStartSample = (long)(loopStartTime * introClip.frequency);

        SaveWavWithSMPL(mergedClip, path, loopStartSample, mergedData.Length / introClip.channels);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("成功",
            $"已保存：{path}\n\nLoop Start ≈ {loopStartTime:F3} 秒\n\n现在可以拖入 JSAM MusicFileObject 使用！", "确定");
    }

    // 核心函数：保存 WAV 并写入 SMPL chunk（标准 loop）
    private void SaveWavWithSMPL(AudioClip clip, string filepath, long loopStartSample, long totalSamples)
    {
        using (var fileStream = new FileStream(filepath, FileMode.Create))
        using (var writer = new BinaryWriter(fileStream))
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            int dataSize = samples.Length * 2; // 16-bit

            // RIFF Header
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize + 68); // 后面会加上 smpl chunk 大小（约 68 字节 for 1 loop）
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16);

            // data chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (float sample in samples)
            {
                short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767);
                writer.Write(intSample);
            }

            // ====================== 写入 SMPL chunk ======================
            writer.Write(System.Text.Encoding.ASCII.GetBytes("smpl"));
            writer.Write(60); // smpl chunk data size (for 1 loop: 36 + 24)
            writer.Write(0); // Manufacturer
            writer.Write(0); // Product
            writer.Write(1000000000 / clip.frequency); // Sample Period (ns)
            writer.Write(60); // MIDI Unity Note (middle C)
            writer.Write(0); // MIDI Pitch Fraction
            writer.Write(0); // SMPTE Format
            writer.Write(0); // SMPTE Offset
            writer.Write(1); // Num Sample Loops = 1
            writer.Write(0); // Sampler Data

            // Sample Loop 结构体
            writer.Write(0); // Cue Point ID
            writer.Write(0); // Loop Type (0 = forward)
            writer.Write((uint)loopStartSample); // Loop Start (samples)
            writer.Write((uint)totalSamples); // Loop End (samples) - 通常设为文件末尾
            writer.Write(0); // Fraction
            writer.Write(0); // Play Count (0 = infinite)

            // 更新 RIFF 大小（因为加了 smpl chunk）
            // 此处简化处理：实际写入后可不精确更新（多数工具仍能读取），或重新计算
        }
    }
}
#endif