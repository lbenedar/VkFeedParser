using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;


namespace VKJsonToBD
{
    [JsonObject]
    public class FeedText
    {
        [JsonProperty]
        string id { get; set; }

        [JsonProperty]
        string text { get; set; }

        public FeedText()
        {
            this.id = null;
            this.text = null;
        }

        public FeedText(string id, string text)
        {
            this.id = id;
            this.text = text;
        }

        static public FeedText CreateFeedText(string id, string text)
        {
            FeedText obj = new FeedText(id, text);
            return obj;
        }

        public string Id()
        {
            return this.id;
        }

        public string Text()
        {
            return this.text;
        }

        public static bool operator ==(FeedText left, FeedText right)
        {
            if (left.id == right.id)
                if (left.text == right.text)
                    return true;
                else
                    return false;
            else
                return false;
        }

        public static bool operator !=(FeedText left, FeedText right)
        {
            if (left.id == right.id)
                if (left.text == right.text)
                    return false;
                else
                    return true;
            else
                return true;
        }
    }

    [JsonObject]
    public class FeedHref
    {
        [JsonProperty]
        string id { get; set; }

        [JsonProperty]
        List<string> href { get; set; }

        public FeedHref(string id, List<string> href)
        {
            this.id = id;
            this.href = new List<string>();
            this.href.AddRange(href);
        }

        static public FeedHref CreateFeedHref(string id, List<string> href)
        {
            FeedHref obj = new FeedHref(id, href);
            return obj;
        }

        public string Id()
        {
            return this.id;
        }

        public List<string> Href()
        {
            return this.href;
        }
    }

    [JsonObject]
    public class FeedImg
    {
        [JsonProperty]
        string id { get; set; }

        [JsonProperty]
        List<string> img { get; set; }

        public FeedImg(string id, List<string> img)
        {
            this.id = id;
            this.img = new List<string>();
            this.img.AddRange(img);
        }

        static public FeedImg CreateFeedImg(string id, List<string> img)
        {
            FeedImg obj = new FeedImg(id, img);
            return obj;
        }

        public string Id()
        {
            return this.id;
        }

        public List<string> Img()
        {
            return this.img;
        }
    }

    public class SqlJsonToDB
    {
        SqlConnection database;
        SqlCommand command;
        Mutex mutexText;
        Mutex mutexHref;
        Mutex mutexImg;

        public SqlJsonToDB(string nameDB)
        {
            database = new SqlConnection();
            database.ConnectionString = "Server=localhost;Database=" + nameDB + ";Trusted_Connection=true";
            command = new SqlCommand("", database);
            mutexText = new Mutex(false, @"Global\MutexText");
            mutexHref = new Mutex(false, @"Global\MutexHref");
            mutexImg = new Mutex(false, @"Global\MutexImg");
        }

        ~SqlJsonToDB()
        {
            command.Dispose();
            mutexText.Dispose();
            mutexHref.Dispose();
            mutexImg.Dispose();
        }

        public void AddTextToDB()
        {
            List<FeedText> objsFeedText = new List<FeedText>();
            mutexText.WaitOne();
            objsFeedText.AddRange(JsonConvert.DeserializeObject<List<FeedText>>(File.ReadAllText("D:\\JSON\\text.json")));
            mutexText.ReleaseMutex();
            List<FeedText> subObjText = new List<FeedText>();

            Monitor.Enter(command);
            try
            {
                command.CommandText = "SELECT id, text FROM mainTable";
                database.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    FeedText sub = new FeedText(reader.GetString(0), reader.GetString(1));
                    subObjText.Add(sub);
                }
                reader.Close();
                database.Close();
            }
            finally
            {
                Monitor.Exit(command);
            }

