using System.Collections;
using UnityEngine;
using UnityEngine.UI; // o TMPro si usas texto TMP

public class GameIntro : MonoBehaviour
{
    public GameObject introPanel; // asigna esto desde el inspector
    public float introDuration = 3f;
    public AudioSource backgroundMusic;

    IEnumerator ShowIntroPanel()
    {
        introPanel.SetActive(true);       // mostrar panel
        Time.timeScale = 0f;              // pausa el juego

        yield return new WaitForSecondsRealtime(introDuration); // espera sin estar ligado a timeScale

        introPanel.SetActive(false);      // ocultar panel
        Time.timeScale = 1f;              // reanudar juego
    }
}
