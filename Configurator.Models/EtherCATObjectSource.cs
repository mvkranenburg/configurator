namespace Configurator.Models;

public class EtherCATObjectSource : IComparable<EtherCATObjectSource>, IEquatable<EtherCATObjectSource>
{
    public EtherCATObjectSourceType Type { get; set; }
    public ushort Index { get; set; }
    public string Name { get; set; } = string.Empty;

    public int CompareTo(EtherCATObjectSource? other)
    {
        // If other is not a valid object reference, this instance is greater.
        if (other == null) return 1;

        // Compare on Type first, then on Index if Type is equal
        int compare = Type.CompareTo(other.Type);
        return compare != 0 ? compare : Index.CompareTo(other.Index);
    }

    public bool Equals(EtherCATObjectSource? other)
    {
        if (other == null) return false;
        if (Type != other.Type) return false;
        if (Index != other.Index) return false;
        if (!Name.Equals(other.Name)) return false;
        return true;
    }

    public override bool Equals(Object other)
    {
        if (other == null)
            return false;

        EtherCATObjectSource sourceObj = other as EtherCATObjectSource;
        if (sourceObj == null)
            return false;
        else
            return Equals(sourceObj);
    }

    public static bool operator ==(EtherCATObjectSource lhs, EtherCATObjectSource rhs)
    {
        if (((object)lhs) == null || ((object)rhs) == null)
            return Object.Equals(lhs, rhs);

        return lhs.Equals(rhs);
    }

    public static bool operator !=(EtherCATObjectSource lhs, EtherCATObjectSource rhs)
    {
        if (((object)lhs) == null || ((object)rhs) == null)
            return !Object.Equals(lhs, rhs);

        return !lhs.Equals(rhs);
    }

    public override string ToString()
    {
        if (Type == EtherCATObjectSourceType.Dictionary)
            return $"{Type}";
        else
            return $"{Type} 0x{Index:X4}: {Name}";
    }
}