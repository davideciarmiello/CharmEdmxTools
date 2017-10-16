using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using CharmEdmxTools.EdmxConfig;
using CharmEdmxTools.EdmxUtils.Models;

namespace CharmEdmxTools.EdmxUtils
{
    public class EdmxManager
    {
        private readonly string _path;
        private XDocument xDoc;
        private EdmxContainer edmx;
        private string xDocLoadStr;
        Action<string> logger;
        CharmEdmxConfiguration config;
        public EdmxManager(string path, Action<string> logger, CharmEdmxConfiguration cfg)
        {
            _path = path;
            xDoc = XDocument.Load(path);
            xDocLoadStr = xDoc.ToString();
            this.logger = logger ?? new Action<string>(s => { });
            config = cfg ?? new CharmEdmxConfiguration();
            edmx = new EdmxContainer(xDoc);
        }

        public bool Salva()
        {
            if (!IsChanged())
                return false;
            xDoc.Save(_path);
            return true;
        }

        public bool IsChanged()
        {
            var currStr = xDoc.ToString();
            return (currStr != xDocLoadStr);
        }

        public void FixPropertiesAttributes()
        {
            var storageModels = edmx.StorageModels;
            var provider = storageModels.XNode.Descendants().Where(it => it.Name.LocalName == "Schema").Select(it => it.Attribute("Provider")).FirstOrDefault();
            if (provider == null || string.IsNullOrEmpty(provider.Value))
                return;

            //Action<Property, Property> fixPropertyAttributes;

            var dynamicProvider = config.EdmMappingConfigurations.FirstOrDefault(it => provider.Value.StartsWith(it.ProviderName));
            if (dynamicProvider == null)
                return;
            //if (dynamicProvider != null)
            //{
            var dt = new Lazy<DataTable>(() => new DataTable());
            //    fixPropertyAttributes = (property, property1) => FixPropertyAttributesDynamic(property, property1, dynamicProvider, dt.Value);
            //}
            //else if (provider.Value.StartsWith("Oracle."))
            //    fixPropertyAttributes = FixPropertyAttributesOracle;
            //else
            //    return;
            var storageModelsEntityType = storageModels.EntityType.ToList();
            var conceptualModels = edmx.ConceptualModels;
            var conceptualModelsEntityType = conceptualModels.EntityType.Where(it => storageModelsEntityType.Select(p => p.Name).Contains(it.NameOriginalOfDb)).ToList();
            foreach (var conceptualEntityType in conceptualModelsEntityType)
            {
                var storageEntityType = storageModelsEntityType.FirstOrDefault(it => it.Name == conceptualEntityType.NameOriginalOfDb);
                var storageProps = storageEntityType.Property.ToList();
                var conceptualProperties = conceptualEntityType.Property.Where(it => storageProps.Select(p => p.Name).Contains(it.NameOriginalOfDb)).ToList();
                foreach (var conceptualProperty in conceptualProperties)
                {
                    var storageProperty = storageProps.FirstOrDefault(it => it.Name == conceptualProperty.NameOriginalOfDb);
                    var oldHtml = conceptualProperty.XNode.ToString();
                    var res = FixPropertyAttributesDynamic(storageProperty, conceptualProperty, dynamicProvider, dt.Value);
                    if (oldHtml != conceptualProperty.XNode.ToString())
                        logger(string.Format(Messages.Current.EseguitoFixPropertiesAttributes, conceptualEntityType.Name, conceptualProperty.Name, string.Join("; ", res)));
                }
            }
        }

        public List<string> StorageTypeNotManaged = new List<string>();

