using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public enum DefenseDirection { None, Left, Right }

    [Header("Key Bindings")]
    public KeyCode specialKey = KeyCode.Space;
    public KeyCode attackLeftKey = KeyCode.A;
    public KeyCode attackRightKey = KeyCode.D;
    public KeyCode defendLeftKey = KeyCode.LeftArrow;
    public KeyCode defendRightKey = KeyCode.RightArrow;

    [Header("Combat Settings")]
    public float baseAttackDuration = 0.8f;
    public float attackImpactDelay = 0.3f;
    public int attackDamage = 10;
    public float attackRange = 1.5f;
    public float maxHealth = 100f;

    [Header("Invulnerability")]
    [Tooltip("Time (seconds) at start of combat during which player is immune")]
    public float initialInvulnerabilityDuration = 5f;
    private float combatStartTime;

    [Header("Combo Settings")]
    public float comboResetTime = 1.0f;
    public float comboSpeedFactor = 0.1f;
    public float minAttackDuration = 0.4f;

    [Header("References")]
    public CanvasGroup panelNegro;
    public CanvasGroup damageCanvas;
    public Animator animator;
    public Image healthBar;
    public Image[] impactSprites;

    [Header("Audio")]
    public AudioSource audioGolpe;

    private float currentHealth;
    private bool isAttacking = false;
    private bool isDead = false;
    private EnemyAI enemy;
    public bool IsDead => isDead;

    private int comboCount = 0;
    private float lastAttackTime = -10f;
    private DefenseDirection lastAttackDirection = DefenseDirection.None;

    private List<int> usedSpriteIndices = new List<int>();
    private Image currentSprite;

    void Start()
    {
        currentHealth = maxHealth;
        enemy = FindObjectOfType<EnemyAI>();
        UpdateHealthUI();

        combatStartTime = Time.time;

        if (panelNegro != null)
        {
            panelNegro.alpha = 0f;
            panelNegro.interactable = false;
            panelNegro.blocksRaycasts = false;
        }

        if (damageCanvas != null)
        {
            damageCanvas.alpha = 0f;
            damageCanvas.interactable = false;
            damageCanvas.blocksRaycasts = false;
        }

        foreach (var img in impactSprites)
            img.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isDead) return;

        HandleAbilityInput();
        HandleAttackInput();
        HandleDefenseInput();
    }

    void HandleAbilityInput()
    {
        if (Input.GetKeyDown(specialKey))
        {
            StartCoroutine(FadeCanvas(panelNegro, 1f, 0.3f)); // Mostrar con animación
            panelNegro.blocksRaycasts = true;
        }
        else if (Input.GetKeyUp(specialKey))
        {
            StartCoroutine(FadeCanvas(panelNegro, 0f, 0.3f)); // Ocultar con animación
            panelNegro.blocksRaycasts = false;
        }
    }

    void HandleAttackInput()
    {
        if (isAttacking) return;

        if (Input.GetKeyDown(attackLeftKey))
        {
            ProcessCombo(DefenseDirection.Left);
            StartCoroutine(PerformAttack("Trigger_A_L", DefenseDirection.Left));
        }
        else if (Input.GetKeyDown(attackRightKey))
        {
            ProcessCombo(DefenseDirection.Right);
            StartCoroutine(PerformAttack("Trigger_A_R", DefenseDirection.Right));
        }
    }

    void ProcessCombo(DefenseDirection currentDirection)
    {
        if (Time.time - lastAttackTime > comboResetTime || currentDirection == lastAttackDirection)
        {
            comboCount = 1;
        }
        else
        {
            comboCount++;
        }

        lastAttackTime = Time.time;
        lastAttackDirection = currentDirection;
    }

    void HandleDefenseInput()
    {
        if (Input.GetKeyDown(defendLeftKey))
            StartCoroutine(PerformDefense("Trigger_D_L", DefenseDirection.Left));
        else if (Input.GetKeyDown(defendRightKey))
            StartCoroutine(PerformDefense("Trigger_D_R", DefenseDirection.Right));
    }

    IEnumerator PerformAttack(string triggerName, DefenseDirection direction)
    {
        isAttacking = true;
        animator.speed = 1.5f;
        animator.SetTrigger(triggerName);

        float speedMultiplier = 1f - (comboCount - 1) * comboSpeedFactor;
        speedMultiplier = Mathf.Clamp(speedMultiplier, minAttackDuration / baseAttackDuration, 1f);
        float effectiveAttackDuration = baseAttackDuration * speedMultiplier;

        yield return new WaitForSeconds(attackImpactDelay);

        if (enemy && Vector3.Distance(transform.position, enemy.transform.position) <= attackRange)
        {
            if (panelNegro.alpha > 0.1f)
            {
                enemy.TakeDamage(attackDamage, direction);
                ShowRandomImpactSprite();
                if (audioGolpe != null)
                    audioGolpe.Play();
            }
        }

        yield return new WaitForSeconds(effectiveAttackDuration - attackImpactDelay);

        animator.speed = 1f;
        isAttacking = false;
    }

    IEnumerator PerformDefense(string triggerName, DefenseDirection direction)
    {
        animator.SetTrigger(triggerName);
        yield return null;
    }

    public void TakeDamage(int damage, DefenseDirection attackDirection)
    {
        // Invulnerable al inicio de combate
        if (Time.time - combatStartTime < initialInvulnerabilityDuration)
            return;

        if (isDead) return;

        if (attackDirection == lastAttackDirection)
        {
            Debug.Log("Perfect block!");
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        UpdateHealthUI();

        if (panelNegro.alpha > 0.1f)
            StartCoroutine(ShowDamageCanvas(true));

        if (currentHealth <= 0)
            Die();
        else
            animator.SetTrigger("Player_D");
    }

    IEnumerator ShowDamageCanvas(bool fade = false)
    {
        if (fade)
            yield return StartCoroutine(FadeCanvas(damageCanvas, 1f, 0.2f));
        else
            damageCanvas.alpha = 1;

        yield return new WaitForSeconds(1.5f);

        if (fade)
            yield return StartCoroutine(FadeCanvas(damageCanvas, 0f, 0.3f));
        else
            damageCanvas.alpha = 0;
    }

    void ShowRandomImpactSprite()
    {
        if (impactSprites.Length == 0) return;

        // Si ya salieron todos, reiniciamos la lista
        if (usedSpriteIndices.Count >= impactSprites.Length)
            usedSpriteIndices.Clear();

        int index;
        do
        {
            index = Random.Range(0, impactSprites.Length);
        } while (usedSpriteIndices.Contains(index));

        usedSpriteIndices.Add(index);

        if (currentSprite != null)
            currentSprite.gameObject.SetActive(false);

        currentSprite = impactSprites[index];
        currentSprite.gameObject.SetActive(true);

        StartCoroutine(HideSpriteAfterDelay(currentSprite, 1f));
    }

    IEnumerator HideSpriteAfterDelay(Image sprite, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sprite != null)
            sprite.gameObject.SetActive(false);
    }

    void Die()
    {
        isDead = true;
        animator.SetTrigger("Player_Death");
        panelNegro.alpha = 0;
        panelNegro.blocksRaycasts = false;
    }

    void UpdateHealthUI()
    {
        if (healthBar)
            healthBar.fillAmount = currentHealth / maxHealth;
    }

    public void ResetPlayer()
    {
        StopAllCoroutines();
        animator.Rebind();
        animator.Update(0f);

        currentHealth = maxHealth;
        isDead = false;
        isAttacking = false;
        comboCount = 0;
        lastAttackTime = -10f;
        lastAttackDirection = DefenseDirection.None;

        UpdateHealthUI();
    }
    IEnumerator FadeCanvas(CanvasGroup cg, float targetAlpha, float duration)
    {
        if (cg == null) yield break;

        float startAlpha = cg.alpha;
        float elapsed = 0f;

        // Si vamos a mostrar el panel, habilitamos raycasts desde ya
        bool willShow = targetAlpha > startAlpha;
        if (willShow)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        cg.alpha = targetAlpha;

        // Si lo hemos ocultado, deshabilitamos raycasts
        if (!willShow)
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }
    }


}
