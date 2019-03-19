using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CharmEdmxTools.Core.Containers;
using CharmEdmxTools.Core.EdmxConfig;
using CharmEdmxTools.Core.EdmxXmlModels;

namespace CharmEdmxTools.Core.ExtensionsMethods
{
    public static class ItemExtensions
    {
        public static string RemoveNs(this string str, StorageOrConceptualModels owner)
        {
            if (str.StartsWith(owner.Namespace) && str[owner.Namespace.Length] == '.')
                return str.Substring(owner.Namespace.Length + 1);
            if (str.StartsWith(owner.Alias) && str[owner.Alias.Length] == '.')
                return str.Substring(owner.Alias.Length + 1);
            throw new ArgumentException();
        }

        public static IEnumerable<T> NotDeleted<T>(this IEnumerable<T> source) where T : BaseItem
        {
            return source.Where(x => x.IsDeleted == false);
        }

        public static string GetAttribute(this XElement xNode, string key)
        {
            var name = xNode.Attribute(key);
            return name == null ? null : name.Value;
        }


        public static TSource Add<TSource>(this TSource source, HashSet<BaseItem> hashSet) where TSource : BaseItem
        {
            hashSet.Add(source);
            return source;
        }

        public static TSource GetOrAddNew<TSource>(this ConcurrentDictionary<string, TSource> source,
            string keySelector) where TSource : new()
        {
            var res = source.GetOrAdd(keySelector, s => new TSource());
            return res;
        }
        public static TSource Get<TSource>(this ConcurrentDictionary<string, TSource> source,
            string keySelector)
        {
            var res = source.GetOrNull(keySelector);
            if (res == null)
                throw new Exception(keySelector + " non trovato");
            return res;
        }
        //public static TSource GetMultiNs<TSource>(this ConcurrentDictionary<string, TSource> source,
        //    string keySelector, params string[] namespaces)
        //{
        //    var res = source.GetOrNullMultiNs(keySelector, namespaces);
        //    if (res == null)
        //        throw new Exception(keySelector + " non trovato");
        //    return res;
        //}

        //public static TSource GetOrNullMultiNs<TSource>(this ConcurrentDictionary<string, TSource> source, string keySelector, params string[] namespaces)
        //{
        //    TSource res;
        //    if (source.TryGetValue(keySelector, out res))
        //        return res;
        //    if (keySelector.Contains("."))
        //    {
        //        keySelector = keySelector.Split(new[] { '.' }, StringSplitOptions.None).Last();
        //        if (source.TryGetValue(keySelector, out res))
        //            return res;
        //    }
        //    foreach (var ns in namespaces)
        //    {
        //        var keyWithNs = ns + "." + keySelector;
        //        if (source.TryGetValue(keyWithNs, out res))
        //            return res;
        //    }
        //    return default(TSource);
        //}
        public static TSource GetOrNull<TSource, TKey>(this ConcurrentDictionary<TKey, TSource> source, TKey keySelector)
        {
            TSource res;
            if (source.TryGetValue(keySelector, out res))
                return res;
            return default(TSource);
        }
        public static IEnumerable<TSource> GetItemsByKeyOrAll<TSource>(this ConcurrentDictionary<string, TSource> source, string keySelector)
        {
            if (string.IsNullOrWhiteSpace(keySelector) || keySelector == "*")
                return source.Values;
            TSource res;
            if (source.TryGetValue(keySelector, out res))
                return Enumerable.Repeat(res, 1);
            return Enumerable.Empty<TSource>();
        }
        public static TSource GetOrNull<TSource, TKey>(this Dictionary<TKey, TSource> source, TKey keySelector)
        {
            TSource res;
            if (source.TryGetValue(keySelector, out res))
                return res;
            return default(TSource);
        }

        public static ConcurrentDictionary<TKey, TSource> ToConcurrentDictionary<TSource, TKey>(
            this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return source.ToConcurrentDictionary(keySelector, source1 => source1);
        }

