using System.Linq;
using CharmEdmxTools.Core.EdmxXmlModels;

namespace CharmEdmxTools.Core.Containers
{
    public class ReferentialConstraintRelation
    {
        private readonly BaseItem _principalOrDependent;
        private readonly End _end;
        private readonly PropertyRef _propertyRef;

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
}