        private List<string> FixPropertyAttributesDynamic(Property storagePropertyItem, Property conceptualPropertyItem, edmMappingConfiguration cfg, DataTable dt)
        {
            var storageProperty = storagePropertyItem.XNode;

            var mappings = cfg.edmMappings.Where(mapping => FixPropertyAttributesDynamicFilterItem(mapping, storageProperty, dt));

            var item = mappings.FirstOrDefault();
            if (item == null)
                return null;
            var conceptualProperty = conceptualPropertyItem.XNode;
            var lstRes = new List<string>();
            var res = XElementSetAttributeValueOrRemove(conceptualProperty, "Nullable", storageProperty.Attribute("Nullable"));
            if (!string.IsNullOrEmpty(res))
                lstRes.Add(res);
            foreach (var conceptualTrasformation in item.ConceptualTrasformations)
            {
                foreach (var attrName in conceptualTrasformation.NameList)
                {
                    if (!string.IsNullOrWhiteSpace(conceptualTrasformation.ValueStorageAttributeName))
                        res = XElementSetAttributeValueOrRemove(conceptualProperty, attrName, storageProperty.Attribute(conceptualTrasformation.ValueStorageAttributeName));
                    else if (conceptualTrasformation.ValueFromStorageAttribute)
                        res = XElementSetAttributeValueOrRemove(conceptualProperty, attrName, storageProperty.Attribute(attrName));
                    else
                        res = XElementSetAttributeValueOrRemove(conceptualProperty, attrName, conceptualTrasformation.Value);
                    if (!string.IsNullOrEmpty(res))
                        lstRes.Add(res);
                }
            }
            return lstRes;
        }

        private string XElementSetAttributeValueOrRemove(XElement conceptualProperty, string attributeName, XAttribute attributeValue)
        {
            return XElementSetAttributeValueOrRemove(conceptualProperty, attributeName, attributeValue == null ? null : attributeValue.Value);
        }
        private string XElementSetAttributeValueOrRemove(XElement conceptualProperty, string attributeName, string attributeValue)
        {
            var currAttr = conceptualProperty.Attribute(attributeName);
            var currAttrValue = currAttr == null ? null : currAttr.Value;
            var res = (currAttrValue ?? "") == (attributeValue ?? "") ? "" : string.Concat("'", attributeName, "': '", currAttrValue, "' -> '", attributeValue, "'");
            if (!string.IsNullOrEmpty(attributeValue))
                conceptualProperty.SetAttributeValue(attributeName, attributeValue);
            else if (currAttr != null)
                conceptualProperty.SetAttributeValue(attributeName, null);
            return res;
        }

        private bool FixPropertyAttributesDynamicFilterItem(edmMapping mapping, XElement storageProperty, DataTable dt)
        {
            if (!string.IsNullOrWhiteSpace(mapping.DbType) && !mapping.DbTypes.Contains(storageProperty.Attribute("Type").Value))
                return false;

            if (!string.IsNullOrWhiteSpace(mapping.MinPrecision) && !(Convert.ToInt32(storageProperty.Attribute("Precision").Value) >= Convert.ToInt32(mapping.MinPrecision)))
                return false;
            if (!string.IsNullOrWhiteSpace(mapping.MaxPrecision) && !(Convert.ToInt32(storageProperty.Attribute("Precision").Value) <= Convert.ToInt32(mapping.MaxPrecision)))
                return false;

            if (!string.IsNullOrWhiteSpace(mapping.MinScale) && !(Convert.ToInt32(storageProperty.Attribute("Scale").Value) >= Convert.ToInt32(mapping.MinScale)))
                return false;
            if (!string.IsNullOrWhiteSpace(mapping.MaxScale) && !(Convert.ToInt32(storageProperty.Attribute("Scale").Value) <= Convert.ToInt32(mapping.MaxScale)))
                return false;

            if (!string.IsNullOrWhiteSpace(mapping.Where))
            {
                if (dt == null)
                    dt = new DataTable();
                dt.Rows.Clear();
                dt.Rows.Add();
                foreach (var xAttribute in storageProperty.Attributes())
                {
                    var isString = !(xAttribute.Name.LocalName == "Precision" || xAttribute.Name.LocalName == "Scale" || xAttribute.Name.LocalName == "MaxLength");
                    if (!dt.Columns.Contains(xAttribute.Name.LocalName))
                        dt.Columns.Add(xAttribute.Name.LocalName, isString ? typeof(string) : typeof(int));
                    var newValue = isString ? xAttribute.Value as object : (string.IsNullOrEmpty(xAttribute.Value) ? 0 : Convert.ToInt32(xAttribute.Value));
                    dt.Rows[0][xAttribute.Name.LocalName] = newValue ?? DBNull.Value;
                }
                var rows = dt.Select(mapping.Where);
                if (rows.Length == 0)
                    return false;
            }

            return true;
        }

