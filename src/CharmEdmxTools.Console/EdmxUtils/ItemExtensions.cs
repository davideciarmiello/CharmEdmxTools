using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CharmEdmxTools.EdmxConfig;
using CharmEdmxTools.EdmxUtils.Models;

namespace CharmEdmxTools.EdmxUtils
{
    public static class ItemExtensions
    {
        public static void RemoveAll(this IEnumerable<BaseItem> items)
        {
            var lst = items.ToList();
            foreach (var item in lst)
                if (item != null && item.XNode.Parent != null)
                    item.XNode.Remove();
        }
        public static IEnumerable<T> ToBaseItems<T>(this IEnumerable<XElement> lst) where T : BaseItem
        {
            return lst.Select(ToBaseItem).OfType<T>();
        }
        public static IEnumerable<BaseItem> ToBaseItems(this IEnumerable<XElement> lst)
        {
            return lst.Select(ToBaseItem);
        }
        private static ConcurrentDictionary<XElement, BaseItem> ToBaseItemCache = new ConcurrentDictionary<XElement, BaseItem>();
        public static BaseItem ToBaseItem(this XElement nodeElement)
        {
            return ToBaseItemCache.GetOrAdd(nodeElement, node =>
            {
                switch (node.Name.LocalName)
                {
                    case "StorageModels": return new StorageModels(node);
                    case "ConceptualModels": return new ConceptualModels(node);
                    case "Mappings": return new Mappings(node);
                    case "EntityContainer": return new EntityContainer(node);
                    case "EntitySet": return new EntitySet(node);
                    case "EntityType": return new EntityType(node);
                    case "AssociationSet": return new AssociationSet(node);
                    case "Key": return new Key(node);
                    case "Property": return new Property(node);
                    case "PropertyRef": return new PropertyRef(node);
                    case "NavigationProperty": return new NavigationProperty(node);
                    case "Association": return new Association(node);
                    case "End": return new End(node);
                    case "EntitySetMapping": return new EntitySetMapping(node);
                    case "EntityTypeMapping": return new EntityTypeMapping(node);
                    case "ScalarProperty": return new ScalarProperty(node);
                    default: return new BaseItem(node);
                }
            });
        }


        public static bool FillDefaultConfiguration(this CharmEdmxConfiguration cfg)
        {
            if (cfg.Version < 1 || cfg.NamingNavigationProperty == null)
            {
                cfg.NamingNavigationProperty = cfg.NamingNavigationProperty ?? new NamingNavigationProperty();
                cfg.NamingNavigationProperty.Enabled = false;
                cfg.NamingNavigationProperty.ModelOne = new NamingNavigationPropertyItem() { Pattern = "PrincipalRole" };
                cfg.NamingNavigationProperty.ModelMany = new NamingNavigationPropertyItem() { Pattern = "PrincipalRole_DependentPropertyRef" };
                cfg.NamingNavigationProperty.ListOne = new NamingNavigationPropertyItem() { Pattern = "ListDependentRole" };
                cfg.NamingNavigationProperty.ListMany = new NamingNavigationPropertyItem() { Pattern = "ListDependentRole_DependentPropertyRef" };
            }

            if (cfg.Version < 2)
            {
                cfg.EdmMappingConfigurations.Add(GetEdmMappingConfigurationOracle());
            }

            const int currentMaxVersion = 2;
            if (cfg.Version == currentMaxVersion)
                return false;
            cfg.Version = currentMaxVersion;
            return true;
        }

        private static edmMappingConfiguration GetEdmMappingConfigurationOracle()
        {
            var res = new edmMappingConfiguration() { ProviderName = "Oracle.ManagedDataAccess.Client" };
            res.edmMappings.Add(new edmMapping("guid raw", new AttributeTrasformation("Type", "Guid"), new AttributeTrasformation("MaxLength;FixedLength;Unicode;", null)));
            res.edmMappings.Add(new edmMapping("date", new AttributeTrasformation("Type", "DateTime"), new AttributeTrasformation("MaxLength;FixedLength;Unicode;", null)));
            res.edmMappings.Add(new edmMapping("char;varchar2", new AttributeTrasformation("Type", "String"), new AttributeTrasformation("MaxLength", null) { ValueStorageAttributeName = "MaxLength" }));
            res.edmMappings.Add(new edmMapping("nclob;clob", new AttributeTrasformation("Type", "String"), new AttributeTrasformation("MaxLength", "Max")));
            res.edmMappings.Add(new edmMapping("blob", new AttributeTrasformation("Type", "Binary"), new AttributeTrasformation("MaxLength", "Max"), new AttributeTrasformation("FixedLength", "false"), new AttributeTrasformation("Unicode", null)));

            res.edmMappings.Add(new edmMapping("number", new AttributeTrasformation("Type", "Boolean"), new AttributeTrasformation("Precision;Scale;MaxLength;FixedLength;Unicode;", null))
            {
                MinPrecision = "1",
                MaxPrecision = "1",
                MaxScale = "0"
            });
            res.edmMappings.Add(new edmMapping("number", new AttributeTrasformation("Type", "Byte"), new AttributeTrasformation("Precision;Scale;MaxLength;FixedLength;Unicode;", null))
            {
                MinPrecision = "2",
                MaxPrecision = "3",
                MaxScale = "0"
            });
            res.edmMappings.Add(new edmMapping("number", new AttributeTrasformation("Type", "Int16"), new AttributeTrasformation("Precision;Scale;MaxLength;FixedLength;Unicode;", null))
            {
                MinPrecision = "4",
                MaxPrecision = "5",
                MaxScale = "0"
            });
            res.edmMappings.Add(new edmMapping("number", new AttributeTrasformation("Type", "Int32"), new AttributeTrasformation("Precision;Scale;MaxLength;FixedLength;Unicode;", null))
            {
                MinPrecision = "6",
                MaxPrecision = "10",
                MaxScale = "0"
            });
            res.edmMappings.Add(new edmMapping("number", new AttributeTrasformation("Type", "Int64"), new AttributeTrasformation("Precision;Scale;MaxLength;FixedLength;Unicode;", null))
            {
                MinPrecision = "11",
                MaxPrecision = "19",
                MaxScale = "0"
            });
            res.edmMappings.Add(new edmMapping("number", new AttributeTrasformation("Type", "Decimal"), new AttributeTrasformation("MaxLength;FixedLength;Unicode;", null), new AttributeTrasformation("Precision;Scale;", null) { ValueFromStorageAttribute = true }));

            return res;
        }
    }

}
