using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PostProcessingRadiaBlur : PostProcessingBase
{
    private PostProcessingManager postManager = null;
    static class Uniforms
    {
        internal static readonly int _RadiaBlurTex = Shader.PropertyToID("_RadiaBlurTex");
        internal static readonly int _SampleDist = Shader.PropertyToID("_SampleDist");
        internal static readonly int _SampleStrength = Shader.PropertyToID("_SampleStrength");
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
        blendMat.SetFloat(Uniforms._SampleDist, postManager.rediaData.SampleDist);
        blendMat.SetFloat(Uniforms._SampleStrength, postManager.rediaData.SampleStrength);
    }
}
