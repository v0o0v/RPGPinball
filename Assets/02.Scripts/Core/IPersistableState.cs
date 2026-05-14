namespace RPGPinball.Core
{
    /// <summary>
    /// 인게임 런타임 상태를 직렬화 가능한 객체에 부착한다.
    /// Ball / Flipper / Boss / Monster / Gimmick / Projectile 등이 구현.
    /// PauseManager 가 OnApplicationPause(true) 또는 명시적 Snapshot 요청 시 수집.
    /// </summary>
    public interface IPersistableState
    {
        /// <summary>고유 카테고리 — "ball" / "boss" / "monster" / "gimmick" 등. RuntimeStageSerializer 가 분류용.</summary>
        string PersistKey { get; }

        /// <summary>현재 상태를 JsonUtility 직렬화 가능한 plain 객체로 직렬화.</summary>
        object CaptureState();

        /// <summary>JsonUtility.FromJson 결과를 받아 상태를 복원.</summary>
        void RestoreState(object state);
    }
}
