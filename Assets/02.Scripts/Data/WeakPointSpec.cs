using System;
using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 보스 약점 부위 스펙. BossBase가 weakPoints[]를 자식 GameObject로 생성하여
    /// WeakPointHitbox 컴포넌트를 부착. 공이 약점 콜라이더에 충돌하면 본체에 가중치 데미지 적용.
    /// 식충식물 꽃봉오리, 무장 게 배, 크라켄 본체 눈 등에서 사용.
    /// </summary>
    [Serializable]
    public struct WeakPointSpec
    {
        [Tooltip("부위 식별용 이름 (꽃봉오리, 배 등)")]
        public string label;

        [Tooltip("보스 본체 기준 상대 좌표")]
        public Vector2 localOffset;

        [Tooltip("약점 콜라이더 반지름")]
        public float radius;

        [Tooltip("true면 단계 [9]에서 DEF=0 처리")]
        public bool ignoresDefense;

        [Tooltip("해당 Phase부터 활성 (P1=모든 페이즈, P2=Phase 2부터, P3=Phase 3부터)")]
        public BossPhase activeFromPhase;

        [Tooltip("약점 타격 시 데미지 가중치 (1.5 = 1.5배 데미지)")]
        public float damageMultiplier;
    }
}
