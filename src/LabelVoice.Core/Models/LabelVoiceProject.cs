using System.Xml;
using System.Xml.Serialization;
using LabelVoice.Core.Utils;

namespace LabelVoice.Models;

[XmlRoot("LVProject")]
public class ProjectModel
{
    #region Properties

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

    [XmlElement("Name")] public string Name { get; set; } = "";

    [XmlArray("LabelSchema")] public List<LayerDefinition> LabelSchema { get; set; } = new(0);

    [XmlArray("Languages")] public List<LanguageDefinition> Languages { get; set; } = new(0);

    [XmlArray("Speakers")] public List<SpeakerDefinition> Speakers { get; set; } = new(0);

    [XmlIgnore] public Dictionary<string, ItemResource> ItemResources { get; set; } = new(0);

    [XmlArray("ItemResources")]
    [XmlArrayItem(typeof(ItemDefinition))]
    [XmlArrayItem(typeof(Placeholder))]
    public ItemResource[] XML_ItemResources
    {
        get => ItemResources.Values.OrderBy(item => item).ToArray();
        set => ItemResources = value.GroupBy(item => item.Id).ToDictionary(group => group.Key, group => group.First());
    }

    #endregion

    #region Methods

    public static ProjectModel LoadFrom(TextReader reader)
    {
        var model = (ProjectModel)new XmlSerializer(typeof(ProjectModel)).Deserialize(reader)!;
        model.Validate(true);
        return model;
    }

    public void SaveTo(TextWriter writer)
    {
        Validate();
        new XmlSerializer(typeof(ProjectModel)).Serialize(
            XmlWriter.Create(writer, new XmlWriterSettings { Indent = true }), this,
            new XmlSerializerNamespaces(new[] { new XmlQualifiedName("") }));
    }

    public void Validate(bool register = false)
    {
        ValidateLayers();
        ValidateLanguages(register);
        ValidateSpeakers(register);
        ValidateItemResources(register);
    }

    /// <summary>
    /// Validate and simplify layer bindings in <see cref="LabelSchema"/>.
    /// </summary>
    public void ValidateLayers()
    {
        for (var i = 0; i < LabelSchema.Count; ++i)
        {
            var layer = LabelSchema[i];
            if (layer.SubdivisionOf >= i || layer.SubdivisionOf < -1)
            {
                layer.SubdivisionOf = -1;
            }

            if (layer.AlignedWith >= i || layer.AlignedWith < -1)
            {
                layer.AlignedWith = -1;
            }

            if (layer.SubdivisionOf == layer.AlignedWith)
            {
                layer.SubdivisionOf = -1;
            }
        }

        var buffer = new List<int>();
        for (var i = 0; i < LabelSchema.Count; ++i)
        {
            if (LabelSchema[i].AlignedWith <= 0)
            {
                continue;
            }

            var j = i;
            while (LabelSchema[j].SubdivisionOf >= LabelSchema[i].AlignedWith)
            {
                buffer.Add(j);
                j = LabelSchema[j].SubdivisionOf;
            }

            if (j == LabelSchema[i].AlignedWith)
            {
                foreach (var idx in buffer)
                {
                    LabelSchema[idx].SubdivisionOf = -1;
                    LabelSchema[idx].AlignedWith = LabelSchema[i].AlignedWith;
                }
            }

            buffer.Clear();
        }
    }

    /// <summary>
    /// Validate language IDs in <see cref="Languages"/>.
    /// </summary>
    /// <param name="register">Whether register all language IDs (for deserialization).</param>
    public void ValidateLanguages(bool register = false)
    {
        var i = 0;
        while (i < Languages.Count)
        {
            var language = Languages[i];
            if (!HexCodeGenerator.IsValidFormat(language.Id, 4) || register && !HexCodeGenerator.GlobalRegistry.Register(language.Id))
            {
                Languages.RemoveAt(i);
            }
            else
            {
                ++i;
            }
        }
    }

    /// <summary>
    /// Validate speaker IDs in <see cref="Speakers"/>.
    /// </summary>
    /// <param name="register">Whether register all speaker IDs (for deserialization).</param>
    public void ValidateSpeakers(bool register = false)
    {
        var i = 0;
        while (i < Speakers.Count)
        {
            var speaker = Speakers[i];
            if (!HexCodeGenerator.IsValidFormat(speaker.Id, 4) || register && !HexCodeGenerator.GlobalRegistry.Register(speaker.Id))
            {
                Speakers.RemoveAt(i);
            }
            else
            {
                ++i;
            }
        }
    }

