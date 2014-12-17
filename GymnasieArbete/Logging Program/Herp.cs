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
        public const string version = "0.7.2";
        public const string logPath = @"C:\Users\Novie\Desktop\GymLog";
        public const string databasePATH = @"C:\Users\Novie\Desktop\GymnaArbete\mainDatabase.db";
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
            databaseConnection.Open();
            if (databaseConnection.State != System.Data.ConnectionState.Open)
                throw new Exception("Failed to open the Database");
            commander = databaseConnection.CreateCommand();
            this.conString = conString;
        }


        public bool AutoFlush
        {
            get { return autoFlush; }
            set { autoFlush = value; }
        }
        public int getTableCount(string tableName)
        {
            return int.Parse(ExecuteQuery(@"SELECT COUNT(*) FROM " + tableName).Rows[0].ItemArray[0].ToString());
        }
        public List<string> ListTables()
        {
            List<string> tables = new List<string>();
            DataTable asd = databaseConnection.GetSchema("Tables");
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
                commander.CommandText = SQL;
                commander.ExecuteNonQuery();
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
        }
        public DataTable ExecuteQuery(string SQL)
        {
            commander.CommandText = SQL;
            using (SQLiteDataReader reader = commander.ExecuteReader())
            {
                DataTable table = new DataTable();
                table.Load(reader);
                return table;
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
            result.TimeWhenDataTaken = DateTime.Now;
            matchWindow = Regex.Split(matchWindow, "<div class=\"title\">last 30 bets</div>")[0];


            #region MatchID
            {
                result.MatchID = int.Parse(matchLink.Split('=')[1]);
            }
            #endregion
            #region Opponents
            {
                string[] splits = Regex.Split(matchWindow, @"</b><br><i>");

                var matches = Regex.Matches(splits[0], "<b>");
                result.Opp1 = splits[0].Substring(matches[matches.Count - 1].Index + matches[matches.Count - 1].Length);
                if (result.Opp1.Contains("(win)"))
                    result.Opp1 = result.Opp1.Remove(5);
                result.Opp1 = Regex.Replace(result.Opp1, @"[^a-zA-Z]", "");

                matches = Regex.Matches(splits[1], "<b>");
                result.Opp2 = splits[1].Substring(matches[matches.Count - 1].Index + matches[matches.Count - 1].Length);
                if (result.Opp2.Contains("(win)"))
                    result.Opp2 = result.Opp2.Remove(5);
                result.Opp2 = Regex.Replace(result.Opp2, @"[^a-zA-Z]", "");

                result.Opp1Procent = int.Parse(Regex.Split(splits[1], @"%</i>")[0]);
                result.Opp2Procent = int.Parse(Regex.Split(splits[2], @"%</i>")[0]);
            }
            #endregion
            #region antalMatcher
            {
                string rawData = Regex.Split(matchWindow, "28%;\">")[1];
                rawData = Regex.Split(rawData, "</div>")[0];
                rawData = Regex.Replace(rawData, "[^0-9]", ""); //Borde verkligen lära mig regex ^^
                if (rawData != "")
                    result.MatchCount = int.Parse(rawData);
            }
            #endregion
            #region Winner
            {
                if (Regex.IsMatch(matchWindow, @"\(win\)"))
                {
                    string tempString = matchWindow.Remove(Regex.Match(matchWindow, @"\(win\)").Index);
                    var patternMatches = Regex.Matches(tempString, "<b>");
                    tempString = tempString.Substring(patternMatches[patternMatches.Count - 1].Index + patternMatches[patternMatches.Count - 1].Length).Trim();

                    if (tempString == result.Opp1)
                        result.Winner = result.Opp1;
                    else
                        result.Winner = result.Opp2;
                    result.Winner = Regex.Replace(result.Winner, @"[^a-zA-Z]", "");
                }
            }
            #endregion
            #region Date & Time
            {
                string[] var5 = Regex.Split(matchWindow, "33%;\">");
                result.Ago = Regex.Split(var5[1], "</div>")[0];
                result.Time = Regex.Split(var5[2], "</div>")[0];
                result.Time = Regex.Replace(result.Time, "[^0-9:]", "");
            }
            #endregion
            #region Betting
            {
                //string var6 = Regex.Split(matchWindow, "full\">")[2];
                //var6 = Regex.Split(var6, "</div>")[0];
                //var6 = var6.Trim();
                //string[] var5 = var6.Split(' ');
                
                string[] var5 = Regex.Split(Regex.Split(matchWindow, "full\">")[2], "</div>")[0].Trim().Split(' ');


                int tempVar;// Något bug på deras sida vilket gör att typ en gång i hundra så visar den inte amount så behöver TryParse
                int.TryParse(var5[0], out tempVar);
                result.AmountOfPeopleBetting = tempVar;
                int.TryParse(var5[3], out tempVar);
                result.AmountOfItemsBetted = tempVar;
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
                tour = tour.Trim();
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
                //FIX
                //TODO
                comment = "";
                //comment = Regex.Split(data, "#D12121\"> ")[1];
                //comment = Regex.Split(comment, "</span>")[0];
            }
            #endregion

            UniqueMatch newInfo = grabInfoFromMatchPage(mainLink + "/match?m=" + matchID);
            newInfo.Tournament = tour;
            newInfo.Comment = comment;

            return newInfo;
        }
        public static List<UniqueMatch> grabInfoFromWeb(string mainLink)
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
        private string tournament { get; set; }
        private int matchID { get; set; }
        private string opp1 { get; set; }
        private string opp2 { get; set; }
        private int opp1Procent { get; set; }
        private int opp2Procent { get; set; }
        private string comment { get; set; }
        private int matchCount { get; set; }
        private string winner { get; set; }
        private int amountOfPeopleBetting { get; set; }
        private int amountOfItemsBetted { get; set; }
        private string ago { get; set; }
        private string time { get; set; }
        private DateTime timeWhenDataTaken { get; set; }


        public UniqueMatch()
        {
        }
        public UniqueMatch(string data)
        {
            string[] individualData = Regex.Split(data, "<--->");

            tournament = individualData[0];
            matchID = int.Parse(individualData[1]);
            opp1 = individualData[2];
            opp2 = individualData[3];
            opp1Procent = int.Parse(individualData[4]);
            opp2Procent = int.Parse(individualData[5]);
            comment = individualData[6];
            matchCount = int.Parse(individualData[7]);
            winner = individualData[8];
            amountOfPeopleBetting = int.Parse(individualData[9]);
            amountOfItemsBetted = int.Parse(individualData[10]);
            ago = individualData[11];
            time = individualData[12];
            timeWhenDataTaken = DateTime.Parse(individualData[13]);
        }

        public string Tournament
        {
            get { return tournament; }
            set { tournament = value; }
        }
        public int MatchID
        {
            get { return matchID; }
            set { matchID = value; }
        }
        public string Opp1
        {
            get { return opp1; }
            set { opp1 = value; }
        }
        public string Opp2
        {
            get { return opp2; }
            set { opp2 = value; }
        }
        public int Opp1Procent
        {
            get { return opp1Procent; }
            set { opp1Procent = value; }
        }
        public int Opp2Procent
        {
            get { return opp2Procent; }
            set { opp2Procent = value; }
        }
        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }
        public int MatchCount
        {
            get { return matchCount; }
            set { matchCount = value; }
        }
        public string Winner
        {
            get { return winner; }
            set { winner = value; }
        }
        public int AmountOfPeopleBetting
        {
            get { return amountOfPeopleBetting; }
            set { amountOfPeopleBetting = value; }
        }
        public int AmountOfItemsBetted
        {
            get { return amountOfItemsBetted; }
            set { amountOfItemsBetted = value; }
        }
        public string Ago
        {
            get { return ago; }
            set { ago = value; }
        }
        public string Time
        {
            get { return time; }
            set { time = value; }
        }
        public DateTime TimeWhenDataTaken
        {
            get { return timeWhenDataTaken; }
            set { timeWhenDataTaken = value; }
        }



        public void SaveToLoc(string loc)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(tournament + "<--->");
            builder.Append(matchID + "<--->");
            builder.Append(opp1 + "<--->");
            builder.Append(opp2 + "<--->");
            builder.Append(opp1Procent + "<--->");
            builder.Append(opp2Procent + "<--->");
            builder.Append(comment + "<--->");
            builder.Append(matchCount + "<--->");
            builder.Append(winner + "<--->");
            builder.Append(amountOfPeopleBetting + "<--->");
            builder.Append(amountOfItemsBetted + "<--->");
            builder.Append(ago + "<--->");
            builder.Append(time + "<--->");
            builder.Append(timeWhenDataTaken.ToString());

            using (StreamWriter writer = new StreamWriter(Path.Combine(loc, matchID + ".txt"), true))
            {
                writer.WriteLine(builder.ToString());
            }
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