            foreach (FeedText item in objsFeedText)
            {
                bool a = true;
                FeedText lastObjText = new FeedText();
                foreach (FeedText obj in subObjText)
                {
                    if (item == obj)
                    {
                        a = false;
                        break;
                    }
                    if (item.Id().Equals(obj.Id()))
                    {
                        lastObjText = obj;
                        break;
                    }
                }
                if (a)
                {
                    if (lastObjText.Id() == null)
                    {
                        Monitor.Enter(command);
                        try
                        {
                            command.CommandText = "INSERT INTO mainTable (id, text, urlExist, imgExist) VALUES ('" + item.Id() + "', '" + item.Text() + "', 'NonExist', 'NonExist');";
                            database.Open();
                            command.ExecuteNonQuery();
                            database.Close();
                        }
                        catch
                        {
                            command.CommandText = "update mainTable set text = '" + item.Text() + "' where id = '" + item.Id() + "';";
                            command.ExecuteNonQuery();
                            database.Close();
                        }
                        Monitor.Exit(command);
                    }
                    else
                    {
                        Monitor.Enter(command);
                        try
                        {
                            command.CommandText = "update mainTable set text = '" + item.Text() + "' where id = '" + item.Id() + "';";
                            database.Open();
                            command.ExecuteNonQuery();
                            database.Close();
                        }
                        finally
                        {
                            Monitor.Exit(command);
                        }
                    }
                }
            }

        }

        public void AddHrefToDB()
        {
            List<FeedHref> objsFeedHref = new List<FeedHref>();
            mutexHref.WaitOne();
            objsFeedHref.AddRange(JsonConvert.DeserializeObject<List<FeedHref>>(File.ReadAllText("D:\\JSON\\href.json")));
            mutexHref.ReleaseMutex();
            List<FeedText> subObjText = new List<FeedText>();

            Monitor.Enter(command);
            try
            {
                command.CommandText = "SELECT id, urlExist FROM mainTable";
                database.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    FeedText sub = new FeedText(reader.GetString(0), reader.GetString(1));
                    subObjText.Add(sub);
                }
                reader.Close();
                database.Close();
            }
            finally
            {
                Monitor.Exit(command);
            }

