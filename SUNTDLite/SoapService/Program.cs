using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Web;

using SoapService.KbdapiService;

namespace SoapService
{
    class Program
    {
        static void Main(string[] args)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Name = "KBDAPI_docsSoap";
            binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            binding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;

            EndpointAddress ea = new EndpointAddress("http://89.109.33.138:1210/eda/KBDAPI_docs");
            
            KBDAPI_docsSoapClient client = new KBDAPI_docsSoapClient(binding, ea);
            client.ClientCredentials.UserName.UserName = "api";
            client.ClientCredentials.UserName.Password = "api987";

            try
            {
                client.Open();
                var attributes = client.GetCardAttributes();
                if (attributes != null)
                {
                    foreach(var attr in attributes.item_CardAttribute)
                    {
                        Console.WriteLine($"{attr.title} : {attr.type}");
                    }
                }
                else
                {
                    Console.WriteLine("Attributes is null");
                }
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
            Console.ReadLine();
        }
    }
}
