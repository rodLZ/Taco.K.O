using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string sceneName; 

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Return)) 
        {
            LoadScene();
        }
    }

    
    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
