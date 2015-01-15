using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Logging_Program
{
    public static class Config
    {
        public const int ConnectionTries = 1000;
        public const int FailedToConnecetSleep = 10000;
        public const string version = "0.9.0";
        public const string logPath = @"F:\Data\GymnaArbete\GymLog";
        public const string databasePATH = @"F:\Data\GymnaArbete\DataBase\Main.db";
    }
    public class DatabaseConncter : IDisposable
    {
        private SQLiteConnection databaseConnection;
        private SQLiteCommand commander;
        private string conString;
        private bool autoFlush = true;
        StringBuilder flushedSQLCommands = new StringBuilder();


        public DatabaseConncter(string conString)
        {
            databaseConnection = new SQLiteConnection(conString);
            this.conString = conString;
        }

        public int getTableCount(string tableName)
        {
            return int.Parse(ExecuteQuery(@"SELECT COUNT(*) FROM " + tableName).Rows[0].ItemArray[0].ToString());
        }
        public List<string> ListTables()
        {
            Open();
            List<string> tables = new List<string>();
            DataTable asd = databaseConnection.GetSchema("Tables");
            Close();
            foreach (DataRow item in asd.Rows)
            {
                tables.Add(item[2].ToString());
            }
            return tables;
        }
        private void ExecuteNonQuery(bool ignoreFlush, string SQL)
        {
            if (ignoreFlush)
            {
                Open();
                commander.CommandText = SQL;
                commander.ExecuteNonQuery();
                Close();
            }
        }
        public void ExecuteNonQuery(string SQL)
        {
            if (autoFlush)
                ExecuteNonQuery(true, SQL);
            else
                flushedSQLCommands.Append(SQL);
        }
        public void Flush()
        {
            ExecuteNonQuery(true, flushedSQLCommands.ToString());
            flushedSQLCommands.Clear();
        }
        public DataTable ExecuteQuery(string SQL)
        {
            Open();
            commander.CommandText = SQL;
            DataTable table = new DataTable();
            using (SQLiteDataReader reader = commander.ExecuteReader())
            {
                table.Load(reader);
            }
            Close();
            return table;
        }


        private void Open()
        {
            databaseConnection.Open();
            if (databaseConnection.State != System.Data.ConnectionState.Open)
                throw new Exception("Failed to open the Database");
            commander = databaseConnection.CreateCommand();
        }
        private void Close()
        {
            databaseConnection.Close();
        }
        public bool AutoFlush
        {
            get { return autoFlush; }
            set
            {
                autoFlush = value;
                if (autoFlush)
                    Flush();
            }
        }
        public void Dispose()
        {
            databaseConnection.Close();
            databaseConnection.Dispose();
            commander.Dispose();
        }
        public string ConString
        {
            set { conString = value; }
        }
    }
    
    public static class GatherData
    {
        private static bool hasInternetConnection()
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    return new System.Net.NetworkInformation.Ping().Send("www.google.com").Status == System.Net.NetworkInformation.IPStatus.Success;
                }
                catch (Exception) { }
            }
            return false;
        }
        private static bool Derp(HttpWebResponse response)
        {
            if (response != null)
            {
                if (response.StatusCode == HttpStatusCode.OK)
                    return true;
            }
            return false;
        }
        private static HttpWebResponse tryCatchGetResponse(string link)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
            request.AllowAutoRedirect = false;
            request.Timeout = 5000;
            HttpWebResponse response = null;


            for (int i = 0; i < Config.ConnectionTries && !Derp(response); i++)
            {
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (Exception e)
                {
                    if (hasInternetConnection())
                    {
                        //TODO
                        //VarClass.writeToLog("NoResponse", e.ToString(), request.Address.ToString(), e.Data.ToString(), e.TargetSite.ToString(), e.StackTrace.ToString(), e.Source.ToString(), e.Message.ToString(), e.InnerException.ToString());
                        System.Threading.Thread.Sleep(Config.FailedToConnecetSleep);
                    }
                    else
                    {
                        //TODO
                        //VarClass.writeToLog("NoInternet", e.ToString(), request.Address.ToString());
                    }
                }
            }
            if (!Derp(response))
                throw new System.Exception("Failed to get a GOOD response from server");
            else
                return response;
        }
        public static string getHTML(string link)
        {
            HttpWebResponse response = tryCatchGetResponse(link);

            Stream receiveStream = response.GetResponseStream();
            StreamReader readStream = null;
            if (response.CharacterSet == null)
                readStream = new StreamReader(receiveStream);
            else
                readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
            string data = readStream.ReadToEnd();

            response.Dispose();
            readStream.Dispose();
            return data;
        }

        public static UniqueMatch grabInfoFromMatchPage(string matchLink)
        {
            UniqueMatch result = new UniqueMatch();

            string matchWindow = getHTML(matchLink);
            result.timeWhenDataTaken = DateTime.Now;
            matchWindow = Regex.Split(matchWindow, "<div class=\"title\">last 30 bets</div>")[0];


            #region MatchID
            {
                result.matchID = int.Parse(matchLink.Split('=')[1]);
            }
            #endregion
            #region Opponents & Winner
            {
                result.winner = "";
                string[] splits = Regex.Split(matchWindow, @"</b><br><i>");

                var matches = Regex.Matches(splits[0], "<b>");
                result.opp1 = splits[0].Substring(matches[matches.Count - 1].Index + matches[matches.Count - 1].Length);
                if (result.opp1.Contains("(win)"))
                {
                    result.opp1 = result.opp1.Remove(5);
                    result.winner = result.opp1;
                }
                
                matches = Regex.Matches(splits[1], "<b>");
                result.opp2 = splits[1].Substring(matches[matches.Count - 1].Index + matches[matches.Count - 1].Length);
                if (result.opp2.Contains("(win)"))
                {
                    result.opp2 = result.opp2.Remove(5);
                    result.winner = result.opp2;
                }

                result.opp1Procent = int.Parse(Regex.Split(splits[1], @"%</i>")[0]);
                result.opp2Procent = int.Parse(Regex.Split(splits[2], @"%</i>")[0]);
                result.opp1 = Regex.Replace(result.opp1, @"[^a-zA-Z0-9 ]", "").ToLower();
                result.opp2 = Regex.Replace(result.opp1, @"[^a-zA-Z0-9 ]", "").ToLower();
                result.winner = Regex.Replace(result.winner, @"[^a-zA-Z0-9 ]", "").ToLower();
            }
            #endregion
            #region antalMatcher
            {
                string rawData = Regex.Split(matchWindow, "28%;\">")[1];
                rawData = Regex.Split(rawData, "</div>")[0];
                rawData = Regex.Replace(rawData, "[^0-9]", ""); //Borde verkligen lära mig regex ^^
                if (rawData != "")
                    result.matchCount = int.Parse(rawData);
            }
            #endregion
            #region Date & Time
            {
                string[] var5 = Regex.Split(matchWindow, "33%;\">");
                result.ago = Regex.Split(var5[1], "</div>")[0];
                result.time = Regex.Split(var5[2], "</div>")[0];
                result.time = Regex.Replace(result.time, "[^0-9:]", "");
            }
            #endregion
            #region Betting
            {                
                string[] var5 = Regex.Split(Regex.Split(matchWindow, "full\">")[2], "</div>")[0].Trim().Split(' ');


                int tempVar;// Något bug på deras sida vilket gör att typ en gång i hundra så visar den inte amount så behöver TryParse
                int.TryParse(var5[0], out tempVar);
                result.amountOfPeopleBetting = tempVar;
                tempVar = 0;//TODO Hade glömt att noll ställa tempVar. Dubble kolla i databasen så det inte finns någon match med People och Items är de samma..
                //Extremt otroligt att det skulle hända natuligt.
                int.TryParse(var5[3], out tempVar);
                result.amountOfItemsBetted = tempVar;
            }
            #endregion

            return result;
        }
        private static UniqueMatch processMainPageInfo(string data, string mainLink)
        {
            string tour;
            int matchID;
            string comment;

            #region tournament
            {
                tour = Regex.Split(data, "eventm\">")[1];
                tour = Regex.Split(tour, "</div>")[0];
                tour = Regex.Replace(tour.Trim(), @"[^a-zA-Z0-9 ]", "").ToLower();
            }
            #endregion
            #region matchID
            {
                string rawData = Regex.Split(data, "href=\"")[1];
                rawData = Regex.Split(rawData, "\">")[0];
                matchID = int.Parse(rawData.Split('=')[1]);
            }
            #endregion
            #region comment
            {
                comment = Regex.Split(data, "#D12121\">")[1];
                comment = Regex.Split(comment, "</span>")[0];
            }
            #endregion

            UniqueMatch newInfo = grabInfoFromMatchPage(mainLink + "/match?m=" + matchID);
            newInfo.tournament = tour;
            newInfo.comment = comment;

            return newInfo;
        }
        public static List<UniqueMatch> MainGather(string mainLink)
        {
            List<UniqueMatch> list = new List<UniqueMatch>();
            string mainPageData = getHTML(mainLink);


            mainPageData = mainPageData.Substring(Regex.Matches(mainPageData, "<section class=\"box\">")[1].Index);
            mainPageData = mainPageData.Remove(Regex.Match(mainPageData, "<aside id=\"submenu\">").Index);
            string[] splits = Regex.Split(mainPageData, "<div class=\"matchmain\">");


            for (int i = 1; i < splits.Count(); i++)
            {
                if (splits[i].Contains("predict"))
                    continue;
                list.Add(processMainPageInfo(splits[i], mainLink));
            }
            return list;
        }
    }
    public class UniqueMatch
    {
        public string tournament { get; set; }
        public int matchID { get; set; }
        public string opp1 { get; set; }
        public string opp2 { get; set; }
        public int opp1Procent { get; set; }
        public int opp2Procent { get; set; }
        public string comment { get; set; }
        public int matchCount { get; set; }
        public string winner { get; set; }
        public int amountOfPeopleBetting { get; set; }
        public int amountOfItemsBetted { get; set; }
        public string ago { get; set; }
        public string time { get; set; }
        public DateTime timeWhenDataTaken { get; set; }
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
            using (StreamWriter writer = new StreamWriter(Path.Combine(Config.logPath, DateTime.Now.ToString().Replace(':', ',') + ".txt")))
            {
                writer.WriteLine("ErrorSlot - " + text);
                writer.WriteLine("Version - " + Config.version);
                writer.WriteLine("DateTime - " + DateTime.Now.ToString());
                writer.WriteLine();
                writer.WriteLine("---------------------------");
                writer.WriteLine();
                writer.WriteLine(error.Message);
                writer.WriteLine();
                writer.WriteLine("---------------------------");
                writer.WriteLine();
                writer.WriteLine(error.StackTrace);
                writer.WriteLine();
                writer.WriteLine("---------------------------");
                writer.WriteLine();
                writer.WriteLine(error.ToString());
            }
        }
    }
}
