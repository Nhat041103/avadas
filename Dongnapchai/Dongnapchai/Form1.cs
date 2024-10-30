using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using S7.Net; // khai báo thư viện
using SymbolFactoryDotNet;
using System.Diagnostics;


namespace Dongnapchai
{
    public partial class Form1 : Form
    {
        Plc _Plc = new Plc(CpuType.S71200, "10.8.81.155", 0, 1); // Ip truyền vào địa chỉ Ip host, rank 0, slot là 1 

        bool OperatingStatus; // Q0.0 - Operating Status
        bool Conveyor1;       // Q0.1 - Conveyor 1
        bool Conveyor2;       // Q0.2 - Conveyor 2
        bool Pump;            // Q0.3 - Pump
        bool Valve;           // Q0.4 - Valve
        bool Cylinder1;       // Q0.5 - Cylinder 1
        bool Cylinder2;       // Q0.6 - Cylinder 2
        int setProductNumber, setTankNumber, setWaterLevel;
        int step = 5; // Khoảng cách di chuyển mỗi lần

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ConnectPLC();

            // Bắt đầu timer1 với khoảng thời gian 1 giây
            timer1.Interval = 100;
            timer1.Start();
        }

        private void ConnectPLC()
        {
            try
            {
                _Plc.Open();
                if (_Plc.Open() == ErrorCode.NoError)
                {
                    MessageBox.Show("Kết nối PLC thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Không thể kết nối PLC. Mã lỗi: " + _Plc.Open().ToString(), "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi kết nối PLC: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Hàm cập nhật trạng thái băng tải thông qua timer1
        private void timer1_Tick(object sender, EventArgs e)
        {
            byte[] readData = _Plc.ReadBytes(DataType.Output, 0, 0, 3); // Đọc 3 byte từ vùng Output

            if (readData.Length >= 3)
            {
                // Cập nhật trạng thái các biến global
                OperatingStatus = (readData[0] & (1 << 0)) != 0; // Q0.0 - Operating Status
                Conveyor1 = (readData[0] & (1 << 1)) != 0;       // Q0.1 - Conveyor 1
                Conveyor2 = (readData[0] & (1 << 2)) != 0;       // Q0.2 - Conveyor 2
                Pump = (readData[0] & (1 << 3)) != 0;            // Q0.3 - Pump
                Valve = (readData[0] & (1 << 4)) != 0;           // Q0.4 - Valve
                Cylinder1 = (readData[0] & (1 << 5)) != 0;       // Q0.5 - Cylinder 1
                Cylinder2 = (readData[0] & (1 << 6)) != 0;       // Q0.6 - Cylinder 2

                // Hiển thị trạng thái của các thiết bị
                Console.WriteLine($"Trạng thái hoạt động (Q0.0): {(OperatingStatus ? "true" : "false")}");
                Console.WriteLine($"Trạng thái băng tải 1 (Q0.1): {(Conveyor1 ? "true" : "false")}");
                Console.WriteLine($"Trạng thái băng tải 2 (Q0.2): {(Conveyor2 ? "true" : "false")}");
                Console.WriteLine($"Trạng thái bơm (Q0.3): {(Pump ? "true" : "false")}");
                Console.WriteLine($"Trạng thái van (Q0.4): {(Valve ? "true" : "false")}");
                Console.WriteLine($"Trạng thái xilanh 1 (Q0.5): {(Cylinder1 ? "true" : "false")}");
                Console.WriteLine($"Trạng thái xilanh 2 (Q0.6): {(Cylinder2 ? "true" : "false")}");

                // Cập nhật trạng thái cho DenRun
                DenRun.DiscreteValue2 = OperatingStatus;
                DenRun.DiscreteValue1 = !OperatingStatus;

                // Cập nhật trạng thái cho BT10 và BT11
                BT10.DiscreteValue2 = Conveyor1;
                BT11.DiscreteValue2 = Conveyor1;
                BT10.DiscreteValue1 = !Conveyor1;
                BT11.DiscreteValue1 = !Conveyor1;

                // Cập nhật trạng thái cho BT20 và BT21
                BT20.DiscreteValue2 = Conveyor2;
                BT21.DiscreteValue2 = Conveyor2;
                BT20.DiscreteValue1 = !Conveyor2;
                BT21.DiscreteValue1 = !Conveyor2;
                // Cập nhật trạng thái cho Bom va Van
                Bom.DiscreteValue2 = Pump;
                Van1.DiscreteValue2 = Valve;
                Van2.DiscreteValue2 = Valve;
                Bom.DiscreteValue1 = !Pump;
                Van1.DiscreteValue1 = !Valve;
                Van2.DiscreteValue2 = Valve;
                // Cập nhật trạng thái của xi lanh
                XL1ON.Visible = Cylinder1;
                XL1OFF.Visible = !Cylinder1;
                Nap1.Visible = Cylinder1;
                XL2ON.Visible = Cylinder2;
                XL2OFF.Visible = !Cylinder2;
                Nap2.Visible = Cylinder2;


                SimulateConveyor1Operation(); // Mô phỏng hoạt động băng tải 1
                SimulateConveyor2Operation(); // Mô phỏng hoạt động băng tải 2
                var valueSP = _Plc.Read("MW10");
                var valueThungHang = _Plc.Read("MW60");
                var valueLevelWater = _Plc.Read("MW40");
                // Chuyển đổi giá trị về kiểu string
                string stringValueSP = valueSP.ToString();
                string stringValueThungHang = valueThungHang.ToString();
                string stringValueLevelWater = valueLevelWater.ToString();

                txtGetProductNumber.Text = stringValueSP;
                txtGetTankNumber.Text = stringValueThungHang;
                txtGetLevelWater.Text = stringValueLevelWater;
            }
            else
            {
                Console.WriteLine("Mảng readData không đủ dữ liệu.");
            }
        }

        // Hàm mô phỏng hoạt động của băng tải 1
        private void SimulateConveyor1Operation()
        {
            Point chaiLocation = Chai.Location; // Lấy vị trí hiện tại của Chai

            // Cập nhật vị trí Chai ngay cả khi băng tải không hoạt động
            if (Conveyor1)
            {
                // Nếu băng tải 1 đang chạy, tăng tọa độ X của Chai
                chaiLocation.X += step;
            }

            // Ghi lại vị trí của Chai, kể cả khi băng tải dừng lại
 
            _Plc.Write("M2.1", chaiLocation.X == 150 ? 1 : 0);
            _Plc.Write("M2.2", chaiLocation.X == 295 ? 1 : 0);
            _Plc.Write("M2.3", chaiLocation.X == 450 ? 1 : 0);


            // Nếu Chai đã tới vị trí 450, đặt lại về vị trí ban đầu (khi băng tải đang chạy)
            if (Conveyor1 && chaiLocation.X >= 450)
            {
                chaiLocation.X = 45; // Trở về vị trí ban đầu
            }

            // Cập nhật lại vị trí mới cho Chai
            Chai.Location = chaiLocation;
        }

        // Hàm mô phỏng hoạt động của băng tải 2
        private void SimulateConveyor2Operation()
        {
            Point emptyBoxLocation = emptyBox.Location; // Lấy vị trí hiện tại của emptyBox

        
            // Cập nhật vị trí emptyBox ngay cả khi băng tải 2 không hoạt động
            if (Conveyor2)
            {
                // Nếu băng tải 2 đang chạy, tăng tọa độ X của emptyBox
                emptyBoxLocation.X += step;
                if(emptyBoxLocation.X >= 450)
                {
                    productInBox.Location = new Point(emptyBoxLocation.X + 50, emptyBoxLocation.Y + 17);
                }

            }
            int.TryParse(txtGetProductNumber.Text, out int productNumber);
            if (emptyBoxLocation.X > 450 || productNumber > 0)
            {
                productInBox.Visible = true;
            }
            else
            {
                productInBox.Visible = false;
            }
            // Ghi lại vị trí của emptyBox, kể cả khi băng tải dừng lại
            _Plc.Write("M2.4", emptyBoxLocation.X == 450 ? 1 : 0);

            if (Conveyor2 && emptyBoxLocation.X >= 835)
            {
                emptyBoxLocation.X = 285; // Trở về vị trí 285
            }

            // Cập nhật lại vị trí mới cho emptyBox
            emptyBox.Location = emptyBoxLocation;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _Plc.Write("M6.0", 1);
            _Plc.Write("M6.0", 0);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _Plc.Write("M6.2", 1);
            _Plc.Write("M6.2", 0);
        }

        private void btnAuto_Click(object sender, EventArgs e)
        {
            _Plc.Write("M6.1", 1);
            _Plc.Write("M6.1", 0);
        }

        private void btnSetValue_Click(object sender, EventArgs e)
        {
            txtSetProductNumber.Enabled = true ;
            txtSetTankNumer.Enabled =  true;
            txtSetWater.Enabled = true;
        }
        private void btnConfirm_Click(object sender, EventArgs e)
        {
            // khóa lại 
            txtSetProductNumber.Enabled = false;
            txtSetTankNumer.Enabled = false;
            txtSetWater.Enabled = false;
            // gửi  dữ liệu đi 
                
            if (int.TryParse(txtSetProductNumber.Text,out setProductNumber)&& int.TryParse(txtSetTankNumer.Text, out setTankNumber) && int.TryParse(txtSetWater.Text, out setWaterLevel))
            {
                _Plc.Write("MW20", setProductNumber); 
                _Plc.Write("MW50", setTankNumber);
                _Plc.Write("MW30", setWaterLevel);

            }
            else
            {
                // Nếu giá trị không hợp lệ, thông báo lỗi
                MessageBox.Show("Giá trị nhập vào không hợp lệ");
            }
        }
    }
}
