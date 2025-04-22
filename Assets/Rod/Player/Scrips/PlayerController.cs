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
    private bool isDefendingEvent = false;
    private DefenseDirection defendingEventDir = DefenseDirection.None;
    public bool IsDead => isDead;

    private int comboCount = 0;
    private float lastAttackTime = -10f;
    private DefenseDirection lastAttackDirection = DefenseDirection.None;

    private List<int> usedSpriteIndices = new List<int>();
    private Image currentSprite;

    public Image[] fireComboSprites; // Asigna los 4 sprites en el Inspector
    private List<DefenseDirection> fullCombo = new List<DefenseDirection>();
    private int fireComboIndex = 0;

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
        if (Time.time - lastAttackTime > comboResetTime)
        {
            comboCount = 1;
            fullCombo.Clear();
            fireComboIndex = 0;
        }
        else
        {
            comboCount++;
        }

        lastAttackTime = Time.time;
        lastAttackDirection = currentDirection;
        fullCombo.Add(currentDirection);

        // Mostrar sprite de fuego
        if (fireComboIndex < fireComboSprites.Length)
        {
            fireComboSprites[fireComboIndex].gameObject.SetActive(true);
            StartCoroutine(HideSpriteAfterDelay(fireComboSprites[fireComboIndex], 1f));
            fireComboIndex++;
        }
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

        // calcula finalDamage y duración como antes…
        int finalDamage = attackDamage;
        // … lógica de combo que actualiza finalDamage …

        yield return new WaitForSeconds(attackImpactDelay);

        bool inRange = enemy != null
                       && Vector3.Distance(transform.position, enemy.transform.position) <= attackRange;
        bool specialOn = panelNegro != null && panelNegro.alpha > 0.1f;
        // —> o directamente: bool specialOn = isSpecialActive;

        if (inRange && specialOn)
        {
            // Aplicas daño **una sola vez**, ya con el posible bono de combo
            enemy.TakeDamage(finalDamage, direction);
            ShowRandomImpactSprite();
            audioGolpe?.Play();
        }

        // Espera el resto de la animación
        yield return new WaitForSeconds((baseAttackDuration /*ajustado por combo*/)
                                       - attackImpactDelay);

        animator.speed = 1f;
        isAttacking = false;
    }


    IEnumerator PerformDefense(string triggerName, DefenseDirection direction)
    {
        CurrentDefenseDirection = direction;
        animator.SetTrigger(triggerName);

        // Esperar hasta que la animación actual sea la de defensa
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsTag("Defense"));

        float animLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animLength);

        CurrentDefenseDirection = DefenseDirection.None;
    }

    public void TakeDamage(int damage, DefenseDirection attackDirection)
    {
        Debug.Log("TakeDamage called with damage: " + damage);
        if (Time.time - combatStartTime < initialInvulnerabilityDuration) return;
        if (isDead) return;
        if (attackDirection == lastAttackDirection) return; // Perfect block

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        UpdateHealthUI();

        if (panelNegro != null && panelNegro.alpha > 0.1f)
            StartCoroutine(ShowDamageCanvas(true));
        else
            StartCoroutine(ShowDamageCanvas(false));
        
        Debug.Log($"TakeDamage invoked. Health now={currentHealth}. Setting Player_D trigger.");
        animator.SetTrigger("Player_D");

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
    // Llamados desde tu animación "defensa izquierda"
    public void DefensaIzquierdaInicio()
    {
        isDefendingEvent = true;
        defendingEventDir = DefenseDirection.Left;
        Debug.Log(">> Defensa IZQUIERDA: inicio");
    }

    public void DefensaIzquierdaFinal()
    {
        isDefendingEvent = false;
        defendingEventDir = DefenseDirection.None;
        Debug.Log(">> Defensa IZQUIERDA: final");
    }

    // (Repite para defensa derecha si la tienes en otra animación)
    public void DefensaDerechaInicio()
    {
        isDefendingEvent = true;
        defendingEventDir = DefenseDirection.Right;
        Debug.Log(">> Defensa DERECHA: inicio");
    }

    public void DefensaDerechaFinal()
    {
        isDefendingEvent = false;
        defendingEventDir = DefenseDirection.None;
        Debug.Log(">> Defensa DERECHA: final");
    }
    public void OnEnemyAttackEvent(string attackSide)
    {
        // Convertir a tu enum
        DefenseDirection attackDir = (attackSide == "Left")
    ? DefenseDirection.Right
    : DefenseDirection.Left;

        if (isDefendingEvent)
        {
            if (defendingEventDir == attackDir)
            {
                Debug.Log("✅ El player se defendió del lado CORRECTO");
                return; // bloquea el daño
            }
            else
            {
                Debug.Log("⚠️ El player se defendió del lado INCORRECTO");
                // aquí podrías aplicar penalización o permitir algo de daño
            }
        }

        Debug.Log("❌ El player NO se defendió y recibió el golpe");
        // Si quieres que luego se aplique tu TakeDamage normal, puedes llamarlo:
        //
    }
}