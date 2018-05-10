using System.Collections.Generic;
using System.Xml.Linq;
using CharmEdmxTools.Core.Containers;

namespace CharmEdmxTools.Core.EdmxXmlModels
{
    public class Association : BaseItem
    {
        public Association(XElement node)
            : base(node)
        {
        }

        public Dictionary<string, ReferentialConstraintRelation> ConceptualRoles { get; set; }
        public ReferentialConstraintRelation Principal { get; set; }
        public ReferentialConstraintRelation Dependent { get; set; }

        public bool MatchTableAndField(EntityRelation endEntity, string property)
        {
            if (Principal != null && Principal.EndEntity == endEntity && Principal.PropertyRef == property)
                return true;
            if (Dependent != null && Dependent.EndEntity == endEntity && Dependent.PropertyRef == property)
                return true;
            return false;
        }

    }
}