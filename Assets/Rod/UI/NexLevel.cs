using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialUI : MonoBehaviour
{
    public void CargarSiguienteNivel()
    {
        // Carga la siguiente escena según el orden en el Build Settings
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
