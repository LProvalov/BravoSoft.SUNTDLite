using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SUNTDLite
{
    public abstract class ApplicationLogger
    {
        private readonly string _logTag;
        private NLog.Logger _mainLogger = NLog.LogManager.GetLogger("MainLogger");

        public ApplicationLogger(string logTag)
        {
            _logTag = logTag;
        }

        protected virtual void LOG_ERROR(string message, Exception ex = null)
        {
            if (_mainLogger != null)
            {
                _mainLogger.Error($"{_logTag} {message}");
                if (ex != null)
                {
                    _mainLogger.Error($"{_logTag} {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _mainLogger.Error($"{_logTag} {ex.InnerException.Message}");
                    }
                }    
            }    
        }

        protected virtual void LOG_TRACE(string message)
        {
            if (_mainLogger != null)
            {
                _mainLogger.Trace($"{_logTag} {message}");
            }
        }

        protected virtual void LOG_DEBUG(string message)
        {
            if (_mainLogger != null)
            {
                _mainLogger.Debug($"{_logTag} {message}");
            }
        }

    }
}
