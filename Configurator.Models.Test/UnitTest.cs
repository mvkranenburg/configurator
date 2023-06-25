namespace Configurator.Models.Test;

using Configurator.Models;

public class UnitTest
{
    public static TheoryData<EtherCATObjectSource, EtherCATObjectSource?, int> EtherCATObjectSourceCompareTo_Data => new()
    {
        {
            new EtherCATObjectSource { Type = EtherCATObjectSourceType.Dictionary },
            null,
            1
        },
        {
            new EtherCATObjectSource { Type = EtherCATObjectSourceType.Dictionary },
            new EtherCATObjectSource { Type = EtherCATObjectSourceType.Dictionary },
            0
        },
        {
            new EtherCATObjectSource { Type = EtherCATObjectSourceType.Dictionary },
            new EtherCATObjectSource { Type = EtherCATObjectSourceType.RxPDO },
            -1
        },
        {
            new EtherCATObjectSource { Type = EtherCATObjectSourceType.RxPDO },
            new EtherCATObjectSource { Type = EtherCATObjectSourceType.Dictionary },
            1
        },
        {
            new EtherCATObjectSource { Type = EtherCATObjectSourceType.RxPDO, Index = 100 },
            new EtherCATObjectSource { Type = EtherCATObjectSourceType.RxPDO, Index = 101 },
            -1
        },
        {
            new EtherCATObjectSource { Type = EtherCATObjectSourceType.RxPDO, Index = 101 },
            new EtherCATObjectSource { Type = EtherCATObjectSourceType.RxPDO, Index = 100 },
            1
        },
    };

    [Theory]
    [MemberData(nameof(EtherCATObjectSourceCompareTo_Data))]
    public void EtherCATObjectSourceCompareTo(EtherCATObjectSource lhs, EtherCATObjectSource? rhs, int expected)
    {
        // Act
        var result = lhs.CompareTo(rhs);

        // Assert
        result.Should().Be(expected);
    }
}