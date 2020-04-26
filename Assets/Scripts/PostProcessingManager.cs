using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum EffectType
{
    None = 0,
    Bloom,
    FXAA,
    MSAA,
    DepthOfFiled,
    LUT,
    MeanBlur,
    MotionBlur,
    RediaBlur,
    WideScreenVignette,
    SSAO,
    Max,
}
public enum MobileDevicelevel
{
    High,
    Low,
}
public enum BloomType
{
    Elementary = 0,
    Advanced,
}
public enum LowBloomSampleType
{
    Elementary = 0,
    Advanced,
}

public enum DOFType
{
    Elementary = 0,
    Advanced,
}
public enum WideScreenType
{
    Circle = 0,
    Horizontal,
    Vertical,
}

[Serializable]
public struct AdvancedBloom
{
    [Range(1, 4.75f), Tooltip("发光范围, 数值越大越消耗性能")]
    public float bloomRadius;
    [Range(0, 5), Tooltip("发光强度")]
    public float bloomIntensity;
    [Range(0, 4), Tooltip("发光阈值")]
    public float bloomThreshold;
    [Range(0, 1), Tooltip("发光的模糊的边缘强度")]
    public float bloomSoftKnee;
};

[Serializable]
public struct ElementaryBloom
{
    [Range(0.0f, 1.0f), Tooltip("发光阈值")]
    public float threshold;//0.35f;

    [Range(0.0f, 2.5f), Tooltip("发光强度")]
    public float intensity;//0.75f;

    [Range(0.2f, 1.0f), Tooltip("模糊强度")]
    public float blurSize;//1.0f
    [Tooltip("模糊采样类型")]
    public LowBloomSampleType sampleType;
};
[Serializable]
public struct AdvancedDOF
{
    [Range(0f, 3f), Tooltip("景深的模糊程度")]
    public float dofBlur;//=1;
    [Tooltip("景深效果的清晰区域的距离")]
    public float dofRange;// = 1;
    [Range(-0.01f, 0.01f), Tooltip("微调角色周围的清晰范围")]
    public float dofDistance;// = 0;
    [Tooltip("焦点对象(这个是实时运算摄像机与该对象距离用的)")]
    public GameObject focusObj;
};

[Serializable]
public struct ElementaryDOF
{
    [Range(0.0f, 100.0f), Tooltip("焦点与摄像机的距离")]
    public float focusDistance;//10.0f;
    [Range(0.0f, 100.0f), Tooltip("模糊区域的近裁剪面")]
    public float nearBlurScale; //= 0.0f;
    [Range(0.0f, 1000.0f), Tooltip("模糊区域的远裁剪面")]
    public float farBlurScale;// = 50.0f;
    [Range(1.0f, 2.0f), Tooltip("分辨率缩放比")]
    public int resolutionScale;// = 1;
    [Range(0.0f, 5.0f), Tooltip("模糊强度")]
    public float blur_anount;
};

[Serializable]
public struct BloomData
{
    [Tooltip("Bloom颜色")]
    public Color color;
    [Tooltip("Bloom渲染级别（Elementary低级，Advanced高级（针对高端机型））")]
    public BloomType bloomType;
    [Tooltip("高级渲染参数")]
    public AdvancedBloom advancedBloom;
    [Tooltip("普通渲染参数")]
    public ElementaryBloom elementaryBloom;
};
[Serializable]
public struct DOFData
{
    [Tooltip("景深渲染级别（Elementary低级，Advanced高级（针对高端机型））")]
    public DOFType dofType;
    [Tooltip("高级渲染参数")]
    public ElementaryDOF elementaryDof;
    [Tooltip("普通渲染参数")]
    public AdvancedDOF advancedDof;
};
[Serializable]
public struct FAXXData
{
    [Range(0.0f, 1.0f), Tooltip("忽略像素检测的基础值")]
    public float Contrast;

    [Range(0f, 1.0f), Tooltip("忽略像素检测的相对值")]
    public float Relative;

