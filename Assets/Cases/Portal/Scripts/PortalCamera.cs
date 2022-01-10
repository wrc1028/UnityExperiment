using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PortalCamera : MonoBehaviour
{
    struct Portal
    {
        public Camera camera;
        public Transform transform;
        public MeshRenderer mesh;
    }

    public GameObject portalRed;
    public GameObject portalBlue;
    private Portal redPortal;
    private Portal bluePortal;
    private Camera mainCamera;
    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        redPortal = InitPortal(mainCamera, portalRed);
        bluePortal = InitPortal(mainCamera, portalBlue);
        // 位置变换
        // UpdatePortalCameraMatrix(portal_A.transform, portal_B.transform, this.transform, portalCamera.transform);
        // Matrix4x4 tempMatrix = UpdatePortalCameraMatrix(portalRed.transform.worldToLocalMatrix, portalBlue.transform.localToWorldMatrix, transform.localToWorldMatrix);
        // portalCamera.transform.SetPositionAndRotation(tempMatrix.GetColumn(3), tempMatrix.rotation);
    }
    // 初始化Screen
    private Portal InitPortal(Camera camera, GameObject portal)
    {
        Portal tempPortal = new Portal();
        tempPortal.camera = portal.GetComponentInChildren<Camera>();
        tempPortal.camera.CopyFrom(camera);
        tempPortal.transform = portal.transform;
        tempPortal.mesh = portal.transform.Find("Screen").GetComponent<MeshRenderer>();
        return tempPortal;
    }
    
    private void Update()
    {
        // UpdateCameraView();
        // Matrix4x4 tempMatrix = UpdatePortalCameraMatrix(srcPortal.transform.worldToLocalMatrix, destPortal.transform.localToWorldMatrix, transform.localToWorldMatrix);
        // portalCamera.transform.SetPositionAndRotation(tempMatrix.GetColumn(3), tempMatrix.rotation);
    }
    // 更新传送门相机信息




    // 更新Portal的Trans
    private void UpdatePortalCameraMatrix(Transform srcPortal, Transform destPortal, Transform srcTrans, Transform destTrans)
    {
        destPortal.Rotate(Vector3.up * 180);
        Debug.Log(destPortal.localToWorldMatrix);
        Matrix4x4 transformMatrix = destPortal.localToWorldMatrix * srcPortal.worldToLocalMatrix * srcTrans.localToWorldMatrix;
        destPortal.Rotate(Vector3.up * 180);
        Debug.Log(destPortal.localToWorldMatrix);
        destTrans.SetPositionAndRotation(transformMatrix.GetColumn(3), transformMatrix.rotation);
    }
    private Matrix4x4 UpdatePortalCameraMatrix(Matrix4x4 srcPortalW2L, Matrix4x4 destPortalL2W, Matrix4x4 srcCameraL2W)
    {
        // destPortalL2W[0, 0] *= -1;
        // destPortalL2W[2, 2] *= -1;
        return destPortalL2W * srcPortalW2L * srcCameraL2W;
    }
    // 获得原相机的参数设置
    private void GetCurrentCameraProp(Camera srcCamera, ref Camera targetCamera)
    {
        if (targetCamera == null) targetCamera = new Camera();

        targetCamera.clearFlags = srcCamera.clearFlags;
        targetCamera.backgroundColor = srcCamera.backgroundColor;
        targetCamera.cullingMask = srcCamera.cullingMask;
        targetCamera.fieldOfView = srcCamera.fieldOfView;
        targetCamera.nearClipPlane = srcCamera.nearClipPlane;
        targetCamera.farClipPlane = srcCamera.farClipPlane;
        targetCamera.aspect = srcCamera.aspect;
        targetCamera.orthographicSize = srcCamera.orthographicSize;
    }
    // 相机视角移动
    private void UpdateCameraView()
    {
        // float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        // float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
    }
}
