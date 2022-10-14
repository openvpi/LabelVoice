using System.Collections;
using Wintellect.PowerCollections;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectGraphVisitors;

namespace LabelVoice.Models;

public class TextLabelModel
{
    #region Fields

    [YamlIgnore] private readonly HashSet<int> _ids = new();

    #endregion

    #region Properties

    [YamlMember(Alias = "version")] public int Version { get; set; }

    [YamlMember(Alias = "audio")] public AudioSourceReference AudioSource { get; set; } = new();

    [YamlMember(typeof(List<TextLayer>), Alias = "layers")] public List<TextLayer> Layers { get; set; } = new(0);

    [YamlMember(Alias = "groups")] public List<OrderedSet<int>> BindingGroups { get; set; } = new(0);

    [YamlIgnore] public int MaxIdReached { get; private set; }

    #endregion

    #region Methods

    public static TextLabelModel LoadFrom(TextReader reader)
    {
        var model = new Deserializer().Deserialize<TextLabelModel>(reader);
        model.Validate();
        return model;
    }

    public void SaveTo(TextWriter writer)
    {
        Validate();
        new SerializerBuilder()
            .WithIndentedSequences()
            .WithTypeConverter(new NumberFormatConverter("F3"))
            .WithEmissionPhaseObjectGraphVisitor(args => new PrimitiveSequenceVisitor(
                args.InnerVisitor,
                args.EventEmitter,
                args.GetPreProcessingPhaseObjectGraphVisitor<AnchorAssigner>()))
            .Build()
            .Serialize(writer, this);
    }

    public void Validate()
    {
        if (AudioSource.In < 0)
        {
            AudioSource.In = 0;
        }

        if (AudioSource.Out < 0)
        {
            AudioSource.Out = 0;
        }

        if (AudioSource.In > AudioSource.Out)
        {
            (AudioSource.In, AudioSource.Out) = (AudioSource.Out, AudioSource.In);
        }

        _ids.Clear();
        MaxIdReached = 0;

        foreach (var layer in Layers)
        {
            layer.Boundaries.RemoveAll(boundary =>
            {
                if (_ids.Contains(boundary.Id) || boundary.Id < 0)
                {
                    return true;
                }

                _ids.Add(boundary.Id);
                if (boundary.Id > MaxIdReached)
                {
                    MaxIdReached = boundary.Id;
                }

                return false;
            });
        }

        foreach (var group in BindingGroups)
        {
            group.RemoveAll(id => !_ids.Contains(id));
        }

        BindingGroups.RemoveAll(group => group.Count == 0);
    }

    public int NextId()
    {
        return ++MaxIdReached;
    }

    #endregion
}

public class AudioSourceReference
{
    [YamlMember(Alias = "path", ScalarStyle = ScalarStyle.SingleQuoted)] public string Path { get; set; } = "";

    [YamlMember(Alias = "in")] public double In { get; set; }

    [YamlMember(Alias = "out")] public double Out { get; set; }
}

public class TextLayer
{
    [YamlMember(Alias = "name")] public string Name { get; set; } = "";

    [YamlMember(Alias = "boundaries")] public OrderedSet<Boundary> Boundaries { get; set; } = new();
}

public class Boundary : IComparable<Boundary>
{
    [YamlIgnore] private string _text = "";

    [YamlMember(Alias = "id")] public int Id { get; set; }

    [YamlMember(Alias = "position")] public double Position { get; set; }

    [YamlMember(Alias = "text", ScalarStyle = ScalarStyle.SingleQuoted)]
    public string Text
    {
        get => _text;
        set => _text = value ?? "";
    }

    public int CompareTo(Boundary? other)
    {
        if (other == null)
        {
            return -1;
        }
        return Position.CompareTo(other.Position);
    }
}

/// <summary>
/// Should upgrade this after .NET 7 public release, which should be:
/// <code>
/// public class NumberFormatConverter&lt;TNumber&gt; : IYamlTypeConverter where TNumber : IFormattable, IParseable&lt;TNumber&gt;
/// </code>
/// </summary>
internal class NumberFormatConverter : IYamlTypeConverter
{
    private readonly string? _format;

    private readonly IFormatProvider? _provider;

    public NumberFormatConverter(string? format, IFormatProvider? provider = null)
    {
        _format = format;
        _provider = provider;
    }

    public bool Accepts(Type type)
    {
        return type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }
    
    public object ReadYaml(IParser parser, Type type)
    {
        // Only able to use specified double.Parse() currently. This can be replaced with IParseable<TSelf> in .NET 7.
        return double.Parse(parser.Consume<Scalar>().Value);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var str = ((IFormattable)value).ToString(_format, _provider);
        emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, str, ScalarStyle.Any, true, false));
    }
}

/// <summary>
/// This class is used to serialize sequences of primitive types in the FLOW style instead of BLOCK style.
/// </summary>
internal class PrimitiveSequenceVisitor : ChainedObjectGraphVisitor
{
    private readonly IEventEmitter eventEmitter;
    private readonly IAliasProvider aliasProvider;
    private readonly HashSet<AnchorName> emittedAliases = new();

    public PrimitiveSequenceVisitor(IObjectGraphVisitor<IEmitter> nextVisitor, IEventEmitter eventEmitter,
        IAliasProvider aliasProvider)
        : base(nextVisitor)
    {
        this.eventEmitter = eventEmitter;
        this.aliasProvider = aliasProvider;
    }

    public override bool Enter(IObjectDescriptor value, IEmitter context)
    {
        if (value.Value == null)
        {
            return base.Enter(value, context);
        }

        var alias = aliasProvider.GetAlias(value.Value);
        if (alias.IsEmpty || emittedAliases.Add(alias))
        {
            return base.Enter(value, context);
        }

        var aliasEventInfo = new AliasEventInfo(value, alias);
        eventEmitter.Emit(aliasEventInfo, context);
        return aliasEventInfo.NeedsExpansion;
    }

    public override void VisitMappingStart(IObjectDescriptor mapping, Type keyType, Type valueType, IEmitter context)
    {
        var anchor = aliasProvider.GetAlias(mapping.NonNullValue());
        eventEmitter.Emit(new MappingStartEventInfo(mapping) { Anchor = anchor }, context);
    }

    public override void VisitSequenceStart(IObjectDescriptor sequence, Type elementType, IEmitter context)
    {
        var info = new SequenceStartEventInfo(sequence);
        if (sequence.Type.IsAssignableTo(typeof(IEnumerable)) && elementType.IsPrimitive)
        {
            info.Style = SequenceStyle.Flow;
        }

        var anchor = aliasProvider.GetAlias(sequence.NonNullValue());
        info.Anchor = anchor;
        eventEmitter.Emit(info, context);
    }

    public override void VisitScalar(IObjectDescriptor scalar, IEmitter context)
    {
        var scalarInfo = new ScalarEventInfo(scalar);
        if (scalar.Value != null)
        {
            scalarInfo.Anchor = aliasProvider.GetAlias(scalar.Value);
        }

        eventEmitter.Emit(scalarInfo, context);
    }
}
