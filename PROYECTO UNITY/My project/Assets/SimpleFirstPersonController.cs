using UnityEngine;

// Script de movimiento del avatar (cuerpo). La ROTACIÓN de la cámara ya NO
// se maneja aquí: ahora la controla el componente "Tracked Pose Driver"
// puesto directamente sobre Main Camera, que recibe los datos reales del
// visor (celular + VRidge) vía OpenXR.
//
// Este script solo mueve el CUERPO del avatar, caminando hacia donde esté
// mirando la cámara en cada momento (típico en VR simple sin controles de
// movimiento propios). Usa los botones físicos W/A/S/D si hay Bluetooth
// asignado, o el teclado como respaldo.

[RequireComponent(typeof(CharacterController))]
public class SimpleFirstPersonController : MonoBehaviour
{
    [Header("Velocidad")]
    public float moveSpeed = 3f;

    [Header("Referencias")]
    public Transform cameraTransform; // la Main Camera, con su Tracked Pose Driver

    [Header("Entrada real (arrastra aquí el objeto GestorBluetooth)")]
    public ConexionBluetooth bluetooth;

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float moveX;
        float moveZ;

        if (bluetooth != null)
        {
            moveZ = 0f;
            if (bluetooth.ObtenerBotonW()) moveZ += 1f;
            if (bluetooth.ObtenerBotonS()) moveZ -= 1f;

            moveX = 0f;
            if (bluetooth.ObtenerBotonD()) moveX += 1f;
            if (bluetooth.ObtenerBotonA()) moveX -= 1f;
        }
        else
        {
            moveX = Input.GetAxis("Horizontal");
            moveZ = Input.GetAxis("Vertical");
        }

        // Dirección "adelante" y "derecha" según hacia dónde mira la cámara
        // (aplanadas en el eje Y para no caminar hacia el techo/piso al
        // inclinar la cabeza hacia arriba o abajo).
        Vector3 adelanteCamara = cameraTransform.forward;
        adelanteCamara.y = 0f;
        adelanteCamara.Normalize();

        Vector3 derechaCamara = cameraTransform.right;
        derechaCamara.y = 0f;
        derechaCamara.Normalize();

        Vector3 move = derechaCamara * moveX + adelanteCamara * moveZ;
        controller.Move(move * moveSpeed * Time.deltaTime);
    }
}