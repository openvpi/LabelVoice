using System.Xml;
using System.Xml.Serialization;
using LabelVoice.Core.Utils;

namespace LabelVoice.Models;

[XmlRoot("LVItem")]
public class ItemModel
{
    #region Properties

    /// <summary>
    /// The local registry to hold all slice IDs belonging to this item.
    /// </summary>
    [XmlIgnore] public HexCodeGenerator LocalRegistry = new();

    [XmlIgnore] public Version Version { get; set; } = new();

    [XmlElement("Version")]
    public string XML_Version
    {
        get => Version.ToString();
        set
        {
            if (Version.TryParse(value, out var result))
            {
                Version = result;
            }
        }
    }

    [XmlElement("Id")] public string Id { get; set; }

    [XmlElement("Name")] public string Name { get; set; } = "";

    [XmlElement("OwnerProject")] public string OwnerProject { get; set; }

    [XmlElement("AudioSource")] public string AudioSource { get; set; }

    [XmlIgnore] public Dictionary<string, SliceDefinition> Slices { get; set; } = new(0);

    [XmlArray("Slices")]
    public SliceDefinition[] XML_Slices
    {
        get => Slices.Values.OrderBy(slice => slice).ToArray();
        set => Slices = value.GroupBy(slice => slice.Id).ToDictionary(group => group.Key, group => group.First());
    }

    #endregion

    #region Methods

    public static ItemModel LoadFrom(TextReader reader)
    {
        var model = (ItemModel)new XmlSerializer(typeof(ItemModel)).Deserialize(reader)!;
        model.Validate(true);
        return model;
    }

    public void SaveTo(TextWriter writer)
    {
        Validate();
        new XmlSerializer(typeof(ItemModel)).Serialize(
            XmlWriter.Create(writer, new XmlWriterSettings { Indent = true }), this,
            new XmlSerializerNamespaces(new[] { new XmlQualifiedName("") }));
    }
    
    public void Validate(bool register = false)
    {
        foreach (var key in Slices.Keys.Where(key => 
                     !HexCodeGenerator.IsValidFormat(key, 8)
                     || Slices[key].Out <= Slices[key].In))
        {
            Slices.Remove(key);
        }

        /*
        About overlapping detection:
        Overlapping is useful in some cases like sharing the same silence parts between two slices.
        For this reason, there is no overlapping detection here. It is up to the UI and ViewModel implementations.
        */

        if (!register)
        {
            return;
        }

        foreach (var key in Slices.Keys)
        {
            LocalRegistry.Register(key);
        }
    }

    #endregion
}

[XmlType("Slice")]
public class SliceDefinition : IComparable<SliceDefinition>
{
    [XmlAttribute("Id")] public string Id;

    [XmlAttribute("In")] public double In;

    [XmlAttribute("Out")] public double Out;

    [XmlAttribute("Language")] public string? Language;

    public int CompareTo(SliceDefinition? other)
    {
        if (other == null)
        {
            return -1;
        }

        var comp = In.CompareTo(other.In);
        return comp != 0 ? comp : Out.CompareTo(other.Out);
    }
}
