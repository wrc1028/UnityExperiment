using UnityEngine;

public class SSPRWater : MonoBehaviour
{
    private float distance2;
    private SSPR sspr;
    private void Start()
    {
        sspr = Camera.main.GetComponent<SSPR>();
    }
    
    private void OnWillRenderObject() 
    {
        if (sspr == null) return;
        // 判断场景中有没有水, 也就是有没有这个物体
        Vector3 cameraToWater = transform.position -  Camera.main.transform.position;
        distance2 = Vector3.Dot(cameraToWater, cameraToWater);
        sspr.waterHeightAndDistance2s.Add(new Vector2(transform.position.y, distance2));
    }

}
