using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.IO;

namespace serviceJsonToDB
{
    [JsonObject]
    public class FeedText
    {
        [JsonProperty]
        public string id { get; set; }

        [JsonProperty]
        public string text { get; set; }

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
    }

    [JsonObject]
    public class FeedHref
    {
        [JsonProperty]
        public string id { get; set; }

        [JsonProperty]
        public List<string> href { get; set; }

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
    }

    [JsonObject]
    public class FeedImg
    {
        [JsonProperty]
        public string id { get; set; }

        [JsonProperty]
        public List<string> img { get; set; }

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
    }

    public class SqlJsonToDB
    {
        SqlConnection database;
        SqlDataAdapter mainDataAdapter;
        SqlDataAdapter hrefDataAdapter;
        SqlDataAdapter imgDataAdapter;
        DataSet mainData;
        DataSet hrefData;
        DataSet imgData;
        Mutex mutexText;
        Mutex mutexHref;
        Mutex mutexImg;

        public SqlJsonToDB(string nameDB)
        {

            database = new SqlConnection();
            database.ConnectionString = "Server=localhost;Database=" + nameDB + ";Trusted_Connection=true";
            mainData = new DataSet();
            hrefData = new DataSet();
            imgData = new DataSet();
            mainDataAdapter = new SqlDataAdapter("Select * from mainTable", database);
            hrefDataAdapter = new SqlDataAdapter("Select * from urlArray", database);
            imgDataAdapter = new SqlDataAdapter("Select * from imgArray", database);
            SqlCommandBuilder mainCommandBuilder;
            SqlCommandBuilder hrefCommandBuilder;
            SqlCommandBuilder imgCommandBuilder;
            mainCommandBuilder = new SqlCommandBuilder(mainDataAdapter);
            hrefCommandBuilder = new SqlCommandBuilder(hrefDataAdapter);
            imgCommandBuilder = new SqlCommandBuilder(imgDataAdapter);
            database.Open();
            mainDataAdapter.Fill(mainData);
            imgDataAdapter.Fill(imgData);
            hrefDataAdapter.Fill(hrefData);
            database.Close();
            mutexText = new Mutex(false, @"Global\MutexText");
            mutexHref = new Mutex(false, @"Global\MutexHref");
            mutexImg = new Mutex(false, @"Global\MutexImg");
        }

        ~SqlJsonToDB()
        {
            mainDataAdapter.Dispose();
            database.Dispose();
            mainData.Dispose();
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
            DataTable mainTable = mainData.Tables[0];
            foreach (FeedText item in objsFeedText)
            {
                bool a = true;
                FeedText lastObjText = new FeedText();
                int i = 0;
                Monitor.Enter(mainData);
                foreach (DataRow row in mainTable.Rows)
                {
                    if ((row["id"].Equals(item.id)) && (row["text"].Equals(item.text)))
                    {
                        a = false;
                        break;
                    }
                    if (row["id"].Equals(item.id))
                    {
                        lastObjText.id = row["id"].ToString();
                        break;
                    }
                    i++;
                }
                if (a)
                {
                    if (lastObjText.id == null)
                    {
                        DataRow subRow = mainTable.NewRow();
                        subRow["id"] = item.id;
                        subRow["text"] = item.text;
                        subRow["urlExist"] = "NonExist";
                        subRow["imgExist"] = "NonExist";
                        mainTable.Rows.Add(subRow);
                    }
                    else
                    {
                        mainTable.Rows[i]["text"] = item.text;
                    }
                }
                Monitor.Exit(mainData);
            }
        }

