using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessingWideScreen : PostProcessingBase
{
    private float timeStep = 1.0f;
    private float stretchX = 1f;
    private float stretchY = 1f;
    private PostProcessingManager postManager = null;
    static class Uniforms
    {
        internal static readonly int _WideScreenTex = Shader.PropertyToID("_WideScreenTex");
        internal static readonly int _TimeStep = Shader.PropertyToID("_TimeStep");
        internal static readonly int _ViewArea = Shader.PropertyToID("_ViewArea");
        internal static readonly int _OpeningVal = Shader.PropertyToID("_OpeningVal");
        internal static readonly int _ViewAreaSmooth = Shader.PropertyToID("_ViewAreaSmooth");
        internal static readonly int _StretchX = Shader.PropertyToID("_StretchX");
        internal static readonly int _StretchY = Shader.PropertyToID("_StretchY");
        internal static readonly int _ScreenResolution = Shader.PropertyToID("_ScreenResolution");
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
        if (postManager.screenEffectType == WideScreenType.Circle)
        {
            blendMat.EnableKeyword("CIRCLE");
            blendMat.DisableKeyword("HORIZONTAL");
            blendMat.DisableKeyword("VERTICAL");
        }
        else if (postManager.screenEffectType == WideScreenType.Horizontal)
        {
            blendMat.EnableKeyword("HORIZONTAL");
            blendMat.DisableKeyword("CIRCLE");
            blendMat.DisableKeyword("VERTICAL");
        }
        else if (postManager.screenEffectType == WideScreenType.Vertical)
        {
            blendMat.EnableKeyword("VERTICAL");
            blendMat.DisableKeyword("HORIZONTAL");
            blendMat.DisableKeyword("CIRCLE");
        }
        timeStep += Time.deltaTime;
        if (timeStep > 100) timeStep = 0;
        blendMat.SetFloat(Uniforms._TimeStep, timeStep);
        blendMat.SetFloat(Uniforms._ViewArea, postManager.wideScreenData.viewArea);
        blendMat.SetFloat(Uniforms._OpeningVal, postManager.wideScreenData.openingVal);
        blendMat.SetFloat(Uniforms._ViewAreaSmooth, postManager.wideScreenData.viewAreaSmooth);
        blendMat.SetFloat(Uniforms._StretchX, stretchX);
        blendMat.SetFloat(Uniforms._StretchY, stretchY);
        blendMat.SetVector(Uniforms._ViewAreaSmooth, new Vector4(Screen.width, Screen.height, 0.0f, 0.0f));
    }
}
