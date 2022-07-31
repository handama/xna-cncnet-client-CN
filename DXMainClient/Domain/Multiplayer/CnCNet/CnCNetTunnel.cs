﻿using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A CnCNet tunnel server.
    /// </summary>
    public class CnCNetTunnel
    {
        private const int REQUEST_TIMEOUT = 10000; // In milliseconds

        public CnCNetTunnel() { }

        /// <summary>
        /// Creates and returns a CnCNetTunnel based on a formatted string that
        /// contains the tunnel server's information. 
        /// Returns null if parsing fails.
        /// </summary>
        /// <param name="str">The string that contains the tunnel server's information.</param>
        /// <returns>A </returns>
        public static CnCNetTunnel Parse(string str)
        {
            // For the format, check http://cncnet.org/master-list

            try
            {
                string[] parts = str.Split(';');

                string address = parts[0];
                string[] detailedAddress = address.Split(new char[] { ':' });
                var tunnel = new CnCNetTunnel();
                tunnel.Address = detailedAddress[0];
                tunnel.Port = int.Parse(detailedAddress[1]);
                tunnel.Country = parts[1];
                tunnel.CountryCode = parts[2];
                tunnel.Name = parts[3];
                tunnel.RequiresPassword = parts[4] != "0";
                tunnel.Clients = int.Parse(parts[5]);
                tunnel.MaxClients = int.Parse(parts[6]);
                int status = int.Parse(parts[7]);
                tunnel.Official = status == 2;
                if (!tunnel.Official)
                    tunnel.Recommended = status == 1;
                if (tunnel.Name.IndexOf("CN") != -1) { tunnel.CNServer = status == 3; tunnel.isCNServer = true; }

                CultureInfo cultureInfo = CultureInfo.InvariantCulture;

                tunnel.Latitude = double.Parse(parts[8], cultureInfo);
                tunnel.Longitude = double.Parse(parts[9], cultureInfo);
                tunnel.Version = int.Parse(parts[10]);
                tunnel.Distance = double.Parse(parts[11], cultureInfo);
                tunnel.PingInMs = -1;

                return tunnel;
            }
            catch (Exception ex)
            {
                if (ex is FormatException || ex is OverflowException || ex is IndexOutOfRangeException)
                {
                    Logger.Log("Parsing tunnel information failed: " + ex.Message + Environment.NewLine + "Parsed string: " + str);
                    return null;
                }

                throw ex;
            }
        }

        public string Address { get; private  set; }
        public int Port { get; private set; }
        public string Country { get; private set; }
        public string CountryCode { get; private set; }
        public string Name { get; private set; }
        public bool RequiresPassword { get; private set; }
        public int Clients { get; private set; }
        public int MaxClients { get; private set; }
        public bool Official { get; private set; }
        public bool Recommended { get; private set; }
        public bool CNServer { get; private set; }
        public bool isCNServer { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public int Version { get; private set; }
        public double Distance { get; private set; }
        public int PingInMs { get; set; }

        /// <summary>
        /// Gets a list of player ports to use from a specific tunnel server.
        /// </summary>
        /// <returns>A list of player ports to use.</returns>
        public List<int> GetPlayerPortInfo(int playerCount)
        {
            try
            {
                Logger.Log("Contacting tunnel at " + Address + ":" + Port);

                string addressString = string.Format("http://{0}:{1}/request?clients={2}",
                    Address, Port, playerCount);
                Logger.Log("Downloading from " + addressString);

                using (ExtendedWebClient client = new ExtendedWebClient(REQUEST_TIMEOUT))
                {
                    string data = client.DownloadString(addressString);

                    data = data.Replace("[", String.Empty);
                    data = data.Replace("]", String.Empty);

                    string[] portIDs = data.Split(new char[] { ',' });
                    List<int> playerPorts = new List<int>();

                    foreach (string _port in portIDs)
                    {
                        playerPorts.Add(Convert.ToInt32(_port));
                        Logger.Log("Added port " + _port);
                    }

                    return playerPorts;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to connect to the specified tunnel server. Returned error message: " + ex.Message);
            }

            return new List<int>();
        }
    }
}
