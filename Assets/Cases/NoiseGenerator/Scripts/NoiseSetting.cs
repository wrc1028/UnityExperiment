using UnityEngine;

namespace Custom.Cloud
{
    [CreateAssetMenu(fileName = "Noise Setting", menuName = "Cloud/Noise Setting")]
    public class NoiseSetting : ScriptableObject
    {
        // [LabelText("第一层细分数")]
        // [Range(1, 32)]
        public int layer01DivisionNum = 4;
        // [LabelText("第二层细分数")]
        // [Range(1, 32)]
        public int layer02DivisionNum = 8;
        // [LabelText("第三层细分数")]
        // [Range(1, 32)]
        public int layer03DivisionNum = 12;
        // [LabelText("叠加因子")]
        // [Range(0, 1)]
        public float persistence = 0;

        // [LabelText("采样高度")]
        // [Range(0, 1)]
        public float sliderValue;
        
        // [LabelText("反转结果")]
        public bool isInvert = false;

        // 结果
        public Vector3[] layer01Points;
        public Vector3[] layer02Points;
        public Vector3[] layer03Points;

        // 初始化
        private void OnEnable()
        {
            if (layer01Points == null)
                layer01Points = GetRandomPoint(layer01DivisionNum);
            if (layer02Points == null)
                layer02Points = GetRandomPoint(layer02DivisionNum);
            if (layer03Points == null)
                layer03Points = GetRandomPoint(layer03DivisionNum);
        }

        // 获得随机位置
        public Vector3[] GetRandomPoint(int cellNums)
        {
            Vector3[] points = new Vector3[cellNums * cellNums * cellNums];
            float cellSize = 1.0f / cellNums;
            for (int x = 0; x < cellNums; x++)
            {
                for (int y = 0; y < cellNums; y++)
                {
                    for (int z = 0; z < cellNums; z++)
                    {
                        Vector3 offsetPos = new Vector3(Random.value, Random.value, Random.value);
                        Vector3 pointPos = (new Vector3(x, y, z) + offsetPos) * cellSize;
                        int index = x + (y + z * cellNums) * cellNums;
                        points[index] = pointPos;
                    }
                }
            }
            return points;
        }
    }
}
