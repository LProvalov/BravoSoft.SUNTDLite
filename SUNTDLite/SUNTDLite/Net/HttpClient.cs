using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace SUNTDLite.Net
{
    public class HttpClient : ApplicationLogger
    {
        private readonly string _server_ip = string.Empty;
        public HttpClient(string server_ip) : base("[HttpClient]")
        {
            _server_ip = server_ip;
        }

        private HttpWebRequest CreateHttpRequest(Uri uri, string method)
        {
            LOG_TRACE($"CreateHttpRequest {uri.AbsoluteUri}, {method}");
            HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
            request.Method = method;
            request.Accept = "text/html,application/xhtml+xml,application/xml";
            request.UserAgent = "SUNTDLite";
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            request.Host = _server_ip;
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 2;
            request.AutomaticDecompression = DecompressionMethods.GZip;
            return request;
        }

        public string HttpGetRequest(string path)
        {
            LOG_TRACE($"HttpGetRequest: {path}");
            Uri uri;
            try
            {
                string urlStr = $"http://{_server_ip}{path}";
                LOG_TRACE(urlStr);
                uri = new Uri(urlStr);
                var request = CreateHttpRequest(uri, WebRequestMethods.Http.Get);
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    Stream receiveStream = response.GetResponseStream();
                    StreamReader readStream = null;

                    if (string.IsNullOrEmpty(response.CharacterSet))
                    {
                        readStream = new StreamReader(receiveStream);
                    }
                    else
                    {
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                    }
                    string responseStr = readStream.ReadToEnd();
                    readStream.Close();
                    return responseStr;
                }
            }
            catch (UriFormatException ufEx)
            {
                LOG_ERROR($"Неверный формат URI: {path}", ufEx);
                throw new Exception($"Произошла ошибка при формировании Http запроса. Неверный формат URL ({path}). Проверте *.cfg файл приложения.", ufEx);
            }
            catch (WebException wEx)
            {
                LOG_ERROR($"Ошибка при выполнении http запроса. HttpGetRequest {path}", wEx);
                HttpWebResponse response = (HttpWebResponse)wEx.Response;
                if (response != null && response.StatusCode == HttpStatusCode.Forbidden)
                {
                    LOG_ERROR("Forbidden! Wrong user/password pair.", wEx);
                    throw new Exception("Ошибка выполнения http запроса. Доступ запрещен, неверное сочетание user/password.", wEx);
                }
                response.Close();
                throw new Exception($"Ошибка выполнения http запроса. HttpGetRequest {path}", wEx);
            }
            catch (Exception ex)
            {
                LOG_ERROR($"Неизвестная ошибка при выполнении http запроса. HttpGetRequest {path}", ex);
                throw new Exception($"Неизвестная ошибка при выполнении http запроса. HttpGetRequest {path}", ex);
            }
        }
    }
}
