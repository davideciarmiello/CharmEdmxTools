using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CharmEdmxTools.EdmxUtils;
using CharmEdmxTools.EdmxUtils.Models;

namespace CharmEdmxTools.ClassiTest
{
    public class EdmxContainerNew
    {
        private readonly XDocument _xDoc;

        public EdmxContainerNew(XDocument xDoc)
        {
            _xDoc = xDoc;

            FillItems();
            //StorageModels = xDoc.Document.Descendants().ToBaseItems<StorageModels>().FirstOrDefault();
            //ConceptualModels = xDoc.Document.Descendants().ToBaseItems<ConceptualModels>().FirstOrDefault();
            //Mappings = xDoc.Document.Descendants().ToBaseItems<Mappings>().FirstOrDefault();
        }

        public void FillItems()
        {
            var runtime = _xDoc.Document.Root.Elements().First(x => x.Name.LocalName == "Runtime");
            var runtimeItems = runtime.Elements().ToBaseItems().ToList();

            var storageModels = runtimeItems.OfType<StorageModels>().First();
            var conceptualModels = runtimeItems.OfType<ConceptualModels>().First();
            var mappings = runtimeItems.OfType<Mappings>().First();

            var storageModelsItems = storageModels.XNode.Elements().Single(x => x.Name.LocalName == "Schema").Elements().ToBaseItems().ToList();
            var conceptualModelsItems = conceptualModels.XNode.Elements().Single(x => x.Name.LocalName == "Schema").Elements().ToBaseItems().ToList();
            var entitySetMapping = mappings.XNode.Descendants().First(x => x.Name.LocalName == "EntityContainerMapping")
                .Elements().ToBaseItems<EntitySetMapping>().ToList();
            var entitySetMappingPerName = entitySetMapping.ToConcurrentDictionary(x => x.StoreEntitySet);
            var storageEntityContainerItems = storageModelsItems.OfType<EntityContainer>().Single().XNode.Elements()
                .ToBaseItems().ToList();
            var conceptualEntityContainerItems = conceptualModelsItems.OfType<EntityContainer>().Single().XNode.Elements()
                .ToBaseItems().ToList();

            var itemsManaged = new HashSet<BaseItem>();

            {
                var storageEntityTypes = storageModelsItems.OfType<EntityType>().ToList();
                var storageEntityContainerItemsEntitySet =
                    storageEntityContainerItems.OfType<EntitySet>().ToConcurrentDictionary(x => x.Name);
                var conceptualEntityTypes =
                    conceptualModelsItems.OfType<EntityType>().ToConcurrentDictionary(x => x.Name);
                var conceptualEntityContainerItemsEntitySetPerEntityType =
                    conceptualEntityContainerItems.OfType<EntitySet>().ToConcurrentDictionary(x => x.EntityType);

                var entities = Entities = new List<EntityRelation>();
                //storageEntityTypes = storageEntityTypes.Where(x => x.Name == "TA_DQG_DATI_QUALIFICA_GRIN").ToList();
                foreach (var storageEntityType in storageEntityTypes)
                {
                    var it = new EntityRelation();
                    entities.Add(it);
                    it.Storage = storageEntityType;
                    it.StorageEntitySet = storageEntityContainerItemsEntitySet.GetOrNull(storageEntityType.Name);
                    it.Mapping = entitySetMappingPerName.GetOrNull(storageEntityType.Name);
                    if (it.Mapping == null)
                        continue;
                    it.ConceptualEntitySet = conceptualEntityContainerItemsEntitySetPerEntityType
                        .GetOrNull(it.Mapping.ConceptualTypeName).Add(itemsManaged);
                    if (it.ConceptualEntitySet == null)
                        continue;
                    it.Conceptual = conceptualEntityTypes.GetOrNull(it.ConceptualEntitySet.EntityTypeWithoutNamespace)
                        .Add(itemsManaged);
                }

                var conceptualOrfani = conceptualEntityTypes.Values.Except(itemsManaged).Cast<EntityType>().ToList();
                if (conceptualOrfani.Any())
                {
                    var conceptualEntityContainerItemsEntitySetPerEntityTypeWithoutNs =
                        conceptualEntityContainerItems.OfType<EntitySet>()
                            .ToConcurrentDictionary(x => x.EntityTypeWithoutNamespace);
                    var entitySetMappingPerConceptualTypeName =
                        entitySetMapping.ToConcurrentDictionary(x => x.ConceptualTypeName);

                    foreach (var conceptualEntityType in conceptualOrfani)
                    {
                        var it = new EntityRelation();
                        entities.Add(it);
                        it.Conceptual = conceptualEntityType;
                        it.ConceptualEntitySet =
                            conceptualEntityContainerItemsEntitySetPerEntityTypeWithoutNs[conceptualEntityType.Name];
                        it.Mapping = entitySetMappingPerConceptualTypeName[it.ConceptualEntitySet.EntityType];
                    }
                }
            }


            {
                var storageAssociationTypes = storageModelsItems.OfType<Association>().ToList();
                var storageAssociationContainerItemsAssociationSet =
                    storageEntityContainerItems.OfType<AssociationSet>().ToConcurrentDictionary(x => x.Name);
                var conceptualAssociationTypes =
                    conceptualModelsItems.OfType<Association>().ToConcurrentDictionary(x => x.Name);
                var conceptualAssociationContainerItemsAssociationSetPerAssociationType =
                    conceptualEntityContainerItems.OfType<AssociationSet>().ToConcurrentDictionary(x => x.Name);

                var assocations = Associations = new List<AssociationRelation>();
                //storageAssociationTypes = storageAssociationTypes.Where(x => x.Name == "TA_DQG_DATI_QUALIFICA_GRIN").ToList();
                foreach (var storageAssociationType in storageAssociationTypes)
                {
                    var it = new AssociationRelation();
                    assocations.Add(it);
                    it.Storage = storageAssociationType;
                    it.StorageAssociationSet = storageAssociationContainerItemsAssociationSet.GetOrNull(storageAssociationType.Name);
                    it.ConceptualAssociationSet = conceptualAssociationContainerItemsAssociationSetPerAssociationType
                        .GetOrNull(it.Storage.Name).Add(itemsManaged);
                    if (it.ConceptualAssociationSet == null)
                        continue;
                    it.Conceptual = conceptualAssociationTypes.GetOrNull(it.ConceptualAssociationSet.AssociationWithoutNamespace).Add(itemsManaged);
                }

                var conceptualOrfani = conceptualAssociationTypes.Values.Except(itemsManaged).Cast<Association>().ToList();
                if (conceptualOrfani.Any())
                {
                    var conceptualAssociationContainerItemsAssociationSetPerAssociationTypeWithoutNs =
                        conceptualEntityContainerItems.OfType<AssociationSet>()
                            .ToConcurrentDictionary(x => x.AssociationWithoutNamespace);

                    foreach (var conceptualAssociationType in conceptualOrfani)
                    {
                        var it = new AssociationRelation();
                        assocations.Add(it);
                        it.Conceptual = conceptualAssociationType;
                        it.ConceptualAssociationSet =
                            conceptualAssociationContainerItemsAssociationSetPerAssociationTypeWithoutNs[conceptualAssociationType.Name];
                    }
                }
            }


        }

