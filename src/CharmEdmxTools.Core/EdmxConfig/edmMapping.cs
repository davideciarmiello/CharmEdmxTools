using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CharmEdmxTools.Core.EdmxConfig
{
    public class edmMapping
    {
        private string _dbType;

        public edmMapping()
        {
            //conceptualAttributes = new conceptualAttributes();
            ConceptualTrasformations = new List<AttributeTrasformation>();
        }

        public edmMapping(string dbType, params AttributeTrasformation[] trasformations)
            : this()
        {
            DbType = dbType;
            if (trasformations != null)
                ConceptualTrasformations.AddRange(trasformations);
        }

        [XmlAttribute("DbType")]
        public string DbType
        {
            get { return _dbType; }
            set
            {
                _dbType = value;
                DbTypes = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
        [NonSerialized]
        [XmlIgnore]
        public string[] DbTypes;


        [XmlAttribute]
        public string Where { get; set; }

        [XmlAttribute]
        public string MinPrecision { get; set; }
        [XmlAttribute]
        public string MaxPrecision { get; set; }
        [XmlAttribute]
        public string MinScale { get; set; }
        [XmlAttribute]
        public string MaxScale { get; set; }

        //public conceptualAttributes conceptualAttributes { get; set; }
        public List<AttributeTrasformation> ConceptualTrasformations { get; set; }
    }
}