        private void FixPropertyAttributesOracle(Property storagePropertyItem, Property conceptualPropertyItem)
        {
            var storageProperty = storagePropertyItem.XNode;
            var conceptualProperty = conceptualPropertyItem.XNode;
            var storageType = storageProperty.Attribute("Type").Value;
            switch (storageType)
            {
                case "guid raw":
                    conceptualProperty.SetAttributeValue("Type", "Guid");
                    conceptualProperty.SetAttributeValue("MaxLength", null);
                    conceptualProperty.SetAttributeValue("FixedLength", null);
                    conceptualProperty.SetAttributeValue("Unicode", null);
                    break;
                case "date":
                    conceptualProperty.SetAttributeValue("Type", "DateTime");
                    conceptualProperty.SetAttributeValue("MaxLength", null);
                    conceptualProperty.SetAttributeValue("FixedLength", null);
                    conceptualProperty.SetAttributeValue("Unicode", null);
                    break;
                case "char":
                case "varchar2":
                    conceptualProperty.SetAttributeValue("Type", "String");
                    conceptualProperty.SetAttributeValue("MaxLength", storageProperty.Attribute("MaxLength").Value);
                    break;
                case "nclob":
                case "clob":
                    conceptualProperty.SetAttributeValue("Type", "String");
                    conceptualProperty.SetAttributeValue("MaxLength", "Max");
                    break;
                case "number":
                    /*
          <add NETType="bool" MinPrecision="1" MaxPrecision="1" DBType="Number" />
          <add NETType="byte" MinPrecision="2" MaxPrecision="3" DBType="Number" />
          <add NETType="int16" MinPrecision="4" MaxPrecision="5" DBType="Number" />
          <add NETType="int32" MinPrecision="6" MaxPrecision="10" DBType="Number" />
          <add NETType="int64" MinPrecision="11" MaxPrecision="19" DBType="Number" />*/
                    var precision = Convert.ToInt32(storageProperty.Attribute("Precision").Value);
                    var scale = Convert.ToInt32(storageProperty.Attribute("Scale").Value);
                    if (precision == 1 && scale == 0)
                    {
                        conceptualProperty.SetAttributeValue("Type", "Boolean");
                        conceptualProperty.SetAttributeValue("Precision", null);
                        conceptualProperty.SetAttributeValue("Scale", null);
                    }
                    else if (precision > 19 || scale > 0)
                    {
                        conceptualProperty.SetAttributeValue("Type", "Decimal");
                        conceptualProperty.SetAttributeValue("Precision", precision.ToString());
                        conceptualProperty.SetAttributeValue("Scale", scale.ToString());
                    }
                    else
                    {
                        string netType = "";
                        if (precision >= 2 && precision <= 3)
                            netType = "Byte";
                        else if (precision >= 4 && precision <= 5)
                            netType = "Int16";
                        else if (precision >= 6 && precision <= 10)
                            netType = "Int32";
                        else if (precision >= 11 && precision <= 19)
                            netType = "Int64";
                        conceptualProperty.SetAttributeValue("Type", netType);
                        conceptualProperty.SetAttributeValue("Precision", null);
                        conceptualProperty.SetAttributeValue("Scale", null);
                    }
                    conceptualProperty.SetAttributeValue("MaxLength", null);
                    conceptualProperty.SetAttributeValue("FixedLength", null);
                    conceptualProperty.SetAttributeValue("Unicode", null);
                    break;
                case "blob":
                    conceptualProperty.SetAttributeValue("Type", "Binary");
                    conceptualProperty.SetAttributeValue("MaxLength", "Max");
                    conceptualProperty.SetAttributeValue("FixedLength", "false");
                    conceptualProperty.SetAttributeValue("Unicode", null);
                    break;
                default:
                    if (!StorageTypeNotManaged.Contains(storageType))
                    {
                        StorageTypeNotManaged.Add(storageType);
                        logger(string.Format(Messages.Current.ErroreFixPropertiesAttributes, storageType));
                    }
                    break;
            }

            var nullable = storageProperty.Attribute("Nullable");
            if (nullable != null && !string.IsNullOrEmpty(nullable.Value))
                conceptualProperty.SetAttributeValue("Nullable", nullable.Value);
            else if (conceptualProperty.Attribute("Nullable") != null)
                conceptualProperty.SetAttributeValue("Nullable", null);
        }


