using UnityEngine;
using Sirenix.OdinInspector;

namespace Custom.Cloud
{
    [CreateAssetMenu(fileName = "Texture Setting", menuName = "Cloud/Texture Setting")]
    public class TextureSetting : ScriptableObject
    {
        [HideInInspector]
        public bool isFoldoutSetting;
        [HideInInspector]
        public NoiseSetting currentNoiseSetting;
        public TextureSize textureSize = TextureSize.normal_128x128;
        // [LabelText("贴图分辨率")]
        // [ValueDropdown("TestureSizes")]
        public int resolution = 64;
        // [LabelText("通道选择")]
        // [EnumToggleButtons]
        public Channel channel;
        // [FoldoutGroup("通道噪点设置")]
        // [LabelText("R通道噪点设置")]
        public NoiseSetting rSetting;
        // [FoldoutGroup("通道噪点设置")]
        // [LabelText("G通道噪点设置")]
        public NoiseSetting gSetting;
        // [FoldoutGroup("通道噪点设置")]
        // [LabelText("B通道噪点设置")]
        public NoiseSetting bSetting;
        // [FoldoutGroup("通道噪点设置")]
        // [LabelText("A通道噪点设置")]
        public NoiseSetting aSetting;

        public void UpdateCurrentNoiseSetting(Channel currentChannel)
        {
            switch (currentChannel)
            {
                case Channel.R:
                    currentNoiseSetting = rSetting;
                    break;
                case Channel.G:
                    currentNoiseSetting = gSetting;
                    break;
                case Channel.B:
                    currentNoiseSetting = bSetting;
                    break;
                default:
                    currentNoiseSetting = aSetting;
                    break;
            }
        }
    }
}
