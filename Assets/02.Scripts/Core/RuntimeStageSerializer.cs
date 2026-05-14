using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Core
{
    /// <summary>
    /// 인게임 객체(Ball/Boss/Monster/Gimmick/...) 의 런타임 상태를 JSON 스냅샷으로 직렬화.
    /// OnApplicationPause(true) 시 PauseManager 가 호출 → 앱 강제 종료 후 복원용.
    /// 본 마일스톤은 ball/boss/timer/combo/mana 만 capture; 다른 카테고리는 IPersistableState 자동 수집으로 확장 가능.
    /// </summary>
    public static class RuntimeStageSerializer
    {
        public static string FilePath => Path.Combine(Application.persistentDataPath, Constants.RuntimeSnapshotFileName);

        /// <summary>현재 씬에서 IPersistableState 구현체를 수집하지 않고 직접 호출.</summary>
        public static RuntimeStageSnapshot Snapshot()
        {
            var snap = new RuntimeStageSnapshot
            {
                snapshotTimestampIso = DateTime.UtcNow.ToString("o")
            };

            // ── 타이머 / 마나 / 콤보 ────────────────────────────
            if (RPGPinball.Combat.StageTimer.Instance != null)
                snap.remainingTimeSec = RPGPinball.Combat.StageTimer.Instance.Remaining;
            if (RPGPinball.Combat.ManaSystem.Instance != null)
                snap.manaGauge = RPGPinball.Combat.ManaSystem.Instance.Mana;
            if (RPGPinball.Combat.ComboSystem.Instance != null)
                snap.comboCount = RPGPinball.Combat.ComboSystem.Instance.Combo;

            // ── 공 ────────────────────────────────────────────
            var balls = UnityEngine.Object.FindObjectsByType<RPGPinball.Physics.BallController>(FindObjectsSortMode.None);
            if (balls != null && balls.Length > 0)
            {
                snap.ballState = CaptureBall(balls[0]);
                for (int i = 1; i < balls.Length; i++)
                    snap.multiBalls.Add(CaptureBall(balls[i]));
            }

            // ── IPersistableState 자동 수집 (확장) ───────────────
            var persistables = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in persistables)
            {
                if (mb is IPersistableState ips)
                {
                    var state = ips.CaptureState();
                    if (state == null) continue;
                    // 카테고리별로 저장 — gimmick 만 예시. 확장 여지 둠.
                    if (ips.PersistKey == "gimmick" && state is GimmickSnapshot g) snap.activatedGimmicks.Add(g);
                    if (ips.PersistKey == "monster" && state is MonsterSnapshot m) snap.monstersAlive.Add(m);
                    if (ips.PersistKey == "boss" && state is BossSnapshot b) snap.bossState = b;
                }
            }

            return snap;
        }

        public static bool SnapshotToDisk()
        {
            try
            {
                var snap = Snapshot();
                var json = JsonUtility.ToJson(snap);
                File.WriteAllText(FilePath, json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RuntimeStageSerializer] 스냅샷 저장 실패: {e.Message}");
                return false;
            }
        }

        public static bool HasSnapshot() => File.Exists(FilePath);

        public static bool TryLoadFromDisk(out RuntimeStageSnapshot snap)
        {
            snap = null;
            try
            {
                if (!HasSnapshot()) return false;
                var json = File.ReadAllText(FilePath);
                snap = JsonUtility.FromJson<RuntimeStageSnapshot>(json);
                return snap != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RuntimeStageSerializer] 스냅샷 로드 실패: {e.Message}");
                return false;
            }
        }

        public static void DeleteSnapshot()
        {
            try { if (HasSnapshot()) File.Delete(FilePath); } catch { /* ignore */ }
        }

        /// <summary>스냅샷을 씬에 적용. 본 마일스톤은 timer/mana/combo + ball state 복원만.</summary>
        public static void Restore(RuntimeStageSnapshot snap)
        {
            if (snap == null) return;
            if (RPGPinball.Combat.StageTimer.Instance != null)
                RPGPinball.Combat.StageTimer.Instance.ResetTimer(snap.remainingTimeSec);
            if (RPGPinball.Combat.ManaSystem.Instance != null)
                RPGPinball.Combat.ManaSystem.Instance.SetManaDirect(snap.manaGauge);
            // 콤보 복원은 ComboSystem 의 public API 가 없어 무시 — 본 마일스톤 한계, M8 인계.
            var balls = UnityEngine.Object.FindObjectsByType<RPGPinball.Physics.BallController>(FindObjectsSortMode.None);
            if (balls != null && balls.Length > 0)
                ApplyBall(balls[0], snap.ballState);
        }

        private static BallSnapshot CaptureBall(RPGPinball.Physics.BallController ball)
        {
            var snap = new BallSnapshot();
            if (ball == null) return snap;
            snap.position = ball.transform.position;
            var rb = ball.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                snap.velocity = rb.linearVelocity;
                snap.angularVelocity = rb.angularVelocity;
            }
            return snap;
        }

        private static void ApplyBall(RPGPinball.Physics.BallController ball, BallSnapshot snap)
        {
            if (ball == null) return;
            ball.transform.position = snap.position;
            var rb = ball.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = snap.velocity;
                rb.angularVelocity = snap.angularVelocity;
            }
        }
    }
}
