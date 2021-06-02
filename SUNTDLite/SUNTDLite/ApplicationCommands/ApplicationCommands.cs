using SUNTDLite.ApplicationConfiguration;
using SUNTDLite.DocsSoapService;
using SUNTDLite.Services;
using SUNTDLite.Services.KBDAPIServiceModels;
using SUNTDLite.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SUNTDLite
{
    public class ApplicationCommands : ApplicationLogger
    {
        private readonly Configuration<MainAppConfigurationModel> configuration;
        private readonly KBDAPIService kbdService;
        private readonly FilesWorkingService filesWorkingService;
        public ApplicationCommands() : base("[ApplicatonCommands]")
        {
            configuration = SingledToolbox.Get<Configuration<MainAppConfigurationModel>>();
            kbdService = SingledToolbox.Get<KBDAPIService>();
            filesWorkingService = SingledToolbox.Get<FilesWorkingService>();
        }

        private List<SearchDocsService.DocListItem> SearchDocument(string searchString)
        {
            var documentListInfo = kbdService.FuzzySearch(searchString, 18);
            if (documentListInfo != null)
            {
                List<SearchDocsService.DocListItem> docListItems = new List<SearchDocsService.DocListItem>();
                for (int i = 0; i < documentListInfo.parts; i++)
                {
                    var listPart = kbdService.GetSearchList(documentListInfo.id, null, i, null, null);
                    if (listPart != null)
                    {
                        docListItems.AddRange(listPart);
                    }
                }
                if (docListItems.Count > 0)
                {
                    return docListItems;
                }
            }
            return null;
        }
        internal IEnumerable<Tuple<string, long>> SearchDocumentsCommand(string searchString)
        {
            LOG_TRACE($"SearchDocumentsCommand: {searchString}");
            if (string.IsNullOrEmpty(searchString))
            {
                throw new ArgumentException("Search string can't be null or empty for search documents command.");
            }
            try
            {
                var firstDocListItems = SearchDocument(searchString);
                if (firstDocListItems != null && firstDocListItems.Count > 0)
                {
                    return firstDocListItems.Select(di => { return new Tuple<string, long>(di.name, di.nd); }).ToList();
                }
                return null;
            }
            catch (Exception ex)
            {
                LOG_ERROR("KbdService throw exception.", ex);
                throw new Exception("КБД Сервис (SearchDocumentsCommand method) сгенерировал исключение.", ex);
            }
        }

        internal string DeleteDocumentCommand(long uid)
        {
            LOG_TRACE($"DeleteDocumentCommand: {uid}");
            if (uid > 0)
            {
                try
                {
                    string requestGuid = kbdService.RemoveDocument(uid);
                    Thread.Sleep(5 * 1000);
                    string status = kbdService.CheckRequestStatus(requestGuid);
                    LOG_TRACE(status);
                    return status;
                }
                catch(Exception ex)
                {
                    LOG_ERROR("Ошибка при удалении документа.", ex);
                    throw new Exception("Ошибка при удалении документа.", ex);
                }   
            }
            return null;
        }

        internal DocumentDesc SaveDocumentCommand(List<KBDAPIService.CreateCardAttribute> createCardAttributes, FileInfo documentFileInfo)
        {
            if (documentFileInfo != null)
            {
                try
                {
                    using (FileStream stream = documentFileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        stream.Close();
                    }
                }
                catch(IOException ioEx)
                {
                    throw new Exception("Невозможно открыть файл для отправки на сервер.", ioEx);
                }
            }
            string guidRequest = kbdService.CreateCard(createCardAttributes);
            var documentDesc = kbdService.GetDocumentDesc(guidRequest);
            if (documentDesc != null && documentFileInfo != null)
            {
                switch(documentFileInfo.Extension.ToLower())
                {
                    case ".doc":
                    case ".docx":
                    case ".rtf":
                    case ".pdf":
                        kbdService.SetText((int)documentDesc.oid, null, documentFileInfo);
                        break;
                    default:
                        kbdService.AddAttachments((int)documentDesc.oid, null, documentFileInfo);
                        break;
                }
            }
            return documentDesc;
        }

        internal string MakeRefsOnDocumentCommand(FileInfo documentFileInfo)
        {
            filesWorkingService.DeleteFileFromTempDirIfExist(documentFileInfo);
            var copyedDocumentFileInfo = filesWorkingService.CopyFileToTempDir(documentFileInfo);
            if (copyedDocumentFileInfo != null)
            {
                filesWorkingService.OpenFileForEdit(copyedDocumentFileInfo);
                return copyedDocumentFileInfo.FullName;
            }
            return null;
        }

        internal List<DocumentType> GetAttributesCommand()
        {
            var cardAttributes = kbdService?.GetCardAttributes();
            if (cardAttributes != null)
            {
                var defaultAttributes = configuration.GetPropertyValue<List<DefaultAttribute>>("DefaultAttributes");

                List<DocumentType> documentArguments = new List<DocumentType>();
                foreach (var cardAttribute in cardAttributes)
                {
                    var defaultAttribute = defaultAttributes.FirstOrDefault(da => da.AttributeNumber == cardAttribute.AttributeNumber);
                    if (defaultAttribute != null)
                    {
                        switch (cardAttribute.AttributeType)
                        {
                            case AttributeType.String:
                                {
                                    documentArguments.Add(new DocumentString(
                                        string.IsNullOrEmpty(defaultAttribute.AttributeName) ? cardAttribute.Title : defaultAttribute.AttributeName,
                                        cardAttribute.AttributeNumber)
                                    { Value = string.Empty });
                                }
                                break;
                            case AttributeType.Data:
                                {
                                    documentArguments.Add(new DocumentDate(
                                        string.IsNullOrEmpty(defaultAttribute.AttributeName) ? cardAttribute.Title : defaultAttribute.AttributeName,
                                        cardAttribute.AttributeNumber));
                                }
                                break;
                            case AttributeType.Classificator:
                                {
                                    var classificatorDesc = kbdService.GetClassificatorDesc(cardAttribute);
                                    if (classificatorDesc != null)
                                    {
                                        documentArguments.Add(new DocumentClassificator(
                                            string.IsNullOrEmpty(defaultAttribute.AttributeName) ? cardAttribute.Title : defaultAttribute.AttributeName,
                                            cardAttribute.AttributeNumber, 0, classificatorDesc));
                                    }
                                }
                                break;
                            case AttributeType.ClassificatorExp:
                                {
                                    var classificatorExpDesc = kbdService.GetClassificatorExpDescRecursive(cardAttribute);
                                    if (classificatorExpDesc != null)
                                    {
                                        documentArguments.Add(new DocumentClassificatorExp(
                                            string.IsNullOrEmpty(defaultAttribute.AttributeName) ? cardAttribute.Title : defaultAttribute.AttributeName,
                                            cardAttribute.AttributeNumber, 0, classificatorExpDesc));
                                    }
                                }
                                break;
                        }
                    }
                }
                return documentArguments;
            }
            return new List<DocumentType>();
        }
    }
}
