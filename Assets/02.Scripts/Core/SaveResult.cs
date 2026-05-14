namespace RPGPinball.Core
{
    public enum SaveResult
    {
        Success = 0,
        IOError = 1,
        Throttled = 2  // 인터벌 미달, 큐에 적재됨
    }

    public enum LoadResult
    {
        Success = 0,
        NotFound = 1,
        Tampered = 2,
        Corrupted = 3,
        VersionMismatch = 4,
        IOError = 5
    }

    public enum CloudSyncResult
    {
        Success = 0,
        NotAuthenticated = 1,
        Conflict = 2,
        Timeout = 3,
        IOError = 4
    }
}