        public static ConcurrentDictionary<TKey, TElement> ToConcurrentDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return new ConcurrentDictionary<TKey, TElement>(source.ToDictionary(keySelector, elementSelector));
        }

        public static ConcurrentDictionary<TKey, TSource> ToConcurrentDictionaryDistinctOrRemove<TSource, TKey>(
            this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) where TSource : BaseItem
        {
            var res = new ConcurrentDictionary<TKey, TSource>();
            foreach (var it in source)
            {
                var key = keySelector(it);
                if (res.TryAdd(key, it) == false)
                    it.Remove();
            }
            return res;
        }


        public static bool EqualsInvariant(this string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        public static void RemoveAll(this IEnumerable<BaseItem> items)
        {
            var lst = items.ToList();
            foreach (var item in lst)
            {
                Remove(item);
            }
        }

        public static void Remove(this BaseItem item)
        {
            if (item != null && item.XNode.Parent != null)
            {
                item.XNode.Remove();
                item.IsDeleted = true;
            }
        }


        private static readonly ConcurrentDictionary<string, BaseItem> ToBaseItemsIsOfTypeCache = new ConcurrentDictionary<string, BaseItem>();
        public static bool ToBaseItemsIsOfType<T>(this XElement node)
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
        private static readonly ConcurrentDictionary<XElement, BaseItem> ToBaseItemCache = new ConcurrentDictionary<XElement, BaseItem>();
        public static readonly ConcurrentDictionary<XDocument, ConcurrentDictionary<XElement, BaseItem>> ToBaseItemDocumentCache = new ConcurrentDictionary<XDocument, ConcurrentDictionary<XElement, BaseItem>>();
        public static BaseItem ToBaseItem(this XElement nodeElement)
        {
            var cache = ToBaseItemCache;
            if (nodeElement.Document != null)
                cache = ToBaseItemDocumentCache.GetOrAdd(nodeElement.Document,
                    document => new ConcurrentDictionary<XElement, BaseItem>());
            return cache.GetOrAdd(nodeElement, node =>
            {
                switch (node.Name.LocalName)
                {
                    //case "StorageModels": return new StorageModels(node);
                    //case "ConceptualModels": return new ConceptualModels(node);
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
                cfg.NamingNavigationProperty.Enabled = true;
                cfg.NamingNavigationProperty.ModelOne = new NamingNavigationPropertyItem() { Pattern = "PrincipalRole" };
                cfg.NamingNavigationProperty.ModelMany = new NamingNavigationPropertyItem() { Pattern = "PrincipalRole_DependentPropertyRef" };
                cfg.NamingNavigationProperty.ListOne = new NamingNavigationPropertyItem() { Pattern = "DependentRole" };
                cfg.NamingNavigationProperty.ListMany = new NamingNavigationPropertyItem() { Pattern = "DependentRole_DependentPropertyRef" };
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

            if (versionLower(4))
            {
                cfg.EdmMappingConfigurations.AddIfNotExists(GetEdmMappingConfigurationSql());
            }

            if (versionLower(5))
            {
                if (cfg.ManualOperations == null)
                    cfg.ManualOperations = new List<ManualOperation>();
                cfg.ManualOperations.Add(new ManualOperation() { TableName = "TABLE_TEST", FieldName = "Field1", Type = ManualOperationType.RemoveField });
                cfg.ManualOperations.Add(new ManualOperation() { TableName = "TABLE_TEST", FieldName = "Field1", Type = ManualOperationType.SetFieldAttribute, AttributeName = "Nullable", AttributeValue = "false" });
                cfg.ManualOperations.Add(new ManualOperation() { Type = ManualOperationType.RemoveAssociation, AssociationName = "FK_TEST" });
            }

            if (versionLower(6))
            {
                cfg.AutoFixOnSave = false;
            }

            if (versionLower(7))
            {
                if (cfg.ManualOperations == null)
                    cfg.ManualOperations = new List<ManualOperation>();
                cfg.ManualOperations.Add(new ManualOperation() { TableName = "", FieldName = "RowVersion", Type = ManualOperationType.SetConceptualFieldAttribute, AttributeName = "ConcurrencyMode", AttributeValue = "Fixed" });
            }

            if (versionLower(8))
            {
                var sql = GetEdmMappingConfigurationSql();
                cfg.EdmMappingConfigurations.Where(x => x.ProviderName == "System.Data.EntityClient").ToList().ForEach(x => x.ProviderName = sql.ProviderName);
                cfg.EdmMappingConfigurations.AddIfNotExists(sql);
            }

            if (cfg.Version >= maxVersion)
                return false;
            cfg.Version = maxVersion;
            return true;
        }

        private static edmMappingConfiguration GetEdmMappingConfigurationSql()
        {
            var at = new AttributeTrasformationHelper();
            var res = new edmMappingConfiguration() { ProviderName = "System.Data.SqlClient" };
            res.edmMappings.Add(new edmMapping("guid raw", at.New("Type", "Guid"), at.New("MaxLength;FixedLength;Unicode;", "")));
            res.edmMappings.Add(new edmMapping("date", at.New("Type", "DateTime"), at.New("MaxLength;FixedLength;Unicode;", "")));
            res.edmMappings.Add(new edmMapping("char;nchar;varchar;nvarchar", at.New("Type", "String"), new AttributeTrasformation("MaxLength", null) { ValueStorageAttributeName = "MaxLength" }));
            res.edmMappings.Add(new edmMapping("text;ntext", at.New("Type", "String"), at.New("MaxLength", "Max")));
            res.edmMappings.Add(new edmMapping("varbinary", at.New("Type", "Binary"), at.New("MaxLength", "Max"), at.New("FixedLength", "false"), at.New("Unicode", "")));
            res.edmMappings.Add(new edmMapping("bit", at.New("Type", "Boolean"), at.New("Precision;Scale;MaxLength;FixedLength;Unicode;", "")));
            res.edmMappings.Add(new edmMapping("tinyint", at.New("Type", "Byte"), at.New("Precision;Scale;MaxLength;FixedLength;Unicode;", "")));
            res.edmMappings.Add(new edmMapping("smallint", at.New("Type", "Int16"), at.New("Precision;Scale;MaxLength;FixedLength;Unicode;", "")));
            res.edmMappings.Add(new edmMapping("int", at.New("Type", "Int32"), at.New("Precision;Scale;MaxLength;FixedLength;Unicode;", "")));
            res.edmMappings.Add(new edmMapping("bigint", at.New("Type", "Int64"), at.New("Precision;Scale;MaxLength;FixedLength;Unicode;", "")));
            res.edmMappings.Add(new edmMapping("decimal;numeric;money", at.New("Type", "Decimal"), at.New("MaxLength;FixedLength;Unicode;", ""), new AttributeTrasformation("Precision;Scale;", null) { ValueFromStorageAttribute = true }));
            return res;
            //var cfg = CharmEdmxConfiguration.Load(@"C:\Davide\UserProfile\Desktop\SGMSolution.sln.CharmEdmxTools");
            //string ss = null;
            //foreach (var cf in cfg.EdmMappingConfigurations[0].edmMappings)
            //{
            //    var trasforms = cf.ConceptualTrasformations.Select(it => string.Format("at.New(\"{0}\", {1})", it.Name, it.Value == null ? "null" : String.Concat("\"", it.Value, "\""))).ToList();
            //    var xx = string.Format("res.edmMappings.Add(new edmMapping(\"{0}\", {1}));", cf.DbType, string.Join(", ", trasforms));
            //    ss += (xx) + Environment.NewLine;
            //}
            //throw new Exception();
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
