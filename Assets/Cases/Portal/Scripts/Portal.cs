using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    }

    private void Update()
    {
        PortalCameraRender();
    }
    
    private void PortalCameraRender()
    {
        if (linkPortal == null) return;
        Matrix4x4 portalMatrix = transform.localToWorldMatrix;
        Matrix4x4 linkPortalMatrix = linkPortal.transform.worldToLocalMatrix;
        Matrix4x4 transformMatrix = portalMatrix * linkPortalMatrix * mainCamera.transform.localToWorldMatrix;
        portalCamera.transform.SetPositionAndRotation(transformMatrix.GetColumn(3), transformMatrix.rotation);
    }

}
