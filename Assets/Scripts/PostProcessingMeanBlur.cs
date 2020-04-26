using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PostProcessingMeanBlur : PostProcessingBase
{
    private PostProcessingManager postManager = null;
    static class Uniforms
    {
        internal static readonly int _MeanBlurTex = Shader.PropertyToID("_MeanBlurTex");
        internal static readonly int _BlurAmount = Shader.PropertyToID("_BlurAmount");
    }
    public override void Enable(PostProcessingManager post, Camera camera = null)
    {
        postManager = post;
    }
    public override void Disable(Camera camera = null)
    {
        if (mat != null)
            Object.DestroyImmediate(mat);
    }
    public override void PostRender(RenderTexture src, RenderTexture teg, Material blendMat, Camera camera = null)
    {
         blendMat.SetFloat(Uniforms._BlurAmount, postManager.blurAmountMean);    
    }
}
