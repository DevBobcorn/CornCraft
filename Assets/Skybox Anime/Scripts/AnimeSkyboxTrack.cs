using UnityEngine;
using UnityEngine.Timeline;

namespace AnimeSkybox
{
    [TrackClipType(typeof (AnimeSkyboxAsset))] // 表明这个轨道接受MySkyboxAsset类型的片段
    [TrackBindingType(typeof (Material))] // 表明这个轨道可以绑定到一个Material类型的对象
    public class AnimeSkyboxTrack : TrackAsset
    {
        // 这里你可以添加自定义的功能，但是这个基本的版本应该就足够了
    }
}