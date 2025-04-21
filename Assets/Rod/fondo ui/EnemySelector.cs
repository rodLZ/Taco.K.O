using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemySelector : MonoBehaviour
{
    public Transform sartenPivot;             // Objeto que rota (el sartén)
    public Transform[] enemies;               // Lista de enemigos en orden circular
    public string[] sceneNames;               // Escenas correspondientes
    public int[] requiredLevels;              // Nivel requerido para cada enemigo

    public float rotationStep = 45f;
    public float rotationSpeed = 5f;

    private int currentIndex = 0;
    private Quaternion targetRotation;
    private int unlockedLevel;

    void Start()
    {
        targetRotation = sartenPivot.rotation;
        unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);
        UpdateEnemiesVisibility();
    }

    void Update()
    {
        // Gira el sartén con flechas
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentIndex = (currentIndex + 1) % enemies.Length;
            targetRotation *= Quaternion.Euler(0, 0, rotationStep);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentIndex = (currentIndex - 1 + enemies.Length) % enemies.Length;
            targetRotation *= Quaternion.Euler(0, 0, -rotationStep);
        }

        // Rotación suave
        sartenPivot.rotation = Quaternion.Lerp(sartenPivot.rotation, targetRotation, Time.deltaTime * rotationSpeed);

        // Selección con espacio
        if (Input.GetKeyDown(KeyCode.Space))
        {
            int required = requiredLevels[currentIndex];

            if (unlockedLevel >= required)
            {
                SceneManager.LoadScene(sceneNames[currentIndex]);
            }
            else
            {
                Debug.Log("¡Este enemigo está bloqueado! Derrota al anterior primero.");
            }
        }
    }

    void UpdateEnemiesVisibility()
    {
        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i].gameObject.SetActive(unlockedLevel >= requiredLevels[i]);
        }
    }

    // Este método lo llamas desde otra escena cuando ganas
    public static void UnlockNextLevel(int levelToUnlock)
    {
        int current = PlayerPrefs.GetInt("UnlockedLevel", 1);
        if (levelToUnlock > current)
        {
            PlayerPrefs.SetInt("UnlockedLevel", levelToUnlock);
            PlayerPrefs.Save();
        }
    }
}
