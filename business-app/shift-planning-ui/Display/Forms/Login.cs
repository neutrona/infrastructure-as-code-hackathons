using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace shift.ui.architect.display.login
{
    public partial class Login : Form
    {

        public string Username { get { return this.textBoxUsername.Text; } }
        public string Password { get { return this.textBoxPassword.Text; } }

        public string ConfigFile { get { return this.comboBoxConfig.SelectedItem.ToString(); } }

        public Login()
        {
            InitializeComponent();
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Login_Load(object sender, EventArgs e)
        {
            DirectoryInfo dir_info = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            
            comboBoxConfig.Items.AddRange(dir_info.EnumerateFiles("config_*.json").Select(i => i.Name).ToArray());
            comboBoxConfig.SelectedIndex = 0;
        }
    }
}
