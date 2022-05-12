using System;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QuickMatchStatusMessage : EventArgs
    {
        public string Message { get; set; }
    }
}
