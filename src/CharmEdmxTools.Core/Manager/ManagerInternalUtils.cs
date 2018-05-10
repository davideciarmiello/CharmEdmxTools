using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CharmEdmxTools.Core.EdmxConfig;
using CharmEdmxTools.Core.EdmxXmlModels;

namespace CharmEdmxTools.Core.Manager
{
    internal static class ManagerInternalUtils
    {

        public static List<string> FixPropertyAttributesDynamic(Property storagePropertyItem, Property conceptualPropertyItem, edmMappingConfiguration cfg, DataTable dt)
        {
            var storageProperty = storagePropertyItem.XNode;

            var mappings = cfg.edmMappings.Where(mapping => FixPropertyAttributesDynamicFilterItem(mapping, storageProperty, dt));

            var item = mappings.FirstOrDefault();
            if (item == null)
                return null;
            var conceptualProperty = conceptualPropertyItem.XNode;
            var lstRes = new List<string>();
            var res = XElementSetAttributeValueOrRemove(conceptualProperty, "Nullable", storageProperty.Attribute("Nullable"));
            if (!string.IsNullOrEmpty(res))
                lstRes.Add(res);
            foreach (var conceptualTrasformation in item.ConceptualTrasformations)
            {
                foreach (var attrName in conceptualTrasformation.NameList)
                {
                    if (!string.IsNullOrWhiteSpace(conceptualTrasformation.ValueStorageAttributeName))
                        res = XElementSetAttributeValueOrRemove(conceptualProperty, attrName, storageProperty.Attribute(conceptualTrasformation.ValueStorageAttributeName));
                    else if (conceptualTrasformation.ValueFromStorageAttribute)
                        res = XElementSetAttributeValueOrRemove(conceptualProperty, attrName, storageProperty.Attribute(attrName));
                    else
                        res = XElementSetAttributeValueOrRemove(conceptualProperty, attrName, conceptualTrasformation.Value);
                    if (!string.IsNullOrEmpty(res))
                        lstRes.Add(res);
                }
            }
            return lstRes;
        }


        private static bool FixPropertyAttributesDynamicFilterItem(edmMapping mapping, XElement storageProperty, DataTable dt)
        {
            if (!string.IsNullOrWhiteSpace(mapping.DbType) && !mapping.DbTypes.Contains(storageProperty.Attribute("Type").Value))
                return false;

            if (!string.IsNullOrWhiteSpace(mapping.MinPrecision) && !(Convert.ToInt32(storageProperty.Attribute("Precision").Value) >= Convert.ToInt32(mapping.MinPrecision)))
                return false;
            if (!string.IsNullOrWhiteSpace(mapping.MaxPrecision) && !(Convert.ToInt32(storageProperty.Attribute("Precision").Value) <= Convert.ToInt32(mapping.MaxPrecision)))
                return false;

            if (!string.IsNullOrWhiteSpace(mapping.MinScale) && !(Convert.ToInt32(storageProperty.Attribute("Scale").Value) >= Convert.ToInt32(mapping.MinScale)))
                return false;
            if (!string.IsNullOrWhiteSpace(mapping.MaxScale) && !(Convert.ToInt32(storageProperty.Attribute("Scale").Value) <= Convert.ToInt32(mapping.MaxScale)))
                return false;

            if (!string.IsNullOrWhiteSpace(mapping.Where))
            {
                if (dt == null)
                    dt = new DataTable();
                dt.Rows.Clear();
                dt.Rows.Add();
                foreach (var xAttribute in storageProperty.Attributes())
                {
                    var isString = !(xAttribute.Name.LocalName == "Precision" || xAttribute.Name.LocalName == "Scale" || xAttribute.Name.LocalName == "MaxLength");
                    if (!dt.Columns.Contains(xAttribute.Name.LocalName))
                        dt.Columns.Add(xAttribute.Name.LocalName, isString ? typeof(string) : typeof(int));
                    var newValue = isString ? xAttribute.Value as object : (string.IsNullOrEmpty(xAttribute.Value) ? 0 : Convert.ToInt32(xAttribute.Value));
                    dt.Rows[0][xAttribute.Name.LocalName] = newValue ?? DBNull.Value;
                }
                var rows = dt.Select(mapping.Where);
                if (rows.Length == 0)
                    return false;
            }

            return true;
        }

        private static string XElementSetAttributeValueOrRemove(XElement conceptualProperty, string attributeName, XAttribute attributeValue)
        {
            return XElementSetAttributeValueOrRemove(conceptualProperty, attributeName, attributeValue == null ? null : attributeValue.Value);
        }
        private static string XElementSetAttributeValueOrRemove(XElement conceptualProperty, string attributeName, string attributeValue)
        {
            var currAttr = conceptualProperty.Attribute(attributeName);
            var currAttrValue = currAttr == null ? null : currAttr.Value;
            var res = (currAttrValue ?? "") == (attributeValue ?? "") ? "" : string.Concat("'", attributeName, "': '", currAttrValue, "' -> '", attributeValue, "'");
            if (!string.IsNullOrEmpty(attributeValue))
                conceptualProperty.SetAttributeValue(attributeName, attributeValue);
            else if (currAttr != null)
                conceptualProperty.SetAttributeValue(attributeName, null);
            return res;
        }

    }
}
