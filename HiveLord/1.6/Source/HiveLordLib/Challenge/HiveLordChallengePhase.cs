namespace HiveLordLib
{
    //标识委托据点在进入、清理、交战、重试和完成之间的持久阶段。
    public enum HiveLordChallengePhase
    {
        PendingEntry,
        ClearingSpores,
        Fighting,
        WaitingForRetry,
        Completed
    }
}
