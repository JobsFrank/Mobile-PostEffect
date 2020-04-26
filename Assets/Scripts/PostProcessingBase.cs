using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessingBase
{
    protected Shader shader = null;
    protected Material mat = null;
    public virtual void Enable(PostProcessingManager post,Camera camera =null){ }
    public virtual void Disable(Camera camera = null) { }
    public virtual void PostRender(RenderTexture src, RenderTexture teg, Material blendMat, Camera camera = null) { }

    protected RenderTexture CreateRenderTexture(string name, int width, int height, int depth, RenderTextureFormat rf, RenderTextureReadWrite rw = RenderTextureReadWrite.Default, int aa = 1)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height, depth, rf, rw, aa);
        rt.filterMode = FilterMode.Bilinear;
        rt.wrapMode = TextureWrapMode.Clamp;

        rt.name = "PostEffectBuff_" + name;
        return rt;
    }
    protected Material CheckShaderAndCreateMaterial(Shader shader, Material material)
    {
        if (shader == null)
        {
            return null;
        }

        if (shader.isSupported && material && material.shader == shader)
            return material;

        if (!shader.isSupported)
        {
            return null;
        }
        else
        {
            material = new Material(shader);
            material.hideFlags = HideFlags.HideAndDontSave;
            if (material)
                return material;
            else
                return null;
        }
    }

    public static RenderTextureFormat ChangeRenderTextureFormat(RenderTextureFormat format)
    {
        RenderTextureFormat typeFormat = RenderTextureFormat.ARGBHalf;
        if (SystemInfo.SupportsRenderTextureFormat(format))
            typeFormat = format;
        return typeFormat;
    }
    public static float GetDistanceToTarget(Vector3 targetPos, Vector3 curPos)
    {
        return (targetPos - curPos).magnitude;
    }
}
