using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string text;
        public AudioClip audioClip;
    }

    public DialogueLine[] dialogueLines;
    public TextMeshProUGUI dialogueText;
    public AudioSource audioSource;
    public float typingSpeed = 0.05f;

    private int currentLine = 0;
    private bool isTyping = false;

    void Start()
    {
        if (dialogueLines.Length > 0)
        {
            StartCoroutine(PlayDialogue());
        }
    }

    IEnumerator PlayDialogue()
    {
        while (currentLine < dialogueLines.Length)
        {
            DialogueLine line = dialogueLines[currentLine];

            // Reproducir el audio
            if (line.audioClip != null)
            {
                audioSource.clip = line.audioClip;
                audioSource.Play();
            }

            // Escribir texto letra por letra
            yield return StartCoroutine(TypeText(line.text));

            // Esperar a que termine el audio
            while (audioSource.isPlaying)
            {
                yield return null;
            }

            currentLine++;
        }
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }
}
