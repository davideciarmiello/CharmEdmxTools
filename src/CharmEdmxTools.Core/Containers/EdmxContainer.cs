using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CharmEdmxTools.Core.EdmxXmlModels;
using CharmEdmxTools.Core.ExtensionsMethods;
using CharmEdmxTools.Core.Interfaces;

namespace CharmEdmxTools.Core.Containers
{
    public class EdmxContainer
    {
        private readonly XDocument _xDoc;

        public EdmxContainer(XDocument xDoc)
        {
            _xDoc = xDoc;

            FillItems();
        }

        private StorageOrConceptualModels conceptualModels;
        public StorageOrConceptualModels storageModels;
        public void FillItems()
        {
            ItemsRemoved = false;
            var dictCache = new ConcurrentDictionary<XElement, BaseItem>();
            ItemExtensions.ToBaseItemDocumentCache.TryRemove(_xDoc, out dictCache);

            var runtime = _xDoc.Document.Root.Elements().First(x => x.Name.LocalName == "Runtime");
            var runtimeItems = runtime.Elements().ToBaseItems().ToList();

            storageModels = new StorageOrConceptualModels(runtimeItems.First(x => x.XNode.Name.LocalName == "StorageModels"));
            conceptualModels = new StorageOrConceptualModels(runtimeItems.First(x => x.XNode.Name.LocalName == "ConceptualModels"));

            var mappings = runtimeItems.OfType<Mappings>().First();
            var entitySetMapping = mappings.XNode.Descendants().First(x => x.Name.LocalName == "EntityContainerMapping")
                .Elements().ToBaseItems<EntitySetMapping>().ToList();
            var entitySetMappingPerName = entitySetMapping.ToConcurrentDictionary(x => x.StoreEntitySet);

            var itemsManaged = new HashSet<BaseItem>();

            {
                var storageEntityTypes = storageModels.SchemaElements.OfType<EntityType>().ToList();
                var storageEntityContainerItemsEntitySet = storageModels.EntityContainerElements.OfType<EntitySet>().ToConcurrentDictionary(x => x.Name);
                var conceptualEntityTypesWithNs = conceptualModels.SchemaElements.OfType<EntityType>().ToConcurrentDictionary(x => conceptualModels.Namespace + "." + x.Name);
                var conceptualEntityContainerItemsEntitySetPerEntityTypeWithNs = conceptualModels.EntityContainerElements.OfType<EntitySet>().ToConcurrentDictionary(x => x.EntityType);

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
                    it.ConceptualEntitySet = conceptualEntityContainerItemsEntitySetPerEntityTypeWithNs.GetOrNullMultiNs(it.Mapping.ConceptualTypeName, conceptualModels.Namespace, conceptualModels.Alias).Add(itemsManaged);
                    if (it.ConceptualEntitySet == null)
                        continue;
                    it.Conceptual = conceptualEntityTypesWithNs.GetOrNullMultiNs(it.ConceptualEntitySet.EntityType, conceptualModels.Namespace, conceptualModels.Alias).Add(itemsManaged);
                }

                var conceptualOrfani = conceptualEntityTypesWithNs.Values.Except(itemsManaged).Cast<EntityType>().ToList();
                if (conceptualOrfani.Any())
                {
                    //var conceptualEntityContainerItemsEntitySetPerEntityType = conceptualModels.EntityContainerElements.OfType<EntitySet>().ToConcurrentDictionary(x => x.EntityType);
                    var entitySetMappingPerConceptualTypeName = entitySetMapping.ToConcurrentDictionary(x => x.ConceptualTypeName);

                    foreach (var conceptualEntityType in conceptualOrfani)
                    {
                        var it = new EntityRelation();
                        entities.Add(it);
                        it.Conceptual = conceptualEntityType;
                        it.ConceptualEntitySet =
                            conceptualEntityContainerItemsEntitySetPerEntityTypeWithNs.GetMultiNs(conceptualEntityType.Name, conceptualModels.Namespace, conceptualModels.Alias);
                        it.Mapping = entitySetMappingPerConceptualTypeName.GetMultiNs(it.ConceptualEntitySet.EntityType, conceptualModels.Namespace, conceptualModels.Alias);
                    }
                }
            }


