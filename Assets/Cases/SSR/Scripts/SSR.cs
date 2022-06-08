using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SSR : MonoBehaviour
{
    public bool EnableSSR = true;
    private bool isAddCMD = false;
    private Camera mainCamera;
    private CommandBuffer copyCameraColorCMD;
    private static int cameraColorTextureId = Shader.PropertyToID("_CustomColorTexture");

    private RenderTexture colorTargetTexture;
    private RenderTexture depthTargetTexture;
    private void Start() 
    {
        if (!TryGetComponent<Camera>(out mainCamera)) EnableSSR = false;
        else mainCamera.depthTextureMode = DepthTextureMode.Depth;
    }
    
    private void OnPreRender() 
    {
        if (EnableSSR) CopyCameraColor();
        else CleanupBuffer();
    }
    private void CopyCameraColor()
    {
        if (isAddCMD) return;
        copyCameraColorCMD = new CommandBuffer { name = "Grab Screen" };

        copyCameraColorCMD.GetTemporaryRT(cameraColorTextureId, Screen.width, Screen.height, 0, FilterMode.Bilinear);
        copyCameraColorCMD.Blit(BuiltinRenderTextureType.CurrentActive, cameraColorTextureId);

        mainCamera.AddCommandBuffer(CameraEvent.AfterSkybox, copyCameraColorCMD);
        isAddCMD = true;
    }

    private void CleanupBuffer()
    {
        if (!isAddCMD) return;
        copyCameraColorCMD.ReleaseTemporaryRT(cameraColorTextureId);
        mainCamera.RemoveCommandBuffer(CameraEvent.AfterSkybox, copyCameraColorCMD);
        isAddCMD = false;
    }
}
