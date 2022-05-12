using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QmData
    {
        public IEnumerable<QmLadder> Ladders { get; set; }
        
        public IEnumerable<QmUserAccount> UserAccounts { get; set; }
    }
}
