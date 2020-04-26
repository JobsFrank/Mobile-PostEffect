using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessingDepthOfFiledBase : PostProcessingBase
{
    private PostProcessingManager postManager = null;
    private Dictionary<int, PostProcessingDepthOfFiledBase> dofDic;// key type DOFType
    private void Init()
    {
        dofDic = new Dictionary<int, PostProcessingDepthOfFiledBase>();
        PostProcessingDepthOfFiled obj_normal = new PostProcessingDepthOfFiled();
        dofDic.Add((int)DOFType.Elementary, obj_normal);
        PostProcessingQualityDepthOfFiled quality_obj = new PostProcessingQualityDepthOfFiled();
        dofDic.Add((int)DOFType.Advanced, quality_obj);
    }

    public override void Enable(PostProcessingManager post, Camera camera = null)
    {
        postManager = post;
        Init();
        dofDic[(int)DOFType.Elementary].Enable(post,camera);
        dofDic[(int)DOFType.Advanced].Enable(post, camera);
    }
    public override void Disable(Camera camera = null)
    {
        dofDic[(int)postManager.dofData.dofType].Disable(camera);
        dofDic.Clear();
        dofDic = null;
    }
    public override void PostRender(RenderTexture src, RenderTexture teg, Material blendMat, Camera camera = null)
    {
        if (postManager.dofData.dofType == DOFType.Advanced)
        {
            blendMat.EnableKeyword("QUALITYDOF");
            blendMat.DisableKeyword("DOF");
        }
        else if (postManager.dofData.dofType == DOFType.Elementary)
        {
            blendMat.EnableKeyword("DOF");
            blendMat.DisableKeyword("QUALITYDOF");
        }
        dofDic[(int)postManager.dofData.dofType].PostRender(src, teg, blendMat, camera);
    }
}
