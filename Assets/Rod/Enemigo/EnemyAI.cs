// EnemyAI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [System.Serializable]
    public class ActionConfig
    {
        [Tooltip("Probability (0-100)")]
        public int probability;
        [Tooltip("Min/Max duration (ya no se usa para animaciones)")]
        public Vector2 duration = new Vector2(1f, 3f);
    }

    [Header("Combat Settings")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float defenseBypassChance = 5f;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float defenseDamageReduction = 0.5f;

    [Header("AI Configuration")]
    [SerializeField] private ActionConfig attackConfig;
    [SerializeField] private ActionConfig defenseConfig;
    [SerializeField] private ActionConfig idleConfig;

    [Header("Warning Settings")]
    [SerializeField] private CanvasGroup warningCanvasLeft;
    [SerializeField] private CanvasGroup warningCanvasRight;
    [SerializeField] private float warningDuration = 0.5f;

    [Header("References")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Animator animator;

    [Header("UI References")]
    [SerializeField] private CanvasGroup panelNegro;
    [SerializeField] private CanvasGroup victoryCanvas;
    [SerializeField] private CanvasGroup defeatCanvas;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField] private AudioClip[] damageClips;
    [SerializeField] private AudioClip[] idleClips;
    [SerializeField] private AudioClip winClip;

    private List<AudioClip> availAttackClips;
    private List<AudioClip> availDamageClips;
    private List<AudioClip> availIdleClips;

    private enum State { AttackingLeft, AttackingRight, Defending, Idle }
    private State currentState;
    private float currentHealth;
    private bool canAct = true, isDead = false;
    private PlayerController player;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        availAttackClips = new List<AudioClip>(attackClips);
        availDamageClips = new List<AudioClip>(damageClips);
        availIdleClips = new List<AudioClip>(idleClips);

        InitCanvas(panelNegro, false);
        InitCanvas(victoryCanvas, false);
        InitCanvas(defeatCanvas, false);
        InitCanvas(warningCanvasLeft, false);
        InitCanvas(warningCanvasRight, false);

        currentHealth = maxHealth;
        UpdateHealthVisual();

        StartCoroutine(AIBehaviorLoop());
    }

    private void InitCanvas(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    private IEnumerator AIBehaviorLoop()
    {
        while (!isDead)
        {
            yield return new WaitWhile(() => Time.timeScale == 0f);
            if (canAct)
            {
                canAct = false;
                currentState = CalculateNextAction();
                yield return StartCoroutine(ActionRoutine(currentState));
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

    private IEnumerator ActionRoutine(State action)
    {
        switch (action)
        {
            case State.AttackingLeft:
                yield return StartCoroutine(EnemyAttackRoutine("Atack_L", PlayerController.DefenseDirection.Left, warningCanvasLeft));
                break;
            case State.AttackingRight:
                yield return StartCoroutine(EnemyAttackRoutine("Atack_R", PlayerController.DefenseDirection.Right, warningCanvasRight));
                break;
            case State.Defending:
                yield return StartCoroutine(EnemyDefenseRoutine());
                break;
            case State.Idle:
                yield return StartCoroutine(EnemyIdleRoutine());
                break;
        }
    }

    private IEnumerator EnemyAttackRoutine(string trigger, PlayerController.DefenseDirection dir, CanvasGroup warningCanvas)
    {
        InitCanvas(warningCanvas, true);
        yield return new WaitForSeconds(warningDuration);
        InitCanvas(warningCanvas, false);

        // Guarda la dirección para usarla luego en DealDamageFromAnimation
        currentState = (trigger == "Atack_L") ? State.AttackingLeft : State.AttackingRight;

        animator.SetTrigger(trigger);
        yield return null; // Espera un frame para que el Animator entre al estado correcto
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animLength = stateInfo.length;

        // Espera a que termine la animación (debe regresar sola a Idle)
        yield return new WaitForSeconds(animLength);
    }


    private IEnumerator EnemyDefenseRoutine()
    {
        animator.SetTrigger("Defense");
        yield return null;
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
    }

    private IEnumerator EnemyIdleRoutine()
    {
        animator.SetTrigger("Idle");
        yield return null;
        yield return StartCoroutine(PlayClipRoutine(idleClips, availIdleClips));
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
    }

    private IEnumerator PlayClipRoutine(AudioClip[] array, List<AudioClip> pool)
    {
        if (array.Length == 0 || audioSource == null) yield break;
        if (pool.Count == 0) pool.AddRange(array);
        int idx = Random.Range(0, pool.Count);
        var clip = pool[idx];
        pool.RemoveAt(idx);

        yield return new WaitWhile(() => audioSource.isPlaying);
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void AttemptAttack(PlayerController.DefenseDirection dir)
    {
        bool heavy = Random.value * 100f < defenseBypassChance;
        int dmg = heavy ? attackDamage * 2 : attackDamage;
        player.TakeDamage(dmg, dir);

        if (player.IsDead)
        {
            InitCanvas(defeatCanvas, true);
            audioSource.PlayOneShot(winClip);
        }
    }

    public void TakeDamage(int damage, PlayerController.DefenseDirection attackDir)
    {
        if (isDead) return;
        StartCoroutine(PlayClipRoutine(damageClips, availDamageClips));
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        UpdateHealthVisual();
        if (currentHealth <= 0) HandleDeath();
    }

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
        if (panelNegro != null && panelNegro.alpha > 0f)
            yield return FadeCanvas(panelNegro, 0f, 0.3f);

        animator.SetTrigger("Death");
        yield return new WaitForSeconds(1f);

        if (victoryCanvas != null)
            yield return FadeCanvas(victoryCanvas, 1f, 0.3f);

        Destroy(gameObject);
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float targetAlpha, float duration)
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
    public void DealDamageFromAnimation()
    {
        // Determina la dirección
        var dir = (currentState == State.AttackingLeft)
                  ? PlayerController.DefenseDirection.Left
                  : PlayerController.DefenseDirection.Right;

        // Comprueba defensa y rango
        bool defended = (player.CurrentDefenseDirection == dir);
        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (!defended && dist <= attackRange)
            AttemptAttack(dir);
        else
            Debug.Log(defended
                ? "🚫 Bloqueado por defensa"
                : $"🚶‍♂️ Fuera de rango ({dist:F2} > {attackRange})");
    }
}
