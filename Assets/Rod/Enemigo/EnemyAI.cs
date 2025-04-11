// EnemyAI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    #region Configuration
    [System.Serializable]
    public class ActionConfig
    {
        [Tooltip("Probability (0-100)")]
        public int probability;
        [Tooltip("Min/Max duration")]
        public Vector2 duration = new Vector2(1f, 3f);
    }

    [Header("Combat Settings")]
    [SerializeField] int attackDamage = 10;
    [SerializeField] float attackRange = 2f;
    [SerializeField] float defenseBypassChance = 5f;
    [SerializeField] int maxHealth = 100;
    [SerializeField] float defenseDamageReduction = 0.5f;
    [SerializeField] float attackImpactDelay = 0.3f; // Delay until damage applies

    [Header("AI Configuration")]
    [SerializeField] ActionConfig attackConfig;
    [SerializeField] ActionConfig defenseConfig;
    [SerializeField] ActionConfig idleConfig;

    [Header("Warning Settings")]
    [SerializeField] private CanvasGroup warningCanvasLeft;
    [SerializeField] private CanvasGroup warningCanvasRight;
    [SerializeField] private float warningDuration = 0.5f;

    [Header("References")]
    [SerializeField] Image healthBar;
    [SerializeField] Animator animator;

    [Header("UI References")]
    [SerializeField] private CanvasGroup panelNegro;
    [SerializeField] private CanvasGroup victoryCanvas;
    [SerializeField] private CanvasGroup defeatCanvas;

    #region Audio Settings
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField] private AudioClip[] damageClips;
    [SerializeField] private AudioClip[] idleClips;
    [SerializeField] private AudioClip winClip;
    #endregion

    #region State Variables
    private enum State { AttackingLeft, AttackingRight, Defending, Idle }
    private State currentState;
    private PlayerController.DefenseDirection currentDefense = PlayerController.DefenseDirection.None;
    private float currentHealth;
    private bool canAct = true;
    private bool isDead = false;
    private PlayerController player;
    #endregion
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        // Referencias
        player = FindObjectOfType<PlayerController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (!panelNegro) panelNegro = GameObject.Find("PanelNegro")?.GetComponent<CanvasGroup>();
        if (!victoryCanvas) victoryCanvas = GameObject.Find("VictoryCanvas")?.GetComponent<CanvasGroup>();
        if (!defeatCanvas) defeatCanvas = GameObject.Find("DefeatCanvas")?.GetComponent<CanvasGroup>();

        // Oculta y desactiva interacción de UIs
        InitCanvas(panelNegro, false);
        InitCanvas(victoryCanvas, false);
        InitCanvas(defeatCanvas, false);
        InitCanvas(warningCanvasLeft, false);
        InitCanvas(warningCanvasRight, false);

        if (!animator) Debug.LogError("Enemy Animator missing!");

        // Salud
        currentHealth = maxHealth;
        UpdateHealthVisual();

        StartCoroutine(AIBehaviorLoop());
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
    }

    private void InitCanvas(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }
    #endregion

    #region AI Behavior
    private IEnumerator AIBehaviorLoop()
    {
        while (!isDead)
        {
            while (Time.timeScale == 0f)
                yield return null;

            if (canAct)
            {
                canAct = false;
                currentState = CalculateNextAction();
                ProcessAction(currentState);
                yield return new WaitForSeconds(GetActionDuration(currentState));
                canAct = true;
            }
            yield return null;
        }
    }

    private State CalculateNextAction()
    {
        int total = attackConfig.probability + defenseConfig.probability + idleConfig.probability;
        int r = Random.Range(0, total);
        if (r < attackConfig.probability)
            return Random.value > 0.5f ? State.AttackingLeft : State.AttackingRight;
        if (r < attackConfig.probability + defenseConfig.probability)
            return State.Defending;
        return State.Idle;
    }
    #endregion

    #region Action Execution
    private void ProcessAction(State action)
    {
        currentDefense = action == State.Defending
            ? (Random.value > 0.5f ? PlayerController.DefenseDirection.Left : PlayerController.DefenseDirection.Right)
            : PlayerController.DefenseDirection.None;

        switch (action)
        {
            case State.AttackingLeft:
                StartCoroutine(EnemyAttackRoutine("Atack_L", PlayerController.DefenseDirection.Left, warningCanvasLeft));
                break;
            case State.AttackingRight:
                StartCoroutine(EnemyAttackRoutine("Atack_R", PlayerController.DefenseDirection.Right, warningCanvasRight));
                break;
            case State.Defending:
                animator.SetTrigger("Defense");
                break;
            case State.Idle:
                animator.SetTrigger("Idle");
                PlayRandomClip(idleClips);
                break;
        }
    }

    private IEnumerator EnemyAttackRoutine(string trigger, PlayerController.DefenseDirection dir, CanvasGroup warningCanvas)
    {
        // 1) Mostrar advertencia
        InitCanvas(warningCanvas, true);
        yield return new WaitForSeconds(warningDuration);
        InitCanvas(warningCanvas, false);

        // 2) Animación de ataque
        animator.SetTrigger(trigger);
        PlayRandomClip(attackClips);

        // 3) Esperar al impacto
        yield return new WaitForSeconds(attackImpactDelay);

        // 4) Aplicar daño si está en rango
        if (Vector3.Distance(transform.position, player.transform.position) <= attackRange)
            AttemptAttack(dir);
    }

    private void AttemptAttack(PlayerController.DefenseDirection dir)
    {
        bool heavy = Random.Range(0f, 100f) < defenseBypassChance;
        int dmg = heavy ? attackDamage * 2 : attackDamage;
        player.TakeDamage(dmg, dir);

        if (player.IsDead)
        {
            if (defeatCanvas != null)
                StartCoroutine(FadeCanvas(defeatCanvas, 1f, 0.3f));
            if (winClip != null)
                audioSource.PlayOneShot(winClip);
        }
    }
    #endregion

    #region Combat Logic
    public void TakeDamage(int damage, PlayerController.DefenseDirection attackDir)
    {
        if (isDead) return;
        bool defended = currentDefense == attackDir;
        if (defended) damage = Mathf.FloorToInt(damage * defenseDamageReduction);
        PlayRandomClip(damageClips);

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        UpdateHealthVisual();

        if (currentHealth <= 0) HandleDeath();
    }
    #endregion

    #region Health & Death
    private void UpdateHealthVisual()
    {
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;
    }

    private void HandleDeath()
    {
        isDead = true;
        StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        bool habilidadActiva = panelNegro && panelNegro.alpha > 0f;
        if (habilidadActiva)
            yield return StartCoroutine(FadeCanvas(panelNegro, 0f, 0.3f));

        animator.SetTrigger("Death");
        yield return new WaitForSeconds(1f);

        if (victoryCanvas != null)
            yield return StartCoroutine(FadeCanvas(victoryCanvas, 1f, 0.3f));

        Destroy(gameObject);
    }
    #endregion

    #region Utils
    private float GetActionDuration(State s)
    {
        var cfg = (s == State.AttackingLeft || s == State.AttackingRight)
            ? attackConfig
            : s == State.Defending
                ? defenseConfig
                : idleConfig;
        return Random.Range(cfg.duration.x, cfg.duration.y);
    }

    private void PlayRandomClip(AudioClip[] clips)
    {
        if (audioSource == null || clips == null || clips.Length == 0) return;
        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float targetAlpha, float duration)
    {
        if (cg == null) yield break;
        float startAlpha = cg.alpha, elapsed = 0f;
        cg.blocksRaycasts = true;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;
        cg.interactable = targetAlpha > 0f;
    }
    #endregion
}
