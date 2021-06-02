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

            /* BASIC HTTP CONTEXT BINDING */
            BasicHttpContextBinding binding = new BasicHttpContextBinding(BasicHttpSecurityMode.TransportCredentialOnly);
            binding.Name = "KBDAPI_docsSoap";
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            string endpointAddress = $"http://{ kbdServiceConfig.ServiceUrl }/{ kbdServiceConfig.EndpointUrl}";
            LOG_TRACE($"endpointAddress: {endpointAddress}");
            EndpointAddress ea = new EndpointAddress(endpointAddress);
            _client = new KBDAPI_docsSoapClient(binding, ea);

            _client.ClientCredentials.UserName.UserName = "apiuser";
            _client.ClientCredentials.UserName.Password = "apiadm!";


            if (searchServiceConfig == null)
            {
                LOG_ERROR($"SearchServiceConfig is null");
                throw new Exception("Wrong applicaton config file, it should have SearchServiceConfig parameters!");
            }
            BasicHttpContextBinding ssBinding = new BasicHttpContextBinding(BasicHttpSecurityMode.TransportCredentialOnly);
            ssBinding.Name = "apiSoap";
            ssBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            string ssEndpointAddress = $"http://{searchServiceConfig.ServiceUrl}/{searchServiceConfig.EndpointUrl}";
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
            var _logSb = new StringBuilder();
            if (_client != null)
            {
                ArrayOfCardAttribute attributes;

                using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                    attributes = _client.GetCardAttributes();
                }

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
            var _logSb = new StringBuilder();
            if (_client != null && cardAttribute.AttributeType == AttributeType.Classificator)
            {
                ClassificatorDesc classificatorDesc;

                using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                    classificatorDesc = _client.GetClassificatorDesc((int)cardAttribute.AttributeNumber, 0);
                }

                _logSb.Clear();
                _logSb.Append(classificatorDesc.type).Append("/");
                if (classificatorDesc != null && classificatorDesc.values.Any())
                    foreach (var item in classificatorDesc.values)
                    {
                        _logSb.Append(item.oid).Append("/").Append(item.name).Append("\t");
                    }
                LOG_TRACE($"{_logSb.ToString()}");
                return new ClassificatorDescModel(classificatorDesc);
            }
            return null;
        }

        public ClassificatorDescModel GetClassificatorExpDescRecursive(CardAttributeModel cardAttribute)
        {
            LOG_TRACE($"GetClassificatorExpDesc");
            if (_client != null && cardAttribute.AttributeType == AttributeType.ClassificatorExp)
            {
                ClassificatorDesc classificatorDesc = null;
                try
                {
                    using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                    {
                        HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                        httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                            Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                        OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                        classificatorDesc = _client.GetClassificatorDesc((int)cardAttribute.AttributeNumber, 0);
                    }
                }
                catch (InvalidCastException)
                { }

                if (classificatorDesc != null)
                {
                    ClassificatorDescModel ret = new ClassificatorDescModel(classificatorDesc.type);

                    foreach (var item in classificatorDesc.values)
                    {
                        var addedItem = new ClassificatorDescModel.ClassificatorDescModelItem(item.name, item.oid);
                        ret.Values.Add(addedItem);
                        GetClassificatorDescModelItemRecursive((int)cardAttribute.AttributeNumber, item.oid, addedItem.Childrens);
                    }
                    return ret;
                }
            }
            return null;
        }

        private void GetClassificatorDescModelItemRecursive(int attributeNumber, int oid, List<ClassificatorDescModel.ClassificatorDescModelItem> item)
        {
            ClassificatorDesc classificatorDesc = null;
            try
            {
                using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                    classificatorDesc = _client.GetClassificatorDesc(attributeNumber, oid);
                }
            }
            catch (InvalidCastException)
            { }

            if (classificatorDesc != null)
            {
                foreach (var value in classificatorDesc.values)
                {
                    var subItem = new ClassificatorDescModel.ClassificatorDescModelItem(value.name, value.oid);
                    item.Add(subItem);
                    GetClassificatorDescModelItemRecursive(attributeNumber, value.oid, subItem.Childrens);
                }
            }
        }
        public ClassificatorDescModel GetClassificatorExpDesc(CardAttributeModel cardAttribute)
        {
            LOG_TRACE($"GetClassificatorExpDesc");
            if (_client != null && cardAttribute.AttributeType == AttributeType.ClassificatorExp)
            {
                ClassificatorDesc classificatorDesc;

                using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                    classificatorDesc = _client.GetClassificatorDesc((int)cardAttribute.AttributeNumber, 0);
                }

                if (classificatorDesc != null)
                {
                    ClassificatorDescModel ret = new ClassificatorDescModel(classificatorDesc);
                    foreach (var item in classificatorDesc.values)
                    {
                        try
                        {
                            ClassificatorDesc extDesc;

                            using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                            {
                                HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                                httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                                    Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                                extDesc = _client.GetClassificatorDesc((int)cardAttribute.AttributeNumber, item.oid);
                            }

                            if (extDesc != null && extDesc.values.Any())
                            {
                                ret.AddChilds(item.oid, extDesc.values);
                            }
                        }
                        catch (InvalidCastException)
                        { }
                    }
                    return ret;
                }
            }
            return null;
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
                for (int i = 0; i < listofattributes.Count; i++)
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
                    string guid;

                    using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                    {
                        HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                        httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                            Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                        OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                        guid = _client.CreateCard(stringOfAttributes, formatOfAttributes);
                    }

                    _requestGUIDList.Add(guid);
                    sb.Clear();
                    foreach (var item in _requestGUIDList)
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

        public DocListInfo FuzzySearch(string searchString, int searchIndex = -1)
        {
            LOG_TRACE($"FuzzySearch({searchString}, {searchIndex})");
            if (_searchClient != null)
            {
                string bparserStr = _httpClient?.HttpGetRequest($"/{_searchServiceConfig.BParserUrl}?parse=\"{searchString}\"");
                LOG_TRACE($"bparserStr: {bparserStr}");

                using (OperationContextScope scope = new OperationContextScope(_searchClient.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(_searchClient.ClientCredentials.UserName.UserName + ":" + _searchClient.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                    var response = _searchClient.FuzzySearch(searchString, null, searchIndex, null, bparserStr);
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
                    using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                    {
                        HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                        httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                            Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                        OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                        docDesc = _client.GetDocumentDesc(guidRequest);
                    }

                    return docDesc;
                }
                catch (CommunicationException)
                {
                    LOG_TRACE($"Первый вызов не вернул результата, ожидаем 10сек и повторяем.");
                    Thread.Sleep(10 * 1000);
                    try
                    {
                        using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                        {
                            HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                            httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                                Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                            OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                            docDesc = _client.GetDocumentDesc(guidRequest);
                        }

                        return docDesc;
                    }
                    catch (CommunicationException)
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
                switch (file.Extension.ToLower())
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
                        LOG_ERROR($"Попытка сохранить документ в неизвестном формате {file.Extension}. Возможна ошибка при сохранении.");
                        format = file.Extension.TrimStart('.');
                        break;
                }
                var bytes = File.ReadAllBytes(file.FullName);
                string base64str = Convert.ToBase64String(bytes);

                using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                    return _client.SetText(oid, guid, format, base64str);
                }
            }
            return null;
        }

        public string AddAttachments(int oid, string guid, FileInfo file)
        {
            if (_client != null && file != null && file.Exists)
            {
                var bytes = File.ReadAllBytes(file.FullName);
                string base64str = Convert.ToBase64String(bytes);

                using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                    return _client.AddAttachment(oid, guid, file.Name, base64str, 0, 0, 0, 0, 0);
                }
            }
            return null;
        }

        public string CheckRequestStatus(string requestGuid)
        {
            if (_client != null && !string.IsNullOrEmpty(requestGuid))
            {
                var _logSb = new StringBuilder();
                DocsSoapService.ArrayOfstring status; // = _client.CheckRequestStatus(requestGuid);

                using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                    status = _client.CheckRequestStatus(requestGuid);
                }

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

        public string RemoveDocument(long uid)
        {
            if (_client != null && uid > 0)
            {
                using (OperationContextScope scope = new OperationContextScope(_client.InnerChannel))
                {
                    HttpRequestMessageProperty httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers[System.Net.HttpRequestHeader.Authorization] = "Basic " +
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(_client.ClientCredentials.UserName.UserName + ":" + _client.ClientCredentials.UserName.Password));
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;
                    return _client.RemoveDocument((int)uid, null);
                }
            }
            return null;
        }
    }
}
