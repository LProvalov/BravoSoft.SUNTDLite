using SUNTDLite.DocsSoapService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SUNTDLite.Services.KBDAPIServiceModels
{
    public class ClassificatorDescModel
    {
        public enum ClassificatorDescModelType
        {
            Linear,
            Multiple,
            Unknown
        }

        public class ClassificatorDescModelItem
        {
            public ClassificatorDescModelItem(string name, long oid)
            {
                Name = name;
                Oid = oid;
            }
            public string Name { get; set; }
            public long Oid { get; set; }
            public List<ClassificatorDescModelItem> Childrens { get; set; } = new List<ClassificatorDescModelItem>();
        }

        public ClassificatorDescModel(string typeStr)
        {
            Type = GetType(typeStr);
        }

        public ClassificatorDescModel(ClassificatorDesc classificatorDesc)
        {
            Type = GetType(classificatorDesc.type);
            foreach(var item in classificatorDesc.values)
            {
                Values.Add(new ClassificatorDescModelItem(item.name, item.oid));
            }
        }

        private ClassificatorDescModelType GetType(string typeStr)
        {
            if(!string.IsNullOrEmpty(typeStr))
            {
                switch(typeStr)
                {
                    case "Линейный классификатор":
                        return ClassificatorDescModelType.Linear;
                    case "Иерархический классификатор":
                        return ClassificatorDescModelType.Multiple;
                }
            }
            return ClassificatorDescModelType.Unknown;
        }

        public ClassificatorDescModelType Type { get; protected set; }
        public List<ClassificatorDescModelItem> Values { get; private set; } = new List<ClassificatorDescModelItem>();

        public void AddChilds(int oid, ArrayOfClassificatorValue values)
        {
            var foundedItem = Values.Find(v => v.Oid == oid);
            if (foundedItem != null && values != null && values.Any())
            {
                if (foundedItem.Childrens == null)
                {
                    foundedItem.Childrens = new List<ClassificatorDescModelItem>();
                }
                else
                {
                    foundedItem.Childrens.Clear();
                }

                foundedItem.Childrens.AddRange(values.Select(v => new ClassificatorDescModelItem(v.name, v.oid)));
            }
        }

        public void AddChild(int oid, string chldName, int chldOid)
        {
            var foundItem = Values.Find(v => v.Oid == oid);
            if (foundItem != null)
            {
                foundItem.Childrens.Add(new ClassificatorDescModelItem(chldName, chldOid));
            }
        }
    }
}
