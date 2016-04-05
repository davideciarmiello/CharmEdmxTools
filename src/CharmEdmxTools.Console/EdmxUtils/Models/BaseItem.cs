using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CharmEdmxTools.EdmxUtils.Models
{
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

        public virtual string Name
        {
            get { var name = XNode.Attribute("Name"); return name == null ? null : name.Value; }
            set { var name = XNode.Attribute("Name"); if (name != null) { name.Value = value; } }
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
    }

    public class StorageModels : BaseItem
    {
        public StorageModels(XElement node)
            : base(node)
        {
        }
    }
    public class ConceptualModels : BaseItem
    {
        public ConceptualModels(XElement node)
            : base(node)
        {
        }
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
        }
    }
    public class EntitySet : BaseItem
    {
        public EntitySet(XElement node)
            : base(node)
        {
        }
    }
    public class AssociationSet : BaseItem
    {
        public AssociationSet(XElement node)
            : base(node)
        {
        }
    }
    public class EntityType : BaseItem
    {
        public EntityType(XElement node)
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
        public Association(XElement node)
            : base(node)
        {
        }

        public string PrincipalRole { get { return XNode.Descendants().Where(it => it.Name.LocalName == "Principal").Select(it => it.Attribute("Role").Value).FirstOrDefault(); } }
        public string PrincipalPropertyRef { get { return XNode.Descendants().Where(it => it.Name.LocalName == "Principal").SelectMany(it => it.Descendants()).ToBaseItems<PropertyRef>().Select(it => it.Name).FirstOrDefault(); } }
        public string DependentRole { get { return XNode.Descendants().Where(it => it.Name.LocalName == "Dependent").Select(it => it.Attribute("Role").Value).FirstOrDefault(); } }
        public string DependentPropertyRef { get { return XNode.Descendants().Where(it => it.Name.LocalName == "Dependent").SelectMany(it => it.Descendants()).ToBaseItems<PropertyRef>().Select(it => it.Name).FirstOrDefault(); } }
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
