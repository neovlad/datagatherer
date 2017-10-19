using System;
using System.Drawing;
using System.Security.Principal;
using System.Windows.Forms;

namespace datagatherer
{
    public partial class Form1 : Form
    {
        private PCInfo _inf;
        private readonly SaveFileDialog _saveFileDialog1;

        public Form1()
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator))
                MessageBox.Show(
                    @"Program not started with Administrator Rights. We will not be able to gather all the data!" +
                    @"Please restart Data Gatherer with Administrator rights by right clicking and saying Run as Administrator"
                    , @"Data Gatherer",
                    MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            _saveFileDialog1 = new SaveFileDialog();
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            progressBar1.Enabled = true;
            progressBar1.Increment(50);
            textBox1.AppendText("Collecting data ...\r\n");
            
            _inf = new PCInfo();
            progressBar1.Increment(39);
            _inf.insertData();
            progressBar1.Increment(33);
            textBox1.AppendText(_inf.getInfo());
            Console.WriteLine(_inf.getInfo());
            Console.WriteLine(_inf.getDetailedInfo());
            
            progressBar1.Increment(-100);
            progressBar1.Enabled = false;
            button2.Enabled = true;
            button3.Enabled = true;
            textBox1.AppendText("\r\nData Collected you can now save and/or send the Data!");
            this.Cursor = Cursors.Default;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                var conn = new SQLConnector("MDHSBBB023VLAN.medicine.washington.edu\\SQLEXPRESS", "test", "test",
                    "12345",
                    ref _inf);
                if (conn.checkDBforComputer())
                {
                    MessageBox.Show(@"Database already contains your computer.", @"Data Gatherer",
                        MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    textBox1.AppendText("\r\nData in Database!");
                    return;
                }
                conn.insertComputer();
                conn.insertPrinters();
                conn.insertUsers();
                conn.insertIPAddr();

                textBox1.AppendText("\r\nData Sent");
            }
            catch (Exception exception)
            {
                MessageBox.Show(@"Unable to send data please try again!", @"Data Gatherer",
                    MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                Console.WriteLine(exception);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var write = _inf.getInfo() + "\r\n" +
                        "\r\nDetailed Info \r\n" + _inf.getDetailedInfo();
            _saveFileDialog1.Filter = @"txt files (*.txt)|*.txt|All files (*.*)|*.*";
            _saveFileDialog1.FilterIndex = 2;
            _saveFileDialog1.RestoreDirectory = true;
            _saveFileDialog1.DefaultExt = ".txt";
            _saveFileDialog1.FileName = "HWInfo.txt";
            if (_saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // code here for user pressing OK instead of the 'X' or 'Cancel'
                var mystream = _saveFileDialog1.OpenFile();
                Utils.GenerateStreamFromString(write, mystream);
                mystream.Close();
                textBox1.AppendText("\r\nFile saved at " + _saveFileDialog1.FileName);
            }
        }
    }
}
