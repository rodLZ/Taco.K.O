using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialUI : MonoBehaviour
{
    [Header("Nivel que se desbloquear� al ganar")]
    public int nextLevelToUnlock = 2;

    public void CargarSiguienteNivel()
    {
        // Desbloquea el siguiente enemigo
        EnemySelector.UnlockNextLevel(nextLevelToUnlock);

        // Carga la escena del selector
        SceneManager.LoadScene("SelectPlayer");
    }
}
