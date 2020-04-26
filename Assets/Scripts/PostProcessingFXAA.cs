using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**************************************************
*  Copyright: newstyle games 
*  Author: zcm
*  Date:2018-2-15 
*  Description:镜头后处理-快速抗锯齿 
**************************************************/
public class PostProcessingFXAA : PostProcessingBase
{
    private PostProcessingManager postManager = null;
    static class Uniforms
    {
        internal static readonly int _FXAATex = Shader.PropertyToID("_FXAATex");
        internal static readonly int _Contrast = Shader.PropertyToID("_Contrast");
        internal static readonly int _Relative = Shader.PropertyToID("_Relative");
        internal static readonly int _Subpixel = Shader.PropertyToID("_Subpixel");
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
        blendMat.SetFloat(Uniforms._Contrast, postManager.faxxData.Contrast);
        blendMat.SetFloat(Uniforms._Relative, postManager.faxxData.Relative);
        blendMat.SetFloat(Uniforms._Subpixel, postManager.faxxData.Subpixel);
    }
}

