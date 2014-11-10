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
using System.IO;

namespace Logging_Program
{
    public partial class Form1 : Form
    {
        Worker worker;
        Logger log;

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
        


        public Form1(ref Logger log)
        {
            this.log = log;
            log.MainForm = this;
            worker = new Worker();
            InitializeComponent();


            notifyIcon1.Icon = Icon.ExtractAssociatedIcon(@"C:\Program Files (x86)\Mozilla Firefox\firefox.exe");
            notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            notifyIcon1.Dispose();
            worker.Dispose();
        }
        public void Log(string logText)
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
            MainFunc(@"http://dota2lounge.com/");
            MainFunc(@"http://csgolounge.com");

            timer.Change(300000, Timeout.Infinite);//5 min
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
                if (path.Contains("csgo"))
                {
                    item.SaveToLoc(@"D:\CsData\");
                }
                else
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
                    if (item.Winner != "" && item.Winner != null)//TODO Dubbelkolla så denna är som den ska
                        data.Add("winner", "'" + item.Winner + "'");
                    if (item.Ago != "")
                        data.Add("ago", "'" + item.Ago + "'");
                    if (item.Time != "")
                        data.Add("time", "'" + item.Time + "'");

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
        public Form1 MainForm { get; set; }

        
        public Logger(string logPath)
        {
            this.LogPath = logPath;
        }

        public void Warn(string text)
        {

        }
        public void Debug(string text)
        {

        }
        public void Error(string text)
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(Config.logPath, DateTime.Now.ToString().Replace(':', ',') + ".txt")))
            {
                writer.WriteLine("ErrorSlot - " + text);
                writer.WriteLine("Version - " + Config.version);
                writer.WriteLine("DateTime - " + DateTime.Now.ToString());
            }
        }
        public void Error(string text, Exception error)
        {
            Error(text);

            File.AppendAllText(Path.Combine(Config.logPath, "1.txt"), error.Data.ToString());
            File.AppendAllText(Path.Combine(Config.logPath, "2.txt"), error.HelpLink);
            File.AppendAllText(Path.Combine(Config.logPath, "3.txt"), error.HResult.ToString());
            File.AppendAllText(Path.Combine(Config.logPath, "4.txt"), error.Message);
            File.AppendAllText(Path.Combine(Config.logPath, "5.txt"), error.Source);
            File.AppendAllText(Path.Combine(Config.logPath, "6.txt"), error.StackTrace);
            File.AppendAllText(Path.Combine(Config.logPath, "7.txt"), error.ToString());
            File.AppendAllText(Path.Combine(Config.logPath, "8.txt"), error.TargetSite.ToString());
            File.AppendAllText(Path.Combine(Config.logPath, "9.txt"), error.InnerException.Data.ToString());
            File.AppendAllText(Path.Combine(Config.logPath, "10.txt"), error.InnerException.HelpLink);
            File.AppendAllText(Path.Combine(Config.logPath, "11.txt"), error.InnerException.HResult.ToString());
            File.AppendAllText(Path.Combine(Config.logPath, "12.txt"), error.InnerException.Message);
            File.AppendAllText(Path.Combine(Config.logPath, "13.txt"), error.InnerException.Source);
            File.AppendAllText(Path.Combine(Config.logPath, "14.txt"), error.InnerException.StackTrace);
            File.AppendAllText(Path.Combine(Config.logPath, "15.txt"), error.InnerException.ToString());
            File.AppendAllText(Path.Combine(Config.logPath, "16.txt"), error.InnerException.TargetSite.ToString());
        }
    }
}