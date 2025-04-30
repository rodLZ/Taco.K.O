using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    [System.Serializable]
    public class ActionConfig
    {
        public int probability;
    }
    [Header("Auto-Defense Settings")]
    [SerializeField] private int hitsToTriggerDefense = 3;
    [SerializeField] private float hitWindowDuration = 2f;
    private bool isInvulnerable = false;
    private int recentHits = 0;
    private float lastHitTime = -999f;
    private bool forcedDefense = false;

    [Header("Combat Settings")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float defenseBypassChance = 5f;
    [SerializeField] private int maxHealth = 100;

    [Header("AI Action Probabilities")]
    [SerializeField] private ActionConfig attackConfig;
    [SerializeField] private ActionConfig defenseConfig;
    [SerializeField] private ActionConfig idleConfig;


    [Header("UI References")]
    [SerializeField] private CanvasGroup warningCanvasLeft;
    [SerializeField] private CanvasGroup warningCanvasRight;
    [SerializeField] private CanvasGroup panelNegro;
    [SerializeField] private CanvasGroup victoryCanvas;
    [SerializeField] private CanvasGroup defeatCanvas;
    [SerializeField] private Image healthBar;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField] private AudioClip[] damageClips;
    [SerializeField] private AudioClip[] idleClips;
    [SerializeField] private AudioClip winClip;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    public float actionCooldown = 1.0f;

    private enum State { Idle, AttackingLeft, AttackingRight, Defending, Hurt, Dead }
    private State currentState = State.Idle;

    private PlayerController player;
    private float currentHealth;
    private bool canAct = true;
    private bool isDead = false;
    private bool victoryShown = false;
    private List<AudioClip> availAttackClips, availDamageClips, availIdleClips;
    private Coroutine aiCoroutine;

    private const string HURT_TRIGGER = "Hurt";

    private void Start()
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

        aiCoroutine = StartCoroutine(AIBehaviorLoop());
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
            if (!canAct)
            {
                yield return null;
                continue;
            }

            State next = ChooseNextAction();
            canAct = false;
            yield return StartCoroutine(PerformAction(next));
            yield return null;
        }
    }

    private State ChooseNextAction()
    {
        if (forcedDefense)
        {
            forcedDefense = false;   // Reset
            recentHits = 0;           // Reset
            return State.Defending;   // Forzar defensa
        }

        int total = attackConfig.probability + defenseConfig.probability + idleConfig.probability;
        int roll = Random.Range(0, total);

        if (roll < attackConfig.probability)
            return Random.value < 0.5f ? State.AttackingLeft : State.AttackingRight;
        if (roll < attackConfig.probability + defenseConfig.probability)
            return State.Defending;
        return State.Idle;
    }


    private IEnumerator PerformAction(State action)
    {
        currentState = action;

        switch (action)
        {
            case State.AttackingLeft:
                yield return StartCoroutine(AttackRoutine("Attack_L", warningCanvasLeft));
                break;
            case State.AttackingRight:
                yield return StartCoroutine(AttackRoutine("Attack_R", warningCanvasRight));
                break;
            case State.Defending:
                yield return StartCoroutine(DefenseRoutine());
                break;
            case State.Idle:
                yield return StartCoroutine(IdleRoutine());
                break;
        }

        if (!isDead)
        {
            currentState = State.Idle;
            CrossfadeAnim("Idle", 0.1f);

            // 🔥 Aquí respetamos el cooldown antes de volver a actuar
            yield return new WaitForSeconds(actionCooldown);

            canAct = true;
        }
    }


    private IEnumerator AttackRoutine(string triggerName, CanvasGroup warningCanvas)
    {
        InitCanvas(warningCanvas, true);
        yield return new WaitForSeconds(0.5f);
        InitCanvas(warningCanvas, false);

        CrossfadeAnim(triggerName, 0.1f);
        PlayRandomClip(availAttackClips, attackClips);

        yield return new WaitForSeconds(GetAnimationLength(triggerName));
    }

    private IEnumerator DefenseRoutine()
    {
        CrossfadeAnim("Defense", 0.1f);
        yield return new WaitForSeconds(GetAnimationLength("Defense"));
    }

    private IEnumerator IdleRoutine()
    {
        CrossfadeAnim("Idle", 0.1f);
        PlayRandomClip(availIdleClips, idleClips);
        yield return new WaitForSeconds(Random.Range(2f, 3f));
    }

    private float GetAnimationLength(string animName)
    {
        var clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == animName);
        return clip != null ? clip.length : 1f;
    }

    private void PlayRandomClip(List<AudioClip> pool, AudioClip[] originalArray)
    {
        if (pool.Count == 0) pool.AddRange(originalArray);
        if (pool.Count > 0 && audioSource != null)
        {
            int index = Random.Range(0, pool.Count);
            AudioClip clip = pool[index];
            pool.RemoveAt(index);

            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private void CrossfadeAnim(string animName, float fadeDuration)
    {
        if (HasAnimation(animName))
        {
            animator.CrossFade(animName, fadeDuration);
        }
        else
        {
            Debug.LogWarning($"Animation '{animName}' not found!");
        }
    }

    private bool HasAnimation(string animName)
    {
        return animator.runtimeAnimatorController.animationClips.Any(c => c.name == animName);
    }

    public void DealDamageFromAnimation()
    {
        if (isDead || player == null) return;

        var expectedDir = currentState == State.AttackingLeft
            ? PlayerController.DefenseDirection.Left
            : PlayerController.DefenseDirection.Right;

        bool defended = player.CurrentDefenseDirection == expectedDir;
        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (!defended && distance <= attackRange)
            AttemptAttack(expectedDir);
        else
            Debug.Log(defended ? "🛡️ Ataque bloqueado" : "🚶‍♂️ Fuera de rango");
    }

    private void AttemptAttack(PlayerController.DefenseDirection dir)
    {
        int damage = Random.value * 100f < defenseBypassChance ? attackDamage * 2 : attackDamage;
        player.TakeDamage(damage, dir);
        if (player.IsDead)
        {
            InitCanvas(defeatCanvas, true);
            if (winClip != null) audioSource.PlayOneShot(winClip);
        }
    }

    public void TakeDamage(int amount, PlayerController.DefenseDirection attackDir)
    {
        if (isDead || isInvulnerable) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        UpdateHealthVisual();
        PlayRandomClip(availDamageClips, damageClips);

        TrackHits(); // 👈 Sigue contando golpes para la Auto-Defensa

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // ✅ SOLO hacer "Hurt" si estaba en Idle
            if (currentState == State.Idle)
                StartCoroutine(HurtRoutine());
        }
    }

    private IEnumerator HurtAndDefendRoutine()
    {
        if (aiCoroutine != null) StopCoroutine(aiCoroutine);

        currentState = State.Hurt;
        CrossfadeAnim(HURT_TRIGGER, 0.1f);
        yield return new WaitForSeconds(GetAnimationLength(HURT_TRIGGER));

        if (!isDead)
        {
            // Luego de recibir daño, se defiende
            CrossfadeAnim("Defense", 0.1f);
            yield return new WaitForSeconds(GetAnimationLength("Defense"));

            // Luego regresa al comportamiento normal
            aiCoroutine = StartCoroutine(AIBehaviorLoop());
        }
    }

    private void Die()
    {
        isDead = true;
        CrossfadeAnim("Death", 0.1f);

        if (!victoryShown)
        {
            victoryShown = true;
            StartCoroutine(FadeCanvas(victoryCanvas, 1f, 0.3f));
            StartCoroutine(DestroyAfter(victoryCanvas, 0.3f));
        }
    }

    private void UpdateHealthVisual()
    {
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float targetAlpha, float duration)
    {
        if (cg == null) yield break;
        float start = cg.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, targetAlpha, elapsed / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;
        cg.blocksRaycasts = targetAlpha > 0;
        cg.interactable = targetAlpha > 0;
    }

    private IEnumerator DestroyAfter(CanvasGroup cg, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private void TrackHits()
    {
        if (Time.time - lastHitTime > hitWindowDuration)
        {
            // Si pasó mucho tiempo desde el último golpe, resetea
            recentHits = 0;
        }

        recentHits++;
        lastHitTime = Time.time;

        if (recentHits >= hitsToTriggerDefense && !forcedDefense)
        {
            // Forzar defensa si recibió demasiados golpes en poco tiempo
            forcedDefense = true;
        }
    }

    private IEnumerator HurtRoutine()
    {
        if (aiCoroutine != null) StopCoroutine(aiCoroutine);

        isInvulnerable = true;  // Se vuelve invulnerable
        CrossfadeAnim(HURT_TRIGGER, 0.1f);
        yield return new WaitForSeconds(GetAnimationLength(HURT_TRIGGER));

        isInvulnerable = false; // Puede ser golpeado de nuevo
        if (!isDead)
            aiCoroutine = StartCoroutine(AIBehaviorLoop());
    }

    public void OnIdleComplete() { }
}

