using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using CharmEdmxTools.Core.Containers;
using CharmEdmxTools.Core.CoreGlobalization;
using CharmEdmxTools.Core.EdmxConfig;
using CharmEdmxTools.Core.EdmxXmlModels;
using CharmEdmxTools.Core.ExtensionsMethods;

namespace CharmEdmxTools.Core.Manager
{
    public class EdmxManager
    {
        public EdmxContainer EdmxContainer => _edmx;
        private readonly string _path;
        public readonly XDocument _xDoc;
        private readonly EdmxContainer _edmx;
        private readonly string _xDocLoadStr;
        readonly Action<string> _logger;
        readonly CharmEdmxConfiguration _config;
        public EdmxManager(string path, string xmlContent, Action<string> logger, CharmEdmxConfiguration cfg)
        {
            _path = path;
            _xDoc = string.IsNullOrEmpty(xmlContent) ? XDocument.Load(path) : XDocument.Parse(xmlContent);
            _xDocLoadStr = _xDoc.ToString();
            _logger = logger ?? new Action<string>(s => { });
            _config = cfg ?? new CharmEdmxConfiguration();
            _edmx = new EdmxContainer(_xDoc);
        }

        public bool Salva()
        {
            if (!IsChanged())
                return false;
            _xDoc.Save(_path);
            return true;
        }

        public bool IsChanged()
        {
            var currStr = _xDoc.ToString();
            return (currStr != _xDocLoadStr);
        }

        public void ExecAllFixs()
        {
            FieldsManualOperations();
            var hasFkWithDifferentTypes = AssociationContainsDifferentTypes();
            if (!hasFkWithDifferentTypes)
            {
                FixTabelleECampiEliminati();
                FixAssociationEliminate();
                FixTabelleNonPresentiInConceptual();
                if (_edmx.ItemsRemoved)
                    _edmx.FillItems();
            }
            FixAssociations();
            FixPropertiesAttributes();
            FixConceptualModelNames();
        }


        public void FieldsManualOperations()
        {
            if (_config.ManualOperations == null || _config.ManualOperations.Count == 0)
                return;
            if (_config.ManualOperations.All(x => x.TableName == "TABLE_TEST" || x.AssociationName == "FK_TEST"))
                return;
            var storageModelsEntityType = _edmx.Entities.Where(x => x.Storage != null).ToConcurrentDictionary(x => x.Storage.Name);


            var dictRes = new ConcurrentDictionary<string, List<string>>();
            var removeAssociationOperations = _config.ManualOperations.Where(x => x.Type == ManualOperationType.RemoveAssociation).ToList();
            if (removeAssociationOperations.Any())
            {
                var storageAssociations = _edmx.Associations.Where(x => x.Storage != null).ToConcurrentDictionary(x => x.Storage.Name);
                foreach (var operation in removeAssociationOperations)
                {
                    var association = storageAssociations.GetOrNull(operation.AssociationName);
                    if (association == null)
                        continue;
                    association.Remove(_edmx);
                    dictRes.GetOrAddNew(operation.AssociationName).Add("FK Deleted");
                }
            }

            foreach (var operation in _config.ManualOperations.Except(removeAssociationOperations))
            {
                var op = operation.Type;
                foreach (var storageEntityType in storageModelsEntityType.GetItemsByKeyOrAll(operation.TableName))
                    foreach (var storageProp in storageEntityType.PropertiesPerStorageName.GetItemsByKeyOrAll(operation.FieldName))
                    {
                        if (op == ManualOperationType.RemoveField)
                        {
                            storageProp.Remove(_edmx);
                            dictRes.GetOrAddNew(storageEntityType.Storage.Name + "." + storageProp.Storage.Name).Add("Field Deleted");
                        }
                        else if (op == ManualOperationType.SetFieldAttribute || op == ManualOperationType.SetConceptualFieldAttribute)
                        {
                            var xnode = op == ManualOperationType.SetFieldAttribute ? storageProp.Storage.XNode : storageProp.Conceptual.XNode;
                            if (operation.AttributeValue != null)
                            {
                                var attrValue = xnode.GetAttribute(operation.AttributeName);
                                if (attrValue != operation.AttributeValue)
                                {
                                    xnode.SetAttributeValue(operation.AttributeName, operation.AttributeValue);
                                    dictRes.GetOrAddNew(storageEntityType.Storage.Name + "." + storageProp.Storage.Name).Add(operation.AttributeName + " [" + attrValue + "] -> [" + operation.AttributeValue + "]");
                                }
                            }
                            else
                            {
                                var attrib = xnode.Attribute(operation.AttributeName);
                                if (attrib != null)
                                {
                                    attrib.Remove();
                                    dictRes.GetOrAddNew(storageEntityType.Storage.Name + "." + storageProp.Storage.Name).Add("Deleted attr " + operation.AttributeName);
                                }
                            }
                        }
                    }
            }

            foreach (var dictRe in dictRes)
            {
                _logger(dictRe.Key + ": " + string.Join(", ", dictRe.Value));
            }

        }