        public List<EntityRelation> Entities { get; private set; }

        public List<AssociationRelation> Associations { get; private set; }

        //public Mappings Mappings { get; set; }

        //public ConceptualModels ConceptualModels { get; set; }

        //public StorageModels StorageModels { get; set; }
    }

    public class EntityRelation
    {
        private ConcurrentDictionary<string, PropertyRelation> _propertiesPerStorageName;
        private ConcurrentDictionary<string, PropertyRelation> _propertiesPerConceptualName;

        public override string ToString()
        {
            if (Storage != null)
                return Storage.Name;
            if (Conceptual != null)
                return Conceptual.Name;
            return base.ToString();
        }

        public EntityType Storage { get; set; }
        public EntitySetMapping Mapping { get; set; }
        public EntitySet StorageEntitySet { get; set; }
        public EntitySet ConceptualEntitySet { get; set; }
        public EntityType Conceptual { get; set; }

        public List<PropertyRelation> Properties { get; set; }

        public ConcurrentDictionary<string, PropertyRelation> PropertiesPerStorageName
        {
            get => _propertiesPerStorageName ?? (_propertiesPerStorageName = Properties.Where(x => x.Storage != null).ToConcurrentDictionary(x => x.Storage.Name));
            set => _propertiesPerStorageName = value;
        }
        public ConcurrentDictionary<string, PropertyRelation> PropertiesPerConceptualName
        {
            get => _propertiesPerConceptualName ?? (_propertiesPerConceptualName = Properties.Where(x => x.Conceptual != null).ToConcurrentDictionary(x => x.Conceptual.Name));
            set => _propertiesPerConceptualName = value;
        }
    }

