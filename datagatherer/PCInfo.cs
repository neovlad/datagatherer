using System;
using System.Collections.Generic;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace datagatherer
{
    internal class PCInfo
    {
        private enum SID_NAME_USE
        {
            SidTypeUser = 1,
            SidTypeGroup,
            SidTypeDomain,
            SidTypeAlias,
            SidTypeWellKnownGroup,
            SidTypeDeletedAccount,
            SidTypeInvalid,
            SidTypeUnknown,
            SidTypeComputer
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool LookupAccountSid(
            string lpSystemName,
            [MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
            StringBuilder lpName,
            ref uint cchName,
            StringBuilder ReferencedDomainName,
            ref uint cchReferencedDomainName,
            out SID_NAME_USE peUse);

        public SystemDetails Item;
        public Dictionary<string, List<string>> Data;

        public PCInfo()
        {
            Data = new Dictionary<string, List<string>>();
            insertCSInfo();
            insertBIOSInfo();
        }

        public void insertData()
        {
            insertTpmInfo();
            insertCPUInfo();
            inserDiskInfo();
            insertGPUInfo();
            insertLocalUserAccounts();
            insertPrinters();
            insertOSInfo();
        }

        private void insertCSInfo()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem");
                insertInfo(searcher, "Win32_ComputerSystem");
                foreach (var o in searcher.Get())
                {
                    var v = (ManagementObject) o;
                    var sys = new SystemDetails();
                    v.Get();
                    sys.DNSHostName = (string) v["DNSHostname"];
                    sys.Domain = (string) v["Domain"];
                    sys.Manufacturer = (string) v["Manufacturer"];
                    sys.Model = (string) v["Model"];
                    sys.Name = (string) v["Name"];
                    sys.PartOfDomain = (bool) v["PartOfDomain"];
                    sys.UserName = (string) v["UserName"];
                    sys.TotalPhysicalMemory = (ulong) v["TotalPhysicalMemory"];
                    Item = sys;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void insertBIOSInfo()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("Select * from Win32_BIOS");
                insertInfo(searcher, "Win32_BIOS");
                foreach (var obj in searcher.Get())
                {
                    var v = (ManagementObject) obj;
                    v.Get();
                    Item.SerialNumber = (string) v["SerialNumber"];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void insertTpmInfo()
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator))
            {
                Item.tpmEnabled = false;
                Item.tpmActive = false;
                return;
            }
            try
            {
                var mc = new ManagementClass("/root/CIMv2/Security/MicrosoftTpm:Win32_Tpm");
                var collection = mc.GetInstances();
                foreach (var o in collection)
                {
                    var obj = (ManagementObject) o;
                    Item.tpmActive = (bool) obj["IsActivated_InitialValue"];
                    Item.tpmEnabled = (bool) obj["IsEnabled_InitialValue"];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void insertCPUInfo()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("Select * from Win32_Processor");
                insertInfo(searcher, "Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    var v = (ManagementObject) obj;
                    v.Get();
                    Item.CPU = (string) v["Name"];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void inserDiskInfo()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("Select * from Win32_DiskDrive");
                insertInfo(searcher, "Win32_DiskDrive");
                Item.Disks = new List<string>();
                foreach (var obj in searcher.Get())
                {
                    var v = (ManagementObject) obj;
                    v.Get();
                    var type = (string) v["MediaType"];
                    if (!type.Equals("RemovableMedia"))
                        Item.Disks.Add((string) v["Model"] + "  Size" + ((ulong) v["Size"] / 1048576 / 1000) + "GB");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void insertGPUInfo()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("Select * from Win32_VideoController");
                insertInfo(searcher, "Win32_VideoController");
                foreach (var obj in searcher.Get())
                {
                    var v = (ManagementObject) obj;
                    v.Get();
                    Item.GPU += (string) v["Name"];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void insertPrinters()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("Select * from Win32_Printer");
                insertInfo(searcher, "Win32_Printer");

                Item.printers = new string[searcher.Get().Count];
                Item.printerIP = new string[searcher.Get().Count];
                var i = 0;
                foreach (var obj in searcher.Get())
                {
                    var v = (ManagementObject) obj;
                    v.Get();
                    Item.printers[i] = (string) v["Name"];
                    Item.printerIP[i] = (string) v["PortName"];
                    i++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void insertLocalUserAccounts()
        {
            try
            {
                var query = new SelectQuery("Win32_UserProfile");
                var searcher = new ManagementObjectSearcher(query);
                insertInfo(searcher, "Win32_UserProfile");
                Item.users = new List<string>();
                foreach (var o in searcher.Get())
                {
                    var sid = (ManagementObject) o;
                    StringBuilder name = new StringBuilder();
                    uint cchName = (uint) name.Capacity;
                    StringBuilder referencedDomainName = new StringBuilder();
                    uint cchReferencedDomainName = (uint) referencedDomainName.Capacity;
                    SID_NAME_USE sidUse;
                    SecurityIdentifier sec = new SecurityIdentifier(sid["SID"].ToString());
                    byte[] sidb = new byte[sec.BinaryLength];
                    sec.GetBinaryForm(sidb, 0);
                    if (LookupAccountSid(null, sidb, name, ref cchName, referencedDomainName,
                        ref cchReferencedDomainName, out sidUse))
                    {
                        string user = (referencedDomainName.ToString() + "\\" + name.ToString());
                        Item.users.Add(user);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private string getAccountInfo(string SID)
        {
            try
            {
                var query = "select * from Win32_UserAccount where SID like '" + SID + "'";

                var searcher = new ManagementObjectSearcher(query);
                foreach (var o in searcher.Get())
                {
                    var sid = (ManagementObject) o;
                    return (string) sid["Domain"] + "//" + sid["Name"];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return "";
        }


        private void insertOSInfo()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("Select * from Win32_OperatingSystem");
                insertInfo(searcher, "Win32_OperatingSystem");
                foreach (var obj in searcher.Get())
                {
                    var v = (ManagementObject) obj;
                    v.Get();
                    Item.OS = (string) v["Caption"];
                    var arch = (string) v["OSArchitecture"];
                    Item.issixtyforbit = arch.Trim().ToLower().Equals("64-bit") ? true : false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void insertInfo(ManagementObjectSearcher searcher, string Key)
        {
            try
            {
                var i = 0;
                foreach (var o in searcher.Get())
                {
                    var share = (ManagementObject) o;
                    var propertyName = Key.Equals("Win32_UserProfile") ? "SID" : "Name";
                    Data.Add(Key + " " + i + "\r\n" + share[propertyName], new List<string>());
                    var item = Data[Key + " " + i + "\r\n" + share[propertyName]];
                    i++;
                    if (true)
                        foreach (var PC in share.Properties)
                            if (PC.Value != null && PC.Value.ToString() != "")
                                switch (PC.Value.GetType().ToString())
                                {
                                    case "System.String[]":
                                        var str = (string[]) PC.Value;

                                        var str2 = "";
                                        foreach (var st in str)
                                            str2 += st + " ";

                                        item.Add(PC.Name + " : " + str2);

                                        break;
                                    case "System.UInt16[]":
                                        var shortData = (ushort[]) PC.Value;


                                        var tstr2 = "";
                                        foreach (var st in shortData)
                                            tstr2 += st + " ";

                                        item.Add(PC.Name + " : " + tstr2);

                                        break;

                                    default:
                                        item.Add(PC.Name + " : " + PC.Value);
                                        break;
                                }
                            else
                                item.Add(PC.Name + " : No Information available");
                }
            }

            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }
        }

        /// <summary>
        ///     Method for returning general info about the Computer
        ///     insertInfo() must have inserted the info before running this method
        /// </summary>
        /// <returns>string wiht general info</returns>
        public string getInfo()
        {
            var ret = "Computer Name : " + Item.Name +
                      "\r\nOS : " + Item.OS +
                      "\r\nDNS Host Name : " + Item.DNSHostName +
                      "\r\nPart of a Domain : " + Item.PartOfDomain +
                      "\r\nDomain : " + Item.Domain +
                      "\r\nUser Name : " + Item.UserName +
                      "\r\nManufacturer : " + Item.Manufacturer +
                      "\r\nModel : " + Item.Model +
                      "\r\nSerialNumber : " + Item.SerialNumber +
                      "\r\nCPU : " + Item.CPU +
                      "\r\nGPU : " + Item.GPU +
                      "\r\nDisks : " + Item.Disks[0] +
                      "\r\nMemory : " + Item.TotalPhysicalMemory / 1048576 / 1000 + "GB" +
                      "\r\nTPM Active : " + Item.tpmActive +
                      "\r\nTPM Enabled : " + Item.tpmEnabled +
                      "\r\nUserAccounts";
            foreach (var user in Item.users)
                ret += "\r\n\t " + user;
            ret += "\r\nPrinters";
            foreach (var printer in Item.printers)
                ret += "\r\n\t " + printer;
            ret += "\r\nAssociated IP's";
            var ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            foreach (var address in ip)
                ret += "\r\n\t" + address;
            return ret;
        }

        public string getDetailedInfo()
        {
            var ret = "\r\n\r\n";
            foreach (var key in Data.Keys)
            {
                ret += key + "\r\n";

                foreach (var item in Data[key])
                    ret += "\t" + item + "\r\n";
                ret += "\r\n\r\n";
            }
            return ret;
        }
    }
}