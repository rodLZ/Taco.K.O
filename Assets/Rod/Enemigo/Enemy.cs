using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [SerializeField] float tiempoEntreAtaques = 15f;
    [SerializeField] float velocidadAtaque = 1f;
    [SerializeField] int vidaMaxima = 100;

    private int vidaActual;

    void Start()
    {
        vidaActual = vidaMaxima;
        StartCoroutine(CicloCombate());
    }

    IEnumerator CicloCombate()
    {
        while (true)
        {
            yield return new WaitForSeconds(tiempoEntreAtaques);
            yield return StartCoroutine(EjecutarAtaque());
        }
    }

    IEnumerator EjecutarAtaque()
    {
        // Ejemplo: 3 ataques seguidos
        for (int i = 0; i < 3; i++)
        {
            Debug.Log("Atacando...");
            yield return new WaitForSeconds(velocidadAtaque);
        }
    }

    public void RecibirGolpe(bool ataqueEspecial)
    {
        if (ataqueEspecial)
        {
            vidaActual -= 20;
            Debug.Log("Vida enemigo: " + vidaActual);
            if (vidaActual <= 0) Destroy(gameObject);
        }
    }
}