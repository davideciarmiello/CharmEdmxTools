using System;
using System.Linq;
using System.Xml.Linq;
using CharmEdmxTools.EdmxConfig;
using CharmEdmxTools.EdmxUtils;
using CharmEdmxTools.EdmxUtils.Models;

namespace CharmEdmxTools.ClassiTest
{
    public class EdmxManagerNew
    {
        private readonly string _path;
        private XDocument xDoc;
        private EdmxContainerNew edmx;
        private string xDocLoadStr;
        Action<string> logger;
        CharmEdmxConfiguration config;
        public EdmxManagerNew(string path, Action<string> logger, CharmEdmxConfiguration cfg)
        {
            _path = path;
            xDoc = XDocument.Load(path);
            xDocLoadStr = xDoc.ToString();
            this.logger = logger ?? new Action<string>(s => { });
            config = cfg ?? new CharmEdmxConfiguration();
            edmx = new EdmxContainerNew(xDoc);
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


        public void FieldsManualOperations()
        {
            if (config.ManualOperations == null || config.ManualOperations.Count == 0)
                return;
            if (config.ManualOperations.All(x => x.TableName == "TABLE_TEST" || x.AssociationName == "FK_TEST"))
                return;
            var storageModelsEntityType = edmx.Entities.Where(x => x.Storage != null).ToConcurrentDictionary(x => x.Storage.Name);
            var storageAssociations = edmx.Associations.Where(x => x.Storage != null).ToConcurrentDictionary(x => x.Storage.Name);

            foreach (var operation in config.ManualOperations)
            {
                var op = operation.Type;
                if (op == ManualOperationType.RemoveField || op == ManualOperationType.SetFieldAttribute)
                {
                    var storageEntityType = storageModelsEntityType.GetOrNull(operation.TableName);
                    if (storageEntityType == null)
                        continue;
                    var storageProp = storageEntityType.PropertiesPerStorageName.GetOrNull(operation.FieldName);
                    if (storageProp == null)
                        continue;
                    if (op == ManualOperationType.RemoveField)
                    {
                        storageProp.Remove();
                    }
                    else if (op == ManualOperationType.SetFieldAttribute)
                    {
                        if (operation.AttributeValue != null)
                            storageProp.Storage.XNode.SetAttributeValue(operation.AttributeName, operation.AttributeValue);
                        else
                        {
                            var attrib = storageProp.Storage.XNode.Attribute(operation.AttributeName);
                            if (attrib != null)
                                attrib.Remove();
                        }
                    }
                }
                else if (op == ManualOperationType.RemoveAssociation)
                {
                    var association = storageAssociations.GetOrNull(operation.AssociationName);
                    if (association==null)
                        continue;
                    association.Remove();
                }
            }

        }


        public void FixTabelleECampiEliminati()
        {
            foreach (var entityType in edmx.Entities.ToList())
            {
                var storageEntityType = entityType.Storage;
                if (storageEntityType == null) // è stata eliminata dal db, ma c'è ancora sull'edmx
                {
                    logger(string.Format(Messages.Current.EliminazioneEntityDaConceptualModels, entityType.Conceptual.Name));
                    //DeleteTableFromConceptualModels(conceptualModels, entityType);
                }
                else
                {
                    foreach (var prop in entityType.Properties)
                    {
                        if (prop.Storage == null)
                        {
                            logger(string.Format(Messages.Current.EliminazionePropertyDaConceptualModels, entityType.Conceptual.Name, prop.Conceptual.Name));
                            //DeletePropertyFromConceptualModels(conceptualModels, entityType, prop);
                        } else if (prop.Conceptual == null )
                        {
                            if (prop.StorageKey == null)
                            {
                                logger(string.Format(Messages.Current.ErroreImpossibileEliminarePropertyDaStorage, entityType.Storage.Name, prop.Storage.Name));
                            }
                            else
                            {
                                logger(string.Format(Messages.Current.EliminazionePropertyDaStorageModels, entityType.Storage.Name, prop.Storage.Name));
                                //new[] { prop }.RemoveAll();
                                prop.Remove();
                            }
                        }
                    }
                    
                }
            }

            FixAssociationEliminate();
        }


        private void FixAssociationEliminate()
        {
            foreach (var association in edmx.Associations)
            {
                var storageAssociation = association.Storage;
                if (storageAssociation != null)
                    continue;
                var conceptualAssociation = association.Conceptual;
                // è stata eliminata dal db, ma c'è ancora sull'edmx
                logger(string.Format(Messages.Current.EliminazioneAssociationDaConceptualModels, conceptualAssociation.Name, conceptualAssociation.PrincipalRole, conceptualAssociation.PrincipalPropertyRef, conceptualAssociation.DependentRoleTableName, conceptualAssociation.DependentPropertyRef));
                association.Remove();
            }
        }


    }
}