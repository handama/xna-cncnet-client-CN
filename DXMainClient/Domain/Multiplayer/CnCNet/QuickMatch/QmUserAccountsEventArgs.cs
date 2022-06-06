using System;
using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QmUserAccountsEventArgs : EventArgs
    {
        public readonly IEnumerable<QmUserAccount> UserAccounts;

        public QmUserAccountsEventArgs(IEnumerable<QmUserAccount> userAccounts)
        {
            UserAccounts = userAccounts;
        }
    }
}
