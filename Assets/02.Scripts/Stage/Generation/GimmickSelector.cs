using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Stage.Generation
{
    /// <summary>
    /// 기믹 추첨 엔진.
    /// - 예산 소비 (BudgetConsumer)
    /// - 충돌 방지 (양방향 conflictingIds)
    /// - 시너지 가중치 ×1.5
    /// - 직전 스테이지 등장 가중치 ×0.5
    /// - 액트 전용 가중치 ×2
    /// - 테마 비율 40% 보장
    /// - 보상/버프 최소 1개 강제
    /// - 시련 3개 이상 시 구제 보상 자동 추가
    /// - 2026-05-14: 핀볼 정석 구역화. ForcePlaceZoneKey 로 zone 최소치 강제 + CommitPlacement zone-aware.
    /// </summary>
    public static class GimmickSelector
    {
        public static List<StageBlueprint.GimmickPlacementEntry> Select(
            ActId actId,
            DifficultyBand band,
            BudgetConsumer budget,
            DeterministicRng rng,
            GimmickPool pool,
            HashSet<GimmickId> previousStageIds,
            int middleSegmentCount)
        {
            var placements = new List<StageBlueprint.GimmickPlacementEntry>();
            var placedData = new List<GimmickData>();
            var usedIds = new HashSet<GimmickId>();

            var (minCount, maxCount) = DifficultyBudget.GimmickCountRange(band);
            int targetCount = rng.NextInt(minCount, maxCount + 1);

            var candidates = pool.GetCandidates(actId, GimmickPlacement.AnySegment);
            if (candidates.Count == 0) return placements;

            int themeCount = 0;
            // zone 별 슬롯 카운터 (좌우 페어 단위로 증가). index = (int)PinballZone.
            var slotPlacedByZone = new int[4];

            // 1단계: zone 별 최소 필수 기믹 강제 배치 (페어 단위)
            ForcePlaceZoneKey(placements, placedData, usedIds, pool, actId, budget, rng,
                ref themeCount, slotPlacedByZone, middleSegmentCount);

            // 2단계: 나머지 예산을 weighted 추첨으로 채움
            int safetyLimit = targetCount * 8;
            int attempts = 0;
            while (placements.Count < targetCount && attempts < safetyLimit)
            {
                attempts++;
                var entries = BuildWeightedEntries(candidates, placedData, usedIds, actId, previousStageIds, budget);
                if (entries.Count == 0) break;

                var picked = rng.WeightedPick(entries);
                if (picked == null) break;

                CommitPlacement(placements, placedData, usedIds, picked, budget,
                    ref themeCount, slotPlacedByZone, middleSegmentCount, actId);
            }

            EnsureRewardOrBuff(placements, placedData, usedIds, pool, actId, budget, rng,
                ref themeCount, slotPlacedByZone, middleSegmentCount);
            EnsureRescueReward(placements, placedData, usedIds, pool, actId, budget, rng,
                ref themeCount, slotPlacedByZone, middleSegmentCount);
            EnsureThemeRatio(placements, placedData, usedIds, pool, actId, budget, rng,
                ref themeCount, slotPlacedByZone, middleSegmentCount);

            return placements;
        }

        // ── 1단계: zone 최소 필수 강제 ────────────────────────────

        /// <summary>
        /// 각 PinballZone 에 최소 필수 기믹을 페어 단위로 배치.
        /// 명시 매핑 후보 우선, 없으면 카테고리 fallback. budget cap 도달 시 환급 안 함.
        /// </summary>
        private static void ForcePlaceZoneKey(
            List<StageBlueprint.GimmickPlacementEntry> placements,
            List<GimmickData> placedData,
            HashSet<GimmickId> usedIds,
            GimmickPool pool,
            ActId actId,
            BudgetConsumer budget,
            DeterministicRng rng,
            ref int themeCount,
            int[] slotPlacedByZone,
            int middleSegmentCount)
        {
            for (int z = 0; z < 4; z++)
            {
                var zone = (PinballZone)z;
                int required = PinballZoneMap.MinRequiredCount(zone);
                int placed = 0;
                int safety = 0;
                while (placed < required && safety < required * 6)
                {
                    safety++;
                    var pick = PickZoneCandidate(pool, actId, usedIds, placedData, zone, budget, rng);
                    if (pick == null) break;
                    CommitPlacement(placements, placedData, usedIds, pick, budget,
                        ref themeCount, slotPlacedByZone, middleSegmentCount, actId);
                    placed++;
                }
            }
        }

        /// <summary>주어진 zone 에 매핑되는 기믹 후보 1개 랜덤 픽. 우선순위: 명시 매핑 > fallback.</summary>
        private static GimmickData PickZoneCandidate(
            GimmickPool pool, ActId actId, HashSet<GimmickId> usedIds, List<GimmickData> placedData,
            PinballZone zone, BudgetConsumer budget, DeterministicRng rng)
        {
            var primary = new List<GimmickData>();   // 이 zone 의 기믹
            var secondary = new List<GimmickData>(); // 이 zone 의 기믹 (이미 used)
            foreach (var g in pool.All)
            {
                if (g == null || g.bossOnly) continue;
                if (g.themeOwner != ActId.None && g.themeOwner != actId) continue;
                if (PinballZoneMap.Resolve(g) != zone) continue;
                if (!budget.CanAfford(g.budgetCost)) continue;

                bool conflict = false;
                for (int j = 0; j < placedData.Count; j++)
                    if (GimmickPool.HasConflict(g, placedData[j])) { conflict = true; break; }
                if (conflict) continue;

                if (usedIds.Contains(g.gimmickId)) secondary.Add(g);
                else primary.Add(g);
            }
            if (primary.Count > 0) return primary[rng.NextInt(0, primary.Count)];
            if (secondary.Count > 0) return secondary[rng.NextInt(0, secondary.Count)];
            return null;
        }

        // ── 가중치 후보 빌드 ──────────────────────────────────────

        private static List<(GimmickData, float)> BuildWeightedEntries(
            List<GimmickData> candidates,
            List<GimmickData> alreadyPlaced,
            HashSet<GimmickId> usedIds,
            ActId actId,
            HashSet<GimmickId> previousStageIds,
            BudgetConsumer budget)
        {
            var entries = new List<(GimmickData, float)>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (!budget.CanAfford(c.budgetCost)) continue;

                bool conflict = false;
                bool synergy = false;
                for (int j = 0; j < alreadyPlaced.Count; j++)
                {
                    if (GimmickPool.HasConflict(c, alreadyPlaced[j])) { conflict = true; break; }
                    if (GimmickPool.HasSynergy(c, alreadyPlaced[j])) synergy = true;
                }
                if (conflict) continue;

                float weight = 1f;
                if (c.themeOwner == actId && actId != ActId.None) weight *= Constants.GimmickThemeWeightBonus;
                if (previousStageIds != null && previousStageIds.Contains(c.gimmickId)) weight *= Constants.GimmickDuplicateWeightDecay;
                if (synergy) weight *= Constants.GimmickSynergyWeightBonus;
                if (usedIds.Contains(c.gimmickId)) weight *= 0.25f;
                entries.Add((c, weight));
            }
            return entries;
        }

        // ── 배치 커밋 (zone-aware) ───────────────────────────────

        private static void CommitPlacement(
            List<StageBlueprint.GimmickPlacementEntry> placements,
            List<GimmickData> placedData,
            HashSet<GimmickId> usedIds,
            GimmickData picked,
            BudgetConsumer budget,
            ref int themeCount,
            int[] slotPlacedByZone,
            int middleSegmentCount,
            ActId actId)
        {
            // 2026-05-14 비율 기반 균등 분배: capacity 가 가득 차지 않은 zone 중 가장 덜 찬 것 우선.
            // 모든 zone 이 capacity 도달 시 placement 추가 중단 (이전엔 lap 누적 → magnitude<0.5U 시각 겹침 유발).
            // 검사는 budget/placedData 갱신 이전에 — placedData 와 placements 의 index 페어링을 깨지 않기 위해.
            float bestRatio = float.MaxValue;
            var zone = PinballZoneMap.Resolve(picked);
            bool anyAvailable = false;
            for (int z = 0; z < 4; z++)
            {
                int cap = PinballZoneMap.MaxCapacity((PinballZone)z);
                if (slotPlacedByZone[z] >= cap) continue;
                float ratio = cap > 0 ? (float)slotPlacedByZone[z] / cap : float.MaxValue;
                if (ratio < bestRatio) { bestRatio = ratio; zone = (PinballZone)z; anyAvailable = true; }
            }
            if (!anyAvailable) return; // 모든 zone 가득 — placement skip (capacity 합이 자연 상한)

            budget.TryConsume(picked.budgetCost);
            budget.TrackPlacement(picked.category);
            usedIds.Add(picked.gimmickId);
            placedData.Add(picked);
            if (picked.themeOwner == actId && actId != ActId.None) themeCount++;

            int seg = PinballZoneMap.SegmentIndexFor(zone, middleSegmentCount);
            int slot = slotPlacedByZone[(int)zone];
            // 2026-05-14 중복 방지: TrySwapNonThemeForTheme 가 placement 를 제거할 때 slotPlacedByZone 을
            // 감소시키지 않아 후속 커밋이 같은 (seg, slot) 페어를 재발급할 수 있다. 이미 사용 중인 페어면 다음 slot 으로 bump.
            while (PlacementOccupied(placements, seg, slot)) slot++;
            slotPlacedByZone[(int)zone] = slot + 1;

            placements.Add(new StageBlueprint.GimmickPlacementEntry
            {
                id = picked.gimmickId,
                segmentIndex = seg,
                slotIndex = slot
            });
        }

        private static bool PlacementOccupied(List<StageBlueprint.GimmickPlacementEntry> placements, int seg, int slot)
        {
            for (int k = 0; k < placements.Count; k++)
                if (placements[k].segmentIndex == seg && placements[k].slotIndex == slot) return true;
            return false;
        }

        // ── 강제 카테고리·테마 보장 ──────────────────────────────

        private static void EnsureRewardOrBuff(
            List<StageBlueprint.GimmickPlacementEntry> placements,
            List<GimmickData> placedData,
            HashSet<GimmickId> usedIds,
            GimmickPool pool,
            ActId actId,
            BudgetConsumer budget,
            DeterministicRng rng,
            ref int themeCount,
            int[] slotPlacedByZone,
            int middleSegmentCount)
        {
            if (!budget.NeedsRewardOrBuff()) return;
            ForceAddCategory(placements, placedData, usedIds, pool, actId, budget, rng,
                ref themeCount, slotPlacedByZone, middleSegmentCount, isReward: true);
        }

        private static void EnsureRescueReward(
            List<StageBlueprint.GimmickPlacementEntry> placements,
            List<GimmickData> placedData,
            HashSet<GimmickId> usedIds,
            GimmickPool pool,
            ActId actId,
            BudgetConsumer budget,
            DeterministicRng rng,
            ref int themeCount,
            int[] slotPlacedByZone,
            int middleSegmentCount)
        {
            if (!budget.NeedsRescueReward()) return;
            ForceAddCategory(placements, placedData, usedIds, pool, actId, budget, rng,
                ref themeCount, slotPlacedByZone, middleSegmentCount, isReward: true);
        }

        private static void EnsureThemeRatio(
            List<StageBlueprint.GimmickPlacementEntry> placements,
            List<GimmickData> placedData,
            HashSet<GimmickId> usedIds,
            GimmickPool pool,
            ActId actId,
            BudgetConsumer budget,
            DeterministicRng rng,
            ref int themeCount,
            int[] slotPlacedByZone,
            int middleSegmentCount)
        {
            if (placements.Count == 0 || actId == ActId.None) return;
            int safety = 0;
            int requiredTheme = Mathf.CeilToInt(placements.Count * Constants.ProcThemeRatioMin);
            while (themeCount < requiredTheme && safety < 8)
            {
                safety++;
                var themeCandidates = CollectThemeCandidates(pool, actId, usedIds, placedData);
                if (themeCandidates.Count > 0)
                {
                    var picked = themeCandidates[rng.NextInt(0, themeCandidates.Count)];
                    CommitPlacement(placements, placedData, usedIds, picked, budget,
                        ref themeCount, slotPlacedByZone, middleSegmentCount, actId);
                    requiredTheme = Mathf.CeilToInt(placements.Count * Constants.ProcThemeRatioMin);
                    continue;
                }
                if (!TrySwapNonThemeForTheme(placements, placedData, usedIds, pool, actId, budget, rng,
                        ref themeCount, slotPlacedByZone, middleSegmentCount))
                    break;
                requiredTheme = Mathf.CeilToInt(placements.Count * Constants.ProcThemeRatioMin);
            }
        }

        private static List<GimmickData> CollectThemeCandidates(GimmickPool pool, ActId actId, HashSet<GimmickId> usedIds, List<GimmickData> placedData)
        {
            var list = new List<GimmickData>();
            foreach (var g in pool.All)
            {
                if (g == null || g.bossOnly) continue;
                if (g.themeOwner != actId) continue;
                if (usedIds.Contains(g.gimmickId)) continue;
                bool conflict = false;
                for (int j = 0; j < placedData.Count; j++)
                    if (GimmickPool.HasConflict(g, placedData[j])) { conflict = true; break; }
                if (conflict) continue;
                list.Add(g);
            }
            return list;
        }

        /// <summary>
        /// 모든 테마 기믹이 placed 중 비테마와 충돌해서 들어갈 수 없는 경우,
        /// 비테마(또는 공통) 기믹을 1개 빼서 테마 기믹 자리를 만든다.
        /// </summary>
        private static bool TrySwapNonThemeForTheme(
            List<StageBlueprint.GimmickPlacementEntry> placements,
            List<GimmickData> placedData,
            HashSet<GimmickId> usedIds,
            GimmickPool pool,
            ActId actId,
            BudgetConsumer budget,
            DeterministicRng rng,
            ref int themeCount,
            int[] slotPlacedByZone,
            int middleSegmentCount)
        {
            int bestIndex = -1;
            int bestUnlock = 0;
            for (int i = 0; i < placedData.Count; i++)
            {
                var pd = placedData[i];
                if (pd == null || pd.themeOwner == actId) continue;

                int unlock = 0;
                foreach (var g in pool.All)
                {
                    if (g == null || g.bossOnly || g.themeOwner != actId) continue;
                    if (usedIds.Contains(g.gimmickId)) continue;
                    if (!GimmickPool.HasConflict(g, pd)) continue;
                    bool conflictsElse = false;
                    for (int j = 0; j < placedData.Count; j++)
                    {
                        if (j == i) continue;
                        if (GimmickPool.HasConflict(g, placedData[j])) { conflictsElse = true; break; }
                    }
                    if (!conflictsElse) unlock++;
                }
                if (unlock > bestUnlock) { bestUnlock = unlock; bestIndex = i; }
            }
            if (bestIndex < 0) return false;

            var removed = placedData[bestIndex];
            placedData.RemoveAt(bestIndex);
            placements.RemoveAt(bestIndex);
            usedIds.Remove(removed.gimmickId);
            budget.Refund(System.Math.Max(0, removed.budgetCost));

            var freshCandidates = CollectThemeCandidates(pool, actId, usedIds, placedData);
            if (freshCandidates.Count == 0) return false;
            var picked = freshCandidates[rng.NextInt(0, freshCandidates.Count)];
            CommitPlacement(placements, placedData, usedIds, picked, budget,
                ref themeCount, slotPlacedByZone, middleSegmentCount, actId);
            return true;
        }

        private static void ForceAddCategory(
            List<StageBlueprint.GimmickPlacementEntry> placements,
            List<GimmickData> placedData,
            HashSet<GimmickId> usedIds,
            GimmickPool pool,
            ActId actId,
            BudgetConsumer budget,
            DeterministicRng rng,
            ref int themeCount,
            int[] slotPlacedByZone,
            int middleSegmentCount,
            bool isReward)
        {
            var bucket = new List<GimmickData>();
            foreach (var g in pool.All)
            {
                if (g == null || g.bossOnly) continue;
                if (g.themeOwner != ActId.None && g.themeOwner != actId) continue;
                if (usedIds.Contains(g.gimmickId)) continue;
                bool isRewardCat = g.category == GimmickCategory.Reward || g.category == GimmickCategory.Buff;
                if (isReward != isRewardCat) continue;

                bool conflict = false;
                for (int j = 0; j < placedData.Count; j++)
                    if (GimmickPool.HasConflict(g, placedData[j])) { conflict = true; break; }
                if (conflict) continue;
                bucket.Add(g);
            }
            if (bucket.Count == 0) return;
            var picked = bucket[rng.NextInt(0, bucket.Count)];
            CommitPlacement(placements, placedData, usedIds, picked, budget,
                ref themeCount, slotPlacedByZone, middleSegmentCount, actId);
        }
    }
}
