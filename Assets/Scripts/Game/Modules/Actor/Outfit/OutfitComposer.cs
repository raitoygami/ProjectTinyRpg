using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 部位槽位：用于 <see cref="OutfitSpriteSet"/> 与合成顺序。
/// </summary>
public enum OutfitPart
{
    Template = 0,
    Pants = 1,
    Shoes = 2,
    Top = 3,
    Gloves = 4,
    Helmet = 5,
    Weapon = 6,
}

/// <summary>
/// 各部位一张同尺寸 Sprite（像素矩形一致），供合成最终外观。
/// </summary>
[Serializable]
public class OutfitSpriteSet
{
    public Sprite template;
    public Sprite pants;
    public Sprite shoes;
    public Sprite top;
    public Sprite gloves;
    public Sprite helmet;
    public Sprite weapon;

    public Sprite Get(OutfitPart part)
    {
        return part switch
        {
            OutfitPart.Template => template,
            OutfitPart.Shoes => shoes,
            OutfitPart.Pants => pants,
            OutfitPart.Top => top,
            OutfitPart.Gloves => gloves,
            OutfitPart.Helmet => helmet,
            OutfitPart.Weapon => weapon,
            _ => null
        };
    }
}

/// <summary>
/// 合成方式：<see cref="OutfitComposeMode.CpuGetPixels"/> 需 Read/Write；
/// <see cref="OutfitComposeMode.GpuBlit"/> 使用 RenderTexture + Blit，需着色器 Hidden/SpriteOutfitLayerBlend。
/// </summary>
public enum OutfitComposeMode
{
    /// <summary>GetPixels + CPU 混合，贴图需勾选 Read/Write。</summary>
    CpuGetPixels = 0,
    /// <summary>GPU 叠加，不要求 Read/Write，适合图集子区域。</summary>
    GpuBlit = 1,
}

/// <summary>
/// 将多张<strong>同宽同高</strong>的 Sprite 按「从底到顶」顺序 alpha 合成一张新 Sprite。
/// </summary>
public static class OutfitComposer
{
    /// <summary>默认合成顺序：底→顶（模板在最底，武器在最上）。可按项目改 <see cref="Compose(OutfitSpriteSet,IReadOnlyList{OutfitPart},float)"/> 传入顺序。</summary>
    public static readonly OutfitPart[] DefaultOrderBottomToTop =
    {
        OutfitPart.Template,
        OutfitPart.Pants,
        OutfitPart.Shoes,
        OutfitPart.Top,
        OutfitPart.Gloves,
        OutfitPart.Helmet,
        OutfitPart.Weapon,
    };

    /// <summary>
    /// 按 <paramref name="layersBottomToTop"/> 顺序合成；列表前者在下、后者叠在上。跳过 null。
    /// </summary>
    /// <param name="mode"><see cref="OutfitComposeMode.CpuGetPixels"/> 或 <see cref="OutfitComposeMode.GpuBlit"/>。</param>
    public static Sprite Compose(
        IReadOnlyList<Sprite> layersBottomToTop,
        float pixelsPerUnit,
        Vector2? pivotPixels = null,
        OutfitComposeMode mode = OutfitComposeMode.CpuGetPixels)
    {
        if (!TryValidateLayers(layersBottomToTop, out int w, out int h, out Sprite pivotRef))
            return null;
        if (mode == OutfitComposeMode.GpuBlit)
            return ComposeGpuBlit(layersBottomToTop, w, h, pivotRef, pixelsPerUnit, pivotPixels);
        return ComposeCpuGetPixels(layersBottomToTop, w, h, pivotRef, pixelsPerUnit, pivotPixels);
    }

    /// <summary>仅 CPU GetPixels 合成（与未传 mode 时的默认行为一致）。</summary>
    public static Sprite ComposeCpu(
        IReadOnlyList<Sprite> layersBottomToTop,
        float pixelsPerUnit,
        Vector2? pivotPixels = null)
    {
        if (!TryValidateLayers(layersBottomToTop, out int w, out int h, out Sprite pivotRef))
            return null;
        return ComposeCpuGetPixels(layersBottomToTop, w, h, pivotRef, pixelsPerUnit, pivotPixels);
    }

