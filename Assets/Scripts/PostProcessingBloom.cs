using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**************************************************
*  Copyright: newstyle games 
*  Author: zcm
*  Date:2018-1-25 
*  Description:镜头后处理-全屏泛光 
**************************************************/

public class PostProcessingBloom : PostProcessingBloomBase
{
    static class Uniforms
    {
        internal static readonly int _BloomColor = Shader.PropertyToID("_BloomColor");
        internal static readonly int _ParamData = Shader.PropertyToID("_ParamData");
        internal static readonly int _BloomTex = Shader.PropertyToID("_BloomTex");
        internal static readonly int _BloomStrength = Shader.PropertyToID("_BloomStrength");
    }
    private RenderTextureFormat rtType = ChangeRenderTextureFormat(RenderTextureFormat.RGB565);
    private PostProcessingManager postManager = null;
    private void Init()
    {
        shader = Shader.Find("Effect/PostEffectBloom");
        mat = CheckShaderAndCreateMaterial(shader, mat);
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
        if (postManager.bloomData.elementaryBloom.sampleType == LowBloomSampleType.Elementary)
        {
            mat.EnableKeyword("ELEMENTARY");
            mat.DisableKeyword("ADVANCED");
        }
        if (postManager.bloomData.elementaryBloom.sampleType == LowBloomSampleType.Advanced)
        {
            mat.EnableKeyword("ADVANCED");
            mat.DisableKeyword("ELEMENTARY");
        }
        if (shader == null || mat == null)
        {
            Init();// 打开UI隐藏主摄像机的操作会丢失shader和mat
        }
        if (postManager.bloomData.elementaryBloom.threshold != 0 && postManager.bloomData.elementaryBloom.intensity != 0)
        {
            mat.SetVector(Uniforms._ParamData, new Vector2(postManager.bloomData.elementaryBloom.blurSize * 1.5f, 0.8f - postManager.bloomData.elementaryBloom.threshold));
            RenderTexture rtTempA = RenderTexture.GetTemporary(postManager.resolutionWidth[(int)postManager.devLevel] / 8, postManager.resolutionHeight[(int)postManager.devLevel] / 8, 0,rtType);
            rtTempA.filterMode = FilterMode.Bilinear;
            RenderTexture rtTempB = RenderTexture.GetTemporary(postManager.resolutionWidth[(int)postManager.devLevel] / 8, postManager.resolutionHeight[(int)postManager.devLevel] / 8, 0, rtType);
            rtTempA.filterMode = FilterMode.Bilinear;
            Graphics.Blit(src, rtTempA, mat, 0);
            Graphics.Blit(rtTempA, rtTempB, mat, 1);
            RenderTexture.ReleaseTemporary(rtTempA);
            rtTempA = RenderTexture.GetTemporary(postManager.resolutionWidth[(int)postManager.devLevel] / 8, postManager.resolutionHeight[(int)postManager.devLevel] / 8, 0, rtType);
            rtTempB.filterMode = FilterMode.Bilinear;
            Graphics.Blit(rtTempB, rtTempA, mat, 2);
            blendMat.SetColor(Uniforms._BloomColor, postManager.bloomData.color);
            blendMat.SetFloat(Uniforms._BloomStrength, postManager.bloomData.elementaryBloom.intensity);
            blendMat.SetTexture(Uniforms._BloomTex, rtTempA);
            RenderTexture.ReleaseTemporary(rtTempA);
            RenderTexture.ReleaseTemporary(rtTempB);
            RenderTexture.ReleaseTemporary(teg);
        }
    }
}
