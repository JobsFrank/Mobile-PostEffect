using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PostProcessingMotionBlur : PostProcessingBase
{
    private RenderTexture accumulationTexture;
    private PostProcessingManager postManager = null;
    static class Uniforms
    {
        internal static readonly int _MotionBlurTex = Shader.PropertyToID("_MotionBlurTex");
        internal static readonly int _BlurAmount = Shader.PropertyToID("_BlurAmount");
    }
    private void Init()
    {
        shader = Shader.Find("Effect/PostEffectMotionBlur");
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
        if (accumulationTexture != null)
            RenderTexture.ReleaseTemporary(accumulationTexture);
    }
    public override void PostRender(RenderTexture src, RenderTexture teg, Material blendMat, Camera camera = null)
    {
        if (shader == null || mat == null)
        {
            Init();// 打开UI隐藏主摄像机的操作会丢失shader和mat
        }
        if (accumulationTexture == null || accumulationTexture.width != src.width || accumulationTexture.height != src.height)
        {
            RenderTexture.ReleaseTemporary(accumulationTexture);
            accumulationTexture = new RenderTexture(src.width, src.height, 16);

            // 不显示到面板上也不会保存
            accumulationTexture.hideFlags = HideFlags.HideAndDontSave;
            // 当前src
            Graphics.Blit(src, accumulationTexture);
        }
        // 表示需要进行渲染纹理恢复操作
        // 发生在渲染到渲染纹理而该纹理又没有被提前清空或者销毁的情况下
        accumulationTexture.MarkRestoreExpected();
        mat.SetFloat(Uniforms._BlurAmount, 1.0f - postManager.blurAmountMotion);
        Graphics.Blit(src, accumulationTexture, mat);
        blendMat.SetTexture(Uniforms._MotionBlurTex, accumulationTexture);
    }
}
