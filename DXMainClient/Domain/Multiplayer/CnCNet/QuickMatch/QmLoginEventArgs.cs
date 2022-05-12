using System;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QmLoginEventArgs : EventArgs
    {
        public QmLoginEventStatusEnum Status { get; set; }

        public QmLoginEventArgs(QmLoginEventStatusEnum status)
        {
            Status = status;
        }
    }
}