    public class AssociationRelation
    {
        public override string ToString()
        {
            if (Storage != null)
                return Storage.Name;
            if (Conceptual != null)
                return Conceptual.Name;
            return base.ToString();
        }
        public Association Storage { get; set; }
        public AssociationSet StorageAssociationSet { get; set; }
        public AssociationSet ConceptualAssociationSet { get; set; }
        public Association Conceptual { get; set; }

        public void Remove()
        {
            throw new System.NotImplementedException();
            /* associations.RemoveAll();
                        var names = associations.Select(it => it.Name).ToList();
                        var associationsSet =
                            storageModels.AssociationSet.Where(it => names.Contains(it.Name)).ToList();
                        associationsSet.RemoveAll();
                        
             var conceptualAssociationSet = conceptualModelsAssociationSet.Where(it => it.Name == conceptualAssociation.Name).ToList();
                var conceptualNavigationProperty = conceptualModelsNavigationProperty.Where(it => it.Relationship == conceptualAssociationSet[0].Association).ToList();
                conceptualNavigationProperty.RemoveAll();
                conceptualAssociationSet.RemoveAll();
                new[] { conceptualAssociation }.RemoveAll();
             */
        }
    }

    public class PropertyRelation
    {
        public Property Storage { get; set; }
        public Property Conceptual { get; set; }

        public PropertyRef StorageKey { get; set; }
        public PropertyRef ConceptualKey { get; set; }


        public void Remove()
        {
            throw new System.NotImplementedException();
            //todo
            /*
            var propNames = storageEntityType.Property.Select(it => it.Name).ToList();
                        var columnNames = entityType.Property.Select(it => it.Name).ToList();
                        var mappingsToRemove = entitySetMapping.Descendants<ScalarProperty>().Where(it => !propNames.Contains(it.Name) && !columnNames.Contains(it.ColumnName)).ToList();
                        mappingsToRemove.ForEach(it => logger(string.Format(Messages.Current.EliminazioneMappingsEntityDaMappings, entityType.Name, it.Name, it.ColumnName)));
                        mappingsToRemove.AsEnumerable().RemoveAll();

            storageProps.RemoveAll();
            var associations =
                storageModels.Association
                    .Where(it => it.DependentRoleTableName.EqualsInvariant(operation.TableName))
                    .Where(it => it.DependentPropertyRef.EqualsInvariant(operation.FieldName)).ToList();
            if (associations.Any())
            {
                associations.RemoveAll();
                var names = associations.Select(it => it.Name).ToList();
                var associationsSet =
                    storageModels.AssociationSet.Where(it => names.Contains(it.Name)).ToList();
                associationsSet.RemoveAll();
            }*/

        }
    }

}
