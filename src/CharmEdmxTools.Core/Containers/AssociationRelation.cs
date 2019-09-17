using System.Collections.Generic;
using CharmEdmxTools.Core.EdmxXmlModels;
using CharmEdmxTools.Core.ExtensionsMethods;
using CharmEdmxTools.Core.Interfaces;

namespace CharmEdmxTools.Core.Containers
{
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

        public void Remove(EdmxContainer container)
        {
            if (container.AlreadyRemoved(this))
                return;
            new BaseItem[] { Storage, StorageAssociationSet, ConceptualAssociationSet, Conceptual }.RemoveAll();
            NavigationProperties?.ForEach(x => x.Remove(container));
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
}