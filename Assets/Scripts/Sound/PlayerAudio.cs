using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Whoosh (휘두를 때, 맞든 안 맞든 항상 재생)")]
    public AudioClip[] whooshSounds;

    [Header("Hit Sounds (적을 실제로 맞췄을 때만)")]
    public AudioClip punchAHitSound;
    public AudioClip punchBHitSound;
    public AudioClip punchCHitSound;
    public AudioClip kickAHitSound;   // ★ 추가
    public AudioClip kickCHitSound;   // ★ 추가
    public AudioClip smashHitSound;
    public AudioClip dashAttackHitSound; // ★ 추가

    [Header("Common Hit Impact (어떤 공격이든 맞추면 항상 재생, 볼륨 작게)")] // ★ 추가
    public AudioClip[] commonHitSounds; // 여러 개 넣으면 랜덤 재생
    [Range(0f, 1f)]
    public float commonHitVolume = 0.4f; // ★ 추가: 작게 재생하고 싶으신 볼륨

    [Header("Ground Slam (착지 시)")] // ★ 추가
    public AudioClip groundSlamSound;

    [Header("Player Hit (내가 맞았을 때)")] // ★ 추가
    public AudioClip[] playerHurtSounds; // 여러 개면 랜덤 재생

    [Header("Pitch Variation")]
    public float pitchVariance = 0.05f;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        PunchHitbox.OnEnemyHit += HandleEnemyHit;
        PlayerHealth.OnPlayerTookDamage += HandlePlayerHurt; // ★ 추가
        GetComponent<PlayerController>().OnSlamLand += HandleGroundSlam; // ★ 추가
    }

    void OnDisable()
    {
        PunchHitbox.OnEnemyHit -= HandleEnemyHit;
        PlayerHealth.OnPlayerTookDamage -= HandlePlayerHurt; // ★ 추가

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.OnSlamLand -= HandleGroundSlam; // ★ 추가
    }

    // Animation Event: 펀치/킥/스매시 등 휘두르는 동작에 연결 (모든 공격 공통, String 파라미터 없음)
    public void AnimEvent_PlayWhoosh()
    {
        if (whooshSounds == null || whooshSounds.Length == 0) return;
        AudioClip clip = whooshSounds[Random.Range(0, whooshSounds.Length)];
        PlaySound(clip);
    }

    // PunchHitbox가 실제로 적을 맞췄을 때 자동 호출됨
    void HandleEnemyHit(string hitSoundId)
    {
        // ★ 추가: 어떤 공격이든 맞추면 항상 재생되는 공통 타격음 (작은 볼륨)
        if (commonHitSounds != null && commonHitSounds.Length > 0)
        {
            AudioClip common = commonHitSounds[Random.Range(0, commonHitSounds.Length)];
            PlaySound(common, commonHitVolume);
        }

        // 기존: 펀치A/B/C, Kick 등 개별 히트음 (이건 그대로 유지)
        AudioClip clip = GetHitClip(hitSoundId);
        PlaySound(clip);
    }

    AudioClip GetHitClip(string id)
    {
        switch (id)
        {
            case "A": return punchAHitSound;
            case "B": return punchBHitSound;
            case "C": return punchCHitSound;
            case "KickA": return kickAHitSound;   // ★ 추가
            case "KickC": return kickCHitSound;   // ★ 추가
            case "Smash": return smashHitSound;
            case "DashAttack": return dashAttackHitSound; // ★ 추가
            default: return null;
        }
    }

    // ★ 추가: 플레이어가 맞는 순간 자동 재생
    void HandlePlayerHurt()
    {
        if (playerHurtSounds == null || playerHurtSounds.Length == 0) return;
        AudioClip clip = playerHurtSounds[Random.Range(0, playerHurtSounds.Length)];
        PlaySound(clip);
    }

    // ★ 추가: GroundSlam 착지 순간 자동 재생 (카메라 흔들림과 동일 타이밍)
    void HandleGroundSlam()
    {
        PlaySound(groundSlamSound);
    }

    void PlaySound(AudioClip clip, float volume = 1f) // ★ 변경: volume 파라미터 추가 (기본값 1이라 기존 호출부는 안 건드려도 됨)
    {
        if (clip == null || audioSource == null) return;

        audioSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        audioSource.PlayOneShot(clip, volume); // ★ 변경: PlayOneShot의 두 번째 인자로 볼륨 지정
    }
}