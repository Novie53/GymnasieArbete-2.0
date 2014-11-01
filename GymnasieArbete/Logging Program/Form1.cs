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
        private Random rand = new Random();
        private DateTime dateTime = new DateTime();

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

            notifyIcon1.Icon = Icon.ExtractAssociatedIcon(@"C:\Program Files (x86)\Mozilla Firefox\firefox.exe");
            notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;
        }



        Timer timer = new Timer();
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
            //Console.WriteLine(dateTime.CompareTo(DateTime.Now));
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
}
