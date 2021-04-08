using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SoapService
{
    public class SoapRequestBuilder
    {
        private Uri _basicUri;
        private string _serviceNamespace;

        public SoapRequestBuilder(string basicUrl, string serviceNamespace)
        {
            _basicUri = new Uri(basicUrl);
            _serviceNamespace = serviceNamespace;
        }
        public HttpWebRequest BuildSoapWebRequest(string methodName)
        {
            HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(_basicUri);
            Req.Headers.Add($"SOAPAction:{_serviceNamespace}/{methodName}");
            Req.ContentType = "text/xml;chrset=\"utf-8\"";
            Req.Accept = "text/xml";
            Req.Credentials = new NetworkCredential("api", "api987");
            Req.Method = "POST";
            return Req;
        }

        public void InvokeGetCardAtributes()
        {
            HttpWebRequest req = BuildSoapWebRequest("GetCardAttributes");

        }
    }
}
