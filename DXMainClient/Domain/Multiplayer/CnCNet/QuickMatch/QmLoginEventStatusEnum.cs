namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public enum QmLoginEventStatusEnum
    {
        Unknown = 0,
        Success = 1,
        Unauthorized = 2,
        FailedRefresh = 3,
        Logout = 4,
        FailedDataFetch = 5
    }
}
