using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Globalization;

// Abre el puerto COM Bluetooth del ESP32 y lee continuamente líneas con
// formato "pitch,roll,w,a,s,d,e". La lectura corre en un hilo aparte para
// no congelar Unity mientras espera datos por Bluetooth.

public class ConexionBluetooth : MonoBehaviour
{
    [Header("Configuración del puerto")]
    public string nombrePuerto = "COM5";
    public int baudRate = 115200;

    [Header("Valores recibidos (solo lectura, para depurar en el Inspector)")]
    public float pitch = 0f;
    public float roll = 0f;
    public bool botonW = false;
    public bool botonA = false;
    public bool botonS = false;
    public bool botonD = false;
    public bool botonE = false;

    private SerialPort puerto;
    private Thread hiloLectura;
    private volatile bool corriendo = false;
    private readonly object bloqueo = new object();

    void Start()
    {
        try
        {
            puerto = new SerialPort(nombrePuerto, baudRate);
            puerto.ReadTimeout = 100;
            puerto.Open();
            corriendo = true;

            hiloLectura = new Thread(LeerDatos);
            hiloLectura.IsBackground = true;
            hiloLectura.Start();

            Debug.Log("Bluetooth conectado en " + nombrePuerto);
        }
        catch (System.Exception e)
        {
            Debug.LogError("No se pudo abrir el puerto " + nombrePuerto + ": " + e.Message);
        }
    }

    void LeerDatos()
    {
        while (corriendo)
        {
            try
            {
                string linea = puerto.ReadLine();
                string[] partes = linea.Split(',');

                // Formato esperado: pitch,roll,w,a,s,d,e (7 valores)
                if (partes.Length == 7)
                {
                    float p = float.Parse(partes[0], CultureInfo.InvariantCulture);
                    float r = float.Parse(partes[1], CultureInfo.InvariantCulture);
                    bool w = partes[2].Trim() == "1";
                    bool a = partes[3].Trim() == "1";
                    bool s = partes[4].Trim() == "1";
                    bool d = partes[5].Trim() == "1";
                    bool e = partes[6].Trim() == "1";

                    lock (bloqueo)
                    {
                        pitch = p;
                        roll = r;
                        botonW = w;
                        botonA = a;
                        botonS = s;
                        botonD = d;
                        botonE = e;
                    }
                }
            }
            catch (System.TimeoutException)
            {
                // Normal: no llegó dato nuevo en este intento
            }
            catch (System.Exception)
            {
                // Ignora líneas corruptas ocasionales (ruido típico de Bluetooth)
            }
        }
    }

    public float ObtenerPitch() { lock (bloqueo) { return pitch; } }
    public float ObtenerRoll() { lock (bloqueo) { return roll; } }
    public bool ObtenerBotonW() { lock (bloqueo) { return botonW; } }
    public bool ObtenerBotonA() { lock (bloqueo) { return botonA; } }
    public bool ObtenerBotonS() { lock (bloqueo) { return botonS; } }
    public bool ObtenerBotonD() { lock (bloqueo) { return botonD; } }
    public bool ObtenerBotonE() { lock (bloqueo) { return botonE; } }

    public void EnviarComando(string comando)
    {
        try
        {
            if (puerto != null && puerto.IsOpen)
            {
                puerto.Write(comando);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("No se pudo enviar comando por Bluetooth: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        corriendo = false;
        if (hiloLectura != null) hiloLectura.Join(200);
        if (puerto != null && puerto.IsOpen) puerto.Close();
    }
}