namespace Configurator.Models;

public class EtherCATObjectSource : IComparable<EtherCATObjectSource>, IEquatable<EtherCATObjectSource>
{
    public EtherCATObjectSourceType Type { get; set; }
    public ushort Index { get; set; }
    public string Name { get; set; } = string.Empty;

    public int CompareTo(EtherCATObjectSource? other)
        => other == null ? 1 : (Type, Index, Name).CompareTo((other.Type, other.Index, other.Name));

    public bool Equals(EtherCATObjectSource? other)
        => other != null && Type == other.Type && Index == other.Index && Name == other.Name;

    public override bool Equals(object? obj)
        => obj is EtherCATObjectSource sourceObj && Equals(sourceObj);

    public override int GetHashCode()
        => HashCode.Combine(Type, Index, Name);

    public override string ToString()
    {
        if (Type == EtherCATObjectSourceType.Dictionary)
            return $"{Type}";
        else
            return $"{Type} 0x{Index:X4}: {Name}";
    }
}