using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public interface IPostBodyModel
    {
        Dictionary<string, string> ToDictionary();
    }
}
