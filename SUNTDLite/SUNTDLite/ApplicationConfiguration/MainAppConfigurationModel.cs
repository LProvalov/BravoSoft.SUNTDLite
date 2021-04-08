using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SUNTDLite.ApplicationConfiguration
{
    [Serializable]
    public class MainAppConfigurationModel
    {
        public string DocumentSearchFolder { get; set; }

        public KBDAPIServiceConfig ServiceConfig { get; set; }
        public SearchServiceConfig SearchServiceConfig { get; set; }

        public string TempDirectoryPath { get; set; }
        public ApplicationsForEditingDocuments ApplicationsForEditingDocuments { get; set; }

        public List<DefaultAttribute> DefaultAttributes { get; set; }
    }

    public abstract class SerivceConfig
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public string ServiceUrl { get; set; }
        public string EndpointUrl { get; set; }
    }

    [Serializable]
    public class KBDAPIServiceConfig : SerivceConfig
    {
        
    }

    [Serializable]
    public class SearchServiceConfig : SerivceConfig
    {
        public string BParserUrl { get; set; }
    }

    [Serializable]
    public class ApplicationsForEditingDocuments
    {
        public bool UseDefault { get; set; }
        public string DocumentEditorPath { get; set; }
        public string PDFDocumentEditorPath { get; set; }
    }

    [Serializable]
    public class DefaultAttribute
    {
        public long AttributeNumber { get; set; }
        public string AttributeName { get; set; }
    }
}