        public void ClearEdmxPreservingKeyFields()
        {
            logger(Messages.Current.AvvioClearEdmxPreservingKeyFields);
            var storageModels = edmx.StorageModels;
            var storageModelsKeys = new ConcurrentDictionary<string, List<string>>();
            foreach (var entityType in storageModels.EntityType)
            {
                var keys = entityType.Descendants<Key>().SelectMany(it => it.Descendants<PropertyRef>()).Select(it => it.Name).ToList();
                storageModelsKeys.AddOrUpdate(entityType.Name, keys, (p1, p2) => keys);
                entityType.Property.Where(it => !keys.Contains(it.Name)).RemoveAll();
            }
            storageModels.Association.RemoveAll();
            storageModels.AssociationSet.RemoveAll();


            var conceptualModels = edmx.ConceptualModels;
            foreach (var entityType in conceptualModels.EntityType)
            {
                var keys = entityType.Descendants<Key>().SelectMany(it => it.Descendants<PropertyRef>()).Select(it => it.Name).ToArray();
                entityType.Property.Where(it => !keys.Contains(it.Name)).RemoveAll();
                entityType.NavigationProperty.RemoveAll();
            }
            conceptualModels.Association.RemoveAll();
            conceptualModels.AssociationSet.RemoveAll();

            var mappings = edmx.Mappings;
            foreach (var entityType in mappings.Descendants<EntitySetMapping>())
            {
                List<string> keys;
                if (!storageModelsKeys.TryGetValue(entityType.Name, out keys))
                    keys = new List<string>();
                entityType.Descendants<ScalarProperty>().Where(it => !keys.Contains(it.ColumnName)).RemoveAll();
            }
        }

        public void ClearAllNavigationProperties()
        {
            var conceptualModels = xDoc.Document.Descendants().ToBaseItems<BaseItem>().FirstOrDefault();
            conceptualModels.
                Descendants<NavigationProperty>().RemoveAll();
            conceptualModels.
                Descendants<Association>().RemoveAll();
            conceptualModels.
                Descendants<AssociationSet>().RemoveAll();
        }


        public void FixTabelleECampiEliminati()
        {
            var storageModels = edmx.StorageModels;
            var storageModelsEntityType = storageModels.EntityType.ToList();
            var conceptualModels = edmx.ConceptualModels;
            var conceptualModelsEntityType = conceptualModels.EntityType.ToList();
            foreach (var entityType in conceptualModelsEntityType)
            {
                var storageEntityType = storageModelsEntityType.Where(it => it.Name == entityType.NameOriginalOfDb).FirstOrDefault();
                if (storageEntityType == null) // è stata eliminata dal db, ma c'è ancora sull'edmx
                {
                    logger(string.Format(Messages.Current.EliminazioneEntityDaConceptualModels, entityType.Name));
                    DeleteTableFromConceptualModels(conceptualModels, entityType);
                }
                else
                {
                    var storageProps = storageEntityType.Property.ToList();
                    var conceptualProps = entityType.Property.ToList();
                    var conceptualPropsToDelete = conceptualProps.Where(it => !storageProps.Select(p => p.Name).Contains(it.NameOriginalOfDb)).ToList();
                    foreach (var prop in conceptualPropsToDelete)
                    {
                        logger(string.Format(Messages.Current.EliminazionePropertyDaConceptualModels, entityType.Name, prop.Name));
                        DeletePropertyFromConceptualModels(conceptualModels, entityType, prop);
                    }
                    var storagePropsToDelete = storageProps.Where(it => !conceptualProps.Select(p => p.NameOriginalOfDb).Contains(it.Name)).ToList();
                    var storagePropsKeys = storageEntityType.Descendants<Key>().SelectMany(it => it.Descendants<PropertyRef>()).ToList();
                    foreach (var prop in storagePropsToDelete)
                    {
                        if (storagePropsKeys.Any(it => it.Name == prop.Name))
                        {
                            logger(string.Format(Messages.Current.ErroreImpossibileEliminarePropertyDaStorage, entityType.Name, prop.Name));
                        }
                        else
                        {
                            logger(string.Format(Messages.Current.EliminazionePropertyDaStorageModels, entityType.Name, prop.Name));
                            new[] { prop }.RemoveAll();
                        }
                    }

                    var entitySetMapping = entityType.MappedEntitySetMapping;
                    if (entitySetMapping != null)
                    {
                        var propNames = storageEntityType.Property.Select(it => it.Name).ToList();
                        var columnNames = entityType.Property.Select(it => it.Name).ToList();
                        var mappingsToRemove = entitySetMapping.Descendants<ScalarProperty>().Where(it => !propNames.Contains(it.Name) && !columnNames.Contains(it.ColumnName)).ToList();
                        mappingsToRemove.ForEach(it => logger(string.Format(Messages.Current.EliminazioneMappingsEntityDaMappings, entityType.Name, it.Name, it.ColumnName)));
                        mappingsToRemove.AsEnumerable().RemoveAll();
                    }
                }
            }

            FixAssociationEliminate(storageModels, conceptualModels);
        }

