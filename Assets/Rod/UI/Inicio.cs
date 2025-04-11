using UnityEngine;

public class Inicio : MonoBehaviour
{
    // Tiempo en segundos antes de destruir el Canvas
    public float tiempoDeDestruccion = 3f;

    void Start()
    {
        // Llama a la función DestruirCanvas después de 3 segundos
        Invoke(nameof(DestruirCanvas), tiempoDeDestruccion);
    }

    void DestruirCanvas()
    {
        Destroy(gameObject);
    }
}
