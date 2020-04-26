using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**************************************************
*  Copyright: newstyle games 
*  Author: zcm
*  Date:2018-3-15 
*  Description:镜头后处理-景深效果 
**************************************************/
public class PostProcessingDepthOfFiled : PostProcessingDepthOfFiledBase
{
    private PostProcessingManager postManager = null;
    static class Uniforms
    {
        internal static readonly int _DOFTex = Shader.PropertyToID("_DOFTex");
        internal static readonly int _DOF_Blue_Amount = Shader.PropertyToID("_DOF_Blue_Amount");
        internal static readonly int _FocusDistance = Shader.PropertyToID("_FocusDistance");
        internal static readonly int _NearBlurScale = Shader.PropertyToID("_NearBlurScale");
        internal static readonly int _FarBlurScale = Shader.PropertyToID("_FarBlurScale");
    }
    private void Init()
    {
        shader = Shader.Find("Effect/PostEffectDepthOfFiled");
        mat = CheckShaderAndCreateMaterial(shader, mat);
    }
    public override void Enable(PostProcessingManager post, Camera camera = null)
    {
        postManager = post;
        Init();
        camera.depthTextureMode |= DepthTextureMode.Depth;
    }
    public override void Disable(Camera camera = null)
    {
        camera.depthTextureMode &= ~DepthTextureMode.Depth;
        if (mat != null)
            Object.DestroyImmediate(mat);
    }
    public override void PostRender(RenderTexture src, RenderTexture teg, Material blendMat, Camera camera = null)
    {
        if (shader == null || mat == null)
        {
            Init();// 打开UI隐藏主摄像机的操作会丢失shader和mat
        }
        //设置焦点限制在远近裁剪面之间
        Mathf.Clamp(postManager.dofData.elementaryDof.focusDistance, camera.nearClipPlane, camera.farClipPlane);
        RenderTexture temp1 = RenderTexture.GetTemporary(src.width >> postManager.dofData.elementaryDof.resolutionScale, src.height >> postManager.dofData.elementaryDof.resolutionScale, 16);
        mat.SetFloat(Uniforms._DOF_Blue_Amount, postManager.dofData.elementaryDof.blur_anount);
        Graphics.Blit(src, temp1, mat);

        blendMat.SetFloat(Uniforms._FocusDistance, RestrainFocusDistance(postManager.dofData.elementaryDof.focusDistance, camera));
        blendMat.SetFloat(Uniforms._NearBlurScale, postManager.dofData.elementaryDof.nearBlurScale);
        blendMat.SetFloat(Uniforms._FarBlurScale, postManager.dofData.elementaryDof.farBlurScale);
        blendMat.SetTexture(Uniforms._DOFTex, temp1);
        RenderTexture.ReleaseTemporary(temp1);
    }
    //焦点被约束到0-1之间，方便与深度图比较
    private float RestrainFocusDistance(float distance, Camera camera)
    {
        return camera.WorldToViewportPoint((distance - camera.nearClipPlane) * camera.transform.forward + camera.transform.position).z / (camera.farClipPlane - camera.nearClipPlane);
    }

}
