﻿using System;
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
	    *Någon form av timer för att kontinuellt hämta data från internet
        *Lägga in Flush mekanik i databas saken
            *med AutoFlash
        *Impenter koppling till databas och möjlighet att lagra och hämta information
        *Skapa en config plats/class
	    *Fixa så att alla krashar lagras på ett bra sätt
        *Separat Thread för hämtning så Form Threaded inte fastnar?
        
        
        HögerKlick propties i notifyIcon, typ som Avsluta, och kanske info om programmet håller på att hämta just då
            *Pausa kanske?
            Info
        Konventerar över allt från Text filer till databasen
	    */

        
        

        public Form1(ref Logger log)
        {
            this.log = log;
            worker = new Worker();
            InitializeComponent();
            initiNotifyIconSub();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            notifyIcon1.Dispose();
            worker.Dispose();
        }


        #region NotificationIcon

        private MenuItem menuItemPause;
        private ContextMenu contextMenu1;
        private MenuItem menuItemClose;
        private NotifyIcon notifyIcon1;

        private void initiNotifyIconSub()
        {
            menuItemPause = new MenuItem();
            contextMenu1 = new ContextMenu();
            menuItemClose = new MenuItem();
            notifyIcon1 = new NotifyIcon();

            contextMenu1.MenuItems.Add(menuItemPause);
            contextMenu1.MenuItems.Add(menuItemClose);

            menuItemPause.Text = "Pause";
            menuItemPause.Click += menuItemPause_Click;

            menuItemClose.Text = "Exit";
            menuItemClose.Click += menuItemClose_Click;

            notifyIcon1.Icon = Icon.ExtractAssociatedIcon(@"C:\Program Files (x86)\Mozilla Firefox\firefox.exe");
            notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;
            notifyIcon1.Text = "GymnasieArbete";
            notifyIcon1.ContextMenu = contextMenu1;
        }
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
                notifyIcon1.ShowBalloonTip(1000);
                this.Hide();
            }
            else if (this.WindowState == FormWindowState.Normal)
            {
                notifyIcon1.Visible = false;
            }
        }
        private void menuItemClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void menuItemPause_Click(object sender, EventArgs e)
        {
            if (menuItemPause.Text[0] == 'P')
            {
                worker.Running = false;
                menuItemPause.Text = "Activate";
            }
            else if (menuItemPause.Text[0] == 'A')
            {
                worker.Running = true;
                menuItemPause.Text = "Pause";
            }
        }

        #endregion
    }
    
    public class Worker : IDisposable
    {
        private DataTable name_table;
        private int dotaTableCount;
        private string dbConString;
        private DatabaseConncter dbConnector;
        private System.Threading.Timer timer;
        private bool active = true;


        public bool Running
        {
            get { return active; }
            set 
            {
                if (value)
                    timer.Change(300000, Timeout.Infinite);
                active = value; 
            }
        }
        public Worker()
        {
            dbConString = @"Data Source=" + Config.databasePATH + @";Version=3;";
            dbConnector = new DatabaseConncter(dbConString);
            dotaTableCount = dbConnector.getTableCount("dota_matches");
            name_table = dbConnector.ExecuteQuery("SELECT * FROM name_table");
            timer = new System.Threading.Timer(TimerTick, null, 5000, Timeout.Infinite);
        }
        public void Dispose()
        {
            dbConnector.Dispose();
            timer.Dispose();
            name_table.Dispose();
        }


        public void TimerTick(object state)
        {
            // WORK
            MainFunc(@"http://dota2lounge.com/");

            if (active)
                timer.Change(300000, Timeout.Infinite);//5 min
        }
        private void MainFunc(string path)
        {
            Dictionary<string, string> data;
            foreach (var item in GatherData.MainGather(path))
            {
                data = new Dictionary<string, string>();
                data.Add("id", dotaTableCount.ToString());
                data.Add("match_id", item.matchID.ToString());
                data.Add("opponent1_procent", item.opp1Procent.ToString());
                data.Add("opponent2_procent", item.opp2Procent.ToString());
                data.Add("match_count", item.matchCount.ToString());
                data.Add("when_taken", Config.DateTimeToUnixTimestamp(item.timeWhenDataTaken).ToString());
                data.Add("winner", item.winner.ToString());

                data.Add("comment", item.comment);
                data.Add("ago", item.ago);
                data.Add("time", item.time);

                data.Add("tournament", findName(item.tournament).ToString());
                data.Add("opponent1", findName(item.opp1).ToString());
                data.Add("opponent2", findName(item.opp2).ToString());
                

                InsertToDatabase("dota_matches", data);
                dotaTableCount++;
            }
        }

        public int findName(string name)
        {
            foreach (DataRow row in name_table.Rows)
            {
                if (name == row.ItemArray[1].ToString())
                    return Convert.ToInt32(row.ItemArray[0]);
            }
            name_table.Rows.Add(name_table.Rows.Count, name);
            InsertToDatabase("name_table", new Dictionary<string, string>() { { "id", (name_table.Rows.Count - 1).ToString() }, { "name", name } });
            return (name_table.Rows.Count - 1);
        }

        private void InsertToDatabase(string tableName, Dictionary<string, string> data)
        {
            string var1 = "INSERT INTO " + tableName + "(";
            for (int i = 0; i < data.Count; i++)
            {
                if (i == data.Count - 1)
                    var1 += data.ElementAt(i).Key;
                else
                    var1 += data.ElementAt(i).Key + ",";
            }
            var1 += ")VALUES(";
            for (int i = 0; i < data.Count; i++)
            {
                if (i == data.Count - 1)
                    var1 += '"' + data.ElementAt(i).Value + '"';
                else
                    var1 += '"' + data.ElementAt(i).Value + "\",";
            }
            var1 += ");";

            dbConnector.ExecuteNonQuery(var1);
        }
    }
}