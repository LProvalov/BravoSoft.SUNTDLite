using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SUNTDLite.Services
{
    public class FilesWorkingService
    {
        NLog.Logger _logger = NLog.LogManager.GetLogger("MainLogger");
        public readonly string LogTag = "[FilesWorkingService]";
        private DirectoryInfo _tempDirectoryInfo = null;
        private bool _isUseDefaultApps = false;
        private bool _isDocumentEditorInstalled = false;
        private bool _isPDFEditorInstalled = false;

        private FileInfo _pdfDocumentEditorFile = null;
        public string PDFDocumentEditorPath
        {
            set
            {
                _logger.Trace($"{LogTag} PDFDocumentEditorPath changed.");
                _logger.Trace($"{LogTag} Value: {value}");
                if (!string.IsNullOrEmpty(value) && File.Exists(value))
                {
                    _pdfDocumentEditorFile = new FileInfo(value);
                    if (!_pdfDocumentEditorFile.Extension.Equals(".exe"))
                    {
                        _logger.Error($"{LogTag} pdfDocumentEditor extension: {_pdfDocumentEditorFile.Extension}");
                        throw new ArgumentException("PDF editor should be *.exe");
                    }
                    _isPDFEditorInstalled = true;
                }
            }
        }

        private FileInfo _documentEditorFile = null;
        public string DocumentEditorPath
        {
            set
            {
                _logger.Trace($"{LogTag} DocumentEditorPath changed.");
                _logger.Trace($"{LogTag} Value: {value}");
                if (!string.IsNullOrEmpty(value) && File.Exists(value))
                {
                    _documentEditorFile = new FileInfo(value);
                    if (!_documentEditorFile.Extension.Equals(".exe"))
                    {
                        _logger.Error($"{LogTag} DocumentEditor extension: {_documentEditorFile.Extension}");
                        throw new ArgumentException("Document editor should be *.exe");
                    }
                    _isDocumentEditorInstalled = true;
                }
            }
        }

        public FilesWorkingService(string tempDirPath, bool useDefaultAppsForOpen = false)
        {
            _logger.Trace($"{LogTag} FilesWorkingService({tempDirPath}, {useDefaultAppsForOpen})");
            if (string.IsNullOrEmpty(tempDirPath))
            {
                _logger.Error($"{LogTag} Files working service can't work with such directory: {tempDirPath}");
                throw new ArgumentException($"Files working service can't work with such directory: {tempDirPath}");
            }
            _tempDirectoryInfo = new DirectoryInfo(tempDirPath);

            if (Utilities.WindowsHasInstalledApplication("Office"))
            {
                _isDocumentEditorInstalled = true;
            }

            if (Utilities.WindowsHasInstalledApplication("Acrobate"))
            {
                _isPDFEditorInstalled = false;
            }

            _isUseDefaultApps = useDefaultAppsForOpen;
            _logger.Trace($"{LogTag} DocumentEditorInstalled: {_isDocumentEditorInstalled}, PDFEditorInstalled: {_isPDFEditorInstalled}, UseDefaultApps: {_isUseDefaultApps}");

        }
        public void DeleteFileFromTempDirIfExist(FileInfo file)
        {
            _logger.Trace($"{LogTag} DeleteFileFromTempDirIfExist.");
            if (file != null && file.Exists)
            {
                string destFileName = Path.Combine(_tempDirectoryInfo.FullName, file.Name);
                _logger.Trace($"{LogTag} destFileName:{destFileName}");
                if (File.Exists(destFileName))
                {
                    _logger.Trace($"{LogTag} FileExist, delete");
                    File.Delete(destFileName);
                }
            }
        }
        public FileInfo CopyFileToTempDir(FileInfo file)
        {
            _logger.Trace($"{LogTag} CopyFileToTempDir.");
            if (file != null && file.Exists && !file.IsReadOnly && !file.DirectoryName.Equals(_tempDirectoryInfo.FullName))
            {
                string destFileName = Path.Combine(_tempDirectoryInfo.FullName, file.Name);
                _logger.Trace($"{LogTag} DestFileName:{destFileName}");
                return file.CopyTo(destFileName);
            }
            return null;
        }

        public void OpenFileForEdit(FileInfo file)
        {
            _logger.Trace($"{LogTag} OpenFileForEdit: {file?.FullName}");
            if (file != null && file.Exists)
            {
                if (!_isUseDefaultApps)
                {
                    switch (file.Extension.ToLower())
                    {
                        case ".doc":
                        case ".docx":
                            {
                                if (_isDocumentEditorInstalled)
                                {
                                    Process.Start($"{_documentEditorFile.FullName}", $"{file.FullName}");
                                    return;
                                }
                                else
                                {
                                    _logger.Trace($"{LogTag} _isDocumentEditorInstalled: {_isDocumentEditorInstalled}, try to start default app");
                                }
                            }
                            break;
                        case ".pdf":
                            {
                                if (_isPDFEditorInstalled)
                                {
                                    Process.Start($"{_pdfDocumentEditorFile.FullName}", $"{file.FullName}");
                                    return;
                                }
                                else
                                {
                                    _logger.Trace($"{LogTag} _isPDFEditorInstalled: {_isPDFEditorInstalled}, try to start default app");
                                }
                            }
                            break;
                    }
                }

                try
                {
                    System.Diagnostics.Process.Start(file.FullName);
                }
                catch (Exception ex)
                {
                    _logger.Error($"{LogTag}  Can't open file by default app: {ex.Message}");
                    throw ex;
                }
            }
        }
    }
}
