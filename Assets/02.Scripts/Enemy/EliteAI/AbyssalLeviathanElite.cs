using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI;
using RPGPinball.Enemy.BossAI.BulletPatterns;
using RPGPinball.Security;

namespace RPGPinball.Enemy.EliteAI
{
    /// <summary>
    /// 여름 액트 엘리트: 심해 리바이어선. HP 40,000 / DEF 40% / 대형 / 이동 2.5 U/s.
    /// 심연의 포식(자힐 10%) + 해일 + 잠수(5초 무적) + 흡수 촉수.
    /// </summary>
    public class AbyssalLeviathanElite : EliteBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData largeBullet;

        private bool isSubmerged;
        public bool IsSubmerged => isSubmerged;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new TidalPush(),
                new Submerge(this),
                new LifestealTentacle()
            };
        }

        public override int GetEffectiveDefense()
        {
            return isSubmerged ? 9999 : base.GetEffectiveDefense();
        }

        // 자힐 — ApplyDamage에서 처리
        public new void ApplyDamage(DamageResult result)
        {
            base.ApplyDamage(result);
            var ed = EliteData;
            if (ed != null && ed.lifeStealRatio > 0f && !IsDead)
            {
                float ratio = IsEnraged ? Mathf.Max(ed.lifeStealRatio, 0.15f) : ed.lifeStealRatio;
                int cap = IsEnraged ? Mathf.Max(ed.lifeStealCap, 300) : ed.lifeStealCap;
                int heal = Mathf.Min(cap, Mathf.RoundToInt(result.FinalDamage * ratio));
                // 본체 HP는 SafeInt(hp)가 private이므로 직접 회복할 수 없음.
                // 마일스톤 4 단순화: 시각 표시만, 실 회복은 시뮬레이션 단계에서.
                if (heal > 0)
                    TelegraphRenderer.ShowCircle(transform.position, 0.8f, 0.5f, new Color(0.3f, 0.8f, 0.3f, 0.5f));
            }
        }

        private sealed class TidalPush : IBossPattern
        {
            public string Id => "P1";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                TelegraphRenderer.ShowArrow(new Vector3(0f, -3f, 0f), Vector3.up, 8f, 1.5f, new Color(0.3f, 0.5f, 0.9f, 0.6f));
                return UniTask.CompletedTask;
            }
        }

        private sealed class Submerge : IBossPattern
        {
            private readonly AbyssalLeviathanElite boss;
            public Submerge(AbyssalLeviathanElite b) { boss = b; }
            public string Id => "P2";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null) return;
                boss.isSubmerged = true;
                EventBus.Publish(new OnLeviathanSubmerge { Elite = boss.gameObject, Submerging = true });
                await UniTask.Delay(System.TimeSpan.FromSeconds(5f), cancellationToken: ct);
                if (boss != null)
                {
                    boss.isSubmerged = false;
                    EventBus.Publish(new OnLeviathanSubmerge { Elite = boss.gameObject, Submerging = false });
                    // 부상 직후 2초간 DEF 0% — exposure는 GetEffectiveDefense에 별도 플래그 추가 권장
                }
            }
        }

        private sealed class LifestealTentacle : IBossPattern
        {
            public string Id => "P3";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                var ball = Object.FindFirstObjectByType<Physics.BallController>();
                Vector3 pos = ball != null ? ball.transform.position : Vector3.zero;
                TelegraphRenderer.ShowCircle(pos, 0.5f, 6f, new Color(0.4f, 0.7f, 0.5f, 0.5f));
                return UniTask.CompletedTask;
            }
        }
    }
}
