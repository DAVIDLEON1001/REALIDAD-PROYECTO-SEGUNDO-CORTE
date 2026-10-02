using UnityEngine;

// Controla el robot móvil según el mapeo definido en la Fase 1:
//   Pitch -> avance/retroceso
//   Roll  -> giro izquierda/derecha
//
// Acepta datos reales del control háptico vía Bluetooth. Si el campo
// "Bluetooth" se deja vacío en el Inspector, sigue funcionando con teclado
// (W/S, A/D) como respaldo para seguir probando sin el hardware conectado.
//
// Además, detecta colisiones y le avisa al ESP32 por Bluetooth para que
// vibre: "1" si chocó con un objeto manipulable, "2" si chocó con una
// pared/obstáculo (etiqueta "Limite").

[RequireComponent(typeof(Rigidbody))]
public class RobotMovilController : MonoBehaviour
{
    [Header("Sensibilidad (ajustar en la Fase 5)")]
    public float velocidadMax = 3f;   // metros por segundo
    public float giroMax = 90f;       // grados por segundo
    public float anguloMax = 30f;     // grados de inclinación que dan velocidad/giro máximos

    [Header("Entrada real (arrastra aquí el objeto GestorBluetooth)")]
    public ConexionBluetooth bluetooth;

    [Header("Valores de Pitch/Roll actuales (para depurar)")]
    [Range(-1f, 1f)] public float pitchSimulado = 0f;
    [Range(-1f, 1f)] public float rollSimulado = 0f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (bluetooth != null)
        {
            // Entrada real: convierte grados de inclinación en un valor -1..1
            pitchSimulado = Mathf.Clamp(bluetooth.ObtenerPitch() / anguloMax, -1f, 1f);
            rollSimulado = Mathf.Clamp(bluetooth.ObtenerRoll() / anguloMax, -1f, 1f);
        }
        else
        {
            // Respaldo de teclado (solo si no hay Bluetooth conectado)
            pitchSimulado = Input.GetAxis("Vertical");
            rollSimulado = Input.GetAxis("Horizontal");
        }
    }

    void FixedUpdate()
    {
        // Mapeo Pitch -> avance/retroceso, en la dirección hacia donde mira el robot
        Vector3 avance = transform.forward * pitchSimulado * velocidadMax * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + avance);

        // Mapeo Roll -> giro sobre el eje vertical
        float rotacionGrados = rollSimulado * giroMax * Time.fixedDeltaTime;
        Quaternion giro = Quaternion.Euler(0f, rotacionGrados, 0f);
        rb.MoveRotation(rb.rotation * giro);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (bluetooth == null) return; // sin Bluetooth conectado, no hay a quién avisarle

        if (collision.gameObject.CompareTag("Manipulable"))
        {
            bluetooth.EnviarComando("1"); // vibración corta y suave
        }
        else if (collision.gameObject.CompareTag("Limite"))
        {
            bluetooth.EnviarComando("2"); // vibración fuerte e intermitente
        }
    }
}