    /// <summary>仅 GPU：RenderTexture + Blit 合成，不要求贴图 Read/Write。</summary>
    public static Sprite ComposeGpu(
        IReadOnlyList<Sprite> layersBottomToTop,
        float pixelsPerUnit,
        Vector2? pivotPixels = null)
    {
        if (!TryValidateLayers(layersBottomToTop, out int w, out int h, out Sprite pivotRef))
            return null;
        return ComposeGpuBlit(layersBottomToTop, w, h, pivotRef, pixelsPerUnit, pivotPixels);
    }

    /// <summary>按部位集合 + 指定顺序（底→顶）合成。</summary>
    public static Sprite Compose(
        OutfitSpriteSet set,
        IReadOnlyList<OutfitPart> orderBottomToTop,
        float pixelsPerUnit,
        Vector2? pivotPixels = null,
        OutfitComposeMode mode = OutfitComposeMode.CpuGetPixels)
    {
        if (set == null || orderBottomToTop == null || orderBottomToTop.Count == 0)
            return null;
        var list = new List<Sprite>(orderBottomToTop.Count);
        foreach (var p in orderBottomToTop)
        {
            var sp = set.Get(p);
            if (sp != null)
                list.Add(sp);
        }
        if (list.Count == 0)
            return null;
        return Compose(list, pixelsPerUnit, pivotPixels, mode);
    }

    /// <summary>使用 <see cref="DefaultOrderBottomToTop"/>。</summary>
    public static Sprite Compose(
        OutfitSpriteSet set,
        float pixelsPerUnit,
        Vector2? pivotPixels = null,
        OutfitComposeMode mode = OutfitComposeMode.CpuGetPixels)
    {
        return Compose(set, DefaultOrderBottomToTop, pixelsPerUnit, pivotPixels, mode);
    }

    private static bool TryValidateLayers(
        IReadOnlyList<Sprite> layersBottomToTop,
        out int w,
        out int h,
        out Sprite pivotRef)
    {
        w = h = 0;
        pivotRef = null;
        if (layersBottomToTop == null || layersBottomToTop.Count == 0)
            return false;
        foreach (var s in layersBottomToTop)
        {
            if (s == null) continue;
            w = Mathf.RoundToInt(s.rect.width);
            h = Mathf.RoundToInt(s.rect.height);
            pivotRef = s;
            break;
        }
        if (pivotRef == null)
            return false;
        foreach (var s in layersBottomToTop)
        {
            if (s == null) continue;
            if (Mathf.RoundToInt(s.rect.width) != w || Mathf.RoundToInt(s.rect.height) != h)
            {
                Debug.LogError($"SpriteOutfitComposer: 尺寸不一致 '{s.name}' {s.rect.width}x{s.rect.height}，期望 {w}x{h}。");
                return false;
            }
        }
        return true;
    }

