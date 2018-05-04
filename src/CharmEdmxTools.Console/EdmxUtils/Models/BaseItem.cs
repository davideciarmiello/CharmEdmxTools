using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CharmEdmxTools.EdmxUtils.Models
{
    public class EdmxContainer
    {
        public EdmxContainer(XDocument xDoc)
        {
            StorageModels = xDoc.Document.Descendants().ToBaseItems<StorageModels>().FirstOrDefault();
            ConceptualModels = xDoc.Document.Descendants().ToBaseItems<ConceptualModels>().FirstOrDefault();
            Mappings = xDoc.Document.Descendants().ToBaseItems<Mappings>().FirstOrDefault();
        }

        public Mappings Mappings { get; set; }

        public ConceptualModels ConceptualModels { get; set; }

        public StorageModels StorageModels { get; set; }
    }


    public class BaseItem
    {
        public override string ToString()
        {
            return XNode.ToString();
        }
        public XElement XNode { get; private set; }
        public BaseItem(XElement node)
        {
            XNode = node;
        }

        private string _name;
        public virtual string Name
        {
            get
            {
                if (_name != null)
                    return _name;
                _name = GetAttribute("Name");
                return _name;
            }
            set { var att = XNode.Attribute("Name"); if (att != null) { att.Value = _name = value; } }
        }

        public IEnumerable<T> Descendants<T>() where T : BaseItem
        {
            return XNode.Descendants().ToBaseItems<T>();
        }

        public string GetAttribute(string key)
        {
            var name = XNode.Attribute(key);
            return name == null ? null : name.Value;
        }

        public bool IsDeleted { get; set; }
    }

    public class StorageModels : BaseItem
    {
        public StorageModels(XElement node)
            : base(node)
        {
            Fill();
        }


        private List<EntityType> _entityType;
        private List<Property> _property;
        private List<Association> _association;
        private List<AssociationSet> _associationSet;
        private List<NavigationProperty> _navigationProperty;
        private List<EntitySet> _entitySet;

        private void Fill()
        {
            _entityType = Descendants<EntityType>().ToList();
            _property = Descendants<Property>().ToList();
            _association = Descendants<Association>().ToList();
            _associationSet = Descendants<AssociationSet>().ToList();
            _navigationProperty = Descendants<NavigationProperty>().ToList();
            _entitySet = Descendants<EntitySet>().ToList();
        }
        public IEnumerable<EntityType> EntityType { get { return _entityType.Where(it => !it.IsDeleted); } }
        public IEnumerable<Property> Property { get { return _property.Where(it => !it.IsDeleted); } }
        public IEnumerable<Association> Association { get { return _association.Where(it => !it.IsDeleted); } }
        public IEnumerable<AssociationSet> AssociationSet { get { return _associationSet.Where(it => !it.IsDeleted); } }
        public IEnumerable<NavigationProperty> NavigationProperty { get { return _navigationProperty.Where(it => !it.IsDeleted); } }
        public IEnumerable<EntitySet> EntitySet { get { return _entitySet.Where(it => !it.IsDeleted); } }

    }
    public class ConceptualModels : BaseItem
    {
        public ConceptualModels(XElement node)
            : base(node)
        {
            Fill();
        }

        public EntityContainer EntityContainer { get; private set; }

        private List<EntityType> _entityType;
        private List<Property> _property;
        private List<Association> _association;
        private List<AssociationSet> _associationSet;
        private List<NavigationProperty> _navigationProperty;
        private List<EntitySet> _entitySet;

        private void Fill()
        {
            EntityContainer = Descendants<EntityContainer>().First();
            _entityType = Descendants<EntityType>().ToList();
            _property = Descendants<Property>().ToList();
            _association = Descendants<Association>().ToList();
            _associationSet = Descendants<AssociationSet>().ToList();
            _navigationProperty = Descendants<NavigationProperty>().ToList();
            _entitySet = Descendants<EntitySet>().ToList();
        }
        public IEnumerable<EntityType> EntityType { get { return _entityType.Where(it => !it.IsDeleted); } }
        public IEnumerable<Property> Property { get { return _property.Where(it => !it.IsDeleted); } }
        public IEnumerable<Association> Association { get { return _association.Where(it => !it.IsDeleted); } }
        public IEnumerable<AssociationSet> AssociationSet { get { return _associationSet.Where(it => !it.IsDeleted); } }
        public IEnumerable<NavigationProperty> NavigationProperty { get { return _navigationProperty.Where(it => !it.IsDeleted); } }
        public IEnumerable<EntitySet> EntitySet { get { return _entitySet.Where(it => !it.IsDeleted); } }
    }

    public class Mappings : BaseItem
    {
        public Mappings(XElement node)
            : base(node)
        {
        }
    }
    public class EntityContainer : BaseItem
    {
        public EntityContainer(XElement node)
            : base(node)
        {
            Fill();
        }

        private List<EntityType> _entityType;
        private List<Property> _property;
        private List<Association> _association;
        private List<AssociationSet> _associationSet;
        private List<NavigationProperty> _navigationProperty;
        private List<EntitySet> _entitySet;

        private void Fill()
        {
            _entityType = Descendants<EntityType>().ToList();
            _property = Descendants<Property>().ToList();
            _association = Descendants<Association>().ToList();
            _associationSet = Descendants<AssociationSet>().ToList();
            _navigationProperty = Descendants<NavigationProperty>().ToList();
            _entitySet = Descendants<EntitySet>().ToList();
        }
        public IEnumerable<EntityType> EntityType { get { return _entityType.Where(it => !it.IsDeleted); } }
        public IEnumerable<Property> Property { get { return _property.Where(it => !it.IsDeleted); } }
        public IEnumerable<Association> Association { get { return _association.Where(it => !it.IsDeleted); } }
        public IEnumerable<AssociationSet> AssociationSet { get { return _associationSet.Where(it => !it.IsDeleted); } }
        public IEnumerable<NavigationProperty> NavigationProperty { get { return _navigationProperty.Where(it => !it.IsDeleted); } }
        public IEnumerable<EntitySet> EntitySet { get { return _entitySet.Where(it => !it.IsDeleted); } }

    }
    public class EntitySet : BaseItem
    {
        public EntitySet(XElement node)
            : base(node)
        {
        }


        public string EntityType { get { var name = XNode.Attribute("EntityType"); return name == null ? null : name.Value; } }

        public string EntityTypeWithoutNamespace
        {
            get
            {
                string clearedName = EntityType;
                var indx = clearedName.IndexOf(".");
                if (indx > -1)
                    clearedName = clearedName.Remove(0, indx + 1);
                return clearedName;
            }
        }

    }
    public class AssociationSet : BaseItem
    {
        public AssociationSet(XElement node)
            : base(node)
        {
        }
        public string Association { get { return XNode.Attribute("Association").Value; } }
        
        public string AssociationWithoutNamespace
        {
            get
            {
                string clearedName = Association;
                var indx = clearedName.IndexOf(".");
                if (indx > -1)
                    clearedName = clearedName.Remove(0, indx + 1);
                return clearedName;
            }
        }
    }
    public class EntityType : BaseItem
    {
        public EntityType(XElement node)
            : base(node)
        {
            Fill();
        }

        string _nameOriginalOfDb;
        public string NameOriginalOfDb
        {
            get
            {
                if (_nameOriginalOfDb != null)
                    return _nameOriginalOfDb;
                var entitySetMapping = MappedEntitySetMapping;
                if (entitySetMapping != null)
                    _nameOriginalOfDb = entitySetMapping.Name;
                else
                    _nameOriginalOfDb = Name;
                return _nameOriginalOfDb;
            }
        }

        EntitySetMapping _mappedEntitySetMapping;
        public EntitySetMapping MappedEntitySetMapping
        {
            get
            {
                if (_mappedEntitySetMapping != null)
                    return _mappedEntitySetMapping;
                var parent = XNode.Parent;
                while (parent != null && parent.Name.LocalName != "ConceptualModels")
                    parent = parent.Parent;
                if (parent != null && parent.Name.LocalName == "ConceptualModels")
                {
                    _mappedEntitySetMapping = parent.Parent.Descendants().ToBaseItems<Mappings>()
                        .SelectMany(it => it.Descendants<EntityTypeMapping>()).Where(it => it.TypeNameWithoutNamespace == this.Name)
                        .Select(it => it.XNode.Parent).ToBaseItems<EntitySetMapping>().FirstOrDefault();
                }
                return _mappedEntitySetMapping;
            }
        }


        //private List<EntityType> _entityType;
        private List<Property> _property;
        //private List<Association> _association;
        //private List<AssociationSet> _associationSet;
        private List<NavigationProperty> _navigationProperty;
        //private List<EntitySet> _entitySet;

        private void Fill()
        {
            //EntityContainer = Descendants<EntityContainer>().First();
            //_entityType = Descendants<EntityType>().ToList();
            _property = Descendants<Property>().ToList();
            //_association = Descendants<Association>().ToList();
            //_associationSet = Descendants<AssociationSet>().ToList();
            _navigationProperty = Descendants<NavigationProperty>().ToList();
            //_entitySet = Descendants<EntitySet>().ToList();
        }
        //public IEnumerable<EntityType> EntityType { get { return _entityType.Where(it => !it.IsDeleted); } }
        public IEnumerable<Property> Property { get { return _property.Where(it => !it.IsDeleted); } }
        //public IEnumerable<Association> Association { get { return _association.Where(it => !it.IsDeleted); } }
        //public IEnumerable<AssociationSet> AssociationSet { get { return _associationSet.Where(it => !it.IsDeleted); } }
        public IEnumerable<NavigationProperty> NavigationProperty { get { return _navigationProperty.Where(it => !it.IsDeleted); } }
        //public IEnumerable<EntitySet> EntitySet { get { return _entitySet.Where(it => !it.IsDeleted); } }


    }
    public class Key : BaseItem
    {
        public Key(XElement node)
            : base(node)
        {
        }
    }
    public class Property : BaseItem
    {
        public Property(XElement node)
            : base(node)
        {
        }



        string _nameOriginalOfDb;
        public string NameOriginalOfDb
        {
            get
            {
                if (_nameOriginalOfDb != null)
                    return _nameOriginalOfDb;
                var entitySetMapping = MappedScalarProperty;
                if (entitySetMapping != null)
                    _nameOriginalOfDb = entitySetMapping.ColumnName;
                else
                    _nameOriginalOfDb = Name;
                return _nameOriginalOfDb;
            }
        }

        ScalarProperty _mappedScalarProperty;
        public ScalarProperty MappedScalarProperty
        {
            get
            {
                if (_mappedScalarProperty != null)
                    return _mappedScalarProperty;
                var parent = XNode.Parent;
                while (parent != null && parent.Name.LocalName != "ConceptualModels")
                    parent = parent.Parent;
                if (parent != null && parent.Name.LocalName == "ConceptualModels")
                {
                    var classTypedName = new[] { XNode.Parent }.ToBaseItems<EntityType>().Single().Name;
                    var colName = this.Name;
                    _mappedScalarProperty = parent.Parent.Descendants().ToBaseItems<Mappings>()
                        .SelectMany(it => it.Descendants<EntityTypeMapping>()).Where(it => it.TypeNameWithoutNamespace == classTypedName)
                        .SelectMany(it => it.Descendants<ScalarProperty>()).Where(it => it.Name == colName).FirstOrDefault();
                }
                return _mappedScalarProperty;
            }
        }
    }
    public class PropertyRef : BaseItem
    {
        public PropertyRef(XElement node)
            : base(node)
        {
        }
    }

    public class NavigationProperty : BaseItem
    {
        public NavigationProperty(XElement node)
            : base(node)
        {
        }
        public string Relationship { get { var name = XNode.Attribute("Relationship"); return name == null ? null : name.Value; } }


        Association _association;
        public Association Association
        {
            get
            {
                if (_association != null && !_association.IsDeleted)
                    return _association;
                var name = Relationship;
                string clearedName = name;
                var indx = clearedName.IndexOf(".");
                if (indx > -1)
                    clearedName = clearedName.Remove(0, indx + 1);
                _association = XNode.Parent.Parent.Descendants().ToBaseItems<Association>().Where(it => it.Name == name || it.Name == clearedName).FirstOrDefault();
                return _association;
            }
        }

        /// <summary>
        /// se non è 1 a 1 è una lista (1 a n)
        /// </summary>
        public bool NavigationIsOneToOne
        {
            get
            {
                var is1a1 = Association.DependentRole == FromRole;
                return is1a1;
            }
        }

        public string FromRole { get { return XNode.Attribute("FromRole").Value; } }
        public string ToRole { get { return XNode.Attribute("ToRole").Value; } }
    }
    public class Association : BaseItem
    {
        private string _principalRole;
        private string _principalPropertyRef;
        private string _dependentRoleOriginal;
        private string _dependentPropertyRef;
        //private string _dependentRole;

        public Association(XElement node)
            : base(node)
        {

        }

        private void FillProps()
        {
            //_dependentRole = DependentRoleGet();
            //if (DependentRole != DependentRoleOriginal)
            //{

            //}
        }

        public string PrincipalRole
        {
            get { return _principalRole ?? (_principalRole = XNode.Descendants().Where(it => it.Name.LocalName == "Principal").Select(it => it.Attribute("Role").Value).FirstOrDefault()); }
        }

        public string PrincipalPropertyRef
        {
            get { return _principalPropertyRef ?? (_principalPropertyRef = XNode.Descendants().Where(it => it.Name.LocalName == "Principal").SelectMany(it => it.Descendants()).ToBaseItems<PropertyRef>().Select(it => it.Name).FirstOrDefault()); }
        }

        public string DependentRole { get { return DependentRoleOriginal; } }

        public string DependentRoleOriginal
        {
            get { return _dependentRoleOriginal ?? (_dependentRoleOriginal = XNode.Descendants().Where(it => it.Name.LocalName == "Dependent").Select(it => it.Attribute("Role").Value).FirstOrDefault()); }
        }

        public string DependentPropertyRef
        {
            get { return _dependentPropertyRef ?? (_dependentPropertyRef = XNode.Descendants().Where(it => it.Name.LocalName == "Dependent").SelectMany(it => it.Descendants()).ToBaseItems<PropertyRef>().Select(it => it.Name).FirstOrDefault()); }
        }

        private readonly ConcurrentDictionary<string, string> _dependentRoleClearCache = new ConcurrentDictionary<string, string>();
        public string DependentRoleTableName
        {
            get { return _dependentRoleClearCache.GetOrAdd(DependentRoleOriginal, DependentRoleGet); }
        }

        private string DependentRoleGet(string dependentRoleOriginal)
        {
            var res = dependentRoleOriginal;
            var parent = XNode.Parent;
            while (parent.Name.LocalName != "Runtime")
                parent = parent.Parent;
            var nodes = parent.Descendants().ToBaseItems<EntityTypeMapping>();
            var end = this.Descendants<End>().First(it => it.Role == res);
            var type = end.GetAttribute("Type");
            var xx = nodes.Where(it => it.TypeName == type).ToList();
            if (xx.Count == 0)
            {
                var entitySet = parent.Descendants().ToBaseItems<EntitySet>().Where(it => it.GetAttribute("EntityType") == type).ToList();
                if (entitySet.Count == 1)
                {
                    return entitySet.First().Name;
                }
            }
            else if (xx.Count == 1)
            {
                var name = xx.Select(it => it.XNode.Parent).ToBaseItems<EntitySetMapping>().Select(it => it.Name).First();
                return name;
            }
            else
            {
                throw new NotImplementedException();
            }
            return res;
        }

    }


    public class End : BaseItem
    {
        public End(XElement node)
            : base(node)
        {
        }

        public virtual string Role
        {
            get { var name = XNode.Attribute("Role"); return name == null ? null : name.Value; }
        }
        public virtual string Multiplicity
        {
            get { var name = XNode.Attribute("Multiplicity"); return name == null ? null : name.Value; }
            set { var name = XNode.Attribute("Multiplicity"); if (name != null) { name.Value = value; } }
        }

    }

    public class EntitySetMapping : BaseItem
    {
        public EntitySetMapping(XElement node)
            : base(node)
        {
        }

        public string ConceptualTypeName
        {
            get { var name = XNode.Elements().First(x => x.Name.LocalName == "EntityTypeMapping").Attribute("TypeName"); return name == null ? null : name.Value; }
        }

        public string StoreEntitySet
        {
            get { var name = XNode.Descendants().First(x => x.Name.LocalName == "MappingFragment").Attribute("StoreEntitySet"); return name == null ? null : name.Value; }
        }

    }
    public class EntityTypeMapping : BaseItem
    {
        public EntityTypeMapping(XElement node)
            : base(node)
        {
        }
        public string TypeName { get { var name = XNode.Attribute("TypeName"); return name == null ? null : name.Value; } }

        public string TypeNameWithoutNamespace
        {
            get
            {
                string clearedName = TypeName;
                var indx = clearedName.IndexOf(".");
                if (indx > -1)
                    clearedName = clearedName.Remove(0, indx + 1);
                return clearedName;
            }
        }

    }
    public class ScalarProperty : BaseItem
    {
        public ScalarProperty(XElement node)
            : base(node)
        {
        }
        public string ColumnName { get { var name = XNode.Attribute("ColumnName"); return name == null ? null : name.Value; } }
    }
}
