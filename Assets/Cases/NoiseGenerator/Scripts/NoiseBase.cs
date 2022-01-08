using System;

namespace Custom.Cloud
{
    [Serializable]
    public enum Channel { R, G, B, A, }
    [Serializable]
    public enum TexType { Shape, Detail, }
    
    public enum TextureSize
    {
        small_32x32 = 32,
        median_64x64 = 64,
        normal_128x128 = 128,
        large_256x256 = 256,
    }
}