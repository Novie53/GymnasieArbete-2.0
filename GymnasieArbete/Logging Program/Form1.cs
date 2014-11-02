using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Logging_Program
{
    public partial class Form1 : Form
    {
        private readonly int minTime = 2, maxTime = 5;
        private readonly string databasePATH = @"C:\Users\Novie\Desktop\GymnaArbete\mainDatabase.db";
        private DatabaseConncter dbConnector;
        private int dotaMatchesCount = 0;

        /*
        *Ha Programmet i System Tray
	        Någon form av timer för att kontinuellt hämta data från internet
	        Impenter koppling till databas och möjlighet att lagra och hämta information
	        Spara Functionerna som skriver till text fil som backup
	        Konventerar över allt från Text filer till databasen
            Skapa en config plats/class
	        Fixa så att alla krashar lagras på ett bra sätt
	
        Separat Thread för hämtning så Form Threaded inte fastnar?
        */
        


        public Form1()
        {
            InitializeComponent();

            timer.Tick += timer_Tick;
            timer.Interval = 50;
            timer.Start();

            dbConnector = new DatabaseConncter(@"Data Source=" + databasePATH + @";Version=3;");
            dotaMatchesCount = dbConnector.getTableCount("dota_matches");

            notifyIcon1.Icon = Icon.ExtractAssociatedIcon(@"C:\Program Files (x86)\Mozilla Firefox\firefox.exe");
            notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;

            List<MainLib.UniqueMatch> list = MainLib.GatherData.grabInfoFromWeb(@"http://dota2lounge.com");
            foreach (var item in list)
            {
                item.SaveToLoc(@"D:\DotaData\");
                InsertToDatabase("dota_matches", new Dictionary<string, string>()
                    {
                        {"id",dotaMatchesCount.ToString()},
                        {"match_id",item.MatchID.ToString()},
                        //{"tournament","'" + item.Tournament + "'"},
                        //{"opponent1","'" + item.Opp1 + "'"},
                        //{"opponent2","'" + item.Opp2 + "'"},
                        //{"opponent1_procent",item.Opp1Procent.ToString()},
                        //{"opponent2_procent",item.Opp2Procent.ToString()},
                        //{"comment","'" + item.Comment + "'"},
                        //{"match_count",item.MatchCount.ToString()},
                        //{"winner",item.Winner.ToString()},
                        //{"people_betting",item.AmountOfPeopleBetting.ToString()},
                        //{"items_betting",item.AmountOfItemsBetted.ToString()},
                        //{"ago","'" + item.Ago + "'"},
                        //{"time","'" + item.Time + "'"},
                        //{"when_taken",DateTimeToUnixTimestamp(item.TimeWhenDataTaken).ToString()}
                    });
                dotaMatchesCount++;
            }
        }

        private int DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (int)(dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }
        private void InsertToDatabase(string tableName, Dictionary<string,string> data)
        {
            string var1 = "INSERT INTO " + tableName + " (";
            for (int i = 0; i < data.Count; i++)
            {
                if (i == data.Count - 1)
                    var1 += data.ElementAt(i).Key;
                else
                    var1 += data.ElementAt(i).Key + ",";
            }
            var1 += ") VALUES (";
            for (int i = 0; i < data.Count; i++)
            {
                if (i == data.Count - 1)
                    var1 += data.ElementAt(i).Value;
                else
                    var1 += data.ElementAt(i).Value + ",";
            }
            var1 += ");";

            dbConnector.ExecuteNonQuery(var1);
        }
        

        #region Timer

        private DateTime dateTime = new DateTime();
        private Random rand = new Random();
        private Timer timer = new Timer();
        void timer_Tick(object sender, EventArgs e)
        {
            if (dateTime.CompareTo(DateTime.Now) <= 0)
            {
                dateTime = DateTime.Now.AddMinutes(rand.Next(minTime, maxTime + 1));
            }
            if (this.WindowState == FormWindowState.Minimized)
                notifyIcon1.Text = dateTime.Subtract(DateTime.Now).ToString(@"mm\:ss");
            else if (this.WindowState == FormWindowState.Normal)
                label1.Text = dateTime.Subtract(DateTime.Now).ToString(@"mm\:ss");
        }

        #endregion
        #region NotificationIcon

        private NotifyIcon notifyIcon1 = new NotifyIcon();
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            notifyIcon1.BalloonTipTitle = "Minimize to Tray App";
            notifyIcon1.BalloonTipText = "You have successfully minimized your form.";

            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(5000);
                this.Hide();
            }
            else if (this.WindowState == FormWindowState.Normal)
            {
                notifyIcon1.Visible = false;
            }
        }

        #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            notifyIcon1.Dispose();
            dbConnector.Dispose();
            timer.Dispose();
        }
        
    }
}



//        public void AddToDataSet(MainLib.UniqueMatch match)
//        {


//            dataSet.Tables[0].Rows.Add(row);
//        }
//        public void updateDataBase()
//        {
//            try
//            {
//                objConnect.UpdateDatabase(dataSet);
//                MessageBox.Show("Database updated");
//            }
//            catch (Exception err)
//            {
//                MessageBox.Show(err.Message);
//            }
//        }

//        private void Form1_Load(object sender, EventArgs e)
//        {
//            try
//            {
//                objConnect = new ClassLibrary.DataBaseConnection();
//                conString = Properties.Settings.Default.DataBaseConnectionString;
//                objConnect.ConnectionString = conString;
//                objConnect.Sql = @"SELECT * FROM MatchesTable";
//                dataSet = objConnect.GetConnection;
//            }
//            catch (Exception err)
//            {
//                MessageBox.Show(err.Message);
//            }
//            dataGridView1.DataSource = dataSet.Tables[0];
//        }
//        private void buttonGatherData_Click(object sender, EventArgs e)
//        {
//            List<MainLib.UniqueMatch> list = MainLib.GatherData.grabInfoFromWeb(@"http://dota2lounge.com");
//            foreach (var item in list)
//            {
//                AddToDataSet(item);
//            }
//            updateDataBase();
//            dataGridView1.DataSource = dataSet.Tables[0];
//            //list = MainLib.GatherData.grabInfoFromWeb(@"http://csgolounge.com");
//            //foreach (var item in list)
//            //    item.saveToLoc(@"D:\CsData\");
//        }
//        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
//        {
//            dataSet.Dispose();
//            objConnect.Dispose();
//        }
//    }