        public void FixTabelleECampiEliminati()
        {
            foreach (var entityType in _edmx.Entities.ToList())
            {
                var storageEntityType = entityType.Storage;
                if (storageEntityType == null) // è stata eliminata dal db, ma c'è ancora sull'edmx
                {
                    _logger(string.Format(Messages.Current.EliminazioneEntityDaConceptualModels, entityType));
                    entityType.Remove(_edmx);
                    //DeleteTableFromConceptualModels(conceptualModels, entityType);
                }
                else
                {
                    foreach (var prop in entityType.Properties.GetOrEmpty())
                    {
                        if (prop.Storage == null)
                        {
                            _logger(string.Format(Messages.Current.EliminazionePropertyDaConceptualModels, entityType.Conceptual.Name, prop.Conceptual.Name));
                            prop.Remove(_edmx);
                        }
                        else if (prop.Conceptual == null)
                        {
                            if (prop.StorageKey == null)
                            {
                                _logger(string.Format(Messages.Current.ErroreImpossibileEliminarePropertyDaStorage, entityType.Storage.Name, prop.Storage.Name));
                            }
                            else
                            {
                                _logger(string.Format(Messages.Current.EliminazionePropertyDaStorageModels, entityType.Storage.Name, prop.Storage.Name));
                                prop.Remove(_edmx);
                            }
                        }
                    }

                }
            }
        }


        private void FixAssociationEliminate()
        {
            foreach (var association in _edmx.Associations.ToList())
            {
                var storageAssociation = association.Storage;
                if (storageAssociation != null)
                    continue;
                var conceptualAssociation = association.Conceptual;
                // è stata eliminata dal db, ma c'è ancora sull'edmx
                _logger(string.Format(Messages.Current.EliminazioneAssociationDaConceptualModels, conceptualAssociation.Name, conceptualAssociation.Principal.Role, conceptualAssociation.Principal.PropertyRef, conceptualAssociation.Dependent.Role, conceptualAssociation.Dependent.PropertyRef));
                association.Remove(_edmx);
            }
        }


        public bool HasEqualsAttribute(BaseItem item1, BaseItem item2, params string[] attributi)
        {
            foreach (var s in attributi)
            {
                var v1 = item1.GetAttribute(s);
                var v2 = item2.GetAttribute(s);
                if (v1 != v2)
                    return false;
            }
            return true;
        }

        private static string FnGetMapping(Property p, params string[] attributi)
        {
            var lst = (from key in attributi where !string.IsNullOrEmpty(p.GetAttribute(key)) select string.Concat(key, "=", p.GetAttribute(key))).ToList();
            return string.Join(", ", lst);
        }

        public bool AssociationContainsDifferentTypes()
        {
            var storageModelsAssociations = _edmx.Associations.ToList();

            var haveErrors = false;
            foreach (var storageModelsAssociation in storageModelsAssociations)
            {
                if (storageModelsAssociation.Storage == null || storageModelsAssociation.Conceptual == null)
                    continue;
                var association = storageModelsAssociation.Storage;
                var principalField = association.Principal.EndEntity
                    .PropertiesPerStorageName.GetOrNull(association.Principal.PropertyRef);
                var dependentField = association.Dependent.EndEntity
                    .PropertiesPerStorageName.GetOrNull(association.Dependent.PropertyRef);
                if (principalField == null || dependentField == null)
                {
                    continue;
                }
                var attributi = new string[] { "Type", "Precision", "Scale" };
                if (HasEqualsAttribute(principalField.Storage, dependentField.Storage, attributi))
                    continue;
                haveErrors = true;
                var msg = string.Format("WARNING FK: {0}.{1} ({2}) non corrisponde a {3}.{4} ({5})",
                    association.Principal.EndEntity.Storage.Name, association.Principal.PropertyRef, FnGetMapping(principalField.Storage, attributi),
                    association.Dependent.EndEntity.Storage.Name, association.Dependent.PropertyRef, FnGetMapping(dependentField.Storage, attributi));
                _logger(msg);
            }
            return haveErrors;
        }

