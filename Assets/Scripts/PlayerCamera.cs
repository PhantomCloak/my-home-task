using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float Sensivity = 0.3f;
    public float ZoomSensivity = 1f;
    public float RotationSensitivity = 10f;

    private GameObject lastClickedObject;

    void Update()
    {
        Vector3 moveDirection = (transform.right * (Input.GetKey(KeyCode.D) ? Sensivity : Input.GetKey(KeyCode.A) ? -Sensivity : 0)) +
                        (transform.forward * (Input.GetKey(KeyCode.W) ? Sensivity : Input.GetKey(KeyCode.S) ? -Sensivity : 0));
        moveDirection.y = 0;
        transform.position += moveDirection;

        if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        {
            float zoomAmount = Input.GetAxis("Mouse ScrollWheel") * ZoomSensivity;
            transform.position = transform.position + transform.forward * zoomAmount;
        }

        if (Input.GetMouseButton(2) || Input.GetMouseButton(1))
        {
            float yaw = Input.GetAxis("Mouse X") * RotationSensitivity;
            transform.Rotate(0, yaw, 0, Space.World);
        }
    }
}

