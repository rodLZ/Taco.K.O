// PlayerController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public enum DefenseDirection { None, Left, Right }
    public DefenseDirection CurrentDefenseDirection { get; private set; } = DefenseDirection.None;

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
        // Ocultar UIs iniciales
        if (panelNegro != null) { panelNegro.alpha = 0f; panelNegro.blocksRaycasts = false; panelNegro.interactable = false; }
        if (damageCanvas != null) { damageCanvas.alpha = 0f; damageCanvas.blocksRaycasts = false; damageCanvas.interactable = false; }
        foreach (var img in impactSprites) img.gameObject.SetActive(false);
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
            StartCoroutine(FadeCanvas(panelNegro, 1f, 0.3f));
            panelNegro.blocksRaycasts = true;
        }
        else if (Input.GetKeyUp(specialKey))
        {
            StartCoroutine(FadeCanvas(panelNegro, 0f, 0.3f));
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
            comboCount = 1;
        else
            comboCount++;

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

        if (enemy != null && Vector3.Distance(transform.position, enemy.transform.position) <= attackRange)
        {
            if (panelNegro != null && panelNegro.alpha > 0.1f)
            {
                enemy.TakeDamage(attackDamage, direction);
                ShowRandomImpactSprite();
                audioGolpe?.Play();
            }
        }

        yield return new WaitForSeconds(effectiveAttackDuration - attackImpactDelay);

        animator.speed = 1f;
        isAttacking = false;
    }

    IEnumerator PerformDefense(string triggerName, DefenseDirection direction)
    {
        CurrentDefenseDirection = direction;
        animator.SetTrigger(triggerName);
        yield return null;
        // Esperar duración de la animación
        var state = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(state.length);
        CurrentDefenseDirection = DefenseDirection.None;
    }

    public void TakeDamage(int damage, DefenseDirection attackDirection)
    {
        if (Time.time - combatStartTime < initialInvulnerabilityDuration) return;
        if (isDead) return;
        if (attackDirection == lastAttackDirection) return; // Perfect block

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        UpdateHealthUI();

        if (panelNegro != null && panelNegro.alpha > 0.1f)
            StartCoroutine(ShowDamageCanvas(true));
        else
            StartCoroutine(ShowDamageCanvas(false));

        if (currentHealth <= 0) Die();
        else animator.SetTrigger("Player_D");
    }

    IEnumerator ShowDamageCanvas(bool fade)
    {
        if (fade)
            yield return StartCoroutine(FadeCanvas(damageCanvas, 1f, 0.2f));
        else
            damageCanvas.alpha = 1f;

        yield return new WaitForSeconds(1.5f);

        if (fade)
            yield return StartCoroutine(FadeCanvas(damageCanvas, 0f, 0.3f));
        else
            damageCanvas.alpha = 0f;
    }

    void ShowRandomImpactSprite()
    {
        if (impactSprites.Length == 0) return;
        if (usedSpriteIndices.Count >= impactSprites.Length) usedSpriteIndices.Clear();
        int index;
        do { index = Random.Range(0, impactSprites.Length); }
        while (usedSpriteIndices.Contains(index));
        usedSpriteIndices.Add(index);

        currentSprite?.gameObject.SetActive(false);
        currentSprite = impactSprites[index];
        currentSprite.gameObject.SetActive(true);
        StartCoroutine(HideSpriteAfterDelay(currentSprite, 1f));
    }

    IEnumerator HideSpriteAfterDelay(Image sprite, float delay)
    {
        yield return new WaitForSeconds(delay);
        sprite?.gameObject.SetActive(false);
    }

    void Die()
    {
        isDead = true;
        animator.SetTrigger("Player_Death");
        if (panelNegro != null) { panelNegro.alpha = 0f; panelNegro.blocksRaycasts = false; panelNegro.interactable = false; }
    }

    void UpdateHealthUI()
    {
        if (healthBar != null) healthBar.fillAmount = currentHealth / maxHealth;
    }

    IEnumerator FadeCanvas(CanvasGroup cg, float targetAlpha, float duration)
    {
        if (cg == null) yield break;
        float startAlpha = cg.alpha, elapsed = 0f;
        bool willShow = targetAlpha > startAlpha;
        if (willShow) { cg.blocksRaycasts = true; cg.interactable = true; }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        cg.alpha = targetAlpha;
        if (!willShow) { cg.blocksRaycasts = false; cg.interactable = false; }
    }
}
