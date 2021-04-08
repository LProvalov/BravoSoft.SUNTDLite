using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.IO;
using System.Xml.Serialization;
using SUNTDLite.DocsSoapService;
using SUNTDLite.SearchDocsService;
using SUNTDLite.ApplicationConfiguration;
using SUNTDLite.Services.KBDAPIServiceModels;
using System.Xml;
using System.Text;
using System.Threading;
using SUNTDLite.Net;
using System.ServiceModel.Channels;

namespace SUNTDLite.Services
{
    public class KBDAPIService : ApplicationLogger
    {
        private StringBuilder _logSb = new StringBuilder();
        private KBDAPI_docsSoapClient _client;
        private apiSoapClient _searchClient;
        private HttpClient _httpClient;
        private SearchServiceConfig _searchServiceConfig;

        public KBDAPIService(KBDAPIServiceConfig kbdServiceConfig, SearchServiceConfig searchServiceConfig) : base("[KBDAPIService]")
        {
            if (kbdServiceConfig == null || searchServiceConfig == null)
            {
                throw new Exception("Wrong application config file, it should have ServiceConfig parameters!");
            }
            _searchServiceConfig = searchServiceConfig;
            //BasicHttpBinding binding = new BasicHttpBinding();
            //binding.Name = "KBDAPI_docsSoap";
            //binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            //binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            //binding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;            

            /* BASIC HTTP CONTEXT BINDING */
            BasicHttpContextBinding binding = new BasicHttpContextBinding(BasicHttpSecurityMode.TransportCredentialOnly);
            binding.Name = "KBDAPI_docsSoap";
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            string endpointAddress = $"http://{ kbdServiceConfig.ServiceUrl }/{ kbdServiceConfig.EndpointUrl}";
            LOG_TRACE($"endpointAddress: {endpointAddress}");
            EndpointAddress ea = new EndpointAddress(endpointAddress);
            _client = new KBDAPI_docsSoapClient(binding, ea);

            //_client = new KBDAPI_docsSoapClient();
            _client.ClientCredentials.UserName.UserName = kbdServiceConfig.Username;
            _client.ClientCredentials.UserName.Password = kbdServiceConfig.Password;
            
            if (searchServiceConfig == null)
            {
                LOG_ERROR($"SearchServiceConfig is null");
                throw new Exception("Wrong applicaton config file, it should have SearchServiceConfig parameters!");
            }
            //BasicHttpBinding ssBinding = new BasicHttpBinding();
            //ssBinding.Security.Mode = BasicHttpSecurityMode.Transport;
            //ssBinding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;

            BasicHttpContextBinding ssBinding = new BasicHttpContextBinding(BasicHttpSecurityMode.TransportCredentialOnly);
            //WSHttpBinding ssBinding = new WSHttpBinding(SecurityMode.TransportWithMessageCredential);
            ssBinding.Name = "apiSoap";
            ssBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            string ssEndpointAddress = $"http://{searchServiceConfig.ServiceUrl}/{searchServiceConfig.EndpointUrl}";
            //LOG_TRACE($"searchServiceEndpointAddress: {ssEndpointAddress}");
            EndpointAddress ssEa = new EndpointAddress(ssEndpointAddress);
            _searchClient = new apiSoapClient(ssBinding, ssEa);
            _searchClient.ClientCredentials.UserName.UserName = searchServiceConfig.Username;
            _searchClient.ClientCredentials.UserName.Password = searchServiceConfig.Password;
            _httpClient = new HttpClient(searchServiceConfig.ServiceUrl);
        }

        public void OpenClients()
        {
            LOG_TRACE($"OpenClients, client:{_client != null}, searchClient:{_searchClient != null}");
            if (_client != null)
            {
                _client.Open();
            }
            if (_searchClient != null)
            {
                _searchClient.Open();
            }
            LOG_TRACE($"Clients are opened");
        }

        public void CloseClients()
        {
            LOG_TRACE($"CloseClients");
            if (_client != null)
            {
                _client.Close();
            }
            if (_searchClient != null)
            {
                _searchClient.Close();
            }
            LOG_TRACE($"Clients are closed");
        }

