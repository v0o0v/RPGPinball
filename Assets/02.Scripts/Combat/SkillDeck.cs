using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 하단 4칸 스킬 덱. 슬롯 선택 → 다음 터치 위치에서 발동. 동일 스킬 중복/궁극기 다중 장착 불가.
    /// 발동 딜레이 0.3초. 발동 중에는 다른 슬롯 사용 차단.
    /// </summary>
    public class SkillDeck : MonoBehaviour
    {
        public static SkillDeck Instance { get; private set; }

        [SerializeField] private SkillData[] equipped = new SkillData[Constants.SkillDeckSize];
        [SerializeField] private int[] levels = new int[Constants.SkillDeckSize];
        [SerializeField] private Transform skillRoot; // 스킬 인스턴스 생성 부모

        private bool isCasting;
        private CancellationTokenSource castCts;

        public bool IsCasting => isCasting;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            for (int i = 0; i < levels.Length; i++)
                if (levels[i] < 1) levels[i] = 1;
        }

        private void OnDisable()
        {
            castCts?.Cancel();
            castCts?.Dispose();
            castCts = null;
            if (Instance == this) Instance = null;
        }

        /// <summary>슬롯 장착. 동일 스킬 중복·궁극기 2개 이상은 거부.</summary>
        public bool Equip(int slot, SkillData skill, int level = 1)
        {
            if (slot < 0 || slot >= equipped.Length) return false;
            if (skill != null)
            {
                for (int i = 0; i < equipped.Length; i++)
                {
                    if (i == slot) continue;
                    if (equipped[i] == null) continue;
                    if (equipped[i].id == skill.id) return false;
                    if (equipped[i].isUltimate && skill.isUltimate) return false;
                }
            }
            equipped[slot] = skill;
            levels[slot] = Mathf.Max(1, level);
            return true;
        }

        public SkillData GetSkill(int slot) => (slot >= 0 && slot < equipped.Length) ? equipped[slot] : null;
        public int GetLevel(int slot) => (slot >= 0 && slot < levels.Length) ? levels[slot] : 0;

        /// <summary>슬롯 발동. 마나·발동중 체크 → 0.3초 딜레이 후 실제 효과 적용.</summary>
        public async UniTask<bool> Use(int slot, Vector2 worldPos)
        {
            if (isCasting) return false;
            var skill = GetSkill(slot);
            if (skill == null) return false;
            if (ManaSystem.Instance == null) return false;
            if (!ManaSystem.Instance.TrySpend(skill.manaCost)) return false;

            isCasting = true;
            castCts?.Dispose();
            castCts = new CancellationTokenSource();

            try
            {
                EventBus.Publish(new OnSkillCast { SkillId = skill.id, Position = worldPos });

                await UniTask.Delay(System.TimeSpan.FromSeconds(Constants.SkillCastDelay), cancellationToken: castCts.Token);

                var instance = CreateSkillInstance(skill, levels[slot]);
                if (instance != null)
                    await instance.Execute(worldPos, castCts.Token);
            }
            catch (System.OperationCanceledException) { /* 캔슬 정상 */ }
            finally
            {
                isCasting = false;
            }
            return true;
        }

        private ActiveSkillBase CreateSkillInstance(SkillData skill, int level)
        {
            // SkillData의 표시명을 기준으로 같은 이름의 prefab을 Resources에서 찾는 방식은 마일스톤 3에서 도입.
            // 현재는 같은 GameObject에 부착된 컴포넌트 중 SkillId가 일치하는 ActiveSkillBase를 찾아 사용.
            var pool = skillRoot != null ? skillRoot : transform;
            var existing = pool.GetComponentsInChildren<ActiveSkillBase>(true);
            foreach (var s in existing)
            {
                if (s.Data != null && s.Data.id == skill.id)
                {
                    s.Initialize(skill, level);
                    s.gameObject.SetActive(true);
                    return s;
                }
            }
            Debug.LogWarning($"[SkillDeck] SkillId={skill.id} ({skill.displayName})에 해당하는 ActiveSkillBase 인스턴스를 찾을 수 없음. skillRoot 하위에 자식으로 배치하세요.");
            return null;
        }
    }
}
