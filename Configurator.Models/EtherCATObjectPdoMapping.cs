namespace Configurator.Models;

public enum EtherCATObjectPdoMapping
{
    TxPDO, // SubDevice to MDevice
    RxPDO, // MDevice to SubDevice
    TxAndRxPDO,
}