        public IEnumerable<CardAttributeModel> GetCardAttributes()
        {
            LOG_TRACE($"GetCardAttributes");
            if (_client != null)
            {
                var attributes = _client.GetCardAttributes();
                if (attributes != null)
                {
                    _logSb.Clear();
                    foreach (var item in attributes)
                    {
                        _logSb.Append(item.attrnum).Append("/").Append(item.title).Append("/").Append(item.type).Append("\t");
                    }
                    LOG_TRACE($"{_logSb.ToString()}");

                    return attributes.Select(a => new CardAttributeModel(a)).ToList();
                }
            }
            return null;
        }

        public ClassificatorDescModel GetClassificatorDesc(CardAttributeModel cardAttribute)
        {
            LOG_TRACE($"GetClassificatorDesc");
            if (_client != null && cardAttribute.AttributeType == AttributeType.Classificator)
            {
                var classificatorDesc = _client.GetClassificatorDesc((int)cardAttribute.AttributeNumber, 0);
                _logSb.Clear();
                _logSb.Append(classificatorDesc.type).Append("/");
                if (classificatorDesc != null && classificatorDesc.values.Any())
                foreach(var item in classificatorDesc.values)
                {
                    _logSb.Append(item.oid).Append("/").Append(item.name).Append("\t");
                }
                LOG_TRACE($"{_logSb.ToString()}");
                return new ClassificatorDescModel(classificatorDesc);
            }
            return null;
        }

        public ClassificatorDescModel GetClassificatorExpDesc(CardAttributeModel cardAttribute)
        {
            LOG_TRACE($"GetClassificatorExpDesc");
            if (_client != null && cardAttribute.AttributeType == AttributeType.ClassificatorExp)
            {
                var classificatorDesc = _client.GetClassificatorDesc((int)cardAttribute.AttributeNumber, 0);
                if (classificatorDesc != null)
                {
                    ClassificatorDescModel ret = new ClassificatorDescModel(classificatorDesc);
                    foreach (var item in classificatorDesc.values)
                    {
                        try
                        {
                            var extDesc = _client.GetClassificatorDesc((int)cardAttribute.AttributeNumber, item.oid);
                            if (extDesc != null && extDesc.values.Any())
                            {
                                ret.AddChilds(item.oid, extDesc.values);
                            }
                        }
                        catch(InvalidCastException)
                        { }
                    }
                    return ret;
                }
            }
            return null;
        }

        public CardAttribute[] GetServiceAttributes()
        {
            throw new NotImplementedException();
        }

        [Serializable]
        public class CreateCardAttribute
        {
            
            public CreateCardAttribute()
            {
                attrnum = -1;
                value = null;
            }
            public CreateCardAttribute(long attrnum, string value)
            {
                this.attrnum = attrnum;
                this.value = value;
            }
            public long attrnum { get; set; }
            public string value { get; set; }
        }

