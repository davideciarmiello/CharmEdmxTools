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
            {
                if (item != null && item.XNode.Parent != null)
                {
                    item.XNode.Remove();
                    item.IsDeleted = true;
                }
            }
        }


        private static readonly ConcurrentDictionary<string, BaseItem> ToBaseItemsIsOfTypeCache = new ConcurrentDictionary<string, BaseItem>();
        public static bool ToBaseItemsIsOfType<T>(XElement node)
        {
            var res = ToBaseItemsIsOfTypeCache.GetOrAdd(node.Name.LocalName, s => node.ToBaseItem());
            return res is T;
        }

        public static IEnumerable<T> ToBaseItems<T>(this IEnumerable<XElement> lst) where T : BaseItem
        {
            return lst.Where(ToBaseItemsIsOfType<T>).Select(ToBaseItem).OfType<T>();
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

        private static void AddIfNotExists(this List<edmMappingConfiguration> lst, edmMappingConfiguration newItem)
        {
            if (lst.All(it => it.ProviderName != newItem.ProviderName))
                lst.Add(newItem);
        }

        public static bool FillDefaultConfiguration(this CharmEdmxConfiguration cfg)
        {
            int maxVersion = -1;
            var versionLower = new Func<int, bool>(version =>
            {
                if (version > maxVersion)
                    maxVersion = version;
                return cfg.Version < version;
            });

            if (versionLower(1) || cfg.NamingNavigationProperty == null)
            {
                cfg.NamingNavigationProperty = cfg.NamingNavigationProperty ?? new NamingNavigationProperty();
                cfg.NamingNavigationProperty.Enabled = false;
                cfg.NamingNavigationProperty.ModelOne = new NamingNavigationPropertyItem() { Pattern = "PrincipalRole" };
                cfg.NamingNavigationProperty.ModelMany = new NamingNavigationPropertyItem() { Pattern = "PrincipalRole_DependentPropertyRef" };
                cfg.NamingNavigationProperty.ListOne = new NamingNavigationPropertyItem() { Pattern = "ListDependentRole" };
                cfg.NamingNavigationProperty.ListMany = new NamingNavigationPropertyItem() { Pattern = "ListDependentRole_DependentPropertyRef" };
            }

            if (versionLower(2))
            {
                cfg.EdmMappingConfigurations.AddIfNotExists(GetEdmMappingConfigurationOracle());
            }

            if (versionLower(3))
            {
                cfg.NamingNavigationProperty.ModelOneParent = new NamingNavigationPropertyItem() { Pattern = "PrincipalRole_PARENT" };
                cfg.NamingNavigationProperty.ListOneChilds = new NamingNavigationPropertyItem() { Pattern = "DependentRole_CHILDREN" };
            }

            //if (versionLower(4))
            //{
            //    cfg.EdmMappingConfigurations.AddIfNotExists(GetEdmMappingConfigurationSql());
            //}

            if (cfg.Version >= maxVersion)
                return false;
            cfg.Version = maxVersion;
            return true;
        }

        private static edmMappingConfiguration GetEdmMappingConfigurationSql()
        {
            throw new NotImplementedException();
        }

        private class AttributeTrasformationHelper
        {
            public AttributeTrasformation New(string name, string value)
            {
                return new AttributeTrasformation(name, value);
            }
        }

        private static edmMappingConfiguration GetEdmMappingConfigurationOracle()
        {
            var at = new AttributeTrasformationHelper();
            var res = new edmMappingConfiguration() { ProviderName = "Oracle.ManagedDataAccess.Client" };
            res.edmMappings.Add(new edmMapping("guid raw", at.New("Type", "Guid"), at.New("MaxLength;FixedLength;Unicode;", null)));
            res.edmMappings.Add(new edmMapping("date", at.New("Type", "DateTime"), at.New("MaxLength;FixedLength;Unicode;", null)));
            res.edmMappings.Add(new edmMapping("char;varchar2", at.New("Type", "String"), new AttributeTrasformation("MaxLength", null) { ValueStorageAttributeName = "MaxLength" }));
            res.edmMappings.Add(new edmMapping("nclob;clob", at.New("Type", "String"), at.New("MaxLength", "Max")));
            res.edmMappings.Add(new edmMapping("blob", at.New("Type", "Binary"), at.New("MaxLength", "Max"), at.New("FixedLength", "false"), at.New("Unicode", null)));

            res.edmMappings.Add(new edmMapping("number", at.New("Type", "Boolean"), at.New("Precision;Scale;MaxLength;FixedLength;Unicode;", null))
            {
                MinPrecision = "1",
                MaxPrecision = "1",
                MaxScale = "0"
            });
            res.edmMappings.Add(new edmMapping("number", at.New("Type", "Byte"), at.New("Precision;Scale;MaxLength;FixedLength;Unicode;", null))
            {
                MinPrecision = "2",
                MaxPrecision = "3",
                MaxScale = "0"
            });
            res.edmMappings.Add(new edmMapping("number", at.New("Type", "Int16"), at.New("Precision;Scale;MaxLength;FixedLength;Unicode;", null))
            {
                MinPrecision = "4",
                MaxPrecision = "5",
                MaxScale = "0"
            });
            res.edmMappings.Add(new edmMapping("number", at.New("Type", "Int32"), at.New("Precision;Scale;MaxLength;FixedLength;Unicode;", null))
            {
                MinPrecision = "6",
                MaxPrecision = "10",
                MaxScale = "0"
            });
            res.edmMappings.Add(new edmMapping("number", at.New("Type", "Int64"), at.New("Precision;Scale;MaxLength;FixedLength;Unicode;", null))
            {
                MinPrecision = "11",
                MaxPrecision = "19",
                MaxScale = "0"
            });
            res.edmMappings.Add(new edmMapping("number", at.New("Type", "Decimal"), at.New("MaxLength;FixedLength;Unicode;", null), new AttributeTrasformation("Precision;Scale;", null) { ValueFromStorageAttribute = true }));

            return res;
        }
    }

}
