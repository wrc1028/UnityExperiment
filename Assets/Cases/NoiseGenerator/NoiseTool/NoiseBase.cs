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

    public enum NoiseType { Normal, Worley, Perlin, }

    public struct SingleLayerNoiseSetting
    {
        public int noiseType;
        public float mixWeight;
        public int subdivideNum;
        public int isInvert;
        public float minValue;
        public float maxValue;
    }

    public class NoiseBase
    {
        public struct NoiseData
        {
            public bool isUsed;
            public float mixWeight;
            public NoiseType noiseType;
            public int subdivideNum;
            public int prevSubdivideNum;
            public bool isInvert;
            public Vector3[] randomPoints;
            public float minValue;
            public float maxValue;
        }
        public NoiseData Layer01;
        public NoiseData Layer02;
        public NoiseData Layer03;
        public NoiseData Layer04;
        public RenderTexture previewRT;
        public Vector4 MixWeight 
        { 
            get
            {
                Vector4 weight = new Vector4(Layer01.mixWeight, Layer02.mixWeight, Layer03.mixWeight, Layer04.mixWeight);
                return weight.normalized;
            }
        }
        
        public NoiseBase()
        {
            Layer01.isUsed = false;
            Layer01.mixWeight = 1;
            Layer01.subdivideNum = 4;
            Layer01.minValue = -1;
            Layer01.maxValue = 1;
            CreateRandomValue(out Layer01.randomPoints, Layer01.subdivideNum, Layer01.noiseType);

            Layer02.isUsed = false;
            Layer02.subdivideNum = 8;
            Layer02.minValue = -1;
            Layer02.maxValue = 1;
            CreateRandomValue(out Layer02.randomPoints, Layer02.subdivideNum, Layer02.noiseType);

            Layer03.isUsed = false;
            Layer03.subdivideNum = 12;
            Layer03.minValue = -1;
            Layer03.maxValue = 1;
            CreateRandomValue(out Layer03.randomPoints, Layer03.subdivideNum, Layer03.noiseType);

            Layer04.isUsed = false;
            Layer04.subdivideNum = 16;
            Layer04.minValue = -1;
            Layer04.maxValue = 1;
            CreateRandomValue(out Layer04.randomPoints, Layer04.subdivideNum, Layer04.noiseType);
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
