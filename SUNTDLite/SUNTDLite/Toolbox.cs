using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SUNTDLite
{
    public static class SingledToolbox
    {
        private static Dictionary<Type, Object> _tools = new Dictionary<Type, object>();

        public static void AddTool(Type toolType, Object toolObject)
        {
            if (_tools.ContainsKey(toolType))
            {
                throw new Exception($"Tool with such type ({toolType.Name}) already exists");
            }
            _tools.Add(toolType, toolObject);
        }

        public static T Get<T>() where T : class
        {
            _tools.TryGetValue(typeof(T), out object tool);
            if (tool == null)
            {
                return null;
            }
            return tool as T;
        }
    }
}
