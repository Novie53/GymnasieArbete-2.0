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
using System.Threading;

namespace Logging_Program
{
    public partial class Form1 : Form
    {
        Worker worker;
        

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

            worker = new Worker();

            notifyIcon1.Icon = Icon.ExtractAssociatedIcon(@"C:\Program Files (x86)\Mozilla Firefox\firefox.exe");
            notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            notifyIcon1.Dispose();
            worker.Dispose();
        }
        private void Log(string logText)
        {
            if (this.WindowState == FormWindowState.Minimized)
                notifyIcon1.Text = logText;
            else if (this.WindowState == FormWindowState.Normal)
                label1.Text = logText;
        }

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
    }
    public class Worker : IDisposable
    {
        private int dotaMatchesCount;
        private DatabaseConncter dbConnector;
        System.Threading.Timer timer;

        public Worker()
        {
            dbConnector = new DatabaseConncter(@"Data Source=" + Config.databasePATH + @";Version=3;");
            dotaMatchesCount = dbConnector.getTableCount("dota_matches");
            timer = new System.Threading.Timer(herpLeDerp, null, 1000, Timeout.Infinite);
        }

        public void herpLeDerp(object state)
        {
            // WORK

            timer.Change(1000, Timeout.Infinite);
        }


        public void Dispose()
        {
            dbConnector.Dispose();
            timer.Dispose();
        }
        private int DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (int)(dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }
        private void MainFunc(string path)
        {
            Dictionary<string, string> data;
            foreach (var item in GatherData.grabInfoFromWeb(path))
            {
                data = new Dictionary<string, string>();
                data.Add("id", dotaMatchesCount.ToString());
                data.Add("match_id", item.MatchID.ToString());
                data.Add("opponent1_procent", item.Opp1Procent.ToString());
                data.Add("opponent2_procent", item.Opp2Procent.ToString());
                data.Add("match_count", item.MatchCount.ToString());
                data.Add("people_betting", item.AmountOfPeopleBetting.ToString());
                data.Add("items_betting", item.AmountOfItemsBetted.ToString());
                data.Add("when_taken", DateTimeToUnixTimestamp(item.TimeWhenDataTaken).ToString());

                if (item.Tournament != "")
                    data.Add("tournament", "'" + item.Tournament + "'");
                if (item.Opp1 != "")
                    data.Add("opponent1", "'" + item.Opp1 + "'");
                if (item.Opp2 != "")
                    data.Add("opponent2", "'" + item.Opp2 + "'");
                if (item.Comment != "")
                    data.Add("comment", "'" + item.Comment + "'");
                if (item.Winner != "")
                    data.Add("winner", "'" + item.Winner + "'");
                if (item.Ago != "")
                    data.Add("ago", "'" + item.Ago + "'");
                if (item.Time != "")
                    data.Add("time", "'" + item.Time + "'");

                if (path == @"http://dota2lounge.com")
                {
                    item.SaveToLoc(@"D:\DotaData\");
                    InsertToDatabase("dota_matches", data);
                    dotaMatchesCount++;
                }
            }
        }
        private void InsertToDatabase(string tableName, Dictionary<string, string> data)
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
    }
    public class Logger
    {
        public enum LogTypes
        {
            Warning,
            Errors,
            Information
        }
        public string LogPath { get; set; }
        public string CrashReportPath { get; set; }

        public Logger(string logPath, string crashPath)
        {
            this.LogPath = logPath;
            this.CrashReportPath = crashPath;
        }

        public void Warn(string text)
        {

        }
        public void Debug(string text)
        {

        }
        public void Error(string text)
        {

        }
        public void Error(string text, Exception error)
        {

        }
    }
}