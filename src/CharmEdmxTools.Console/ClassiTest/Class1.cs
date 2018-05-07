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
        }

        private StorageOrConceptualModels conceptualModels;
        public StorageOrConceptualModels storageModels;
        public void FillItems()
        {
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
                    it.ConceptualEntitySet = conceptualEntityContainerItemsEntitySetPerEntityTypeWithNs.GetOrNull(it.Mapping.ConceptualTypeName).Add(itemsManaged);
                    if (it.ConceptualEntitySet == null)
                        continue;
                    it.Conceptual = conceptualEntityTypesWithNs.GetOrNull(it.ConceptualEntitySet.EntityType).Add(itemsManaged);
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
                            conceptualEntityContainerItemsEntitySetPerEntityTypeWithNs[conceptualModels.Namespace + "." + conceptualEntityType.Name];
                        it.Mapping = entitySetMappingPerConceptualTypeName[it.ConceptualEntitySet.EntityType];
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
            var conceptualProperties = entity.Conceptual.Property.ToConcurrentDictionary(x => x.Name);
            var conceptualKeys = entity.Conceptual.Descendants<Key>().Take(1).SelectMany(it => it.Descendants<PropertyRef>())
                .ToConcurrentDictionary(x => x.Name);

            foreach (var property in entity.Storage.Property)
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
                foreach (var navigationProperty in entity.Conceptual.NavigationProperty)
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
    }

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

    public class EntityRelation : IRemovable
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

        public List<NavigationPropertyRelation> NavigationProperties { get; set; }
        public Dictionary<string, int> NavigationPropertiesOneToOnePerPrincipalRole { get; set; }
        public Dictionary<string, int> NavigationPropertiesOneToManyPerDependentRole { get; set; }

        public bool Removed { get; set; }
        public void Remove(EdmxContainerNew container)
        {
            if (Removed)
                return;
            Removed = true;
            new BaseItem[] { Storage, Mapping, StorageEntitySet, ConceptualEntitySet, Conceptual }.RemoveAll();
            NavigationProperties.ForEach(x => x.Remove(container));
            throw new System.NotImplementedException();
            /*var type = storageEntityType;
                var associationToRemove = storageModels.Association.Where(it => it.DependentRoleTableName == type.Name || it.PrincipalRole == type.Name).ToArray();
                var associationSetToRemove = storageModels.AssociationSet.Where(it => associationToRemove.Select(p => p.Name).Contains(it.Name)).ToArray();
                associationToRemove.RemoveAll();
                associationSetToRemove.RemoveAll();
                var entitySetToRemove = storageModels.EntitySet.Where(it => it.Name == type.Name).ToArray();
                entitySetToRemove.RemoveAll();
                new[] { type }.RemoveAll();*/
        }
    }

    public class AssociationRelation : IRemovable
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
        public List<NavigationPropertyRelation> NavigationProperties { get; set; }

        public void Remove(EdmxContainerNew container)
        {
            if (Removed)
                return;
            Removed = true;
            new BaseItem[] { Storage, StorageAssociationSet, ConceptualAssociationSet, Conceptual }.RemoveAll();
            if (NavigationProperties != null)
                NavigationProperties.ForEach(x => x.Remove(container));
            container.Associations.Remove(this);
            //Storage.XNode.Document 
            //throw new System.NotImplementedException();
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

        public bool Removed { get; set; }
    }

    public class PropertyRelation : IRemovable
    {
        public Property Storage { get; set; }
        public Property Conceptual { get; set; }

        public PropertyRef StorageKey { get; set; }
        public PropertyRef ConceptualKey { get; set; }
        public ScalarProperty ScalarProperty { get; set; }


        public bool Removed { get; set; }
        public void Remove(EdmxContainerNew container)
        {
            if (Removed)
                return;
            Removed = true;
            new BaseItem[] { Storage, Conceptual, StorageKey, ConceptualKey, ScalarProperty }.RemoveAll();

            if (container.PropertiesList == null)
                container.PropertiesList = container.Entities.SelectMany(x => x.Properties,
                        (relation, propertyRelation) => new { relation, propertyRelation })
                    .ToDictionary(x => x.propertyRelation, x => x.relation);


                //from ass in container.Associations
                //from assoc in new []{ass.Conceptual,ass.Storage}.Where(x=>x!= null)
                //from hh in new[] { assoc.Dependent, assoc.Principal}.Where(x=>x!= null)
                

            //container.Associations.Where(x=>(x.Conceptual ?? x.Storage).Dependent)

            //var entity = container.PropertiesList[this];
            //entity.NavigationProperties.Where(x=>x.Association.Conceptual.)


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


    public class NavigationPropertyRelation : IRemovable
    {
        public NavigationProperty NavigationProperty { get; set; }
        public AssociationRelation Association { get; set; }

        /// <summary>
        /// se non è 1 a 1 è una lista (1 a n)
        /// </summary>
        public bool NavigationIsOneToOne
        {
            get
            {
                var is1a1 = Association.Conceptual.Dependent.Role == NavigationProperty.FromRole;
                return is1a1;
            }
        }

        public bool Removed { get; set; }
        public void Remove(EdmxContainerNew container)
        {
            if (Removed)
                return;
            Removed = true;
            new BaseItem[] { NavigationProperty }.RemoveAll();
            Association.Remove(container);
        }

    }

    public class ReferentialConstraintRelation
    {
        private readonly BaseItem _principalOrDependent;
        private readonly End _end;
        private PropertyRef _propertyRef;

        public ReferentialConstraintRelation(BaseItem principalOrDependent, End end)
        {
            _principalOrDependent = principalOrDependent;
            _propertyRef = principalOrDependent.Descendants<PropertyRef>().First();
            IsPrincipal = principalOrDependent.XNode.Name.LocalName == "Principal";
            IsDependent = principalOrDependent.XNode.Name.LocalName == "Dependent";
            _end = end;
        }

        public bool IsPrincipal { get; private set; }
        public bool IsDependent { get; private set; }
        public string Role
        {
            get { return _end.Role; }
        }
        public string PropertyRef
        {
            get { return _propertyRef.Name; }
        }
        public string EndMultiplicity
        {
            get { return _end.Multiplicity; }
            set { _end.Multiplicity = value; }
        }
        public string EndModelType
        {
            get { return _end.GetAttribute("Type"); }
        }

        public EntityRelation EndEntity { get; set; }
    }

    public interface IRemovable
    {
        void Remove(EdmxContainerNew container);
        bool Removed { get; set; }
    }
}
