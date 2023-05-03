using System.Net;
using System.Windows.Forms;

namespace IpScanner
{
    public partial class Form1 : Form
    {
        private NetworkAnalyzer _analyzer;

        private List<string> _adapters;

        public Form1()
        {
            InitializeComponent();

            _analyzer = new();
        }

        private void UpdateAdapters()
        {
            try
            {
                _adapters = textBox1.Text.Split(", ").ToList();
            }
            catch (Exception)
            {

            }
        }

        public void ScanButtonClick(object sender, EventArgs e)
        {
            flowLayoutPanel1.Controls.Clear();

            UpdateAdapters();

            List<DeviceData>  data = _analyzer.ScanNetwork(_adapters);

            foreach (DeviceData device in data)
            {
                AddSearchResult(device);
            }
        }

        public void GetAdaptersButtonClick(object sender, EventArgs e)
        {
            flowLayoutPanel1.Controls.Clear();

            _adapters = _analyzer.GetActiveAdapters();

            textBox1.Text = _adapters.Aggregate((a, b) => a + ", " + b);
        }

        private void AddSearchResult(DeviceData device)
        {
            flowLayoutPanel1.Controls.Add(new Label()
            {
                Text = device.Adapter,
                Width = flowLayoutPanel1.Width / 5,
                Height = 30,
                BackColor = SystemColors.ControlLight,
            });

            flowLayoutPanel1.Controls.Add(new Label()
            {
                Text = device.Name,
                Width = flowLayoutPanel1.Width / 5,
                Height = 30,
                BackColor = SystemColors.ControlLight,
            });

            flowLayoutPanel1.Controls.Add(new Label()
            {
                Text = device.Address,
                Width = flowLayoutPanel1.Width / 5,
                Height = 30,
                BackColor = SystemColors.ControlLight,
            });

            flowLayoutPanel1.Controls.Add(new Label()
            {
                Text = device.Mac,
                Width = flowLayoutPanel1.Width / 5,
                Height = 30,
                BackColor = SystemColors.ControlLight,
            });
        }
    }
}
