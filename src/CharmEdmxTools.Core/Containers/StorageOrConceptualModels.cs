using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CharmEdmxTools.Core.EdmxXmlModels;
using CharmEdmxTools.Core.ExtensionsMethods;

namespace CharmEdmxTools.Core.Containers
{
    public class StorageOrConceptualModels
    {
        private readonly BaseItem _models;

        public StorageOrConceptualModels(BaseItem models)
        {
            _models = models;
            Schema = models.XNode.Elements().Single(x => x.Name.LocalName == "Schema");
            Alias = Schema.GetAttribute("Alias");
            Namespace = Schema.GetAttribute("Namespace");
            SchemaElements = Schema.Elements().ToBaseItems().ToList();

            EntityContainerElements = SchemaElements.OfType<EntityContainer>().Single().XNode.Elements()
                .ToBaseItems().ToList();
        }

        public string Namespace { get; set; }

        public string Alias { get; set; }

        public List<BaseItem> EntityContainerElements { get; set; }

        public List<BaseItem> SchemaElements { get; set; }

        public XElement Schema { get; set; }
    }
}