        private void FixAssociationEliminate(StorageModels storageModels, ConceptualModels conceptualModels)
        {
            var storageModelsAssociation = storageModels.Association.ToList();
            var conceptualModelsAssociation = conceptualModels.Association.ToList();
            var conceptualModelsAssociationSet = conceptualModels.AssociationSet.ToList();
            var conceptualModelsNavigationProperty = conceptualModels.NavigationProperty.ToList();
            foreach (var conceptualAssociation in conceptualModelsAssociation)
            {
                var storageAssociation = storageModelsAssociation.FirstOrDefault(it => it.Name == conceptualAssociation.Name);
                if (storageAssociation != null)
                    continue;
                // è stata eliminata dal db, ma c'è ancora sull'edmx
                logger(string.Format(Messages.Current.EliminazioneAssociationDaConceptualModels, conceptualAssociation.Name, conceptualAssociation.PrincipalRole, conceptualAssociation.PrincipalPropertyRef, conceptualAssociation.DependentRoleTableName, conceptualAssociation.DependentPropertyRef));
                var conceptualAssociationSet = conceptualModelsAssociationSet.Where(it => it.Name == conceptualAssociation.Name).ToList();
                var conceptualNavigationProperty = conceptualModelsNavigationProperty.Where(it => it.Relationship == conceptualAssociationSet[0].Association).ToList();
                conceptualNavigationProperty.RemoveAll();
                conceptualAssociationSet.RemoveAll();
                new[] { conceptualAssociation }.RemoveAll();
            }
        }

        public void FixTabelleNonPresentiInConceptual()
        {
            var storageModels = edmx.StorageModels;
            var storageModelsEntityType = storageModels.EntityType.ToList();
            var conceptualModels = edmx.ConceptualModels;
            var conceptualModelsEntityType = conceptualModels.EntityType.ToList();
            foreach (var storageEntityType in storageModelsEntityType)
            {
                var conceptualEntityType = conceptualModelsEntityType.FirstOrDefault(it => it.NameOriginalOfDb == storageEntityType.Name);
                if (conceptualEntityType != null) // è stata eliminata dal db, ma c'è ancora sull'edmx
                    continue;
                var msg = string.Format("Attenzione: Tabella '{0}' non presente in ConceptualModels. Eliminarla dallo StorageModels?", storageEntityType.Name);
                var res = MessageBox.Show(msg, "Avviso", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res != DialogResult.Yes)
                    continue;
                var type = storageEntityType;
                var associationToRemove = storageModels.Association.Where(it => it.DependentRoleTableName == type.Name || it.PrincipalRole == type.Name).ToArray();
                var associationSetToRemove = storageModels.AssociationSet.Where(it => associationToRemove.Select(p => p.Name).Contains(it.Name)).ToArray();
                associationToRemove.RemoveAll();
                associationSetToRemove.RemoveAll();
                var entitySetToRemove = storageModels.EntitySet.Where(it => it.Name == type.Name).ToArray();
                entitySetToRemove.RemoveAll();
                new[] { type }.RemoveAll();
            }
        }

        public void FixConceptualModelNames()
        {
            if (!config.NamingNavigationProperty.Enabled)
                return;
            var conceptualModels = edmx.ConceptualModels;
            foreach (var entityType in conceptualModels.EntityType)
            {
                var navProps = entityType.NavigationProperty
                    .OrderBy(it => it.NavigationIsOneToOne ? 0 : 1) //processo prima i oneToOne
                    .ToList();
                var allNavPropInEntity = new List<string>();
                foreach (var navProp in navProps)
                {
                    string newName;
                    if (navProp.NavigationIsOneToOne)
                        newName = GetFixedNameForNavigationModel(navProp, navProps, entityType.Name, allNavPropInEntity);
                    else
                        newName = GetFixedNameForNavigationList(navProp, navProps, entityType.Name, allNavPropInEntity);
                    allNavPropInEntity.Add(newName);
                    if (!string.IsNullOrEmpty(newName) && navProp.Name != newName)
                    {
                        logger(string.Format(Messages.Current.RinominoNavigationProperty, entityType.Name, navProp.Name, newName));
                        navProp.Name = newName;
                    }
                }
            }

        }


