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
using System.Text.RegularExpressions;

namespace Display
{
    public partial class Form1 : Form
    {
        DataTable dota_matches;
        DataTable name_table = new DataTable();
        DatabaseConncter newDBConnector = new DatabaseConncter
            (@"Data Source=C:\Users\Novie\Desktop\GymnaArbete\NewTest\Main.db;Version=3;");
        DatabaseConncter oldDBConnector = new DatabaseConncter
            (@"Data Source=" + Config.databasePATH + @";Version=3;");

        public Form1()
        {
            InitializeComponent();
            dota_matches = oldDBConnector.ExecuteQuery("SELECT * FROM dota_matches");
            name_table = newDBConnector.ExecuteQuery("SELECT * FROM name_table");
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            dataGridView1.Width = this.Width - 80;
            dataGridView1.Height = this.Height - 100;
        }
        //public void convertToNew()
        //{
        //    Dictionary<string, string> data;

        //    foreach (DataRow row in dota_matches.Rows)
        //    {
        //        data = new Dictionary<string, string>();
        //        data.Add("id", row.ItemArray[0].ToString());
        //        data.Add("match_id", row.ItemArray[1].ToString());
        //        data.Add("opponent1_procent", row.ItemArray[5].ToString());
        //        data.Add("opponent2_procent", row.ItemArray[6].ToString());
        //        data.Add("match_count", row.ItemArray[8].ToString());
        //        data.Add("people_betting", row.ItemArray[10].ToString());
        //        data.Add("items_betting", row.ItemArray[11].ToString());
        //        data.Add("when_taken", row.ItemArray[14].ToString());
        //        data.Add("comment", row.ItemArray[7].ToString());
        //        data.Add("ago", row.ItemArray[12].ToString());
        //        data.Add("time", row.ItemArray[13].ToString());

        //        data.Add("tournament", findName(Regex.Replace(row.ItemArray[2].ToString(), @"[^a-zA-Z0-9 ]", "")).ToString());
        //        data.Add("opponent1", findName(Regex.Replace(row.ItemArray[3].ToString(), @"[^a-zA-Z0-9 ]", "")).ToString());
        //        data.Add("opponent2", findName(Regex.Replace(row.ItemArray[4].ToString(), @"[^a-zA-Z0-9 ]", "")).ToString());
        //        data.Add("winner", findName(Regex.Replace(row.ItemArray[9].ToString(), @"[^a-zA-Z0-9 ]", "")).ToString());

        //        InsertToDatabase("dota_matches", data, newDBConnector);
        //    }
        //}
    }
}