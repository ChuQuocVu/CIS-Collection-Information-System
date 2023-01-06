using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using Microsoft.Office.Interop.Excel;
using System.Data.SqlClient;


namespace GetInformation
{
    public partial class Form1 : Form
    {

        #region Declare the variables to be used in this program

        // Tạo đối tượng kết nối
        SqlConnection sqlcon = null;

        // Khởi tạo list Student (bao gồm các class Student)
        public List<Student> listStudent;
        public Dictionary<string, string> classInfo;

        SqlDataAdapter adapter = new SqlDataAdapter();


        // Khởi tạo dataTable (datagridview)
        private System.Data.DataTable dataTable = new System.Data.DataTable();

        // Số lượng port COM đang khả dụng 
        int lenCom = 0;
        #endregion

        public Form1()
        {
            InitializeComponent();
            //loadDataGridView();
            string[] baudRate = { "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200" };
            comboBoxBaudRate.Items.AddRange(baudRate);
            string[] databits = { "6", "7", "8" };
            comboBoxDataBit.Items.AddRange(databits);
            string[] paritybits = { "None", "Odd", "Even" };
            comboBoxParityBit.Items.AddRange(paritybits);
            string[] stopbits = { "1", "1.5", "2" };
            comboBoxStopBit.Items.AddRange(stopbits);
        }

        #region Button Click Events

        // Connect to SQL Server 
        private void buttondatabase_Click(object sender, EventArgs e)
        {
            // Tạo chuỗi kết nối
            string strcon = String.Format(@"Data Source={0};Initial Catalog={1};Integrated Security=True", textBoxServer.Text, textBoxDatabase.Text);

            try
            {

                if (sqlcon == null) sqlcon = new SqlConnection(strcon);


                if (sqlcon.State == ConnectionState.Closed)
                {
                    sqlcon.Open();
                    MessageBox.Show("       Connect Sucessfull !");
                    buttondatabase.ForeColor = Color.Red;
                    buttondatabase.Text = "Disconnect to Database";
                }
                else if (sqlcon != null && sqlcon.State == ConnectionState.Open)
                {
                    sqlcon.Close();
                    MessageBox.Show("       Disconnected !");
                    buttondatabase.ForeColor = Color.LimeGreen;
                    buttondatabase.Text = "Connect to Database";
                    sqlcon = null;
                }

                SqlCommand sqlCmd = new SqlCommand();
                sqlCmd.CommandType = CommandType.Text;
                sqlCmd.CommandText = String.Format(@"SELECT * FROM Class_Information");
                sqlCmd.Connection = sqlcon;

                SqlDataReader reader = sqlCmd.ExecuteReader();

                classInfo = new Dictionary<string, string>();

                // Truyền dữ liệu từ SQL vào List Class
                while (reader.Read())
                {
                    classInfo.Add(reader.GetString(2).Trim(), reader.GetString(0).Trim());
                }
                reader.Close();

            }
            catch (Exception ex)
            {
                sqlcon = null;
                MessageBox.Show(ex.Message);
                
            }
        }

        // Nút bấm xóa dữ liệu trong datagridview 
        private void buttonDelete_Click(object sender, EventArgs e)
        {
            SqlCommand sqlCmd = new SqlCommand("DELETE FROM "+ textBoxTable.Text +" WHERE Class_ID = '"+ textBoxID.Text +"'", sqlcon);
            sqlCmd.ExecuteNonQuery();
            loadData();
            textBoxID.Text = textBoxName.Text = "";
        }

