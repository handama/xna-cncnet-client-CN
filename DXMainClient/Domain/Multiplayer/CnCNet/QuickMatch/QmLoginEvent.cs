using System;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QmLoginEvent : EventArgs
    {
        public QmLoginEventStatusEnum Status { get; set; }
        
        public string Error { get; set; }

        public bool IsSuccess => Status == QmLoginEventStatusEnum.Success;
    }
}