        public void FixAssociations()
        {
            var storageModels = edmx.StorageModels;
            //var storageModelsEntityType = storageModels.EntityType.ToList();
            var conceptualModels = edmx.ConceptualModels;
            //var conceptualModelsEntityType = conceptualModels.EntityType.ToList();
            //foreach (var entityType in conceptualModelsEntityType)
            //{
            //    var storageEntityType = storageModelsEntityType.FirstOrDefault(it => it.Name == entityType.NameOriginalOfDb);
            //    if (storageEntityType == null) // è stata eliminata dal db, ma c'è ancora sull'edmx
            //    {
            //        //logger(string.Format("Eliminazione dell'entity '{0}' da ConceptualModels (Entity non trovata in StorageModels)", entityType.Name));
            //        //DeleteTableFromConceptualModels(conceptualModels, entityType);
            //        continue;
            //    }

            //}

            var conceptualModelsAssociations = conceptualModels.Association.ToList();
            var storageModelsAssociations = storageModels.Association.ToList();
            foreach (var conceptualModelsAssociation in conceptualModelsAssociations)
            {
                var storageModelsAssociation = storageModelsAssociations.FirstOrDefault(it => it.Name == conceptualModelsAssociation.Name);
                if (storageModelsAssociation == null)
                    continue;
                var conceptualModelsEnds = conceptualModelsAssociation.Descendants<End>().ToList();
                var storageModelsEnds = storageModelsAssociation.Descendants<End>().ToList();
                foreach (var conceptualModelsEnd in conceptualModelsEnds)
                {
                    var storageModelsEnd = storageModelsEnds.FirstOrDefault(it => it.Role == conceptualModelsEnd.Role);
                    if (storageModelsEnd == null)
                        continue;
                    if (storageModelsEnd.Multiplicity != conceptualModelsEnd.Multiplicity)
                    {
                        logger(string.Format(Messages.Current.CambioValoreMultiplicityFk, conceptualModelsAssociation.Name, conceptualModelsEnd.Role, conceptualModelsEnd.Multiplicity, storageModelsEnd.Multiplicity));
                        conceptualModelsEnd.Multiplicity = storageModelsEnd.Multiplicity;
                    }
                }
            }
        }

        public bool AssociationContainsDifferentTypes()
        {
            var storageModels = edmx.StorageModels;
            var storageModelsAssociations = storageModels.Association.ToList();
            Func<Property, string> fnGetKey = p => string.Concat(p.GetAttribute("Type"), "_", p.GetAttribute("Precision"), "_", p.GetAttribute("Scale"), "_");
            Func<Property, string> fnGetMapping = p =>
            {
                var lst = (from key in new[] { "Type", "Precision", "Scale" } where !string.IsNullOrEmpty(p.GetAttribute(key)) select string.Concat(key, "=", p.GetAttribute(key))).ToList();
                return string.Join(", ", lst);
            };
            var haveErrors = false;
            foreach (var storageModelsAssociation in storageModelsAssociations)
            {

                var association = storageModelsAssociation;

                //if (association.DependentRole == "TA_FAT_TESTATE_FAT_ATT1")
                //{
                //    association.fix();
                //}
                //var xx = storageModels.EntityType.ToList();
                var principalField = storageModels.EntityType.Where(it => it.Name == association.PrincipalRole).SelectMany(it => it.Property.Where(p => p.Name == association.PrincipalPropertyRef)).FirstOrDefault();

                var dependentField = storageModels.EntityType.Where(it => it.Name == association.DependentRoleTableName).SelectMany(it => it.Property.Where(p => p.Name == association.DependentPropertyRef)).FirstOrDefault();
                if (dependentField == null)
                {
                    continue;
                }
                if (fnGetKey(principalField) == fnGetKey(dependentField))
                    continue;
                haveErrors = true;
                var msg = string.Format("WARNING FK: {0}.{1} ({2}) non corrisponde a {3}.{4} ({5})",
                    association.PrincipalRole, association.PrincipalPropertyRef, fnGetMapping(principalField),
                    association.DependentRole, association.DependentPropertyRef, fnGetMapping(dependentField));
                logger(msg);
            }
            return haveErrors;
        }


