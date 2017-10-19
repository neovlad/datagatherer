using System;
using System.Data.SqlClient;
using System.Data;

namespace datagatherer
{
    class SQLConnector
    {
        private SqlConnection conn;
        private PCInfo pcinf;
        public SQLConnector(string Data_Source, string Database, string user, string pw, ref PCInfo pcinf) {
            conn = new SqlConnection("Data Source=" + Data_Source + ";Initial Catalog=" + Database +
                                     ";user id=" + user + ";password=" + pw );
            this.pcinf = pcinf;
        }

        public bool checkDBforComputer() {
            string sn = pcinf.Item.SerialNumber;
            SqlCommand cmd = new SqlCommand
            {
                CommandText = "SELECT SerialNumber FROM Computer WHERE SerialNumber LIKE '" + sn + "'",
                CommandType = CommandType.Text,
                Connection = conn
            };


            conn.Open();
            var reader = cmd.ExecuteReader();
            
            bool ot = reader.Read();
            conn.Close();
            return ot;
        }

        public void insertComputer() {
            string sql = "insert into Computer  (SerialNumber, ComputerName, PartOfDomain, Domain, GPU," +
                         " CPU, RAM, HardDrive, TPMActive, TPMEnabled, Manufacturer, Model, OS, [64bit])" +
                         " values(@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13);";
            SystemDetails det = pcinf.Item;
            string[] prms = { det.SerialNumber, det.Name, det.PartOfDomain.ToString(), det.Domain, det.GPU, det.CPU,
                              det.TotalPhysicalMemory.ToString(), det.Disks[0], det.tpmActive.ToString(), det.tpmEnabled.ToString(),
                              det.Manufacturer, det.Model, det.OS, det.issixtyforbit.ToString()};
            insertData(sql, prms);
        }


        public void insertUsers()
        {
            string sql = "insert into [Users]  (SerialNumber, [User]) values(@0, @1);";
            SystemDetails det = pcinf.Item;
            foreach (var user in det.users) {
                string[] prms = { det.SerialNumber, user};
                insertData(sql, prms);
            }
            
            
        }
        public void insertPrinters() {
            int i = 0;
            foreach (var printer in pcinf.Item.printers)
            {
                if (Utils.isCommonPrinter(printer))
                {
                    i++;
                    continue;
                }
                string sql = "insert into Printers  (SerialNumber, PrinterName, Port) values(@0, @1, @2);";
                string[] prms = {pcinf.Item.SerialNumber, printer, pcinf.Item.printerIP[i] };
                insertData(sql, prms);
                i++;
            }
        }

        public void insertIPAddr()
        {
            var ips = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
            
            foreach (var addr in ips) {
                string sql = "insert into [IP's]  (SerialNumber, IP) values(@0, @1);";
                string[] prms = { pcinf.Item.SerialNumber, addr.ToString()};
                insertData(sql, prms);
            }
        }

        private int insertData(string query, string[] parameters) {
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Connection = conn;
                for (int i = 0; i < parameters.Length; i++) {
                    cmd.Parameters.AddWithValue("@" + i, parameters[i]);
                }
                int recordsAffected = -1;
                try
                {
                    conn.Open();
                    recordsAffected = cmd.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    // error here
                    Console.WriteLine(e);
                }
                finally
                {
                    conn.Close();
                }
                return recordsAffected;
            }
        }
        
    }
}
