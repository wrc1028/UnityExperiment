using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteInEditMode]
public class Portal : MonoBehaviour
{
    public Portal linkPortal;
    public MeshRenderer screen;
    private Camera mainCamera;
    private Camera portalCamera;
    private RenderTexture screenTexture;

    private void Start()
    {
        mainCamera = Camera.main;
        portalCamera = GetComponentInChildren<Camera>();
        portalCamera.CopyFrom(mainCamera);

        screenTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        portalCamera.targetTexture = screenTexture;
        portalCamera.Render();
        screen.sharedMaterial.SetTexture("_MainTex", screenTexture);
    }

    private void Update()
    {
        PortalCameraRender();
    }
    
    public void PortalCameraRender()
    {
        if (linkPortal == null) return;
        // 更改相机位置
        Matrix4x4 transformMatrix = GetPortalMatrix();
        portalCamera.transform.SetPositionAndRotation(transformMatrix.GetColumn(3), transformMatrix.rotation);
    }

    private Matrix4x4 GetPortalMatrix()
    {
        Matrix4x4 portalL2WMatrix = linkPortal.transform.localToWorldMatrix;
        portalL2WMatrix[0, 0] *= -1; portalL2WMatrix[0, 2] *= -1;
        portalL2WMatrix[2, 0] *= -1; portalL2WMatrix[2, 2] *= -1;
        return portalL2WMatrix * transform.worldToLocalMatrix * mainCamera.transform.localToWorldMatrix;
    }
}
