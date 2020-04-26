using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**************************************************
*  Copyright: newstyle games 
*  Author: zcm
*  Date:2018-4-28 
*  Description:镜头后处理-全屏泛光基类(管理两种bloom）
**************************************************/
public class PostProcessingBloomBase : PostProcessingBase
{
    private PostProcessingManager postManager = null;
    private Dictionary<int, PostProcessingBloomBase> bloomDic; // key type BloomType
    private void Init()
    {
        bloomDic = new Dictionary<int, PostProcessingBloomBase>();     
        PostProcessingBloom obj_normal = new PostProcessingBloom();
        bloomDic.Add((int)BloomType.Elementary, obj_normal);
        PostProcessingQualityBloom obj_quality = new PostProcessingQualityBloom();
        bloomDic.Add((int)BloomType.Advanced, obj_quality);
    }
    public override void Enable(PostProcessingManager post, Camera camera = null)
    {
        postManager = post;
        Init();
        bloomDic[(int)BloomType.Elementary].Enable(post, camera);
        bloomDic[(int)BloomType.Advanced].Enable(post, camera);
    }
    public override void Disable(Camera camera = null)
    {
        bloomDic[(int)postManager.bloomData.bloomType].Disable(camera);
        bloomDic.Clear();
        bloomDic = null;
    }
    public override void PostRender(RenderTexture src, RenderTexture teg, Material blendMat, Camera camera = null)
    {
        if (postManager.bloomData.bloomType == BloomType.Advanced)
        {
            blendMat.EnableKeyword("QUALITYBLOOM");
            blendMat.DisableKeyword("BLOOM");
        }
        else if (postManager.bloomData.bloomType == BloomType.Elementary)
        {
            blendMat.EnableKeyword("BLOOM");
            blendMat.DisableKeyword("QUALITYBLOOM");
        }
        bloomDic[(int)postManager.bloomData.bloomType].PostRender(src, teg, blendMat, camera);
    }
}
