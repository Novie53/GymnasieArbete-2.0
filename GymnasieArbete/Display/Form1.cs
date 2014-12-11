using System;
using Logging_Program;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Display
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            string dbConString = @"Data Source=" + Config.databasePATH + @";Version=3;";
            using (DatabaseConncter dbConnector = new DatabaseConncter(dbConString))
            {
                //Console.WriteLine(dbConnector.getTableCount("dota_matches"));
                dataGridView1.DataSource = dbConnector.ExecuteQuery(@"SELECT * FROM dota_matches");
            }
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            dataGridView1.Width = this.Width-80;
            dataGridView1.Height = this.Height-100;
        }
    }
}
