using UnityEngine;

public class RotateSkyBox : MonoBehaviour
{
    public float rotationSpeed = 1.2f;

    void Update()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * rotationSpeed);
    }
}