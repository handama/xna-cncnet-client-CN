using System;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QmStatusMessageEventArgs : EventArgs
    {
        public string Message { get; set; }

        public QmStatusMessageEventArgs(string message)
        {
            Message = message;
        }
    }
}