            {
                var storageAssociationTypes = storageModels.SchemaElements.OfType<Association>().ToList();
                var storageAssociationContainerItemsAssociationSet = storageModels.EntityContainerElements.OfType<AssociationSet>().ToConcurrentDictionary(x => x.Name);
                var conceptualAssociationTypes = conceptualModels.SchemaElements.OfType<Association>().ToConcurrentDictionary(x => conceptualModels.Namespace + "." + x.Name);
                var conceptualAssociationContainerItemsAssociationSetPerName = conceptualModels.EntityContainerElements.OfType<AssociationSet>().ToConcurrentDictionary(x => x.Name);

                var assocations = Associations = new List<AssociationRelation>();
                //storageAssociationTypes = storageAssociationTypes.Where(x => x.Name == "TA_DQG_DATI_QUALIFICA_GRIN").ToList();
                foreach (var storageAssociationType in storageAssociationTypes)
                {
                    var it = new AssociationRelation();
                    assocations.Add(it);
                    it.Storage = storageAssociationType;
                    it.StorageAssociationSet = storageAssociationContainerItemsAssociationSet.GetOrNull(storageAssociationType.Name);
                    it.ConceptualAssociationSet = conceptualAssociationContainerItemsAssociationSetPerName
                        .GetOrNull(it.StorageAssociationSet.Name).Add(itemsManaged);
                    if (it.ConceptualAssociationSet == null)
                        continue;
                    it.Conceptual = conceptualAssociationTypes.GetOrNull(it.ConceptualAssociationSet.Association).Add(itemsManaged);
                }

                var conceptualOrfani = conceptualAssociationTypes.Values.Except(itemsManaged).Cast<Association>().ToList();
                if (conceptualOrfani.Any())
                {
                    var conceptualAssociationContainerItemsAssociationSetPerAssociationType = conceptualModels.EntityContainerElements.OfType<AssociationSet>().ToConcurrentDictionary(x => x.Association);

                    foreach (var conceptualAssociationType in conceptualOrfani)
                    {
                        var it = new AssociationRelation();
                        assocations.Add(it);
                        it.Conceptual = conceptualAssociationType;
                        it.ConceptualAssociationSet =
                            conceptualAssociationContainerItemsAssociationSetPerAssociationType[conceptualModels.Namespace + "." + conceptualAssociationType.Name];
                    }
                }

                var conceptualEntities = this.Entities.Where(x => x.Conceptual != null)
                    .ToConcurrentDictionary(x => this.conceptualModels.Namespace + "." + x.Conceptual.Name);
                var storageEntities = this.Entities.Where(x => x.Storage != null)
                    .ToConcurrentDictionary(x => this.conceptualModels.Alias + "." + x.Storage.Name);

                foreach (var assocation in assocations)
                {
                    FillAssociation(assocation.Conceptual, assocation, conceptualEntities);
                    FillAssociation(assocation.Storage, assocation, storageEntities);
                }
            }

            foreach (var entityRelation in Entities.Where(x => x.Storage != null && x.Conceptual != null))
            {
                FillProperties(entityRelation);
            }

            FillNavigationProperties(Entities);

            PropertiesList = Entities.SelectMany(x => x.Properties,
                    (relation, propertyRelation) => new { relation, propertyRelation })
                .ToDictionary(x => x.propertyRelation, x => x.relation);
        }

        private static void FillAssociation(Association container, AssociationRelation assocation,
            ConcurrentDictionary<string, EntityRelation> conceptualEntities)
        {
            if (container == null)
                return;
            container.ConceptualRoles = new Dictionary<string, ReferentialConstraintRelation>();
            var referentialConstraints = container.XNode
                .Elements().First(it => it.Name.LocalName == "ReferentialConstraint")
                .Elements().ToBaseItems().ToConcurrentDictionary(x => x.GetAttribute("Role"));
            foreach (var end in container.Descendants<End>())
            {
                var it = new ReferentialConstraintRelation(referentialConstraints[end.Role], end);
                it.EndEntity = conceptualEntities.GetOrNull(it.EndModelType);
                if (it.IsDependent)
                    container.Dependent = it;
                else if (it.IsPrincipal)
                    container.Principal = it;
                container.ConceptualRoles.Add(end.Role, it);
            }
        }


