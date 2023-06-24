namespace Configurator.Models;

public enum EtherCATObjectPdoMapping
{
    No,
    TxPDO, // SubDevice to MDevice
    RxPDO, // MDevice to SubDevice
    TxAndRxPDO,
}