        public void AddImgToDB()
        {
            List<FeedImg> objsFeedImg = new List<FeedImg>();
            mutexImg.WaitOne();
            objsFeedImg.AddRange(JsonConvert.DeserializeObject<List<FeedImg>>(File.ReadAllText("D:\\JSON\\img.json")));
            mutexImg.ReleaseMutex();
            DataTable mainTable = mainData.Tables[0];
            foreach (FeedImg item in objsFeedImg)
            {
                bool a = true;
                string lastIdImg = null;
                int i = 0;
                Monitor.Enter(mainData);
                foreach (DataRow row in mainTable.Rows)
                {
                    if ((item.id.Equals(row["id"])) && (row["imgExist"].Equals("Exist")))
                    {
                        a = false;
                        break;
                    }
                    if (item.id.Equals(row["id"]))
                    {
                        lastIdImg = row["id"].ToString();
                        break;
                    }
                    i++;
                }

                if (a)
                {
                    if (lastIdImg == null)
                    {
                        DataRow subRow = mainTable.NewRow();
                        subRow["id"] = item.id;
                        subRow["text"] = "";
                        subRow["urlExist"] = "NonExist";
                        subRow["imgExist"] = "Exist";
                        DataTable imgTable = imgData.Tables[0];
                        int j = 1;
                        foreach (string item2 in item.img)
                        {
                            DataRow subRowImg = imgTable.NewRow();
                            subRowImg["id"] = item.id;
                            subRowImg["img_index"] = j;
                            subRowImg["img"] = item2;
                            j++;
                            imgTable.Rows.Add(subRowImg);
                        }
                        mainTable.Rows.Add(subRow);
                    }
                    else
                    {
                        mainTable.Rows[i]["imgExist"] = "Exist";
                        DataTable imgTable = imgData.Tables[0];
                        int j = 1;
                        foreach (string item2 in item.img)
                        {
                            DataRow subRowImg = imgTable.NewRow();
                            subRowImg["id"] = item.id;
                            subRowImg["img_index"] = j;
                            subRowImg["img"] = item2;
                            j++;
                            imgTable.Rows.Add(subRowImg);
                        }
                    }
                }
                Monitor.Exit(mainData);
            }
        }



        public void AddHrefToDB()
        {
            List<FeedHref> objsFeedHref = new List<FeedHref>();
            mutexHref.WaitOne();
            objsFeedHref.AddRange(JsonConvert.DeserializeObject<List<FeedHref>>(File.ReadAllText("D:\\JSON\\href.json")));
            mutexHref.ReleaseMutex();
            DataTable mainTable = mainData.Tables[0];
            foreach (FeedHref item in objsFeedHref)
            {
                bool a = true;
                string lastIdHref = null;
                int i = 0;
                Monitor.Enter(mainData);
                foreach (DataRow row in mainTable.Rows)
                {
                    if ((item.id.Equals(row["id"])) && (row["urlExist"].Equals("Exist")))
                    {
                        a = false;
                        break;
                    }
                    if (item.id.Equals(row["id"]))
                    {
                        lastIdHref = row["id"].ToString();
                        break;
                    }
                    i++;
                }

                if (a)
                {
                    if (lastIdHref == null)
                    {
                        DataRow subRow = mainTable.NewRow();
                        subRow["id"] = item.id;
                        subRow["text"] = "";
                        subRow["urlExist"] = "Exist";
                        subRow["imgExist"] = "NonExist";
                        DataTable hrefTable = hrefData.Tables[0];
                        int j = 1;
                        foreach (string item2 in item.href)
                        {
                            DataRow subRowHref = hrefTable.NewRow();
                            subRowHref["id"] = item.id;
                            subRowHref["url_index"] = j;
                            subRowHref["url"] = item2;
                            j++;
                            hrefTable.Rows.Add(subRowHref);
                        }
                        mainTable.Rows.Add(subRow);
                    }
                    else
                    {
                        mainTable.Rows[i]["urlExist"] = "Exist";
                        DataTable hrefTable = hrefData.Tables[0];
                        int j = 1;
                        foreach (string item2 in item.href)
                        {
                            DataRow subRowHref = hrefTable.NewRow();
                            subRowHref["id"] = item.id;
                            subRowHref["url_index"] = j;
                            subRowHref["url"] = item2;
                            j++;
                            hrefTable.Rows.Add(subRowHref);
                        }
                    }
                }
                Monitor.Exit(mainData);
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

        public void RefreshDB(int a)
        {
            Thread.Sleep(8000);
            while (true)
            {
                Monitor.Enter(mainData);
                mainDataAdapter.Update(mainData);
                hrefDataAdapter.Update(hrefData);
                imgDataAdapter.Update(imgData);
                Monitor.Exit(mainData);
                Console.WriteLine("DB UPDATED");
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
            Thread t1, t2, t3, t4;
            t1 = new Thread(() => dataBase.AddTextToDB(5000));
            t2 = new Thread(() => dataBase.AddHrefToDB(5000));
            t3 = new Thread(() => dataBase.AddImgToDB(5000));
            t4 = new Thread(() => dataBase.RefreshDB(15000));
            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();
            Thread.Sleep(Timeout.Infinite);
        }

        protected override void OnStop()
        {
        }
    }
}