        // Nút bấm kết nối UART
        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (labelStatus.Text == "Disconnected")
                {
                    Com.PortName = comboBoxSecCom.Text;
                    Com.Open();
                    Com.DiscardInBuffer();
                    buttonConnect.ForeColor = Color.Red;
                    labelStatus.Text = "Connected";
                    labelStatus.ForeColor = Color.LimeGreen;
                    buttonConnect.Text = "Disconnect";
                }
                else
                {
                    Com.Close();
                    buttonConnect.ForeColor = Color.Lime;
                    labelStatus.Text = "Disconnected";
                    labelStatus.ForeColor = Color.Red;
                    buttonConnect.Text = "Connect";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void buttonCreate_Click(object sender, EventArgs e)
        {

            try
            {
                // Code tạo đối tượng thực thi truy vấn
                SqlCommand sqlCmd = new SqlCommand("CREATE TABLE " + textBoxTable.Text + " (Student_Name NVARCHAR(50), Class_Name NVARCHAR(50), Class_ID NCHAR(10))", sqlcon);
                sqlCmd.ExecuteNonQuery();

                MessageBox.Show("Success!");
            }
            catch (Exception ex)
            {
                if (ex.Message == "There is already an object named '" + textBoxTable.Text + "' in the database.")
                {
                    MessageBox.Show("Table already exists! Using '" + textBoxTable.Text + "' table.");
                }
                else MessageBox.Show(ex.Message);
            }
        }

        private void buttonCheck_Click(object sender, EventArgs e)
        {
            try
            {
                loadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void buttonInput_Click(object sender, EventArgs e)
        {
            if (textBoxName.Text != "" || textBoxID.Text != "")
            {
                if (classInfo.ContainsKey(textBoxID.Text))
                {
                    if (classInfo[textBoxID.Text] == txt_StudentName.Text)
                    {
                        AutoClosingMessageBox.Show("This tag has been used by this student!", "", 2000);
                    }
                    else
                    {
                        DialogResult dialogResult = MessageBox.Show($"This tag is already in used by {txt_StudentName.Text}! Do you want to replace?", 
                                                                     "", MessageBoxButtons.YesNo);
                        if (dialogResult == DialogResult.Yes)
                        {
                            try
                            {
                                SqlCommand sqlCmd = new SqlCommand($"INSERT INTO {textBoxTable.Text} (Student_Name, Class_Name, Class_ID) VALUES ('{txt_StudentName.Text}', '{textBoxName.Text}', '{textBoxID.Text}')", sqlcon);
                                sqlCmd.ExecuteNonQuery();

                                // Đưa dữ liệu lên dataGridView             
                                dataGridViewData.Invoke(new System.Action(() =>
                                {
                                    loadData();
                                    dataGridViewData.FirstDisplayedScrollingRowIndex = dataGridViewData.RowCount - 1;
                                }));
                                textBoxName.Text = textBoxID.Text = "";

                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
                        else if (dialogResult == DialogResult.No)
                        {
                            textBoxName.Text = "";
                        }
                    }
                }
                else if (classInfo.ContainsValue(txt_StudentName.Text))
                {
                    AutoClosingMessageBox.Show("This tag already exists in the database!", "", 2000);
                }
                else
                {
                    try
                    {
                        SqlCommand sqlCmd = new SqlCommand($"INSERT INTO {textBoxTable.Text} (Student_Name, Class_Name, Class_ID) VALUES ('{txt_StudentName.Text}', '{textBoxName.Text}', '{textBoxID.Text}')", sqlcon);
                        sqlCmd.ExecuteNonQuery();

                        // Đưa dữ liệu lên dataGridView             
                        dataGridViewData.Invoke(new System.Action(() =>
                        {
                            loadData();
                            dataGridViewData.FirstDisplayedScrollingRowIndex = dataGridViewData.RowCount - 1;
                        }));
                        textBoxName.Text = textBoxID.Text = "";

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            else MessageBox.Show("Incomplete information!");
        }

        private void dataGridViewData_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            int i = dataGridViewData.CurrentRow.Index;

            txt_StudentName.Text = dataGridViewData.Rows[i].Cells[0].Value.ToString();
            textBoxName.Text = dataGridViewData.Rows[i].Cells[1].Value.ToString();
            textBoxID.Text = dataGridViewData.Rows[i].Cells[2].Value.ToString();
        }

        // Nút bấm lưu file excel

        #endregion

        #region Setup UART Box and SQL Server Box

        // Port khả dụng trên PC
        private void timer1_Tick(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames(); // Lấy tất cả các COM đang khả dụng trên PC
            if (lenCom != ports.Length)
            {
                lenCom = ports.Length;
                comboBoxSecCom.Items.Clear();
                for (int i = 0; i < lenCom; i++)
                {
                    comboBoxSecCom.Items.Add(ports[i]);
                }
                comboBoxSecCom.Text = ports[0];
            }
        }

        // Code chọn Baud Rate từ comboBox
        private void comboBoxBaudRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Com.IsOpen) Com.Close();
            Com.BaudRate = Convert.ToInt32(comboBoxBaudRate.Text);
        }

        // Code chọn số bit data từ comboBox
        private void comboBoxDataBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Com.IsOpen) Com.Close();
            Com.DataBits = Convert.ToInt32(comboBoxDataBit.Text);
        }

        // Code chọn Parity bit từ comboBox
        private void comboBoxParityBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Com.IsOpen) Com.Close();
            switch (comboBoxParityBit.SelectedItem.ToString())
            {
                case "Odd":
                    Com.Parity = Parity.Odd;
                    break;
                case "Even":
                    Com.Parity = Parity.Even;
                    break;
                case "None":
                    Com.Parity = Parity.None;
                    break;
            }
        }

        // Code chọn Stop bit từ comboBox
        private void comboBoxStopBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Com.IsOpen) Com.Close();
            switch (comboBoxStopBit.SelectedItem.ToString())
            {
                case "1":
                    Com.StopBits = StopBits.One;
                    break;
                case "1.5":
                    Com.StopBits = StopBits.OnePointFive;
                    break;
                case "2":
                    Com.StopBits = StopBits.Two;
                    break;
            }
        }

