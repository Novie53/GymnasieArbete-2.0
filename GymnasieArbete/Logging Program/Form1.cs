using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Logging_Program
{
    public partial class Form1 : Form
    {
        private readonly int minTime = 2, maxTime = 5;
        private readonly string databasePATH = @"C:\Users\Novie\Desktop\sqlite\test.db";
        private DatabaseConncter dbConnector;

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

            notifyIcon1.Icon = Icon.ExtractAssociatedIcon(@"C:\Program Files (x86)\Mozilla Firefox\firefox.exe");
            notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;
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
//            DataRow row = dataSet.Tables[0].NewRow();
//            row[0] = dataSet.Tables[0].Rows.Count;
//            row[1] = match.MatchID;
//            row[2] = match.Tournament;
//            row[3] = match.Opp1;
//            row[4] = match.Opp2;
//            row[5] = match.Opp1Procent;
//            row[6] = match.Opp2Procent;
//            row[7] = match.Comment;
//            row[8] = match.MatchCount;
//            row[9] = match.Winner;
//            row[10] = match.AmountOfPeopleBetting;
//            row[11] = match.AmountOfItemsBetted;
//            row[12] = match.Ago;
//            row[13] = match.Time;
//            row[14] = match.TimeWhenDataTaken;

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