        private void FillProperties(EntityRelation entity)
        {
            var itemsManaged = new HashSet<BaseItem>();

            var properties = entity.Properties = new List<PropertyRelation>();
            var propMappingPerStorageColumnName = entity.Mapping.Descendants<ScalarProperty>().ToConcurrentDictionary(x => x.ColumnName);
            var storageKeys = entity.Storage.Descendants<Key>().Take(1).SelectMany(it => it.Descendants<PropertyRef>())
                .ToConcurrentDictionary(x => x.Name);
            var conceptualProperties = entity.Conceptual.Properties.ToConcurrentDictionary(x => x.Name);
            var conceptualKeys = entity.Conceptual.Descendants<Key>().Take(1).SelectMany(it => it.Descendants<PropertyRef>())
                .ToConcurrentDictionary(x => x.Name);

            foreach (var property in entity.Storage.Properties)
            {
                var prop = new PropertyRelation();
                properties.Add(prop);
                prop.Storage = property;
                prop.StorageKey = storageKeys.GetOrNull(prop.Storage.Name);
                prop.ScalarProperty = propMappingPerStorageColumnName[prop.Storage.Name];
                prop.Conceptual = conceptualProperties.GetOrNull(prop.ScalarProperty.Name).Add(itemsManaged);
                if (prop.Conceptual == null)
                    continue;
                prop.ConceptualKey = conceptualKeys.GetOrNull(prop.Conceptual.Name);
            }

            var conceptualOrfani = conceptualProperties.Values.Except(itemsManaged).Cast<Property>().ToList();
            if (conceptualOrfani.Any())
            {
                var propMappingPerConceptualColumnName = propMappingPerStorageColumnName.Values.ToConcurrentDictionary(x => x.Name);
                foreach (var property in conceptualOrfani)
                {
                    var prop = new PropertyRelation();
                    properties.Add(prop);
                    prop.Conceptual = property;
                    prop.ConceptualKey = conceptualKeys.GetOrNull(prop.Conceptual.Name);
                    prop.ScalarProperty = propMappingPerConceptualColumnName.GetOrNull(prop.Conceptual.Name);
                }
            }
        }

        private void FillNavigationProperties(List<EntityRelation> entities)
        {
            var assocationsPerNameWithNs = this.Associations.ToConcurrentDictionary(x => this.conceptualModels.Namespace + "." + x.Conceptual.Name);

            foreach (var entity in entities.Where(x => x.Conceptual != null))
            {
                var props = entity.NavigationProperties = new List<NavigationPropertyRelation>();
                foreach (var navigationProperty in entity.Conceptual.NavigationProperties)
                {
                    var prop = new NavigationPropertyRelation();
                    props.Add(prop);
                    prop.NavigationProperty = navigationProperty;
                    prop.Association = assocationsPerNameWithNs[prop.NavigationProperty.Relationship];
                    if (prop.Association.NavigationProperties == null)
                        prop.Association.NavigationProperties = new List<NavigationPropertyRelation>();
                    prop.Association.NavigationProperties.Add(prop);
                }

                entity.NavigationPropertiesOneToOnePerPrincipalRole = props.Where(x => x.NavigationIsOneToOne)
                    .GroupBy(x => x.Association.Conceptual.Principal.EndEntity.Conceptual.Name).ToDictionary(x => x.Key, x => x.Count());
                entity.NavigationPropertiesOneToManyPerDependentRole = props.Where(x => !x.NavigationIsOneToOne)
                    .GroupBy(x => x.Association.Conceptual.Dependent.EndEntity.Conceptual.Name).ToDictionary(x => x.Key, x => x.Count());

            }
        }

        public List<EntityRelation> Entities { get; private set; }

        public List<AssociationRelation> Associations { get; private set; }
        public Dictionary<PropertyRelation, EntityRelation> PropertiesList { get; set; }

        public bool AlreadyRemoved(IRemovable entity)
        {
            if (entity.Removed)
                return true;
            entity.Removed = true;
            this.ItemsRemoved = true;
            return false;
        }

        public bool ItemsRemoved { get; set; }
    }
}
