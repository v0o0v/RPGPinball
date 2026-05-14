using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 엘리트 전용 투기장 고정 레이아웃 (M4 #1 인계).
    /// 4종: 봄 숲 / 심해 / 가을 미로 / 겨울 성벽.
    /// 절차 생성이 아닌 SO에 고정 정의.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Stage/Elite Arena", fileName = "ArenaLayoutData")]
    public class ArenaLayoutData : ScriptableObject
    {
        [Header("식별")]
        public EliteId eliteId;
        public ActId themeOwner;

        [Header("세그먼트 (고정)")]
        public string topSegmentId;
        public string[] middleSegmentIds;
        public string bottomSegmentId;

        [Header("기믹 잠금 / 금지")]
        [Tooltip("투기장에 강제 배치되는 기믹.")]
        public GimmickId[] lockedGimmickIds;
        [Tooltip("투기장에서 절대 등장 금지.")]
        public GimmickId[] forbiddenGimmickIds;

        [TextArea(2, 4)]
        public string descriptionKo;
    }
}
