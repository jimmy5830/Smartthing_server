using UnityEngine;
using UnityEngine.AI;

// killer 프리팹용 싱글플레이어 추격 AI (Mirror 불필요)
[RequireComponent(typeof(NavMeshAgent))]
public class KillerAI : MonoBehaviour
{
    [Header("타겟")]
    [SerializeField] private string playerTag = "Player";

    [Header("추격 거리 설정")]
    [SerializeField] private float detectRange = 15f; // 안에 들어오면 추격 시작
    [SerializeField] private float loseRange   = 25f; // 밖으로 벗어나면 추격 포기
    [SerializeField] private float catchRange  = 1.5f; // 안 = 처치(게임오버)

    [Header("이동 속도")]
    [SerializeField] private float walkSpeed  = 1.5f; // 배회
    [SerializeField] private float chaseSpeed = 4.0f; // 추격

    [Header("배회 지점 (비우면 제자리 대기)")]
    [SerializeField] private Transform[] patrolPoints;

    [Header("애니메이터 (PlayerAIController — float 3개)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleParam = "Idle";
    [SerializeField] private string walkParam = "Walk";
    [SerializeField] private string runParam  = "Run";

    private NavMeshAgent agent;
    private Transform player;
    private int patrolIndex = 0;
    private bool isChasing = false;
    private bool caught = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        agent.speed = walkSpeed;
        GoToNextPatrol();
    }

    private void Update()
    {
        if (caught || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (!isChasing && dist <= detectRange) { isChasing = true; agent.speed = chaseSpeed; }
        else if (isChasing && dist >= loseRange) { isChasing = false; agent.speed = walkSpeed; GoToNextPatrol(); }

        if (isChasing)
        {
            agent.SetDestination(player.position);
            if (dist <= catchRange) Catch();
        }
        else Patrol();

        UpdateAnimator(agent.velocity.magnitude);
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            GoToNextPatrol();
    }

    private void GoToNextPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        agent.SetDestination(patrolPoints[patrolIndex].position);
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    private void Catch()
    {
        caught = true;
        agent.isStopped = true;
        Debug.Log("[KillerAI] 플레이어 처치 — 게임 오버");
        // TODO: 게임오버 UI / 씬 리로드
        // STInteracter.Instance.SendEvent("recovery"); // 기기 안전 복구
    }

    // 개별 State + Any State 전환 구조 — 한 개만 1, 나머지 0
    private void UpdateAnimator(float speed)
    {
        if (animator == null) return;

        bool isIdle = speed < 0.1f;
        bool isRun  = speed > walkSpeed;
        bool isWalk = !isIdle && !isRun;

        animator.SetFloat(idleParam, isIdle ? 1f : 0f);
        animator.SetFloat(walkParam, isWalk ? 1f : 0f);
        animator.SetFloat(runParam,  isRun  ? 1f : 0f);
    }

    // 에디터에서 감지 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, catchRange);
    }
}