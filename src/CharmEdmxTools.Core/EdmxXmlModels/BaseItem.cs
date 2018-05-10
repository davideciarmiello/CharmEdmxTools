using System.Collections.Generic;
using System.Xml.Linq;
using CharmEdmxTools.Core.ExtensionsMethods;

namespace CharmEdmxTools.Core.EdmxXmlModels
{
    public class BaseItem
    {
        public override string ToString()
        {
            return XNode.ToString();
        }
        public XElement XNode { get; private set; }
        public BaseItem(XElement node)
        {
            XNode = node;
        }

        private string _name;
        public virtual string Name
        {
            get
            {
                if (_name != null)
                    return _name;
                _name = GetAttribute("Name");
                return _name;
            }
            set { var att = XNode.Attribute("Name"); if (att != null) { att.Value = _name = value; } }
        }

        public IEnumerable<T> Descendants<T>() where T : BaseItem
        {
            return XNode.Descendants().ToBaseItems<T>();
        }

        public string GetAttribute(string key)
        {
            return XNode.GetAttribute(key);
        }

        public bool IsDeleted { get; set; }
    }
}
