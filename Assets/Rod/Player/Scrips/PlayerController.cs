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
    public CanvasGroup abilityPanel;    // Panel de la habilidad especial (antes panelNegro)
    public CanvasGroup damagePanel;     // Panel que indica al jugador que recibió daño
    public Animator animator;
    public Image healthBar;
    public Image[] impactSprites;

    [Header("Audio")]
    public AudioSource audioGolpe;

    private float currentHealth;
    private bool isAttacking = false;
    private bool isDead = false;
    private EnemyAI enemy;

    private int comboCount = 0;
    private float lastAttackTime = -10f;
    private List<DefenseDirection> fullCombo = new List<DefenseDirection>();
    private int fireComboIndex = 0;

    private List<int> usedSpriteIndices = new List<int>();
    private Image currentSprite;

    private bool isSpecialActive = false;

    public bool IsDead => isDead;

    private bool isSpecialActive = false;

    void Start()
    {
        currentHealth = maxHealth;
        enemy = FindObjectOfType<EnemyAI>();
        UpdateHealthUI();
        combatStartTime = Time.time;

        // Inicializa panels
        if (abilityPanel != null)
        {
            abilityPanel.alpha = 0f;
            abilityPanel.blocksRaycasts = false;
            abilityPanel.interactable = false;
        }

        if (damagePanel != null)
        {
            damagePanel.alpha = 0f;
            damagePanel.blocksRaycasts = false;
            damagePanel.interactable = false;
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
            isSpecialActive = true;
<<<<<<< Updated upstream
            StartCoroutine(FadeCanvas(abilityPanel, 1f, 0.3f));
            abilityPanel.blocksRaycasts = true;
            abilityPanel.interactable = true;
=======
            StartCoroutine(FadeCanvas(panelNegro, 1f, 0.3f));
            panelNegro.blocksRaycasts = true;
>>>>>>> Stashed changes
        }
        else if (Input.GetKeyUp(specialKey))
        {
            isSpecialActive = false;
<<<<<<< Updated upstream
            StartCoroutine(FadeCanvas(abilityPanel, 0f, 0.3f));
            abilityPanel.blocksRaycasts = false;
            abilityPanel.interactable = false;
=======
            StartCoroutine(FadeCanvas(panelNegro, 0f, 0.3f));
            panelNegro.blocksRaycasts = false;
>>>>>>> Stashed changes
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

    void ProcessCombo(DefenseDirection dir)
    {
        if (Time.time - lastAttackTime > comboResetTime)
        {
            comboCount = 1;
            fullCombo.Clear();
            fireComboIndex = 0;
        }
        else comboCount++;

        lastAttackTime = Time.time;
        fullCombo.Add(dir);
    }

    void HandleDefenseInput()
    {
        if (Input.GetKeyDown(defendLeftKey))
            StartCoroutine(PerformDefense("Trigger_D_L", DefenseDirection.Left));
        else if (Input.GetKeyDown(defendRightKey))
            StartCoroutine(PerformDefense("Trigger_D_R", DefenseDirection.Right));
    }

    IEnumerator PerformAttack(string triggerName, DefenseDirection dir)
    {
        isAttacking = true;
        animator.speed = 1.5f;
        animator.SetTrigger(triggerName);

<<<<<<< Updated upstream
        // Calcula daño final (aquí podrías aplicar combo)
        int finalDamage = attackDamage;

        yield return new WaitForSeconds(attackImpactDelay);

        bool inRange = enemy != null && Vector3.Distance(transform.position, enemy.transform.position) <= attackRange;
        bool canDamage = isSpecialActive && inRange;
        if (canDamage)
        {
            enemy.TakeDamage(finalDamage, dir);
=======
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
>>>>>>> Stashed changes
            ShowRandomImpactSprite();
            audioGolpe?.Play();
        }

<<<<<<< Updated upstream
        // Espera fin de animación ajustada por combo
        float effectiveDuration = baseAttackDuration;
        yield return new WaitForSeconds(effectiveDuration - attackImpactDelay);
=======
        // Espera el resto de la animación
        yield return new WaitForSeconds((baseAttackDuration /*ajustado por combo*/)
                                       - attackImpactDelay);
>>>>>>> Stashed changes

        animator.speed = 1f;
        isAttacking = false;
    }

    IEnumerator PerformDefense(string triggerName, DefenseDirection dir)
    {
        // 1) Inicio defensa
        CurrentDefenseDirection = dir;
        animator.SetTrigger(triggerName);

        // 2) Espero a que entre en estado Defense
        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsTag("Defense"));

        // 3) Espero a que salga del estado Defense
        yield return new WaitWhile(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsTag("Defense"));

        // 4) Limpio defensa
        CurrentDefenseDirection = DefenseDirection.None;
        Debug.Log("🔓 Defensa finalizada automáticamente");
    }

    public void TakeDamage(int damage, DefenseDirection attackDir)
    {
        if (Time.time - combatStartTime < initialInvulnerabilityDuration) return;
        if (isDead) return;
        if (attackDir == CurrentDefenseDirection) return;  // perfect block

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        UpdateHealthUI();
<<<<<<< Updated upstream
        animator.SetTrigger("Player_D");
        StartCoroutine(DamageFeedback());
=======

        if (panelNegro != null && panelNegro.alpha > 0.1f)
            StartCoroutine(ShowDamageCanvas(true));
        else
            StartCoroutine(ShowDamageCanvas(false));
        Debug.Log($"TakeDamage invoked. Health now={currentHealth}. Setting Player_D trigger.");
        animator.SetTrigger("Player_D");
>>>>>>> Stashed changes

        if (currentHealth <= 0) Die();
    }

    private IEnumerator DamageFeedback()
    {
        // Muestra el panel de daño con fade in/out
        yield return StartCoroutine(FadeCanvas(damagePanel, 1f, 0.2f));
        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(FadeCanvas(damagePanel, 0f, 0.3f));
    }

    void ShowRandomImpactSprite()
    {
        if (impactSprites.Length == 0) return;
        if (usedSpriteIndices.Count >= impactSprites.Length) usedSpriteIndices.Clear();
        int idx;
        do { idx = Random.Range(0, impactSprites.Length); }
        while (usedSpriteIndices.Contains(idx));
        usedSpriteIndices.Add(idx);

        currentSprite?.gameObject.SetActive(false);
        currentSprite = impactSprites[idx];
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
        // Ocultar panel de habilidad si está activo
        if (abilityPanel != null)
        {
            abilityPanel.alpha = 0f;
            abilityPanel.blocksRaycasts = false;
            abilityPanel.interactable = false;
        }
    }

    void UpdateHealthUI()
    {
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;
    }

    IEnumerator FadeCanvas(CanvasGroup cg, float targetAlpha, float duration)
    {
        if (cg == null) yield break;
        float start = cg.alpha, elapsed = 0f;
        bool showing = targetAlpha > start;
        if (showing) { cg.blocksRaycasts = true; cg.interactable = true; }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, targetAlpha, elapsed / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;
        if (!showing) { cg.blocksRaycasts = false; cg.interactable = false; }
    }
}