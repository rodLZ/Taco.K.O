// EnemyAI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;  // ← para usar Any()

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
    private bool victoryShown = false;

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
    private Coroutine aiBehaviorCoroutine;

    // Nombre del trigger que dispara la animación de daño
    private const string HURT_TRIGGER = "Hirt";

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        availAttackClips = new List<AudioClip>(attackClips);
        availDamageClips = new List<AudioClip>(damageClips);
        availIdleClips = new List<AudioClip>(idleClips);

        // Verificar que exista el Trigger "Hirt" en el Animator
        bool hasHurtParam = animator.parameters
            .Any(p => p.type == AnimatorControllerParameterType.Trigger && p.name == HURT_TRIGGER);
        if (!hasHurtParam)
            Debug.LogWarning($"⚠️ No se encontró el parámetro Trigger '{HURT_TRIGGER}' en el Animator Controller.");

        InitCanvas(panelNegro, false);
        InitCanvas(victoryCanvas, false);
        InitCanvas(defeatCanvas, false);
        InitCanvas(warningCanvasLeft, false);
        InitCanvas(warningCanvasRight, false);

        currentHealth = maxHealth;
        UpdateHealthVisual();

        aiBehaviorCoroutine = StartCoroutine(AIBehaviorLoop());
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

        currentState = trigger == "Atack_L" ? State.AttackingLeft : State.AttackingRight;
        animator.SetTrigger(trigger);
        yield return null;

        yield return StartCoroutine(PlayClipRoutine(attackClips, availAttackClips));

        float animLength = animator.GetCurrentAnimatorStateInfo(0).length;
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

        StartCoroutine(PlayClipRoutine(idleClips, availIdleClips));

        float animLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animLength);
    }

    private IEnumerator PlayClipRoutine(AudioClip[] array, List<AudioClip> pool)
    {
        if (array.Length == 0 || audioSource == null)
            yield break;

        if (pool.Count == 0)
            pool.AddRange(array);

        int idx = Random.Range(0, pool.Count);
        AudioClip clip = pool[idx];
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
            if (!audioSource.isPlaying && winClip != null)
                audioSource.PlayOneShot(winClip);
        }
    }

    public void TakeDamage(int damage, PlayerController.DefenseDirection attackDir)
    {
        if (isDead) return;

        StartCoroutine(PlayClipRoutine(damageClips, availDamageClips));
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        UpdateHealthVisual();

        if (currentHealth <= 0)
        {
            HandleDeath();
        }
        else
        {
            if (currentState == State.Idle)
                StartCoroutine(HurtRoutine());
        }
    }


    private IEnumerator HurtRoutine()
    {
        // 1) Detén el loop de IA actual
        if (aiBehaviorCoroutine != null)
            StopCoroutine(aiBehaviorCoroutine);

        // 2) Dispara el trigger de Hirt
        animator.ResetTrigger(HURT_TRIGGER);
        animator.SetTrigger(HURT_TRIGGER);

        // 3) Calcula la duración de la animación Hirt
        float hirtLength = 0f;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == HURT_TRIGGER)
            {
                hirtLength = clip.length;
                break;
            }
        }
        if (hirtLength <= 0f) hirtLength = 0.5f; // fallback

        // 4) Espera a que termine Hirt
        yield return new WaitForSeconds(hirtLength);

        // 5) Asegura que la IA pueda actuar
        canAct = true;

        // 6) Reinicia el loop de IA para que elija su siguiente acción
        aiBehaviorCoroutine = StartCoroutine(AIBehaviorLoop());
    }


    private void UpdateHealthVisual()
    {
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;
        animator.SetTrigger("Death");
    }

    // Llamado por Animation Event al final de Death
    private void OnDeathAnimationComplete()
    {
        if (victoryShown) return;
        victoryShown = true;

        if (victoryCanvas != null)
            StartCoroutine(FadeCanvas(victoryCanvas, 1f, 0.3f));
        StartCoroutine(DestroyAfter(victoryCanvas, 0.3f));
    }

    private IEnumerator DestroyAfter(CanvasGroup cg, float delay)
    {
        yield return new WaitForSeconds(delay);
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

    // Enlace para Animation Event al impactar
    public void DealDamageFromAnimation()
    {
        var dir = currentState == State.AttackingLeft
                  ? PlayerController.DefenseDirection.Left
                  : PlayerController.DefenseDirection.Right;

        bool defended = player.CurrentDefenseDirection == dir;
        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (!defended && dist <= attackRange)
            AttemptAttack(dir);
        else
            Debug.Log(defended
                ? "🚫 Bloqueado por defensa"
                : $"🚶‍♂️ Fuera de rango ({dist:F2} > {attackRange})");
    }
}
