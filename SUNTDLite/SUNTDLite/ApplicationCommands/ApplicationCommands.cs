using SUNTDLite.ApplicationConfiguration;
using SUNTDLite.DocsSoapService;
using SUNTDLite.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

        private List<SearchDocsService.DocListItem> _SearchDocument(string searchString, KBDAPIService.SearchVariants variant)
        {
            var documentListInfo = kbdService.FuzzySearch(searchString, variant);
            if (documentListInfo != null)
            {
                List<SearchDocsService.DocListItem> docListItems = new List<SearchDocsService.DocListItem>();
                for (int i = 0; i < documentListInfo.parts; i++)
                {
                    var listPart = kbdService.GetSearchList(documentListInfo.id, null, 0, null, null);
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
                var firstDocListItems = _SearchDocument(searchString, KBDAPIService.SearchVariants.Default);
                if (firstDocListItems != null && firstDocListItems.Count > 0)
                {
                    return firstDocListItems.Select(di => { return new Tuple<string, long>(di.name, di.nd); }).ToList();
                }
                else
                {
                    var secondDocListItems = _SearchDocument(searchString, KBDAPIService.SearchVariants.SearchByName);
                    if (secondDocListItems != null && secondDocListItems.Count > 0)
                    {
                        return secondDocListItems.Select(di => { return new Tuple<string, long>(di.name, di.nd); }).ToList();
                    }
                }
                return null;
            }
            catch(Exception ex)
            {
                LOG_ERROR("KbdService throw exception.", ex);
                throw new Exception("КБД Сервис (SearchDocumentsCommand method) сгенерировал исключение.", ex);
            }
        }

        internal bool DeleteDocumentCommand(string uid)
        {
            throw new NotImplementedException();
        }

        internal DocumentDesc SaveDocumentCommand(List<KBDAPIService.CreateCardAttribute> createCardAttributes, FileInfo documentFileInfo)
        {
            string guidRequest = kbdService.CreateCard(createCardAttributes);
            var documentDesc = kbdService.GetDocumentDesc(guidRequest);
            if (documentDesc != null)
            {
                kbdService.SetText((int)documentDesc.oid, null, documentFileInfo);
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
    }
}
