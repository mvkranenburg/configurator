# EtherCATInfoXml
Parser for EtherCAT Slave Information (ESI) files.

The ESI file is parsed to a C# class structure generated from [EtherCAT Slave Information (ESI) Schema](https://ethercat.org/en/downloads/downloads_981F0A9A81044A878CE329DC8818F495.htm) v1.17 using [xscgen](https://www.nuget.org/packages/dotnet-xscgen). The exact command used is listed in the header of the generated `EtherCATInfoXml.cs` class file.