    private static Sprite ComposeCpuGetPixels(
        IReadOnlyList<Sprite> layersBottomToTop,
        int w,
        int h,
        Sprite pivotRef,
        float pixelsPerUnit,
        Vector2? pivotPixels)
    {
        var buffer = new Color[w * h];
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = Color.clear;

        foreach (var s in layersBottomToTop)
        {
            if (s == null) continue;
            if (!TryReadSpritePixels(s, out Color[] src, w, h))
            {
                Debug.LogError($"SpriteOutfitComposer: 无法读取像素（请勾选 Texture Read/Write）：'{s.name}'", s.texture);
                return null;
            }
            AlphaBlendOver(buffer, src);
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "ComposedOutfit",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels(buffer);
        tex.Apply();

        Vector2 pivot = pivotPixels ?? pivotRef.pivot;
        var pivotNorm = new Vector2(pivot.x / w, pivot.y / h);
        return Sprite.Create(tex, new Rect(0, 0, w, h), pivotNorm, pixelsPerUnit);
    }

    private static Material s_BlendMaterial;
    private static readonly int BackgroundTex = Shader.PropertyToID("_BackgroundTex");
    private static readonly int SpriteRect = Shader.PropertyToID("_SpriteRect");

    private static Material GetOrCreateBlendMaterial()
    {
        if (s_BlendMaterial != null)
            return s_BlendMaterial;
        var sh = Shader.Find($"Hidden/SpriteOutfitLayerBlend");
        if (sh == null)
        {
            Debug.LogError("SpriteOutfitComposer: 未找到着色器 Hidden/SpriteOutfitLayerBlend（请确认已导入 SpriteOutfitLayerBlend.shader）。");
            return null;
        }
        s_BlendMaterial = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
        return s_BlendMaterial;
    }

    private static Sprite ComposeGpuBlit(
        IReadOnlyList<Sprite> layersBottomToTop,
        int w,
        int h,
        Sprite pivotRef,
        float pixelsPerUnit,
        Vector2? pivotPixels)
    {
        var mat = GetOrCreateBlendMaterial();
        if (mat == null)
            return null;

        int layerCount = 0;
        foreach (var s in layersBottomToTop)
        {
            if (s != null) layerCount++;
        }
        if (layerCount == 0)
            return null;

        var rtA = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var rtB = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture prev = rtA;
        RenderTexture next = rtB;

        var oldRt = RenderTexture.active;
        RenderTexture.active = prev;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = oldRt;

        foreach (var s in layersBottomToTop)
        {
            if (s == null) continue;
            Texture2D srcTex = s.texture;
            if (srcTex == null)
                continue;
            float tw = srcTex.width;
            float th = srcTex.height;
            if (tw < 1f || th < 1f)
                continue;
            Rect r = s.rect;
            var rect = new Vector4(r.x / tw, r.y / th, r.width / tw, r.height / th);
            mat.SetTexture(BackgroundTex, prev);
            mat.SetVector(SpriteRect, rect);
            Graphics.Blit(srcTex, next, mat);
            (prev, next) = (next, prev);
        }

        RenderTexture finalRt = prev;
        var texOut = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "ComposedOutfit",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        RenderTexture.active = finalRt;
        texOut.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        texOut.Apply();
        RenderTexture.active = oldRt;

        RenderTexture.ReleaseTemporary(rtA);
        RenderTexture.ReleaseTemporary(rtB);

        Vector2 pivot = pivotPixels ?? pivotRef.pivot;
        var pivotNorm = new Vector2(pivot.x, pivot.y);
        return Sprite.Create(texOut, new Rect(0, 0, w, h), pivotNorm, pixelsPerUnit);
    }

    private static bool TryReadSpritePixels(Sprite sp, out Color[] pixels, int expectW, int expectH)
    {
        pixels = null;
        if (sp == null || sp.texture == null) return false;
        var r = sp.rect;
        int rw = Mathf.RoundToInt(r.width);
        int rh = Mathf.RoundToInt(r.height);
        if (rw != expectW || rh != expectH) return false;
        try
        {
            pixels = sp.texture.GetPixels(Mathf.FloorToInt(r.x), Mathf.FloorToInt(r.y), rw, rh);
            return pixels != null && pixels.Length == rw * rh;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SpriteOutfitComposer.GetPixels: {e.Message}");
            return false;
        }
    }

    private static void AlphaBlendOver(Color[] dst, Color[] src)
    {
        int n = Mathf.Min(dst.Length, src.Length);
        for (int i = 0; i < n; i++)
        {
            Color s = src[i], d = dst[i];
            float sa = s.a;
            float inv = 1f - sa;
            dst[i] = new Color(
                s.r * sa + d.r * inv,
                s.g * sa + d.g * inv,
                s.b * sa + d.b * inv,
                sa + d.a * inv);
        }
    }
}