    [Range(1f, 2.0f), Tooltip("混合的最终像素插值")]
    public float Subpixel;
};
[Serializable]
public struct WideScreenData
{
    [Range(0f, 5.0f),Tooltip("显示区域")]
    public float viewArea;
    [Range(0f, 5.0f), Tooltip("显示区域的开合度-只针对Circle模式")]
    public float openingVal;
    [Range(0.01f, 0.4f), Tooltip("显示区域与遮罩边缘的模糊过渡值")]
    public float viewAreaSmooth;
}
[Serializable]
public struct LUTData
{
    [Tooltip("曝光值")]
    public float lut_Exposure;
    [Range(-100, 100), Tooltip("白平衡 - 色温")]
    public float lut_Temperature;
    [Range(-100, 100), Tooltip("白平衡 - 调整绿色或品红的色偏")]
    public float lut_Tint;
    [Range(-180, 180), Tooltip("色相")]
    public float lut_HUE;
    [Range(0, 2), Tooltip("饱和度")]
    public float lut_Saturation;
    [Range(0, 2), Tooltip("对比度")]
    public float lut_Contrast;
}
[Serializable]
public struct RediaBlurData
{
    [Range(0.0f, 1.0f), Tooltip("采样距离")]
    public float SampleDist;

    [Range(0f, 3.0f), Tooltip("采样强度")]
    public float SampleStrength;
}
[ExecuteInEditMode]
public class PostProcessingManager : MonoBehaviour {
    #region PostBase Variable
    private RenderTexture renderTarget;
    private Camera mainCamera;
    private Shader shader;
    private Material blendMat;
    private bool renderTargetNeedInit = false;
    private PostProcessingBase postBase = null;
    private Dictionary<int, PostProcessingBase> postBaseDic;
    private bool lateBloom;
    private bool lateFXAA;
    //private bool lateMSAA;
    private bool lateDOF;
    private bool lateLUT;
    private bool lateMeanBlur;
    private bool lateMotionBlur;
    private bool lateRediaBlur;
    private bool lateWideScreen;
    //private bool lateSSAO;
#if !UNITY_EDITOR
    private static float aspectRatio = (float)Screen.width / (float)Screen.height;
#endif
    //[HideInInspector]
    [Header("设备等级")]
    public MobileDevicelevel devLevel = MobileDevicelevel.Low;
    // 高中低三档机型的分辨率
    [HideInInspector]
#if !UNITY_EDITOR
    public int[] resolutionWidth = new int[2] { (int)(aspectRatio * 1080), (int)(aspectRatio * 720) };
#else
    public int[] resolutionWidth = new int[2] { 1920, 1280 };
#endif
    [HideInInspector]
    public int[] resolutionHeight = new int[2] { 1080, 720 };
    #endregion
    #region Bloom Variable
    [Header("全屏泛光-Bloom")]
    public bool openBloom = false;
    private bool bloom;
    private bool enableBloom
    {
        get { return bloom; }
        set
        {
            if (bloom != value)
                renderTargetNeedInit = true;
            bloom = value;
        }
    }
    public BloomData bloomData = new BloomData
    {
        color = new Color(1.0f, 1.0f, 1.0f, 1.0f),
        bloomType = BloomType.Elementary,
        advancedBloom = new AdvancedBloom
        {
            bloomRadius = 3,
            bloomIntensity = 2,
            bloomThreshold = 1,
            bloomSoftKnee = 0.5f
        },
        elementaryBloom = new ElementaryBloom
        {
            threshold = 0.35f,
            intensity = 1.0f,
            blurSize = 1.0f,
            sampleType = LowBloomSampleType.Elementary,
        }
    };
    #endregion
    #region FXAA Variable
    [Header("快速抗锯齿-FXAA")]
    public bool openFXAA = false;
    public FAXXData faxxData = new FAXXData
    {
        Contrast = 0.0312f,
        Relative = 0.063f,
        Subpixel = 1.0f
    };
    #endregion
    #region MSAA Variable
    [Header("全屏抗锯齿-MSAA")]
    public bool openMSAA = false;
    private bool msaa;
    private bool enableMSAA
    {
        get { return msaa; }
        set
        {
            if (msaa != value)
                renderTargetNeedInit = true;
            msaa = value;
        }
    }
    #endregion
    #region DepthOfFiled Variable
    [Header("景深-DepthOfFiled")]
    public bool openDepthOfFiled = false;
    public DOFData dofData = new DOFData
    {
        dofType = DOFType.Elementary,
        elementaryDof = new ElementaryDOF
        {
            focusDistance = 10.0f,
            nearBlurScale = 0.0f,
            farBlurScale = 50.0f,
            resolutionScale = 1,
            blur_anount = 1
        },
        advancedDof = new AdvancedDOF
        {
            dofBlur = 1,
            dofRange = 1,
            dofDistance = 0,
            focusObj = null
        }
    };
    #endregion
    #region LUT Variable
    [Header("色调映射-LUT")]
    public bool openLUT = false;
    public LUTData lutData = new LUTData
    {
        lut_Exposure = 0,
        lut_Temperature = 0,
        lut_Tint = 0,
        lut_HUE = 0,
        lut_Saturation = 1,
        lut_Contrast = 1
    };
    
