using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PostProcessingQualityBloom : PostProcessingBloomBase
{
    private RenderTextureFormat rtType;
    const int samplBlurLevel = 8;
    private RenderTexture[] blurBuf1;
    private RenderTexture[] blurBuf2;
    private int height, width;
    private PostProcessingManager postManager = null;
    static class Uniforms
    {
        internal static readonly int _Threshold = Shader.PropertyToID("_Threshold");
        internal static readonly int _Curve = Shader.PropertyToID("_Curve");
        internal static readonly int _BaseTex = Shader.PropertyToID("_BaseTex");
        internal static readonly int _QualityBloomTex = Shader.PropertyToID("_QualityBloomTex");
        internal static readonly int _Bloom_Settings = Shader.PropertyToID("_Bloom_Settings");
    }
    private void Init()
    {
        shader = Shader.Find("Effect/PostEffectQualityBloom");
        mat = CheckShaderAndCreateMaterial(shader, mat);
        rtType = ChangeRenderTextureFormat(RenderTextureFormat.RGB565);
        blurBuf1 = new RenderTexture[samplBlurLevel];
        blurBuf2 = new RenderTexture[samplBlurLevel];
        height = postManager.resolutionHeight[(int)postManager.devLevel]/8;
        width = postManager.resolutionWidth[(int)postManager.devLevel] /8;
    }
    public override void Enable(PostProcessingManager post, Camera camera = null)
    {
        postManager = post;
        Init();
    }
    public override void Disable(Camera camera = null)
    {
        if (mat != null)
            Object.DestroyImmediate(mat);
    }
    public override void PostRender(RenderTexture src, RenderTexture teg, Material blendMat, Camera camera = null)
    {
        if (shader == null || mat == null)
        {
            Init();// 打开UI隐藏主摄像机的操作会丢失shader和mat
        }
        float log_height = Mathf.Log(height, 2f) + postManager.bloomData.advancedBloom.bloomRadius - 9f;
        int logh_int = (int)log_height;
        int iterations = Mathf.Clamp(logh_int, 1, samplBlurLevel);

        mat.SetFloat(Uniforms._Threshold, postManager.bloomData.advancedBloom.bloomThreshold);

        float knee = postManager.bloomData.advancedBloom.bloomThreshold * postManager.bloomData.advancedBloom.bloomSoftKnee + 1e-5f;
        var curve = new Vector3(postManager.bloomData.advancedBloom.bloomThreshold - knee, knee * 2f, 0.25f / knee);
        mat.SetVector(Uniforms._Curve, curve);

        float sampleScale = 0.5f + log_height - logh_int;
        Vector2 bloomsetting = new Vector2(sampleScale, postManager.bloomData.advancedBloom.bloomIntensity);
        mat.SetVector(Uniforms._Bloom_Settings, bloomsetting);

        RenderTexture prepass = CreateRenderTexture("QualityBloom", width, height, 0, rtType);
        Graphics.Blit(src, prepass, mat, 0);
        var last = prepass;

        //down sample
        for (int i = 0; i < iterations; i++)
        {
            blurBuf1[i] = CreateRenderTexture("QualityBloom", last.width >> 1, last.height >> 1, 0, rtType);
            Graphics.Blit(last, blurBuf1[i], mat, 1);
            last = blurBuf1[i];
        }
        // Upsample and combine loop
        for (int i = iterations - 2; i >= 0; i--)
        {
            var baseTex = blurBuf1[i];
            mat.SetTexture(Uniforms._BaseTex, baseTex);
            blurBuf2[i] = CreateRenderTexture("QualityBloom", baseTex.width, baseTex.height, 0, rtType);
            Graphics.Blit(last, blurBuf2[i], mat, 2);
            last = blurBuf2[i];
        }
        blendMat.SetTexture(Uniforms._QualityBloomTex, last);
        blendMat.SetVector(Uniforms._Bloom_Settings, bloomsetting);

        // Release the temporary buffers
        for (int i = 0; i < samplBlurLevel; i++)
        {
            if (blurBuf1[i] != null)
                RenderTexture.ReleaseTemporary(blurBuf1[i]);
            if (blurBuf2[i] != null)
                RenderTexture.ReleaseTemporary(blurBuf2[i]);
            blurBuf1[i] = null;
            blurBuf2[i] = null;
        }
        RenderTexture.ReleaseTemporary(prepass);
    }
}
