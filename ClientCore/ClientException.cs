using System;

namespace ClientCore
{
    public class ClientException : Exception
    {
        public ClientException(string message) : base(message)
        {
        }
    }
}