        private List<string> _requestGUIDList = new List<string>();
        public string CreateCard(List<CreateCardAttribute> listofattributes)
        {
            LOG_TRACE($"CreateCard, client:{_client != null}");
            if (_client != null)
            {
                string stringOfAttributes = string.Empty;
                string formatOfAttributes = "json";

                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                for(int i = 0; i < listofattributes.Count; i++)
                {
                    sb.Append($"{{\"attrnum\":{listofattributes[i].attrnum},\"value\":\"{listofattributes[i].value}\"}}");
                    if (i < listofattributes.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append("]");
                stringOfAttributes = sb.ToString();
                LOG_TRACE($"String of attributes: {stringOfAttributes}");
                if (!string.IsNullOrEmpty(stringOfAttributes))
                {
                    var guid = _client.CreateCard(stringOfAttributes, formatOfAttributes);
                    _requestGUIDList.Add(guid);
                    sb.Clear();
                    foreach(var item in _requestGUIDList)
                    {
                        sb.Append(item).Append(",\t");
                    }
                    LOG_TRACE($"RequestGUIDList: {sb.ToString()}");
                    Thread.Sleep(10 * 1000);
                    LOG_TRACE($"wait 10 seconds");
                    return guid;
                }
            }
            return null;
        }

        public enum SearchVariants
        {
            SearchByName,
            Archs,
            Default
        }

        public DocListInfo FuzzySearch(string searchString, SearchVariants variants, int searchIndex = -1)
        {
            LOG_TRACE($"FuzzySearch({searchString}, {Enum.GetName(typeof(SearchVariants), variants)}, {searchIndex})");
            if (_searchClient != null)
            {
                string variantString = null;
                switch (variants)
                {
                    case SearchVariants.SearchByName:
                        variantString = "searchbynames";
                        break;
                    case SearchVariants.Archs:
                        variantString = "archs";
                        break;
                    default:
                        variantString = null;
                        break;
                }
                string bparserStr = _httpClient?.HttpGetRequest($"/{_searchServiceConfig.BParserUrl}?parse=\"{searchString}\"");
                LOG_TRACE($"bparserStr: {bparserStr}");

                using (OperationContextScope scope = new OperationContextScope(_searchClient.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(_searchClient.ClientCredentials.UserName.UserName + ":" + _searchClient.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                    var response = _searchClient.FuzzySearch(searchString, null, searchIndex, variantString, bparserStr);
                    return response;
                }
            }
            return null;
        }

        public DocListItem[] GetSearchList(string id, object order, int part, string searchName, string kdocfield)
        {
            LOG_TRACE($"GetSearchList(id:{id}, order:{order}, part:{part}, searchName:{searchName}, kdocfield:{kdocfield})");
            if (_searchClient != null)
            {
                ArrayOfDocListItem docListItems;
                using (OperationContextScope scope = new OperationContextScope(_searchClient.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(_searchClient.ClientCredentials.UserName.UserName + ":" + _searchClient.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                    docListItems = _searchClient.GetSearchList(id, order, part, searchName, kdocfield);
                }                
                return docListItems.ToArray();
            }
            return null;
        }

        public DocumentDesc GetDocumentDesc(string guidRequest)
        {

            LOG_TRACE($"GetDocumentDesc({guidRequest})");
            if (_client != null && !string.IsNullOrEmpty(guidRequest))
            {
                DocumentDesc docDesc;
                try
                {
                    docDesc = _client.GetDocumentDesc(guidRequest);
                }
                catch(CommunicationException cEx)
                {
                    LOG_TRACE($"Первый вызов не вернул результата, ожидаем 10сек и повторяем.");
                    Thread.Sleep(10 * 1000);
                    try
                    {
                        docDesc = _client.GetDocumentDesc(guidRequest);
                    } 
                    catch(CommunicationException)
                    {
                        LOG_ERROR($"Запрос документа {guidRequest} не вернул результата в течении 20 секунд.");
                        return null;
                    }
                }
            }
            return null;
        }

        public string SetText(int oid, string guid, FileInfo file)
        {
            LOG_TRACE($"SetText({oid}, {guid}, {file?.FullName})");
            if (_client != null && file != null && file.Exists)
            {
                string format;
                switch (file.Extension)
                {
                    case ".pdf":
                        format = "pdf";
                        break;
                    case ".doc":
                        format = "doc";
                        break;
                    case ".docx":
                        format = "docx";
                        break;
                    case ".rtf":
                        format = "rtf";
                        break;
                    default:
                        throw new ArgumentException($"Возможно сохранять документы только с расширениями .pdf/.doc/.docx/.rtf, попытка сохранения документа *.{file.Extension}");
                }
                var bytes = File.ReadAllBytes(file.FullName);
                string base64str = Convert.ToBase64String(bytes);
                return _client.SetText(oid, guid, format, base64str);
            }
            return null;
        }

        public string CheckRequestStatus(string requestGuid)
        {
            if (_client != null && !string.IsNullOrEmpty(requestGuid))
            {
                var status = _client.CheckRequestStatus(requestGuid);
                _logSb.Clear();
                if (status != null)
                {
                    foreach (var str in status)
                    {
                        _logSb.Append(str).Append("\t");
                    }
                    return _logSb.ToString();
                }
            }
            return null;
        }
    }
}
