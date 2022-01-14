using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Experiment.Culling
{
    public class GPUCulling : MonoBehaviour
    {
        public Material notInSight;
        public Material inSight;
        public GameObject cubeContainer;
        private Vector3[] farClipPlanePoints;
        private Camera mainCamera;
        private void Start()
        {
            mainCamera = Camera.main;
            
        }
        [Button("判断")]
        public void Excute()
        {
            // 排除算法
            MeshRenderer[] cubeMeshs = cubeContainer.GetComponentsInChildren<MeshRenderer>();
            Vector4[] cameraFrustumPlanes = GetCameraFrustumPlane(Camera.main);
            foreach (var mesh in cubeMeshs)
            {
                Vector3[] boundsPoints = BoundsPoints(mesh.bounds);
                foreach (var point in boundsPoints)
                {
                    int inFrustumPlaneCount = 0;
                    foreach (var plane in cameraFrustumPlanes)
                    {
                        if (Vector3.Dot(new Vector3(plane.x, plane.y, plane.z), point) + plane.w < 0)
                            inFrustumPlaneCount++;
                    }
                    if (inFrustumPlaneCount == 6)
                    {
                        mesh.sharedMaterial = inSight;
                        // mesh.transform.gameObject.SetActive(true);
                        break;
                    }
                    else
                    {
                        mesh.sharedMaterial = notInSight;
                        // mesh.transform.gameObject.SetActive(false);
                    }
                }
            }
        }
        private Vector3[] BoundsPoints(Bounds bounds)
        {
            Vector3[] boundsPoints = new Vector3[8];
            Vector3 size = (bounds.max - bounds.min) / 2;
            boundsPoints[0] = bounds.min;
            boundsPoints[1] = bounds.min + Vector3.right * size.x;
            boundsPoints[2] = bounds.min + Vector3.up * size.y;
            boundsPoints[3] = bounds.min + Vector3.forward * size.z;
            boundsPoints[4] = bounds.max;
            boundsPoints[5] = bounds.max - Vector3.right * size.x;
            boundsPoints[6] = bounds.max - Vector3.up * size.y;
            boundsPoints[7] = bounds.max - Vector3.forward * size.z;
            return boundsPoints;
        }

        /// <summary>
        ///  获得视锥体的六个面
        /// </summary>
        /// <param name="camera">目标相机</param>
        /// <returns>六个面的四维向量结果</returns>
        private Vector4[] GetCameraFrustumPlane(Camera camera)
        {
            Vector4[] cameraFrustumPlanes = new Vector4[6];
            Vector3[] farClipPlanePoints = GetFarClipPlanePoints(camera);
            // 前后
            cameraFrustumPlanes[0] = GetPlane(-camera.transform.forward, camera.transform.position + camera.transform.forward * camera.nearClipPlane);
            cameraFrustumPlanes[1] = GetPlane(camera.transform.forward, camera.transform.position + camera.transform.forward * camera.farClipPlane);
            // 上右下左
            cameraFrustumPlanes[2] = GetPlane(camera.transform.position, farClipPlanePoints[0], farClipPlanePoints[1]);
            cameraFrustumPlanes[3] = GetPlane(camera.transform.position, farClipPlanePoints[1], farClipPlanePoints[2]);
            cameraFrustumPlanes[4] = GetPlane(camera.transform.position, farClipPlanePoints[2], farClipPlanePoints[3]);
            cameraFrustumPlanes[5] = GetPlane(camera.transform.position, farClipPlanePoints[3], farClipPlanePoints[0]);
            return cameraFrustumPlanes;
        }
        /// <summary>
        /// 获得四个远裁面的的点
        /// </summary>
        /// <param name="camera">目标相机</param>
        /// <returns>始于左上角顺时针排序四个点</returns>
        private Vector3[] GetFarClipPlanePoints(Camera camera)
        {
            // 有局限性：比如特殊场景下改变了视锥体，这个公式就不能获得正确的点
            // 使用相机的矩阵反向变换的到视锥体的点
            Vector3[] farClipPlanePoints = new Vector3[4];
            float halfRadFOV = Mathf.Deg2Rad * camera.fieldOfView * 0.5f;
            float farClipDst = camera.farClipPlane;
            float aspect = camera.aspect;
            float farClipPlaneHeight = Mathf.Tan(halfRadFOV) * farClipDst;
            float farClipPlaneWidth = farClipPlaneHeight * aspect;
            Vector3 farClipPlaneCenterPoint = camera.transform.position + camera.transform.forward * farClipDst;
            farClipPlanePoints[0] = farClipPlaneCenterPoint + camera.transform.up * farClipPlaneHeight - camera.transform.right * farClipPlaneWidth;
            farClipPlanePoints[1] = farClipPlaneCenterPoint + camera.transform.up * farClipPlaneHeight + camera.transform.right * farClipPlaneWidth;
            farClipPlanePoints[2] = farClipPlaneCenterPoint - camera.transform.up * farClipPlaneHeight + camera.transform.right * farClipPlaneWidth;
            farClipPlanePoints[3] = farClipPlaneCenterPoint - camera.transform.up * farClipPlaneHeight - camera.transform.right * farClipPlaneWidth;
            return farClipPlanePoints;
        }
        /// <summary>
        /// 三点确定平面
        /// </summary>
        /// <param name="a">顺时针方向第一个点</param>
        /// <param name="b">顺时针方向第二个点</param>
        /// <param name="c">顺时针方向第三个点</param>
        /// <returns>平面的四维向量</returns>
        private Vector4 GetPlane(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            return GetPlane(normal, a);
        }
        /// <summary>
        /// 三维空间下的任何平面都可以使用一个四维向量表示
        /// 其中前三位是平面的法线
        /// 后一位是平面到原点的最短距离
        /// </summary>
        /// <param name="normal">平面法线</param>
        /// <param name="pointOnThePlane">位于平面上的任意一点</param>
        /// <returns>平面的四维向量</returns>
        private Vector4 GetPlane(Vector3 normal, Vector3 pointOnThePlane)
        {
            return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(pointOnThePlane, normal));
        }
    }
}