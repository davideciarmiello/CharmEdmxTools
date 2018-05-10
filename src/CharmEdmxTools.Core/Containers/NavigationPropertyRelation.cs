using CharmEdmxTools.Core.EdmxXmlModels;
using CharmEdmxTools.Core.ExtensionsMethods;
using CharmEdmxTools.Core.Interfaces;

namespace CharmEdmxTools.Core.Containers
{
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
        public void Remove(EdmxContainer container)
        {
            if (container.AlreadyRemoved(this))
                return;
            new BaseItem[] { NavigationProperty }.RemoveAll();
            Association.Remove(container);
        }

    }
}