using System.Linq;
using CharmEdmxTools.Core.EdmxXmlModels;
using CharmEdmxTools.Core.ExtensionsMethods;
using CharmEdmxTools.Core.Interfaces;

namespace CharmEdmxTools.Core.Containers
{
    public class PropertyRelation : IRemovable
    {
        public Property Storage { get; set; }
        public Property Conceptual { get; set; }

        public PropertyRef StorageKey { get; set; }
        public PropertyRef ConceptualKey { get; set; }
        public ScalarProperty ScalarProperty { get; set; }


        public bool Removed { get; set; }
        public void Remove(EdmxContainer container)
        {
            if (container.AlreadyRemoved(this))
                return;
            new BaseItem[] { Storage, Conceptual, StorageKey, ConceptualKey, ScalarProperty }.RemoveAll();
            
            var entity = container.PropertiesList[this];

            if (this.Storage != null)
            {
                var navsToRemove = container.Associations
                    .Where(x => x.Storage != null && x.Storage.MatchTableAndField(entity, this.Storage.Name)).ToList();
                navsToRemove.ForEach(x => x.Remove(container));
            }
            if (this.Conceptual != null)
            {
                var navsToRemove = container.Associations
                    .Where(x => x.Conceptual != null && x.Conceptual.MatchTableAndField(entity, this.Conceptual.Name)).ToList();
                navsToRemove.ForEach(x => x.Remove(container));
            }


            //entity.NavigationProperties.Where(x=>x.Association.Conceptual.)


            //throw new System.NotImplementedException();
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