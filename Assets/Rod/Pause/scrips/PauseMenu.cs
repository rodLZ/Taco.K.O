using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [Header("Panel de Pausa")]
    [Tooltip("Arrastra el GameObject del panel de pausa (con botones) aquí.")]
    public GameObject pausePanel;

    // Variable para controlar el estado de pausa
    private bool isPaused = false;

    private void Awake()
    {
        // Verifica que exista un EventSystem en la escena; si no, lo crea.
        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Debug.Log("No se encontró EventSystem, se creó uno automáticamente.");
        }
    }

    private void Start()
    {
        // Comprueba que se haya asignado el panel de pausa
        if (pausePanel == null)
        {
            Debug.LogError("No se ha asignado el panel de pausa en el Inspector.");
            enabled = false;
            return;
        }

        // Inicia con el panel oculto y el juego corriendo normalmente
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // Alterna la pausa con la tecla P
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Alterna el estado de pausa.
    /// </summary>
    public void TogglePause()
    {
        if (!isPaused)
            PauseGame();
        else
            ResumeGame();
    }

    /// <summary>
    /// Pausa el juego, muestra el panel y establece Time.timeScale = 0.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        Debug.Log("Juego pausado.");
    }

    /// <summary>
    /// Reanuda el juego, oculta el panel y restaura Time.timeScale = 1.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        Debug.Log("Juego reanudado.");
    }

    /// <summary>
    /// Reinicia la escena actual. Solo se ejecuta si el juego está en pausa.
    /// </summary>
    public void RestartGame()
    {
        if (!isPaused) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Reiniciando la escena.");
    }

    /// <summary>
    /// Cambia a la escena del menú principal. Solo se ejecuta si el juego está en pausa.
    /// </summary>
    public void GoToMenu()
    {
        if (!isPaused) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu"); // Asegúrate de que el nombre de la escena del menú sea correcto
        Debug.Log("Regresando al menú principal.");
    }
}