            foreach (FeedHref item in objsFeedHref)
            {
                bool a = true;
                FeedText lastObj = new FeedText();
                foreach (FeedText obj in subObjText)
                {
                    if ((item.Id().Equals(obj.Id())) && (obj.Text().Equals("Exist")))
                    {
                        a = false;
                        break;
                    }
                    if (item.Id().Equals(obj.Id()))
                    {
                        lastObj = obj;
                        break;
                    }
                }

                if (a)
                {
                    if (lastObj.Id() == null)
                    {
                        Monitor.Enter(command);
                        try
                        {
                            command.CommandText = "INSERT INTO mainTable (id, text, urlExist, imgExist) VALUES ('" + item.Id() + "', '', 'Exist', 'NonExist');";
                            database.Open();
                            command.ExecuteNonQuery();
                            int i = 1;
                            foreach (string item2 in item.Href())
                            {
                                command.CommandText = "INSERT INTO urlArray (id, url_index, url) VALUES ('" + item.Id() + "', '" + i + "', '" + item2 + "');";
                                command.ExecuteNonQuery();
                                i++;
                            }
                            database.Close();
                        }
                        catch
                        {
                            command.CommandText = "update mainTable set urlExist = 'Exist' where id = '" + item.Id() + "';";
                            command.ExecuteNonQuery();
                            int i = 1;
                            foreach (string item2 in item.Href())
                            {
                                command.CommandText = "INSERT INTO urlArray (id, url_index, url) VALUES ('" + item.Id() + "', '" + i + "', '" + item2 + "');";
                                command.ExecuteNonQuery();
                                i++;
                            }
                            database.Close();
                        }
                        Monitor.Exit(command);
                    }
                    else
                    {
                        Monitor.Enter(command);
                        try
                        {
                            command.CommandText = "update mainTable set urlExist = 'Exist' where id = '" + item.Id() + "';";
                            database.Open();
                            command.ExecuteNonQuery();
                            int i = 1;
                            foreach (string item2 in item.Href())
                            {
                                command.CommandText = "INSERT INTO urlArray (id, url_index, url) VALUES ('" + item.Id() + "', '" + i + "', '" + item2 + "');";
                                command.ExecuteNonQuery();
                                i++;
                            }
                            database.Close();
                        }
                        finally
                        {
                            Monitor.Exit(command);
                        }
                    }
                }
            }
        }

        public void AddImgToDB()
        {
            List<FeedImg> objFeedImg = new List<FeedImg>();
            mutexImg.WaitOne();
            objFeedImg.AddRange(JsonConvert.DeserializeObject<List<FeedImg>>(File.ReadAllText("D:\\JSON\\img.json")));
            mutexImg.ReleaseMutex();
            List<FeedText> subObjText = new List<FeedText>();

            Monitor.Enter(command);
            try
            {
                command.CommandText = "SELECT id, imgExist FROM mainTable";
                database.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    FeedText sub = new FeedText(reader.GetString(0), reader.GetString(1));
                    subObjText.Add(sub);
                }
                reader.Close();
                database.Close();
            }
            finally
            {
                Monitor.Exit(command);
            }

            foreach (FeedImg item in objFeedImg)
            {
                bool a = true;
                FeedText lastObj = new FeedText();
                foreach (FeedText obj in subObjText)
                {
                    if ((item.Id().Equals(obj.Id())) && (obj.Text().Equals("Exist")))
                    {
                        a = false;
                        break;
                    }
                    if (item.Id().Equals(obj.Id()))
                    {
                        lastObj = obj;
                        break;
                    }
                }

                if (a)
                {
                    if (lastObj.Id() == null)
                    {
                        Monitor.Enter(command);
                        try
                        {
                            command.CommandText = "INSERT INTO mainTable (id, text, urlExist, imgExist) VALUES ('" + item.Id() + "', '', 'NonExist', 'Exist');";
                            database.Open();
                            command.ExecuteNonQuery();
                            int i = 1;
                            foreach (string item2 in item.Img())
                            {
                                command.CommandText = "INSERT INTO imgArray (id, img_index, img) VALUES ('" + item.Id() + "', '" + i + "', '" + item2 + "');";
                                command.ExecuteNonQuery();
                                i++;
                            }
                            database.Close();
                        }
                        catch
                        {
                            command.CommandText = "update mainTable set imgExist = 'Exist' where id = '" + item.Id() + "';";
                            command.ExecuteNonQuery();
                            int i = 1;
                            foreach (string item2 in item.Img())
                            {
                                command.CommandText = "INSERT INTO imgArray (id, img_index, img) VALUES ('" + item.Id() + "', '" + i + "', '" + item2 + "');";
                                command.ExecuteNonQuery();
                                i++;
                            }
                            database.Close();
                        }
                        Monitor.Exit(command);
                    }
                    else
                    {
                        Monitor.Enter(command);
                        try
                        {
                            command.CommandText = "update mainTable set imgExist = 'Exist' where id = '" + item.Id() + "';";
                            database.Open();
                            command.ExecuteNonQuery();
                            int i = 1;
                            foreach (string item2 in item.Img())
                            {
                                command.CommandText = "INSERT INTO imgArray (id, img_index, img) VALUES ('" + item.Id() + "', '" + i + "', '" + item2 + "');";
                                command.ExecuteNonQuery();
                                i++;
                            }
                            database.Close();
                        }
                        finally
                        {
                            Monitor.Exit(command);
                        }
                    }
                }
            }
        }

        public void AddTextToDB(int a)
        {
            int i = 1;
            while (true)
            {
                AddTextToDB();
                Console.WriteLine("text" + i);
                i++;
                Thread.Sleep(a);
            }
        }

        public void AddHrefToDB(int a)
        {
            int i = 1;
            while (true)
            {
                AddHrefToDB();
                Console.WriteLine("href" + i);
                i++;
                Thread.Sleep(a);
            }
        }

        public void AddImgToDB(int a)
        {
            int i = 1;
            while (true)
            {
                AddImgToDB();
                Console.WriteLine("img" + i);
                i++;
                Thread.Sleep(a);
            }
        }
    }

    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            SqlJsonToDB dataBase = new SqlJsonToDB("MyDataBase");
            Thread t1, t2, t3;
            t1 = new Thread(() => dataBase.AddTextToDB(5000));
            t2 = new Thread(() => dataBase.AddHrefToDB(5000));
            t3 = new Thread(() => dataBase.AddImgToDB(5000));
            t1.Start();
            t2.Start();
            t3.Start();
            Thread.Sleep(Timeout.Infinite);
        }

        protected override void OnStop()
        {
        }
    }
}
