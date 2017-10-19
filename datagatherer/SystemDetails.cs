using System;
using System.Collections.Generic;

namespace datagatherer
{
    [Serializable]
    class SystemDetails
    {
        public string DNSHostName;
        public string Domain;
        public string Manufacturer;
        public string Model;
        public string Name;
        public bool PartOfDomain;
        public ulong TotalPhysicalMemory;
        public string UserName;
        public string SerialNumber;
        public bool tpmActive;
        public bool tpmEnabled;
        public string CPU;
        public List<string> Disks;
        public string GPU;
        public List<string> users;
        public string[] printers;
        public string[] printerIP;
        public string OS;
        public bool issixtyforbit;
    }
}
