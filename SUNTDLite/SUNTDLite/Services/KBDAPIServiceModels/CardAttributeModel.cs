using SUNTDLite.DocsSoapService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SUNTDLite.Services.KBDAPIServiceModels
{
    public enum AttributeType
    {
        Data,
        String,
        Classificator,
        ClassificatorExp,
        Unknown
    }

    public class CardAttributeModel
    {
        public CardAttributeModel(AttributeType type, string value, long attrnum)
        {
            AttributeType = type;
            Title = value;
            AttributeNumber = attrnum;
        }

        public CardAttributeModel(CardAttribute serviceAttribute)
        {
            AttributeType = GetType(serviceAttribute.type);
            Title = serviceAttribute.title;
            AttributeNumber = serviceAttribute.attrnum;
        }

        private AttributeType GetType(string typeDescription)
        {
            switch(typeDescription)
            {
                case "строка":
                    {
                        return AttributeType.String;
                    }
                case "дата":
                    {
                        return AttributeType.Data;
                    }
                case "множественный линейный классификатор":
                    {
                        return AttributeType.Classificator;
                    }
                case "множественный иерархический классификатор":
                    {
                        return AttributeType.ClassificatorExp;
                    }
                default:
                    {
                        return AttributeType.Unknown;
                    }
            }
        }
        public AttributeType AttributeType { get; private set; }
        public string Title { get; private set; }

        public long AttributeNumber { get; private set; }
    }
}
