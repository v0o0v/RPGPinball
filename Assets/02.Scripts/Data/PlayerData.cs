using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 플레이어 영구 데이터. Data_Schema.md §2 일치.
    /// 마일스톤 7 SaveSystem 도입 전까지는 Inspector 값 또는 메모리 상태로 사용.
    /// 핵심 수치는 LevelSystem/SkillTreeManager에서 SafeInt로 래핑하여 사용.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Player Data", fileName = "PlayerData")]
    public class PlayerData : ScriptableObject
    {
        [Header("프로필")]
        public string playerUID = "local-debug";
        public string playerName = "Player";

        [Header("성장")]
        [Range(1, 100)] public int level = 1;
        public int currentXP;
        public int totalSP;
        public int usedSP;

        [Header("재화")]
        public int gold;
        public int manaCrystal;
        public int bossSoul;
        public int respecScrollCount;

        [Header("기타")]
        public int resetCount;
        public int adContinueUsedToday;
        public string lastLoginDate = "2026-05-12";
        public int totalPlayTimeSec;

        public int AvailableSP => Mathf.Max(0, totalSP - usedSP);

        /// <summary>초기 상태로 리셋. 디버그/테스트용.</summary>
        public void ResetToDefault()
        {
            level = 1;
            currentXP = 0;
            totalSP = 0;
            usedSP = 0;
            gold = 0;
            manaCrystal = 0;
            bossSoul = 0;
            respecScrollCount = 0;
            resetCount = 0;
            adContinueUsedToday = 0;
            totalPlayTimeSec = 0;
        }
    }
}
