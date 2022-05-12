using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QMLoginRequest : IPostBodyModel
    {
        public string Email { get; set; }
        public string Password { get; set; }

        public Dictionary<string, string> ToDictionary()
            => new Dictionary<string, string>
            {
                { "email", Email },
                { "password", Password }
            };
    }
}