        public void FixTabelleNonPresentiInConceptual()
        {
            var lst = _edmx.Entities.Where(x => x.Storage != null && x.Conceptual == null).ToList();
            if (lst.Count == 0)
                return;
            var msg = string.Format(Messages.Current.EliminazioneEntityDaStorageModelsQuestion, lst.Select(x => x.ToString()).Join(", "));
            var res = MessageBox.Show(msg, "Charm Edmx Tools", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (res != DialogResult.Yes)
                return;
            foreach (var entity in lst)
                entity.Remove(_edmx);
            
            //foreach (var entity in _edmx.Entities.Where(x => x.Storage != null))
            //{
            //    var storageEntityType = entity.Storage;
            //    var conceptualEntityType = entity.Conceptual;
            //    if (conceptualEntityType != null) // è stata eliminata dal db, ma c'è ancora sull'edmx
            //        continue;
            //    var msg = string.Format("Attenzione: Tabella '{0}' non presente in ConceptualModels. Eliminarla dallo StorageModels?", storageEntityType.Name);
            //    var res = MessageBox.Show(msg, "Avviso", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            //    if (res != DialogResult.Yes)
            //        continue;
            //    entity.Remove(_edmx);
            //}
        }


        public void FixAssociations()
        {
            foreach (var entity in _edmx.Associations.Where(x => x.Conceptual != null))
            {
                var conceptualModelsAssociation = entity.Conceptual;
                var storageModelsAssociation = entity.Storage;
                if (storageModelsAssociation == null)
                    continue;

                foreach (var conceptualModelsEndKeyValue in entity.Conceptual.ConceptualRoles)
                {
                    var storageModelsEnd = entity.Storage.ConceptualRoles.GetOrNull(conceptualModelsEndKeyValue.Key);
                    if (storageModelsEnd == null)
                        continue;
                    var conceptualModelsEnd = conceptualModelsEndKeyValue.Value;
                    if (storageModelsEnd.EndMultiplicity != conceptualModelsEnd.EndMultiplicity)
                    {
                        _logger(string.Format(Messages.Current.CambioValoreMultiplicityFk, conceptualModelsAssociation.Name, conceptualModelsEnd.Role, conceptualModelsEnd.EndMultiplicity, storageModelsEnd.EndMultiplicity));
                        conceptualModelsEnd.EndMultiplicity = storageModelsEnd.EndMultiplicity;
                    }
                }
            }
        }


        public void FixPropertiesAttributes()
        {
            var provider = _edmx.storageModels.Schema.Attribute("Provider")?.Value;
            if (string.IsNullOrEmpty(provider))
                return;

            var dynamicProvider = _config.EdmMappingConfigurations.FirstOrDefault(it => provider.StartsWith(it.ProviderName));
            if (dynamicProvider == null)
            {
                _logger(string.Format(Messages.Current.EdmMappingConfigurationNotFound, provider));
                return;
            }
            var dt = new Lazy<DataTable>(() => new DataTable());
            foreach (var conceptualEntityType in _edmx.Entities.Where(x => x.Storage != null && x.Conceptual != null))
            {
                foreach (var property in conceptualEntityType.Properties.Where(x => x.Storage != null && x.Conceptual != null))
                {
                    var conceptualProperty = property.Conceptual;
                    var storageProperty = property.Storage;
                    var oldHtml = conceptualProperty.XNode.ToString();
                    var res = ManagerInternalUtils.FixPropertyAttributesDynamic(storageProperty, conceptualProperty, dynamicProvider, dt.Value);
                    if (oldHtml != conceptualProperty.XNode.ToString())
                        _logger(string.Format(Messages.Current.EseguitoFixPropertiesAttributes, conceptualEntityType.Conceptual.Name, conceptualProperty.Name, string.Join("; ", res)));
                }
            }
        }


        public void FixConceptualModelNames()
        {
            //foreach (var entityType in _edmx.Entities)
            //{
            //    if (entityType.Mapping != null && entityType.ConceptualEntitySet != null &&
            //        entityType.Mapping.ConceptualTypeName != entityType.ConceptualEntitySet.EntityType)
            //        entityType.ConceptualEntitySet.EntityType = entityType.Mapping.ConceptualTypeName;
            //}

            if (!_config.NamingNavigationProperty.Enabled)
                return;
            //var conceptualModels = edmx.ConceptualModels;
            foreach (var entityType in _edmx.Entities.Where(x => x.Conceptual != null))
            {
                var navProps = entityType.NavigationProperties
                    .OrderBy(it => it.NavigationIsOneToOne ? 0 : 1) //processo prima i oneToOne
                    .ToList();
                var allNavPropInEntity = new List<string>();
                foreach (var navProp in navProps)
                {
                    string newName;
                    if (navProp.NavigationIsOneToOne)
                        newName = GetFixedNameForNavigationModel(navProp, entityType, allNavPropInEntity);
                    else
                        newName = GetFixedNameForNavigationList(navProp, entityType, allNavPropInEntity);
                    allNavPropInEntity.Add(newName);
                    if (!string.IsNullOrEmpty(newName) && navProp.NavigationProperty.Name != newName)
                    {
                        _logger(string.Format(Messages.Current.RinominoNavigationProperty, entityType.Conceptual.Name, navProp.NavigationProperty.Name, newName));
                        navProp.NavigationProperty.Name = newName;
                    }
                }
            }

        }


        public virtual string GetFixedNameForNavigationModel(NavigationPropertyRelation navProp, EntityRelation entity, List<string> allNavPropInEntity)
        {
            var numeroMappings = entity.NavigationPropertiesOneToOnePerPrincipalRole.GetOrNull(navProp.Association.Conceptual.Principal.EndEntity.Conceptual.Name);
            string newName;
            if (numeroMappings > 1)
                newName = GetFixedNameForNavigationFromConfig(navProp.Association, _config.NamingNavigationProperty.ModelMany.Pattern);
            else
            {
                if (navProp.Association.Conceptual.Principal.EndEntity.Conceptual.Name == entity.Conceptual.Name)
                    newName = GetFixedNameForNavigationFromConfig(navProp.Association, _config.NamingNavigationProperty.ModelOneParent.Pattern);
                else
                    newName = GetFixedNameForNavigationFromConfig(navProp.Association, _config.NamingNavigationProperty.ModelOne.Pattern);
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
        public virtual string GetFixedNameForNavigationList(NavigationPropertyRelation navProp, EntityRelation entity, List<string> allNavPropInEntity)
        {
            //var cont = (navProp.Name.Contains("TA_FAT_TESTATE_FAT_ATT"));
            //var cont1 = cont;
            var numeroDiListe = entity.NavigationPropertiesOneToManyPerDependentRole.GetOrNull(navProp.Association.Conceptual.Dependent.EndEntity.Conceptual.Name);
            string newName = "";
            if (numeroDiListe > 1)
                newName = GetFixedNameForNavigationFromConfig(navProp.Association, _config.NamingNavigationProperty.ListMany.Pattern);
            else
            {
                if (navProp.Association.Conceptual.Dependent.EndEntity.Conceptual.Name == entity.Conceptual.Name)
                    newName = GetFixedNameForNavigationFromConfig(navProp.Association, _config.NamingNavigationProperty.ListOneChilds.Pattern);
                else
                    newName = GetFixedNameForNavigationFromConfig(navProp.Association, _config.NamingNavigationProperty.ListOne.Pattern);
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

        private string GetFixedNameForNavigationFromConfig(AssociationRelation assoc, string config)
        {
            return config
                .Replace("DependentRole", assoc.Conceptual.Dependent.EndEntity.Conceptual.Name)
                .Replace("DependentPropertyRef", assoc.Conceptual.Dependent.PropertyRef)
                .Replace("PrincipalRole", assoc.Conceptual.Principal.EndEntity.Conceptual.Name)
                .Replace("PrincipalPropertyRef", assoc.Conceptual.Principal.PropertyRef);
        }

        public void ClearEdmxPreservingKeyFields()
        {
            _logger(Messages.Current.AvvioClearEdmxPreservingKeyFields);
            foreach (var entityType in _edmx.Entities)
            {
                foreach (var entityTypeProperty in entityType.Properties)
                {
                    if (entityTypeProperty.StorageKey != null)
                        continue;
                    entityTypeProperty.Remove(_edmx);
                }
            }
        }
    }
}