using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CharmEdmxTools.Core.EdmxXmlModels;
using CharmEdmxTools.Core.ExtensionsMethods;
using CharmEdmxTools.Core.Interfaces;

namespace CharmEdmxTools.Core.Containers
{
    public class EntityRelation : IRemovable
    {
        private ConcurrentDictionary<string, PropertyRelation> _propertiesPerStorageName;

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
            get
            {
                return _propertiesPerStorageName ?? (_propertiesPerStorageName = Properties.GetOrEmpty().Where(x => x.Storage != null).ToConcurrentDictionary(x => x.Storage.Name));
            }
            set { _propertiesPerStorageName = value; }
        }

        public List<NavigationPropertyRelation> NavigationProperties { get; set; }
        public Dictionary<string, int> NavigationPropertiesOneToOnePerPrincipalRole { get; set; }
        public Dictionary<string, int> NavigationPropertiesOneToManyPerDependentRole { get; set; }

        public bool Removed { get; set; }
        public void Remove(EdmxContainer container)
        {
            if (container.AlreadyRemoved(this))
                return;
            foreach (var comment in new[] { Storage?.WarningMessage, Conceptual?.WarningMessage }.Where(x => x != null))
                comment.Remove();
            new BaseItem[] { Storage, Mapping, StorageEntitySet, ConceptualEntitySet, Conceptual }.RemoveAll();
            NavigationProperties?.ForEach(x => x.Remove(container));
            //throw new System.NotImplementedException();
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
}