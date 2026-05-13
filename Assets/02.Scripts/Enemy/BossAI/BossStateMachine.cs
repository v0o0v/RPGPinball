using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Enemy.BossAI
{
    /// <summary>
    /// 보스 공통 행동 사이클(Idle → Telegraph → Execute → Recovery → Idle) 상태머신.
    /// 패턴 가중치 추첨으로 다음 패턴을 선택하고, 분노 모드면 Recovery에 0.7배 적용.
    /// </summary>
    public class BossStateMachine : MonoBehaviour
    {
        private BossBase boss;
        private BossData data;
        private IBossPattern[] patterns;
        private CancellationTokenSource loopCts;
        private BossActionState state = BossActionState.Idle;
        // 패턴별 마지막 실행 시각 (minIntervalSeconds 적용용)
        private readonly Dictionary<string, float> lastExecutedAt = new();
        // 이전 패턴 ID (연속 동일 패턴 회피)
        private string previousPatternId;
        private bool running;

        public BossActionState State => state;

        public void Configure(BossBase b, BossData d, IBossPattern[] p)
        {
            boss = b;
            data = d;
            patterns = p ?? System.Array.Empty<IBossPattern>();
        }

        public void Begin()
        {
            if (running) return;
            running = true;
            loopCts = new CancellationTokenSource();
            Loop(loopCts.Token).Forget();
        }

        public void Stop()
        {
            running = false;
            if (loopCts != null)
            {
                try { loopCts.Cancel(); } catch { }
                loopCts.Dispose();
                loopCts = null;
            }
        }

        private void OnDestroy() => Stop();

        private async UniTask Loop(CancellationToken ct)
        {
            while (running && !ct.IsCancellationRequested && boss != null && !boss.IsDead)
            {
                state = BossActionState.Idle;
                // 짧은 Idle (0.2초)
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.2f), cancellationToken: ct);
                if (ct.IsCancellationRequested) return;

                // 패턴 선택
                var picked = PickPattern();
                if (picked == null)
                {
                    // 사용 가능 패턴 없음 — 1초 대기 후 재시도
                    await UniTask.Delay(System.TimeSpan.FromSeconds(1f), cancellationToken: ct);
                    continue;
                }
                var (pattern, meta) = picked.Value;

                // Telegraph
                state = BossActionState.Telegraph;
                float teleSec = Mathf.Max(0f, meta.telegraphSeconds);
                if (teleSec > 0f)
                    await UniTask.Delay(System.TimeSpan.FromSeconds(teleSec), cancellationToken: ct);
                if (ct.IsCancellationRequested) return;
                if (boss == null || boss.IsDead) return;

                // Execute
                state = BossActionState.Execute;
                lastExecutedAt[meta.patternId] = Time.time;
                previousPatternId = meta.patternId;
                try
                {
                    await pattern.Execute(boss, ct);
                }
                catch (System.OperationCanceledException) { return; }
                catch (System.Exception e)
                {
                    Debug.LogError($"[BossStateMachine] 패턴 실행 오류: {meta.patternId} / {e}");
                }
                if (ct.IsCancellationRequested) return;
                if (boss == null || boss.IsDead) return;

                // Recovery (분노 시 0.7배)
                state = BossActionState.Recovery;
                float recovery = Mathf.Max(0f, meta.recoverySeconds);
                if (boss.IsEnraged) recovery *= Constants.BossEnragedRecoveryMul;
                if (recovery > 0f)
                    await UniTask.Delay(System.TimeSpan.FromSeconds(recovery), cancellationToken: ct);
            }
        }

        private (IBossPattern pattern, BossPatternMetadata meta)? PickPattern()
        {
            if (patterns.Length == 0 || data == null || data.patterns == null || data.patterns.Length == 0)
                return null;

            // 현재 페이즈에 사용 가능한 패턴만 필터링
            var phase = boss != null ? boss.CurrentPhase : BossPhase.P1;
            var validIndices = new List<int>();
            float totalWeight = 0f;

            for (int i = 0; i < data.patterns.Length && i < patterns.Length; i++)
            {
                var meta = data.patterns[i];
                // 페이즈 조건
                if (meta.exclusiveToPhase)
                {
                    if (meta.availableFromPhase != phase) continue;
                }
                else
                {
                    if ((int)phase < (int)meta.availableFromPhase) continue;
                }
                // 재사용 간격
                if (lastExecutedAt.TryGetValue(meta.patternId, out var t)
                    && (Time.time - t) < meta.minIntervalSeconds)
                {
                    continue;
                }
                validIndices.Add(i);
                totalWeight += Mathf.Max(0f, meta.weightPercent);
            }

            if (validIndices.Count == 0) return null;
            if (totalWeight <= 0f)
            {
                int randIdx = validIndices[Random.Range(0, validIndices.Count)];
                return (patterns[randIdx], data.patterns[randIdx]);
            }

            // 가중치 추첨
            float roll = Random.Range(0f, totalWeight);
            float acc = 0f;
            for (int j = 0; j < validIndices.Count; j++)
            {
                int idx = validIndices[j];
                acc += Mathf.Max(0f, data.patterns[idx].weightPercent);
                if (roll <= acc)
                {
                    return (patterns[idx], data.patterns[idx]);
                }
            }

            // 안전망 — 마지막 항목 반환
            int last = validIndices[validIndices.Count - 1];
            return (patterns[last], data.patterns[last]);
        }
    }
}
