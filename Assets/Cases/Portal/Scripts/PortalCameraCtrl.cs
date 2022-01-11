using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalCameraCtrl : MonoBehaviour
{
    // 视角移动
    public float mouseSensitivity = 100.0f;

    private Transform playBodyTrans;
    private float xRotation = 0.0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        playBodyTrans = transform.parent.transform;
        transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0.0f, 0.0f);
        playBodyTrans.Rotate(Vector3.up * mouseX);
    }

    private void OnWillRenderObject()
    {
        
    }
}
