namespace EtherCATInfoXml;

using System.Globalization;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

public static class Parser
{
    /// <summary>
    /// Parse xs:simpleType HexDecValue to long.
    /// </summary>
    /// <param name="hexDecValue">HexDecValue to parse.</param>
    /// <returns>Parsed value.</returns>
    public static long ParseHexDecValue(string hexDecValue)
    {
        if (hexDecValue.StartsWith("#x"))
            return long.Parse(hexDecValue[2..], NumberStyles.HexNumber);
        else
            return long.Parse(hexDecValue);
    }

    /// <summary>
    /// Parse xs:complexType NameType to string.
    /// </summary>
    /// <param name="nameTypes">Collection of NameType to parse.</param>
    /// <param name="lcid">Language ID to parse, default is 1033 (English)</param>
    /// <returns>Parsed value.</returns>
    public static string? ParseNameType(IEnumerable<NameType> nameTypes, int lcId = 1033)
    {
        return nameTypes.Where(n => n.LcId == lcId).SingleOrDefault()?.Value;
    }

    private static void ExpandDataTypeSubItems(ref EtherCATInfo? esi)
    {
        for (var i = 0; i < esi?.Descriptions.Devices.Count; i++)
        {
            // Only continue if a Profile is defined
            var profile = esi.Descriptions.Devices[i].Profile;
            if (profile.Count == 0)
                continue;

            var dataTypes = profile.Single().Dictionary.DataTypes;
            var arrayInfos = dataTypes.Where(d => d.ArrayInfo.Count > 0)
                .Select(d => (d.Name, d.BaseType, d.BitSize, d.ArrayInfo.Single().LBound, d.ArrayInfo.Single().Elements))
                .ToDictionary(d => d.Name);

            for (var j = 0; j < dataTypes.Count; j++)
            {
                var subItems = dataTypes[j].SubItem;

                // CoE data type ARRAY conditions (ETG.2000 v1.0.14 table 10):
                // - must contain exactly two SubItems
                // - 1st element must have SubIdx 0
                // - 1st element must have Type USINT
                // - 2nd element must have no SubIdx
                // - 2nd element must have ArrayInfo datatype
                if
                (
                    subItems.Count == 2 &&
                    ParseHexDecValue(subItems[0].SubIdx) == 0 &&
                    subItems[0].Type == "USINT" &&
                    subItems[1].SubIdx == null &&
                    arrayInfos.ContainsKey(subItems[1].Type)
                )
                {
                    var bitOffs = subItems[1].BitOffs;
                    var flags = subItems[1].Flags;
                    var (Name, BaseType, BitSize, LBound, Elements) = arrayInfos[subItems[1].Type];

                    subItems.RemoveAt(1);

                    for (var k = LBound; k < LBound + Elements; k++)
                    {
                        var subItem = new SubItemType
                        {
                            SubIdx = $"{k}",
                            Name = $"Element[{k - 1}]",
                            Type = BaseType,
                            BitSize = BitSize,
                            BitOffs = bitOffs,
                            Flags = flags,
                        };
                        subItems.Add(subItem);

                        bitOffs += BitSize;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Deserialize and pre-process EtherCAT Slave Information (ESI) file. The deserialized ESI is pre-processed before return
    /// for easier parsing. The pre-processing steps are:
    /// - Expand SubItems referring to an ArrayInfo DataType
    ///
    /// Constraints:
    /// - Only Devices with a single Profile are supported, exception is thrown otherwise
    /// - Only DataTypes with a single ArrayInfo are supported, exception is thrown otherwise
    /// </summary>
    /// <param name="stream">Stream containing the ESI file.</param>
    /// <returns><c>EtherCATInfo?</c> representing the ESI file.</returns>
    /// <exception cref="XmlException">Thrown when XML parsing fails.</exception>
    public static EtherCATInfo? Parse(Stream stream)
    {
        var schema = new XmlSchemaSet();
        schema.Add(null, Path.Combine(AppContext.BaseDirectory, @"XmlSchema/EtherCATBase.xsd"));
        schema.Add(null, Path.Combine(AppContext.BaseDirectory, @"XmlSchema/EtherCATDiag.xsd"));
        schema.Add(null, Path.Combine(AppContext.BaseDirectory, @"XmlSchema/EtherCATDict.xsd"));
        schema.Add(null, Path.Combine(AppContext.BaseDirectory, @"XmlSchema/EtherCATInfo.xsd"));
        schema.Add(null, Path.Combine(AppContext.BaseDirectory, @"XmlSchema/EtherCATModule.xsd"));

        // Use System.Xml.Linq for schema validation and better error reporting
        var doc = XDocument.Load(stream);
        doc.Validate(schema, null);

        // Use System.Xml.Serialization.XmlSerializer to parse the content
        var serializer = new XmlSerializer(typeof(EtherCATInfo));

        stream.Seek(0, SeekOrigin.Begin);
        var esi = (EtherCATInfo?)serializer.Deserialize(stream);

        // Expand SubItems referring to an ArrayInfo DataType
        ExpandDataTypeSubItems(ref esi);

        return esi;
    }

}