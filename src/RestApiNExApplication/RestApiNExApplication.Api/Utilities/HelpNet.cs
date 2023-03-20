using System;
using System.Net;

namespace RestApiNExApplication.Api.Utilities
{
    public class HelpNet
    {
        public static string GetHostIP()
        {
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST
            // Get the IP
            string ipHostAddress = Dns.GetHostByName(hostName).AddressList[0].ToString();
            return ipHostAddress;
        }


    }
}
