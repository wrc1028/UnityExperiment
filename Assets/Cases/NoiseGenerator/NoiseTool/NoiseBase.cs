using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Custom.Noise
{
    [System.Flags]
    public enum Channel 
    { 
        R = 0,
        G = 1,
        B = 2,
        A = 3,
        // RGBA = 4
    }
    public enum Dimensionality { _2DTexture, _3DTexture, }
    public enum Resolution2D { _32x32 = 32, _64x64 = 64, _128x128 = 128, _256x256 = 256, _512x512 = 512, _1024x1024 = 1024, _2048x2048 = 2048, }
    public enum SaveType { PNG, JPG, TGA, }
    public enum Resolution3D { _32x32 = 32, _64x64 = 64, _128x128 = 128, _256x256 = 256, }
    public enum MixType { Uniform, FBM, FBM_Cell, Custom}

    public enum NoiseType { Hashing, Worley, Perlin, }

    public struct SingleLayerNoiseSetting
    {
        public int noiseType;
        public float mixWeight;
        public int cellNums;
        public int isInvert;
        public float minValue;
        public float maxValue;
    }

    public class NoiseBase
    {
        public struct NoiseData
        {
            public float mixWeight;
            public NoiseType noiseType;
            public int cellNums;
            public int prevSubdivideNum;
            public bool isInvert;
            public Vector3[] randomPoints;
            public float minValue;
            public float maxValue;
        }
        public int noiseLayerCount;
        public MixType mixType;
        public NoiseData[] layers;
        
        public NoiseBase()
        {
            layers = new NoiseData[4];
            mixType = MixType.Uniform;
            for (int i = 0; i < 4; i++)
            {
                layers[i].mixWeight = i == 0 ? 1 : 0;
                layers[i].cellNums = 4 * (i + 1);
                layers[i].minValue = 0;
                layers[i].maxValue = 1;
                CreateRandomValue(out layers[i].randomPoints, layers[i].cellNums, layers[i].noiseType);
            }
        }

        /// <summary>
        /// 创建随机三维值
        /// </summary>
        /// <param name="result">三维结果，范围[0-1]</param>
        /// <param name="subdivideNum">细分数</param>
        public static void CreateRandomValue(out Vector3[] result, int subdivideNum, NoiseType noiseType)
        {
            result = new Vector3[subdivideNum * subdivideNum * subdivideNum];
            for (int z = 0; z < subdivideNum; z++)
            {
                for (int y = 0; y < subdivideNum; y++)
                {
                    for (int x = 0; x < subdivideNum; x++)
                    {
                        int index = x + (y + z * subdivideNum) * subdivideNum;
                        Vector3 offsetPos = new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                        if (noiseType == NoiseType.Worley)
                        {
                            offsetPos += new Vector3(x, y, z);
                            offsetPos /= subdivideNum;
                        }
                        else if (noiseType == NoiseType.Perlin)
                        {
                            offsetPos.x = offsetPos.x * 2 - 1;
                            offsetPos.y = offsetPos.y * 2 - 1;
                            offsetPos.z = offsetPos.z * 2 - 1;
                        }
                        result[index] = offsetPos;
                    }
                }
            }
        }

        public static void CreateTexture()
        {
            
        }
    }
}
