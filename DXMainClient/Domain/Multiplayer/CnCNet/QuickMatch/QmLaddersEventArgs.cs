using System;
using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QmLaddersEventArgs : EventArgs
    {
        public readonly IEnumerable<QmLadder> Ladders;

        public QmLaddersEventArgs(IEnumerable<QmLadder> ladders)
        {
            Ladders = ladders;
        }
    }
}
