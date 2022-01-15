using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Custom.Noise
{
    [System.Flags]
    public enum Channel 
    { 
        R = 1 << 1,
        G = 1 << 2,
        B = 1 << 4,
        A = 1 << 0,
        RGBA = R | G | B | A,
    }
    public enum Dimensionality { _2DTexture, _3DTexture, }
    public enum Resolution2D { _32x32 = 32, _64x64 = 64, _128x128 = 128, _256x256 = 256, _512x512 = 512, _1024x1024 = 1024, _2048x2048 = 2048, }
    public enum SaveType { PNG, JPG, TGA, }
    public enum Resolution3D { _32x32 = 32, _64x64 = 64, _128x128 = 128, _256x256 = 256, }

    public enum NoiseType { Normal, Perlin, Worley, }

    public class NoiseBase
    {
        public struct NoiseData
        {
            public bool isStartUsing;
            public float mixWeight;
            public NoiseType noiseType;
            public int subdivideNum;
            public int prevSubdivideNum;
            public bool isInvert;
            public Vector3[] randomValue;
        }
        public NoiseData Layer01;
        public NoiseData Layer02;
        public NoiseData Layer03;
        public NoiseData Layer04;
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
            Layer01.isStartUsing = true;
            Layer01.mixWeight = 1;
            Layer01.subdivideNum = 1;
            CreateRandomValue(out Layer01.randomValue, Layer01.subdivideNum);

            Layer02.isStartUsing = false;
            Layer02.subdivideNum = 1;
            CreateRandomValue(out Layer02.randomValue, Layer02.subdivideNum);

            Layer03.isStartUsing = false;
            Layer03.subdivideNum = 1;
            CreateRandomValue(out Layer03.randomValue, Layer03.subdivideNum);

            Layer04.isStartUsing = false;
            Layer04.subdivideNum = 1;
            CreateRandomValue(out Layer04.randomValue, Layer04.subdivideNum);
        }

        /// <summary>
        /// 创建随机三维值
        /// </summary>
        /// <param name="result">三维结果，范围[0-1]</param>
        /// <param name="subdivideNum">细分数</param>
        public static void CreateRandomValue(out Vector3[] result, int subdivideNum)
        {
            result = new Vector3[subdivideNum * subdivideNum * subdivideNum];
            for (int z = 0; z < subdivideNum; z++)
            {
                for (int y = 0; y < subdivideNum; y++)
                {
                    for (int x = 0; x < subdivideNum; x++)
                    {
                        int index = x + (y + z * subdivideNum) * subdivideNum;
                        result[index] = new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                    }
                }
            }
        }

        public static void CreateTexture()
        {
            
        }
    }
}
