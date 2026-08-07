#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AudioClipMerger : EditorWindow
{
    private AudioClip introClip;
    private List<AudioClip> loopClips = new List<AudioClip>();
    private string newClipName = "Merged_Intro_Loops";

    [Tooltip("Loop Start 时间（秒），即 Intro 结束的位置")]
    private float loopStartTime = 0f;

    private Vector2 scrollPos;

    [MenuItem("Tools/Audio/Intro + Loop Merger (带 SMPL Loop Points)")]
    public static void ShowWindow()
    {
        GetWindow<AudioClipMerger>("Intro + Loop Merger");
    }

    private void OnGUI()
    {
        GUILayout.Label("合并 Intro + 多个 Loop 并写入 SMPL Loop Points", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Intro
        introClip = (AudioClip)EditorGUILayout.ObjectField("Intro Clip", introClip, typeof(AudioClip), false);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Loop Clips（按顺序依次播放）", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(220));

        for (int i = 0; i < loopClips.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            loopClips[i] = (AudioClip)EditorGUILayout.ObjectField($"Loop {i}", loopClips[i], typeof(AudioClip), false);

            if (GUILayout.Button("↑", GUILayout.Width(24)) && i > 0)
            {
                var tmp = loopClips[i];
                loopClips[i] = loopClips[i - 1];
                loopClips[i - 1] = tmp;
            }
            if (GUILayout.Button("↓", GUILayout.Width(24)) && i < loopClips.Count - 1)
            {
                var tmp = loopClips[i];
                loopClips[i] = loopClips[i + 1];
                loopClips[i + 1] = tmp;
            }
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                loopClips.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("+ 添加 Loop Clip"))
        {
            loopClips.Add(null);
        }

        EditorGUILayout.Space(8);
        newClipName = EditorGUILayout.TextField("新文件名（不带后缀）", newClipName);

        if (introClip != null)
        {
            // 默认建议值为 Intro 时长
            if (loopStartTime <= 0f)
                loopStartTime = introClip.length;

            loopStartTime = EditorGUILayout.FloatField("Loop Start 时间 (秒)", loopStartTime);
            EditorGUILayout.HelpBox(
                $"建议值：{introClip.length:F3} 秒（Intro 时长）\n" +
                $"合并后结构：Intro → Loop0 → Loop1 → … → LoopN\n" +
                $"循环区间：Loop Start → 文件末尾（会依次播完所有 Loop 再回到 Loop Start）",
                MessageType.Info);
        }

        EditorGUILayout.Space(10);

        GUI.enabled = introClip != null && loopClips.Count > 0 && loopClips.Exists(c => c != null);
        if (GUILayout.Button("合并并保存为 .wav（带 SMPL Loop Points）", GUILayout.Height(50)))
        {
            MergeAndSaveWithSMPL();
        }
        GUI.enabled = true;

        EditorGUILayout.HelpBox(
            "生成 .wav 后：\n" +
            "1. Unity 可能读取 SMPL chunk 中的 loop points（取决于导入设置）。\n" +
            "2. 更推荐在 JSAM MusicFileObject 里手动设 Loop with Loop Points：\n" +
            "   - Loop Start = Intro 时长\n" +
            "   - Loop End = 整段总时长\n" +
            "3. 建议把 AudioClip 的 Compression Format 设为 Vorbis。",
            MessageType.Info);
    }

    private void MergeAndSaveWithSMPL()
    {
        // 过滤掉空的 Loop
        List<AudioClip> validLoops = new List<AudioClip>();
        foreach (var c in loopClips)
        {
            if (c != null) validLoops.Add(c);
        }

        if (introClip == null || validLoops.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "请至少指定 Intro 和一个有效的 Loop 文件！", "确定");
            return;
        }

        // 校验采样率 / 声道
        int frequency = introClip.frequency;
        int channels = introClip.channels;

        foreach (var loop in validLoops)
        {
            if (loop.frequency != frequency || loop.channels != channels)
            {
                EditorUtility.DisplayDialog("错误",
                    $"所有音频的采样率和声道数必须相同！\n" +
                    $"Intro: {frequency}Hz / {channels}ch\n" +
                    $"不匹配: {loop.name} ({loop.frequency}Hz / {loop.channels}ch)",
                    "确定");
                return;
            }
        }

        // ========== 合并样本数据 ==========
        // Intro
        float[] introData = new float[introClip.samples * channels];
        introClip.GetData(introData, 0);

        // 所有 Loop
        List<float[]> loopDataList = new List<float[]>();
        int totalLoopSamplesPerChannel = 0;

        foreach (var loop in validLoops)
        {
            float[] data = new float[loop.samples * channels];
            loop.GetData(data, 0);
            loopDataList.Add(data);
            totalLoopSamplesPerChannel += loop.samples;
        }

        int totalSamplesPerChannel = introClip.samples + totalLoopSamplesPerChannel;
        float[] mergedData = new float[totalSamplesPerChannel * channels];

        int writeOffset = 0;
        System.Array.Copy(introData, 0, mergedData, writeOffset, introData.Length);
        writeOffset += introData.Length;

        foreach (var loopData in loopDataList)
        {
            System.Array.Copy(loopData, 0, mergedData, writeOffset, loopData.Length);
            writeOffset += loopData.Length;
        }

        // 创建临时 AudioClip（仅用于方便写 WAV）
        AudioClip mergedClip = AudioClip.Create(
            newClipName,
            totalSamplesPerChannel,
            channels,
            frequency,
            false);
        mergedClip.SetData(mergedData, 0);

        // 保存路径
        string path = EditorUtility.SaveFilePanelInProject(
            "保存带 Loop Points 的 WAV",
            newClipName,
            "wav",
            "选择保存位置");

        if (string.IsNullOrEmpty(path))
        {
            DestroyImmediate(mergedClip);
            return;
        }

        if (!path.ToLower().EndsWith(".wav"))
            path += ".wav";

        // Loop Start（样本，按单声道样本数计算）
        if (loopStartTime <= 0f)
            loopStartTime = introClip.length;

        long loopStartSample = (long)(loopStartTime * frequency);
        // 保护：不要超过总长度
        loopStartSample = System.Math.Max(0, System.Math.Min(loopStartSample, totalSamplesPerChannel - 1));
        long loopEndSample = totalSamplesPerChannel; // 循环到文件末尾

        SaveWavWithSMPL(mergedClip, path, loopStartSample, loopEndSample);

        DestroyImmediate(mergedClip);
        AssetDatabase.Refresh();

        // 统计信息
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"已保存：{path}");
        sb.AppendLine();
        sb.AppendLine($"总时长 ≈ {(float)totalSamplesPerChannel / frequency:F3} 秒");
        sb.AppendLine($"Loop Start ≈ {loopStartTime:F3} 秒（样本 {loopStartSample}）");
        sb.AppendLine($"Loop End   = 文件末尾（样本 {loopEndSample}）");
        sb.AppendLine();
        sb.AppendLine("播放逻辑：");
        sb.AppendLine("1. 从头播到结尾（先听完 Intro，再依次听完所有 Loop）");
        sb.AppendLine("2. 之后在 Loop Start ↔ 文件末尾之间无限循环");

        EditorUtility.DisplayDialog("成功", sb.ToString(), "确定");
    }

    /// <summary>
    /// 保存 16-bit PCM WAV，并写入标准 SMPL chunk（1 个 forward loop）
    /// loopStartSample / loopEndSample 均为「单声道样本索引」
    /// </summary>
    private void SaveWavWithSMPL(AudioClip clip, string filepath, long loopStartSample, long loopEndSample)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        int dataSize = samples.Length * 2; // 16-bit
        const int smplChunkDataSize = 60;  // 36 + 24 * 1 loop
        int smplChunkTotalSize = 8 + smplChunkDataSize; // id + size + data

        // RIFF size = 文件总字节 - 8
        // WAVE(4) + fmt(8+16) + data(8+dataSize) + smpl(8+60)
        int riffSize = 4 + (8 + 16) + (8 + dataSize) + smplChunkTotalSize;

        using (var fileStream = new FileStream(filepath, FileMode.Create))
        using (var writer = new BinaryWriter(fileStream))
        {
            // ---- RIFF Header ----
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(riffSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // ---- fmt chunk ----
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);                    // chunk size
            writer.Write((short)1);              // PCM
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2); // byte rate
            writer.Write((short)(clip.channels * 2));         // block align
            writer.Write((short)16);             // bits per sample

            // ---- data chunk ----
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (float sample in samples)
            {
                short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                writer.Write(intSample);
            }

            // ---- smpl chunk ----
            writer.Write(System.Text.Encoding.ASCII.GetBytes("smpl"));
            writer.Write(smplChunkDataSize);

            writer.Write(0);                                    // Manufacturer
            writer.Write(0);                                    // Product
            writer.Write(clip.frequency > 0 ? 1000000000 / clip.frequency : 0); // Sample Period (ns)
            writer.Write(60);                                   // MIDI Unity Note (C4)
            writer.Write(0);                                    // MIDI Pitch Fraction
            writer.Write(0);                                    // SMPTE Format
            writer.Write(0);                                    // SMPTE Offset
            writer.Write(1);                                    // Num Sample Loops
            writer.Write(0);                                    // Sampler Data size

            // Sample Loop
            writer.Write(0);                                    // Cue Point ID
            writer.Write(0);                                    // Type: 0 = forward
            writer.Write((uint)loopStartSample);                // Start
            writer.Write((uint)loopEndSample);                  // End
            writer.Write(0);                                    // Fraction
            writer.Write(0);                                    // Play Count (0 = infinite)
        }
    }
}

#endif