        //private static class ConfigKeys
        //{
        //    public const string NamingNavigationPropertyFix = "NamingNavigationPropertyFix";
        //    public const string NamingNavigationPropertyModelOne = "NamingNavigationPropertyModelOne";
        //    public const string NamingNavigationPropertyModelMany = "NamingNavigationPropertyModelMany";
        //    public const string NamingNavigationPropertyListOne = "NamingNavigationPropertyListOne";
        //    public const string NamingNavigationPropertyListMany = "NamingNavigationPropertyListMany";
        //}

        //public static Dictionary<string, string> GetDefaultConfigKeys()
        //{
        //    return new Dictionary<string, string>()
        //    {
        //        {ConfigKeys.NamingNavigationPropertyFix,"0"},
        //        {ConfigKeys.NamingNavigationPropertyModelOne ,"PrincipalRole"},
        //        {ConfigKeys.NamingNavigationPropertyModelMany ,"PrincipalRole_DependentPropertyRef"},
        //        {ConfigKeys.NamingNavigationPropertyListOne ,"LIST_DependentRole"},
        //        {ConfigKeys.NamingNavigationPropertyListMany ,"LIST_DependentRole_DependentPropertyRef"},
        //    };
        //}

        public virtual string GetFixedNameForNavigationModel(NavigationProperty navProp, List<NavigationProperty> allNavPropsInTable, string entityName, List<string> allNavPropInEntity)
        {
            var numeroMappings = allNavPropsInTable.Where(a => a.NavigationIsOneToOne && a.Association.PrincipalRole == navProp.Association.PrincipalRole).Count();
            string newName = "";
            if (numeroMappings > 1)
                newName = GetFixedNameForNavigationFromConfig(navProp.Association, config.NamingNavigationProperty.ModelMany.Pattern);
            else
            {
                if (navProp.Association.PrincipalRole == entityName)
                    newName = GetFixedNameForNavigationFromConfig(navProp.Association, config.NamingNavigationProperty.ModelOneParent.Pattern);
                else
                    newName = GetFixedNameForNavigationFromConfig(navProp.Association, config.NamingNavigationProperty.ModelOne.Pattern);
            }
            if (allNavPropInEntity.Contains(newName))
            {
                var newNameOrig = newName;
                var cnt = 1;
                while (allNavPropInEntity.Contains(newName))
                {
                    newName = string.Concat(newNameOrig, "_", cnt);
                    cnt++;
                }
            }
            return newName;
        }
        public virtual string GetFixedNameForNavigationList(NavigationProperty navProp, List<NavigationProperty> allNavPropsInTable, string entityName, List<string> allNavPropInEntity)
        {
            //var cont = (navProp.Name.Contains("TA_FAT_TESTATE_FAT_ATT"));
            //var cont1 = cont;
            var numeroDiListe = allNavPropsInTable.Where(a => !a.NavigationIsOneToOne && a.Association.DependentRoleTableName == navProp.Association.DependentRoleTableName).Count();
            string newName = "";
            if (numeroDiListe > 1)
                newName = GetFixedNameForNavigationFromConfig(navProp.Association, config.NamingNavigationProperty.ListMany.Pattern);
            else
            {
                if (navProp.Association.DependentRoleTableName == entityName)
                    newName = GetFixedNameForNavigationFromConfig(navProp.Association, config.NamingNavigationProperty.ListOneChilds.Pattern);
                else
                    newName = GetFixedNameForNavigationFromConfig(navProp.Association, config.NamingNavigationProperty.ListOne.Pattern);
            }
            if (allNavPropInEntity.Contains(newName))
            {
                var newNameOrig = newName;
                var cnt = 1;
                while (allNavPropInEntity.Contains(newName))
                {
                    newName = string.Concat(newNameOrig, "_", cnt);
                    cnt++;
                }
            }
            return newName;

        }