        /*
         Cài đặt giá trị mặc định của các comboBox và textBox khi mở ứng dụng.
          
         baudRate = { "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200" };
         databits = { "6", "7", "8" };
         paritybits = { "None", "Odd", "Even" };
         stopbits = { "1", "1.5", "2" };
         */

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBoxBaudRate.SelectedIndex = 7;
            comboBoxDataBit.SelectedIndex = 2;
            comboBoxParityBit.SelectedIndex = 0;
            comboBoxStopBit.SelectedIndex = 0;
            textBoxServer.Text = @"ADMIN-PC"; // Tên SQL server
            textBoxDatabase.Text = "LnD_DataBase"; // Tên Database sử dụng
            textBoxTable.Text = "Class_Infomation";
        }
        #endregion

        #region Push data from MCU to PC via UART
        private void OnCom(object sender, SerialDataReceivedEventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;

            // Read 1 byte will interrupt COM -> Send Data from MCU to PC !
            string iD;



            iD = Com.ReadExisting(); // Read ID from Card
            Display(iD);
        }


        // Đoạn code sau để gửi 1 chuỗi data từ MCU qua cổng COM

        private delegate void dldisplay(string s);
        private void Display(string s)
        {
            if (textBoxID.InvokeRequired)
            {
                dldisplay sd = new dldisplay(Display);
                textBoxID.Invoke(sd, new object[] { s });
            }
            else
            {
                textBoxID.Text = s;
            }
        }
        #endregion 

        private void loadData()
        {
            SqlCommand sqlCmd = new SqlCommand("SELECT * FROM "+ textBoxTable.Text +"", sqlcon);
            adapter.SelectCommand = sqlCmd;
            dataTable.Clear();
            adapter.Fill(dataTable);
            dataGridViewData.DataSource = dataTable;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) buttonInput.PerformClick();
        }
    }


    public class Student
    {
        public string Name { get; set; }
        public string ID { get; set; }

        public Student(string name, string id)
        {
            Name = name;
            ID = id;
        }
    }

    public class AutoClosingMessageBox
    {
        System.Threading.Timer _timeoutTimer;
        string _caption;
        AutoClosingMessageBox(string text, string caption, int timeout)
        {
            _caption = caption;
            _timeoutTimer = new System.Threading.Timer(OnTimerElapsed,
                null, timeout, System.Threading.Timeout.Infinite);
            using (_timeoutTimer)
                MessageBox.Show(text, caption);
        }
        public static void Show(string text, string caption, int timeout)
        {
            new AutoClosingMessageBox(text, caption, timeout);
        }
        void OnTimerElapsed(object state)
        {
            IntPtr mbWnd = FindWindow("#32770", _caption); // lpClassName is #32770 for MessageBox
            if (mbWnd != IntPtr.Zero)
                SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _timeoutTimer.Dispose();
        }
        const int WM_CLOSE = 0x0010;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }
}
