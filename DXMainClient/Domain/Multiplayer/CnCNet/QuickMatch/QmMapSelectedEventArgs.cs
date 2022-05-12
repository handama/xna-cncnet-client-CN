using System;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QmMapSelectedEventArgs : EventArgs
    {
        public QmMap Map { get; set; }

        public QmMapSelectedEventArgs(QmMap map)
        {
            Map = map;
        }
    }
}
