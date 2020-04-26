using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PostProcessingQualityDepthOfFiled : PostProcessingDepthOfFiledBase
{
    private RenderTextureFormat rtType;
    //private float filmHeight;
    private PostProcessingManager postManager = null;
    private GameObject player;
    static class Uniforms
    {
        internal static readonly int _QualityDOFTex = Shader.PropertyToID("_QualityDOFTex");
        internal static readonly int _RcpAspect = Shader.PropertyToID("_RcpAspect");
        internal static readonly int _MaxCoC = Shader.PropertyToID("_MaxCoC");
        internal static readonly int _DOFBlur = Shader.PropertyToID("_DOFBlur");
        internal static readonly int _DOFRange = Shader.PropertyToID("_DOFRange");
        internal static readonly int _DOFDistance = Shader.PropertyToID("_DOFDistance");
        internal static readonly int _ClearDistance = Shader.PropertyToID("_ClearDistance");
    }
    private void Init()
    {
        shader = Shader.Find("Effect/PostEffectQualityDepthOfFiled");
        mat = CheckShaderAndCreateMaterial(shader, mat);
        rtType = ChangeRenderTextureFormat(RenderTextureFormat.ARGBHalf);
        //filmHeight = 0.024f;
    }
    public override void Enable(PostProcessingManager post, Camera camera = null)
    {
        postManager = post;
        Init();
        camera.depthTextureMode = DepthTextureMode.Depth;
    }
    public override void Disable(Camera camera = null)
    {
        if (mat != null)
            Object.DestroyImmediate(mat);
        camera.depthTextureMode = DepthTextureMode.None;
    }
    public override void PostRender(RenderTexture src, RenderTexture teg, Material blendMat, Camera camera = null)
    {
        float aspect = (float)(postManager.resolutionWidth[(int)postManager.devLevel] / postManager.resolutionHeight[(int)postManager.devLevel]);
        mat.SetFloat(Uniforms._RcpAspect, 1f / aspect);
        mat.SetFloat(Uniforms._DOFBlur, postManager.dofData.advancedDof.dofBlur);
        mat.SetFloat(Uniforms._MaxCoC, 0.00556f);
        RenderTexture rt1 = CreateRenderTexture("QualityDOF", postManager.resolutionWidth[(int)postManager.devLevel], postManager.resolutionHeight[(int)postManager.devLevel], 0, rtType);
        Graphics.Blit(src, rt1);
        RenderTexture rt2 = CreateRenderTexture("QualityDOF", postManager.resolutionWidth[(int)postManager.devLevel], postManager.resolutionHeight[(int)postManager.devLevel], 0, rtType);
        Graphics.Blit(rt1, rt2, mat, 0);
        Graphics.Blit(rt2, rt1, mat, 1);
        float clearDistance = GetClearDistance(camera);
        blendMat.SetTexture(Uniforms._QualityDOFTex, rt1);
        blendMat.SetFloat(Uniforms._ClearDistance, clearDistance);
        blendMat.SetFloat(Uniforms._DOFRange, postManager.dofData.advancedDof.dofRange);
        blendMat.SetFloat(Uniforms._DOFDistance, postManager.dofData.advancedDof.dofDistance);

        RenderTexture.ReleaseTemporary(rt1);
        RenderTexture.ReleaseTemporary(rt2);
    }
    private float GetClearDistance(Camera cam)
    {
        float distance = GetDistanceToTarget(postManager.dofData.advancedDof.focusObj.transform.position, cam.transform.position);
        float camViewRange = cam.farClipPlane - cam.nearClipPlane;
        return distance / camViewRange;
    }
}
