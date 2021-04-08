using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.ComponentModel;

namespace SUNTDLite.ApplicationConfiguration
{
    public class Configuration<T> where T : class
    {
        private T _configurationModel;
        private XmlSerializer _xmlSerializer;
        private string _configurationFilePath;
        private bool _isLoaded;

        public Configuration(string configurationFilePath)
        {
            if (string.IsNullOrEmpty(configurationFilePath))
            {
                throw new ArgumentException("Configuration file path can't be null or empty");
            }

            if (!File.Exists(configurationFilePath))
            {
                throw new ArgumentException("Configuration file should exist");
            }

            _configurationFilePath = configurationFilePath;
            _xmlSerializer = new XmlSerializer(typeof(T));
            _isLoaded = false;
        }

        public void LoadConfiguration()
        {
            DeserializeConfigModel();
            _isLoaded = true;
        }

        public void SaveConfiguration()
        {
            SerializeConfigModel();
        }

        private bool DeserializeConfigModel()
        {
            if (!File.Exists(_configurationFilePath))
            {
                return false;
            }

            StreamReader streamReader = null;
            try
            {
                using (streamReader = new StreamReader(_configurationFilePath))
                {
                    _configurationModel = _xmlSerializer.Deserialize(streamReader) as T;
                }
                streamReader = null;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (streamReader != null)
                {
                    streamReader.Close();
                }
            }
            return true;
        }

        private bool SerializeConfigModel()
        {
            if (!File.Exists(_configurationFilePath))
            {
                return false;
            }

            StreamWriter streamWriter = null;
            try
            {
                using(streamWriter = new StreamWriter(_configurationFilePath, false))
                {
                    _xmlSerializer.Serialize(streamWriter, _configurationModel);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                if (streamWriter != null)
                {
                    streamWriter.Close();
                }
            }
            return true;
        }

        private PropertyDescriptor GetPropertyFromConfigurationModel(string propertyName)
        {
            var modelProperties = TypeDescriptor.GetProperties(_configurationModel);
            if (modelProperties.Count <= 0)
            {
                return null;
            }
            return modelProperties[propertyName];
        }

        public V GetPropertyValue<V>(string propertyName) where V : class
        {
            if (!_isLoaded)
            {
                return null;
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("Property name can't be null or empty");
            }

            var property = GetPropertyFromConfigurationModel(propertyName);
            if (property == null)
            {
                return null;
            }

            return property.GetValue(_configurationModel) as V;
        }

        public void SetPropertyValue(string propertyName, object value)
        {
            if (!_isLoaded)
            {
                return;
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("Property name can't be null or empty");
            }

            var property = GetPropertyFromConfigurationModel(propertyName);
            if (property == null)
            {
                return;
            }

            property.SetValue(_configurationModel, value);

            SaveConfiguration();
        }
    }
}