    #endregion
    #region MeanBlur Variable
    [Header("均值模糊-MeanBlur")]
    public bool openMeanBlur = false;
    [Range(0.0f, 3.0f),Tooltip("模糊强度")]
    public float blurAmountMean = 0.5f;
    #endregion
    #region MotionBlur Variable
    [Header("运动模糊-MotionBlur")]
    public bool openMotionBlur = false;
    [Range(0.0f, 0.9f), Tooltip("模糊强度")]
    public float blurAmountMotion = 0.5f;
    #endregion
    #region RediaBlur Variable
    [Header("径向模糊-RediaBlur")]
    public bool openRediaBlur = false;
    public RediaBlurData rediaData = new RediaBlurData
    {
        SampleDist = 0.57f,
        SampleStrength = 2.09f
    };
    #endregion
    #region WideScreenVignette Variable
    [Header("屏幕边缘效果-WideScreenVignette")]
    public bool openWideScreenVignette = false;
    [Tooltip("效果类型")]
    public WideScreenType screenEffectType;
    public WideScreenData wideScreenData = new WideScreenData
    {
        viewArea = 0.55f,
        openingVal = 0.5f,
        viewAreaSmooth = 0.01f
    };
    #endregion
    //#region SSAO Variable
    //[Header("屏幕空间环境光遮蔽-SSAO")]
    //public bool openSSAO = false;
    //[HideInInspector]
    //public int[] randomSamples = new int[3] { 32, 32, 32 };
    //[Range(0.0f, 1.0f), Tooltip("采样范围半径")]
    //public float sampleRadius = 0.6f;
    //[Tooltip("采样方向标量")]
    //public Vector2 sampleDirectionVec;
    //#endregion
    private void InitMateial()
    {
        shader = Shader.Find("Effect/PostEffectManager");
        blendMat = new Material(shader);
        blendMat.hideFlags = HideFlags.HideAndDontSave;
        if (mainCamera==null)
            mainCamera = GetComponent<Camera>();
        if (mainCamera!=null)
        {
            mainCamera.allowHDR = false;
            mainCamera.allowMSAA = false;
        }
    }
    private void InitBaseData()
    {
        InitMateial();
        postBaseDic = new Dictionary<int, PostProcessingBase>();
        lateBloom = openBloom;
        lateFXAA = openFXAA;
        //lateMSAA = openMSAA;
        lateDOF = openDepthOfFiled;
        lateLUT = openLUT;
        lateMeanBlur = openMeanBlur;
        lateMotionBlur = openMotionBlur;
        lateRediaBlur = openRediaBlur;
        lateWideScreen = openWideScreenVignette;
        //lateSSAO = openSSAO;
    }
    void ChangePostEffectType()
    {
        if (lateBloom != openBloom)
        {
            if (openBloom)
            {
                postBase = new PostProcessingBloomBase();
                postBase.Enable(this);
                postBaseDic.Add((int)EffectType.Bloom, postBase);
            }
            else
            {
                blendMat.DisableKeyword("BLOOM");
                blendMat.DisableKeyword("QUALITYBLOOM");
                postBaseDic[(int)EffectType.Bloom].Disable();
                postBaseDic.Remove((int)EffectType.Bloom);
            }
            lateBloom = openBloom;
        }
        if (lateFXAA != openFXAA)
        {
            if (openFXAA)
            {
                blendMat.EnableKeyword("FXAA");
                postBase = new PostProcessingFXAA();
                postBase.Enable(this);
                postBaseDic.Add((int)EffectType.FXAA, postBase);
            }
            else
            {
                blendMat.DisableKeyword("FXAA");
                postBaseDic[(int)EffectType.FXAA].Disable();
                postBaseDic.Remove((int)EffectType.FXAA);
            }
            lateFXAA = openFXAA;
        }
        if (lateDOF != openDepthOfFiled)
        {
            if (openDepthOfFiled)
            {
                postBase = new PostProcessingDepthOfFiledBase();
                postBase.Enable(this, mainCamera);
                postBaseDic.Add((int)EffectType.DepthOfFiled, postBase);
            }
            else
            {
                blendMat.DisableKeyword("DOF");
                blendMat.DisableKeyword("QUALITYDOF");
                postBaseDic[(int)EffectType.DepthOfFiled].Disable(mainCamera);
                postBaseDic.Remove((int)EffectType.DepthOfFiled);
            }
            lateDOF = openDepthOfFiled;
        }
        if (lateLUT != openLUT)
        {
            if (openLUT)
            {
                blendMat.EnableKeyword("LUT");
                postBase = new PostProcessingLUT();
                postBase.Enable(this);
                postBaseDic.Add((int)EffectType.LUT, postBase);
            }
            else
            {
                blendMat.DisableKeyword("LUT");
                postBaseDic[(int)EffectType.LUT].Disable();
                postBaseDic.Remove((int)EffectType.LUT);
            }
            lateLUT = openLUT;
        }
        if (lateMeanBlur != openMeanBlur)
        {
            if (openMeanBlur)
            {
                blendMat.EnableKeyword("MEANBLUR");
                postBase = new PostProcessingMeanBlur();
                postBase.Enable(this);
                postBaseDic.Add((int)EffectType.MeanBlur, postBase);
            }
            else
            {
                blendMat.DisableKeyword("MEANBLUR");
                postBaseDic[(int)EffectType.MeanBlur].Disable();
                postBaseDic.Remove((int)EffectType.MeanBlur);
            }
            lateMeanBlur = openMeanBlur;
        }
        if (lateMotionBlur != openMotionBlur)
        {
            if (openMotionBlur)
            {
                blendMat.EnableKeyword("MOTIONBLUR");
                postBase = new PostProcessingMotionBlur();
                postBase.Enable(this);
                postBaseDic.Add((int)EffectType.MotionBlur, postBase);
            }
            else
            {
                blendMat.DisableKeyword("MOTIONBLUR");
                postBaseDic[(int)EffectType.MotionBlur].Disable();
                postBaseDic.Remove((int)EffectType.MotionBlur);
            }
            lateMotionBlur = openMotionBlur;
        }
        if (lateRediaBlur != openRediaBlur)
        {
            if (openRediaBlur)
            {
                blendMat.EnableKeyword("RADIABLUR");
                postBase = new PostProcessingRadiaBlur();
                postBase.Enable(this);
                postBaseDic.Add((int)EffectType.RediaBlur, postBase);
            }
            else
            {
                blendMat.DisableKeyword("RADIABLUR");
                postBaseDic[(int)EffectType.RediaBlur].Disable();
                postBaseDic.Remove((int)EffectType.RediaBlur);
            }
            lateRediaBlur = openRediaBlur;
        }
        if (lateWideScreen != openWideScreenVignette)
        {
            if (openWideScreenVignette)
            {
                blendMat.EnableKeyword("WINDSCREEN");
                postBase = new PostProcessingWideScreen();
                postBase.Enable(this);
                postBaseDic.Add((int)EffectType.WideScreenVignette, postBase);
            }
            else
            {
                blendMat.DisableKeyword("WINDSCREEN");
                postBaseDic[(int)EffectType.WideScreenVignette].Disable();
                postBaseDic.Remove((int)EffectType.WideScreenVignette);
            }
            lateWideScreen = openWideScreenVignette;
        }
        if (openBloom && openDepthOfFiled)
            blendMat.EnableKeyword("MULTIPASS");
        else
            blendMat.DisableKeyword("MULTIPASS");
        //if (lateSSAO != openSSAO)
        //{
        //    if (openSSAO)
        //    {
        //        blendMat.EnableKeyword("SSAO");
        //        postBase = new PostProcessingSSAO();
        //        postBase.Enable(this,mainCamera);
        //        postBaseDic.Add(EffectType.SSAO, postBase);
        //    }
        //    else
        //    {
        //        blendMat.DisableKeyword("SSAO");
        //        postBaseDic[EffectType.SSAO].Disable(mainCamera);
        //        postBaseDic.Remove(EffectType.SSAO);
        //    }
        //    lateSSAO = openSSAO;
        //}
    }
    private void Awake()
    {
        RenderTargetInitializer();
        InitBaseData();
    }
    private void Update()
    {
        ChangePostEffectType();
    }
    private void OnDisable()
    {
        ReleaseRenderTarget();
        if (blendMat)
            DestroyImmediate(blendMat);
    }
    private void OnDestroy()
    {
        ReleaseRenderTarget();
    }
    private void OnPreRender()
    {
        enableBloom = openBloom;
        enableMSAA = openMSAA;
        if (renderTargetNeedInit)
        {
            ReleaseRenderTarget();
            RenderTargetInitializer();
            renderTargetNeedInit = false;
        }
        mainCamera.targetTexture = renderTarget;
    }
    private void OnPostRender()
    {
        if (shader == null || blendMat == null)
            InitMateial();
        mainCamera.targetTexture = null;
        if (postBaseDic == null) return;
        IDictionaryEnumerator it = postBaseDic.GetEnumerator();
        while(it.MoveNext())
        {
            EffectType type = (EffectType)it.Key;
            postBaseDic[(int)type].PostRender(renderTarget, mainCamera.targetTexture, blendMat, mainCamera);
        }
        Graphics.Blit(renderTarget, null, blendMat,0);
    }