        private string GetFixedNameForNavigationFromConfig(Association assoc, string config)
        {
            return config
                .Replace("DependentRole", assoc.DependentRoleTableName)
                .Replace("DependentPropertyRef", assoc.DependentPropertyRef)
                .Replace("PrincipalRole", assoc.PrincipalRole)
                .Replace("PrincipalPropertyRef", assoc.PrincipalPropertyRef);
        }

        private void DeleteTableFromConceptualModels(ConceptualModels conceptualModels, EntityType entityToDelete)
        {
            var navPropsToDelete = conceptualModels.NavigationProperty.Where(it => it.FromRole == entityToDelete.NameOriginalOfDb || it.ToRole == entityToDelete.NameOriginalOfDb)
                .ToList().AsEnumerable();
            var container = conceptualModels.EntityContainer;
            container.EntitySet.Where(it => it.Name == entityToDelete.NameOriginalOfDb).RemoveAll();
            container.AssociationSet.Where(it => navPropsToDelete.Select(prop => prop.Association.Name).Contains(it.Name)).RemoveAll();
            navPropsToDelete.Select(it => it.Association).RemoveAll();
            navPropsToDelete.RemoveAll();
            new BaseItem[] { entityToDelete.MappedEntitySetMapping, entityToDelete }.RemoveAll();
        }
        private void DeletePropertyFromConceptualModels(ConceptualModels conceptualModels, EntityType entity, Property field)
        {
            var navPropsToDelete = conceptualModels.NavigationProperty.Where(it => (it.Association.PrincipalRole == entity.NameOriginalOfDb && it.Association.PrincipalPropertyRef == field.NameOriginalOfDb)
                || (it.Association.DependentRole == entity.NameOriginalOfDb && it.Association.DependentPropertyRef == field.NameOriginalOfDb)).ToList().AsEnumerable();
            var container = conceptualModels.EntityContainer;
            container.AssociationSet.Where(it => navPropsToDelete.Select(prop => prop.Association.Name).Contains(it.Name)).RemoveAll();
            navPropsToDelete.Select(it => it.Association).RemoveAll();
            navPropsToDelete.RemoveAll();
            entity.Descendants<PropertyRef>().Where(it => it.Name == field.Name).RemoveAll();
            new BaseItem[] { field.MappedScalarProperty, field }.RemoveAll();
        }

        public void FieldsManualOperations()
        {
            if (config.ManualOperations == null || config.ManualOperations.Count == 0)
                return;
            var storageModels = edmx.StorageModels;
            var storageModelsEntityType = storageModels.EntityType.ToList();
            //var conceptualModels = edmx.ConceptualModels;
            //var conceptualModelsEntityType = conceptualModels.EntityType.ToList();
            //foreach (var entityType in conceptualModelsEntityType)
            //{
            //    var storageEntityType = storageModelsEntityType.Where(it => it.Name == entityType.NameOriginalOfDb).FirstOrDefault();
            //    if (storageEntityType == null) // è stata eliminata dal db, ma c'è ancora sull'edmx
            //    {
            //        logger(string.Format(Messages.Current.EliminazioneEntityDaConceptualModels, entityType.Name));
            //        DeleteTableFromConceptualModels(conceptualModels, entityType);
            //    }
            //    else
            //    {
            //        var storageProps = storageEntityType.Property.ToList();
            //        var conceptualProps = entityType.Property.ToList();
            //        var conceptualPropsToDelete = conceptualProps.Where(it => !storageProps.Select(p => p.Name).Contains(it.NameOriginalOfDb)).ToList();
            //        foreach (var prop in conceptualPropsToDelete)
            //        {
            //            logger(string.Format(Messages.Current.EliminazionePropertyDaConceptualModels, entityType.Name, prop.Name));
            //            DeletePropertyFromConceptualModels(conceptualModels, entityType, prop);
            //        }
            foreach (var operation in config.ManualOperations)
            {
                var storageEntityType = storageModelsEntityType.FirstOrDefault(it => it.NameOriginalOfDb.Equals(operation.TableName, StringComparison.OrdinalIgnoreCase));
                if (storageEntityType == null)
                    continue;
                var storageProps = storageEntityType.Property.Where(x => x.NameOriginalOfDb.Equals(operation.FieldName, StringComparison.OrdinalIgnoreCase)).ToList();
                var op = operation.Type;
                if (op == ManualOperationType.RemoveField)
                {
                    storageProps.RemoveAll();
                }
            }

        }
    }
}
