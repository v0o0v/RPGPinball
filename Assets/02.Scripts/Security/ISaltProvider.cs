namespace RPGPinball.Security
{
    /// <summary>
    /// AES 키 파생용 솔트 제공자. 빌드 시점/런타임에 따라 다른 구현 주입.
    /// </summary>
    public interface ISaltProvider
    {
        /// <summary>32바이트 솔트.</summary>
        byte[] GetAppSalt();

        /// <summary>롤링 식별자 (예: "v1", "v2"). 솔트 변경 시 마이그레이션 트리거.</summary>
        string GetSaltVersion();
    }
}
