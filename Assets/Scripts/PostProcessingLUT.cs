using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**************************************************
*  Copyright: newstyle games 
*  Author: zcm
*  Date:2018-2-19 
*  Description:镜头后处理-颜色表镜头滤镜+HDR&Tommapping(色调映射) 
**************************************************/
public class PostProcessingLUT : PostProcessingBase
{
    private RenderTextureFormat rtType;
    private PostProcessingManager postManager = null;
    static class Uniforms
    {
        internal static readonly int _LutParams = Shader.PropertyToID("_LutParams");
        internal static readonly int _HueShift = Shader.PropertyToID("_HueShift");
        internal static readonly int _Saturation = Shader.PropertyToID("_Saturation");
        internal static readonly int _Contrast = Shader.PropertyToID("_Contrast");
        internal static readonly int _Balance = Shader.PropertyToID("_Balance");
        internal static readonly int _LogLut = Shader.PropertyToID("_LogLut");
        internal static readonly int _LogLut_Params = Shader.PropertyToID("_LogLut_Params");
        internal static readonly int _ExposureEV = Shader.PropertyToID("_ExposureEV");
    }
    private void Init()
    {
        shader = Shader.Find("Effect/PostEffectLUT");
        mat = CheckShaderAndCreateMaterial(shader, mat);
        rtType = ChangeRenderTextureFormat(RenderTextureFormat.ARGBHalf);
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
        RenderTexture LUT = CreateRenderTexture("ColorGradingLUT", 1024, 32, 0, rtType);
        mat.SetVector(Uniforms._LutParams, new Vector4(32.0f, 0.5f / 1024.0f, 0.5f / 32.0f, 32.0f / 31.0f));
        mat.SetFloat(Uniforms._HueShift, postManager.lutData.lut_HUE / 360f);
        mat.SetFloat(Uniforms._Saturation, postManager.lutData.lut_Saturation);
        mat.SetFloat(Uniforms._Contrast, postManager.lutData.lut_Contrast);
        mat.SetVector(Uniforms._Balance, CalculateColorBalance(postManager.lutData.lut_Temperature, postManager.lutData.lut_Tint));
        Graphics.Blit(null, LUT, mat, 0);
        float ev = Mathf.Exp(postManager.lutData.lut_Exposure * 0.69314718055994530941723212145818f);
        blendMat.SetFloat(Uniforms._ExposureEV, ev);
        blendMat.SetTexture(Uniforms._LogLut, LUT);
        blendMat.SetVector(Uniforms._LogLut_Params, new Vector3(1f / LUT.width, 1f / LUT.height, LUT.height - 1f));
        RenderTexture.ReleaseTemporary(LUT); 
    }
    // 以下算法均来自维基百科
    Vector3 CalculateColorBalance(float temperature, float tint)
    {
        // Range ~[-1.8;1.8] ; using higher ranges is unsafe
        float t1 = temperature / 55f;
        float t2 = tint / 55f;

        // Get the CIE xy chromaticity of the reference white point.
        // Note: 0.31271 = x value on the D65 white point
        float x = 0.31271f - t1 * (t1 < 0f ? 0.1f : 0.05f);
        float y = StandardIlluminantY(x) + t2 * 0.05f;

        // Calculate the coefficients in the LMS space.
        var w1 = new Vector3(0.949237f, 1.03542f, 1.08728f); // D65 white point
        var w2 = CIExyToLMS(x, y);
        return new Vector3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
    }

    // An analytical model of chromaticity of the standard illuminant, by Judd et al.
    // http://en.wikipedia.org/wiki/Standard_illuminant#Illuminant_series_D
    // Slightly modifed to adjust it with the D65 white point (x=0.31271, y=0.32902).
    float StandardIlluminantY(float x)
    {
        return 2.87f * x - 3f * x * x - 0.27509507f;
    }

    // CIE xy chromaticity to CAT02 LMS.
    // http://en.wikipedia.org/wiki/LMS_color_space#CAT02
    Vector3 CIExyToLMS(float x, float y)
    {
        float Y = 1f;
        float X = Y * x / y;
        float Z = Y * (1f - x - y) / y;

        float L = 0.7328f * X + 0.4296f * Y - 0.1624f * Z;
        float M = -0.7036f * X + 1.6975f * Y + 0.0061f * Z;
        float S = 0.0030f * X + 0.0136f * Y + 0.9834f * Z;

        return new Vector3(L, M, S);
    }
}