    private void RenderTargetInitializer()
    {
        RenderTextureFormat rtf = openBloom ? PostProcessingBase.ChangeRenderTextureFormat(RenderTextureFormat.RGB111110Float) : RenderTextureFormat.ARGB32;
        string msaalevel = openMSAA ? "MSAAx4" : "Base";
        renderTarget = new RenderTexture(resolutionWidth[(int)devLevel], resolutionHeight[(int)devLevel], 24, rtf, RenderTextureReadWrite.Linear)
        {
            antiAliasing = openMSAA ? 4 : 1,
            name = "PostProcessing_" + msaalevel,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        renderTarget.Create();
    }
    private void ReleaseRenderTarget()
    {
        mainCamera.targetTexture = null;

        if (renderTarget != null)
        {
            renderTarget.Release();
            Destroy(renderTarget);
            renderTarget = null;
        }
        renderTargetNeedInit = true;
    }
    private void SetPostProcessingRenderLevel()
    {
        switch (devLevel)
        {
            case MobileDevicelevel.High:
                {
                    openBloom = true;
                    bloomData.bloomType = BloomType.Advanced;
                    openMSAA = true;
                    break;
                }
            case MobileDevicelevel.Low:
                {
                    if (!openBloom)
                        openBloom = true;
                    bloomData.bloomType = BloomType.Elementary;
                    if (openMSAA)
                        openMSAA = false;
                    break;
                }
        }
    }
}


