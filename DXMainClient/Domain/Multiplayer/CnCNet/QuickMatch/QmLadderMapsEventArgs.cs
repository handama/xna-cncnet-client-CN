using System;
using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QmLadderMapsEventArgs : EventArgs
    {
        public IEnumerable<QmLadderMap> Maps { get; set; }

        public QmLadderMapsEventArgs(IEnumerable<QmLadderMap> maps)
        {
            Maps = maps;
        }
    }
}
