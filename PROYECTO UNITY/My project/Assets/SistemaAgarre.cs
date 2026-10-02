using UnityEngine;

// Va sobre "PuntoAgarre" (hijo de la cámara), que ya tiene un Sphere
// Collider marcado como Trigger. Detecta objetos con la etiqueta
// "Manipulable" y permite agarrarlos/soltarlos.
//
// Si se le asigna un "GestorBluetooth" en el Inspector, usa el botón E
// físico del control háptico. Si se deja vacío, sigue usando la tecla E
// del teclado como respaldo.

public class SistemaAgarre : MonoBehaviour
{
    [Header("Entrada de teclado (respaldo si no hay Bluetooth)")]
    public KeyCode teclaAgarre = KeyCode.E;

    [Header("Entrada real (arrastra aquí el objeto GestorBluetooth)")]
    public ConexionBluetooth bluetooth;

    private GameObject objetoCercano = null;
    private GameObject objetoAgarrado = null;
    private bool botonAnterior = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Manipulable"))
        {
            objetoCercano = other.gameObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == objetoCercano)
        {
            objetoCercano = null;
        }
    }

    void Update()
    {
        bool botonActual = (bluetooth != null) ? bluetooth.ObtenerBotonE() : Input.GetKey(teclaAgarre);

        // Detecta el flanco de subida: el instante exacto en que pasa de
        // suelto a presionado, para no agarrar/soltar repetidamente
        // mientras se mantiene el botón presionado.
        bool sePresionoAhora = botonActual && !botonAnterior;
        botonAnterior = botonActual;

        if (sePresionoAhora)
        {
            if (objetoAgarrado == null && objetoCercano != null)
            {
                Agarrar(objetoCercano);
            }
            else if (objetoAgarrado != null)
            {
                Soltar();
            }
        }
    }

    void Agarrar(GameObject objeto)
    {
        objetoAgarrado = objeto;

        Rigidbody rb = objeto.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        objeto.transform.SetParent(transform);
        objeto.transform.localPosition = Vector3.zero;
    }

    void Soltar()
    {
        if (objetoAgarrado == null) return;

        Rigidbody rb = objetoAgarrado.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        objetoAgarrado.transform.SetParent(null);
        objetoAgarrado = null;
    }
}