    /// <summary>
    /// Validate IDs and names of <see cref="ItemResources"/>, remove duplicating items and redundant placeholders.
    /// </summary>
    /// <param name="register">Whether register all resource IDs (for deserialization).</param>
    public void ValidateItemResources(bool register = false)
    {
        // Validate IDs, paths and names.
        foreach (var key in ItemResources.Keys)
        {
            if (!HexCodeGenerator.IsValidFormat(key, 8)  /* invalid ID format */
                || Speakers.All(spk => spk.Id != ItemResources[key].Speaker)  /* speaker not found */
                || !VirtualPathValidator.IsValidPath(ItemResources[key].VirtualPath)  /* invalid path format */
                || ItemResources[key] is ItemDefinition item  /* if this key represents an item */
                && (!VirtualPathValidator.IsValidName(item.Name) /* invalid name format */
                    || Languages.All(lang => lang.Id != item.Language)) /* language not found */ )
            {
                ItemResources.Remove(key);
            }
        }

        var resources = ItemResources.Values.OrderBy(item => item).ToArray();

        // Remove duplicating items and redundant placeholders.
        for (var i = 0; i < resources.Length; ++i)
        {
            switch (resources[i])
            {
                case Placeholder placeholder when i < resources.Length - 1 && resources[i + 1].VirtualPath.StartsWith(placeholder.VirtualPath):
                    ItemResources.Remove(resources[i].Id);
                    break;
                case ItemDefinition item:
                    var j = i + 1;
                    while (j < resources.Length && resources[j] is ItemDefinition other && other.VirtualPath == item.VirtualPath && other.Name == item.Name)
                    {
                        ItemResources.Remove(item.Id);
                        ++j;
                    }
                    i = j - 1;
                    break;
            }
        }

        if (!register)
        {
            return;
        }

        // Register all remaining IDs.
        foreach (var key in ItemResources.Keys)
        {
            HexCodeGenerator.GlobalRegistry.Register(key);
        }
    }

    #endregion
}

[XmlType("Layer")]
public class LayerDefinition
{
    [XmlIgnore] public LayerCategory Category { get; set; } = LayerCategory.Custom;

    [XmlAttribute("Category")]
    public string XML_Category
    {
        get => Category.ToString();
        set => Category = Enum.TryParse<LayerCategory>(value, out var result) ? result : LayerCategory.Custom;
    }

    [XmlAttribute("Name")] public string Name { get; set; } = "";

    [XmlIgnore] public int SubdivisionOf { get; set; } = -1;

    [XmlAttribute("SubdivisionOf")]
    public string? XML_SubdivisionOf
    {
        get => SubdivisionOf >= 0 ? SubdivisionOf.ToString() : null;
        set
        {
            if (int.TryParse(value, out var result) && result >= 0)
            {
                SubdivisionOf = result;
            }
            else
            {
                SubdivisionOf = -1;
            }
        }
    }

    [XmlIgnore] public int AlignedWith { get; set; } = -1;

    [XmlAttribute("AlignedWith")]
    public string? XML_AlignedWith
    {
        get => AlignedWith >= 0 ? AlignedWith.ToString() : null;
        set
        {
            if (int.TryParse(value, out var result) && result >= 0)
            {
                AlignedWith = result;
            }
            else
            {
                AlignedWith = -1;
            }
        }
    }

    [XmlIgnore] public ValueType ValueType { get; set; } = ValueType.Text;

    [XmlAttribute("ValueType")]
    public string XML_ValueType
    {
        get => ValueType.ToString();
        set => ValueType = Enum.TryParse<ValueType>(value, out var result) ? result : ValueType.Text;
    }
}

public enum LayerCategory
{
    Sentence,
    Grapheme,
    Phoneme,
    Pitch,
    Duration,
    Custom
}

[XmlType("Language")]
public class LanguageDefinition
{
    [XmlAttribute("Id")] public string Id { get; set; }

    [XmlAttribute("Name")] public string Name { get; set; } = "";

    [XmlAttribute("Dictionary")] public string? DictionaryPath { get; set; }

    [XmlAttribute("PhonemeSet")] public string? PhonemeSetPath { get; set; }

    [XmlAttribute("Aligner")] public string? AlignerRootPath { get; set; }
}

[XmlType("Speaker")]
public class SpeakerDefinition
{
    [XmlAttribute("Id")] public string Id { get; set; }

    [XmlAttribute("Name")] public string Name { get; set; } = "";
}

public abstract class ItemResource : IComparable<ItemResource>
{
    #region Properties

    [XmlAttribute("Id")] public string Id { get; set; }

    [XmlAttribute("Speaker")] public string Speaker { get; set; }

    [XmlAttribute("VirtualPath")] public string VirtualPath { get; set; } = "";

    #endregion

    #region Implementation of IComparable<T>

    public int CompareTo(ItemResource? other)
    {
        if (other == null)
        {
            return -1;
        }

        var comp = string.Compare(VirtualPath, other.VirtualPath, StringComparison.InvariantCulture);
        if (comp != 0)
        {
            return comp;
        }

        if (GetType() != other.GetType())
        {
            return this is Placeholder ? -1 : 1;
        }

        return this is ItemDefinition i1 && other is ItemDefinition i2
            ? string.Compare(i1.Name, i2.Name, StringComparison.InvariantCulture)
            : 0;
    }

    #endregion
}

[XmlType("Item")]
public class ItemDefinition : ItemResource
{
    [XmlAttribute("Name")] public string Name { get; set; } = "";

    [XmlAttribute("Language")] public string Language { get; set; }
}

[XmlType("Placeholder")]
public class Placeholder : ItemResource
{
}
