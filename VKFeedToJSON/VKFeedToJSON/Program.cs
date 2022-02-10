using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.ServiceProcess;
using Newtonsoft.Json;


namespace VKFeedToJSON
{
    [JsonObject]
    public class FeedText
    {
        [JsonProperty]
        string id { get; set; }

        [JsonProperty]
        string text { get; set; }

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


    public class VkFeedPosts
    {
        FirefoxProfile profile;
        FirefoxOptions options;
        FirefoxDriver fireDr;
        Mutex mutexText;
        Mutex mutexHref;
        Mutex mutexImg;
        

        public VkFeedPosts(string urlProfile)
        {
            options = new FirefoxOptions();
            profile = new FirefoxProfile(urlProfile);
            options.Profile = profile;
            fireDr = new FirefoxDriver(options);
            fireDr.Navigate().GoToUrl("https://vk.com/feed");
            mutexText = new Mutex(false, @"Global\MutexText");
            mutexHref = new Mutex(false, @"Global\MutexHref");
            mutexImg = new Mutex(false, @"Global\MutexImg");
            
        }

        public void WriteFeedText(string file)
        {
            string fileText = file;
            string text = "";
            string textId = "";
            List<FeedText> objsFeedText = new List<FeedText>();
            List<IWebElement> webElem = fireDr.FindElements(By.CssSelector("._post.post.page_block.deep_active")).ToList();
            List<IWebElement> existedWebElem = new List<IWebElement>();

            mutexText.WaitOne();
            try
            {
                objsFeedText.AddRange(JsonConvert.DeserializeObject<List<FeedText>>(File.ReadAllText(fileText)));
            }
            catch
            { }
            mutexText.ReleaseMutex();

            existedWebElem.AddRange(from i in webElem
                                    from j in objsFeedText
                                    where i.GetAttribute("id").Equals(j.Id())
                                    select i);
            webElem = webElem.Except<IWebElement>(existedWebElem).ToList();
            existedWebElem.Clear();

            foreach (IWebElement item in webElem)
            {
                if (item.FindElements(By.ClassName("wall_post_text")).Count > 0)
                {
                    text = item.FindElement(By.ClassName("wall_post_text")).Text;
                    text = Regex.Replace(text, "\r\n", "");
                    textId = item.GetAttribute("id");
                    if (!text.Equals(""))
                        objsFeedText.Add(FeedText.CreateFeedText(textId, text));
                }
            }
            mutexText.WaitOne();
            File.WriteAllText(fileText, JsonConvert.SerializeObject(objsFeedText, Formatting.Indented));
            mutexText.ReleaseMutex();
        }

        public void WriteFeedHref(string file)
        {
            string fileHref = file;
            List<string> href = new List<string>();
            string hrefId = "";
            List<FeedHref> objsFeedHref = new List<FeedHref>();
            List<string> cssSets = new List<string>();
            List<IWebElement> webElem = fireDr.FindElements(By.CssSelector("._post.post.page_block.deep_active")).ToList();
            List<IWebElement> existedWebElem = new List<IWebElement>();
            mutexHref.WaitOne();
            try
            {
                objsFeedHref.AddRange(JsonConvert.DeserializeObject<List<FeedHref>>(File.ReadAllText(fileHref)));
            }
            catch
            { }
            mutexHref.ReleaseMutex();
            existedWebElem.AddRange(from i in webElem
                                    from j in objsFeedHref
                                    where i.GetAttribute("id").Equals(j.Id())
                                    select i);
            webElem = webElem.Except<IWebElement>(existedWebElem).ToList();
            existedWebElem.Clear();

            cssSets.Add(".lnk");
            cssSets.Add(".page_media_link_title");
            string setTagA = ".wall_post_text";

            foreach (IWebElement item in webElem)
            {
                hrefId = item.GetAttribute("id");

                foreach (string set in cssSets)
                    foreach (IWebElement elem in item.FindElements(By.CssSelector(set)))
                        if (elem.GetAttribute("href") != null)
                            href.Add(elem.GetAttribute("href"));

                foreach (IWebElement elem in item.FindElements(By.CssSelector(setTagA)))
                    foreach (IWebElement elem2 in elem.FindElements(By.TagName("a")))
                        if (elem2.GetAttribute("href") != null)
                            href.Add(elem2.GetAttribute("href"));

                if (href.Count != 0)
                    objsFeedHref.Add(FeedHref.CreateFeedHref(hrefId, href));

                href.Clear();
            }
            mutexHref.WaitOne();
            File.WriteAllText(fileHref, JsonConvert.SerializeObject(objsFeedHref, Formatting.Indented));
            mutexHref.ReleaseMutex();
        }

        public void WriteFeedImg(string file)
        {
            string fileImg = file;
            List<string> img = new List<string>();
            string imgId = "";
            List<FeedImg> objsFeedImg = new List<FeedImg>();
            List<IWebElement> webElem = fireDr.FindElements(By.CssSelector("._post.post.page_block.deep_active")).ToList();
            List<IWebElement> existedWebElem = new List<IWebElement>();

            mutexImg.WaitOne();
            try
            {
                objsFeedImg.AddRange(JsonConvert.DeserializeObject<List<FeedImg>>(File.ReadAllText(fileImg)));
            }
            catch
            { }
            mutexImg.ReleaseMutex();
            existedWebElem.AddRange(from i in webElem
                                    from j in objsFeedImg
                                    where i.GetAttribute("id").Equals(j.Id())
                                    select i);
            webElem = webElem.Except<IWebElement>(existedWebElem).ToList();
            existedWebElem.Clear();

            foreach (IWebElement item in webElem)
            {

                imgId = item.GetAttribute("id");
                foreach (IWebElement elem in item.FindElements(By.CssSelector(".page_post_thumb_wrap.image_cover")).ToList())
                {
                    string testString = elem.GetAttribute("style");
                    img.Add(Regex.Replace(testString, ".*url\\(\"(?<gr>.*)\"\\).*", @"${gr}"));
                }
                if (img.Count != 0)
                    objsFeedImg.Add(FeedImg.CreateFeedImg(imgId, img));
                img.Clear();
            }
            mutexImg.WaitOne();
            File.WriteAllText(fileImg, JsonConvert.SerializeObject(objsFeedImg, Formatting.Indented));
            mutexImg.ReleaseMutex();
        }

        public void ReadFeedText()
        {
            string fileText = "D:\\JSON\\text.json";
            mutexText.WaitOne();
            List<FeedText> objsFeedText = new List<FeedText>();
            objsFeedText.AddRange(JsonConvert.DeserializeObject<List<FeedText>>(File.ReadAllText(fileText)));
            mutexText.ReleaseMutex();
            Console.WriteLine("ReadText");
        }

        public void ReadFeedHref()
        {
            string fileHref = "D:\\JSON\\href.json";
            mutexHref.WaitOne();
            List<FeedHref> objsFeedHref = new List<FeedHref>();
            objsFeedHref.AddRange(JsonConvert.DeserializeObject<List<FeedHref>>(File.ReadAllText(fileHref)));
            mutexHref.ReleaseMutex();
            Console.WriteLine("ReadHref");
        }

        public void ReadFeedImg()
        {
            string fileImg = "D:\\JSON\\img.json";
            mutexImg.WaitOne();
            List<FeedImg> objsFeedImg = new List<FeedImg>();
            objsFeedImg.AddRange(JsonConvert.DeserializeObject<List<FeedImg>>(File.ReadAllText(fileImg)));
            mutexImg.ReleaseMutex();
            Console.WriteLine("ReadImg");
        }

        public void WriteFeedText(int a)
        {
            int i = 1;
            string fileText = "D:\\JSON\\text.json";
            mutexText.WaitOne();
            if(!File.Exists(fileText))
            {
                FileStream streamText = File.Create(fileText);
                streamText.Close();
            }
            mutexText.ReleaseMutex();
            while (true)
            {
                Monitor.Enter(mutexText);
                WriteFeedText(fileText);
                Console.WriteLine("text" + i);
                i++;
                Monitor.Exit(mutexText);
                Thread.Sleep(a);

            }
        }

        public void WriteFeedHref(int a)
        {
            int i = 1;
            string fileHref = "D:\\JSON\\href.json";
            mutexHref.WaitOne();
            if (!File.Exists(fileHref))
            {
                FileStream streamHref = File.Create(fileHref);
                streamHref.Close();
            }
            mutexHref.ReleaseMutex();
            while (true)
            {
                Monitor.Enter(mutexHref);
                WriteFeedHref(fileHref);
                Console.WriteLine("href" + i);
                i++;
                Monitor.Exit(mutexHref);
                Thread.Sleep(a);
            }
        }

        public void WriteFeedImg(int a)
        {
            int i = 1;
            string fileImg = "D:\\JSON\\img.json";
            mutexImg.WaitOne();
            if (!File.Exists(fileImg))
            {
                FileStream streamImg = File.Create(fileImg);
                streamImg.Close();
            }
            mutexImg.ReleaseMutex();
            while (true)
            {
                Monitor.Enter(mutexImg);
                WriteFeedImg(fileImg);
                Console.WriteLine("img" + i);
                i++;
                Monitor.Exit(mutexImg);
                Thread.Sleep(a);
            }
        }

        public void ReadFeed(int a)
        {
            Thread.Sleep(10000);
            while (true)
            {
                ReadFeedText();
                ReadFeedHref();
                ReadFeedImg();
                Monitor.Enter(mutexText);
                Monitor.Enter(mutexHref);
                Monitor.Enter(mutexImg);
                fireDr.Navigate().Refresh();
                Monitor.Exit(mutexText);
                Monitor.Exit(mutexHref);
                Monitor.Exit(mutexImg);
                Thread.Sleep(a);
                
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {

            VkFeedPosts vk = new VkFeedPosts("z32q4uvt.ggss");
            Thread t1, t2, t3, t4;
            //ServiceController controller = new ServiceController("Service");
            t1 = new Thread(() => vk.WriteFeedText(5000));
            t2 = new Thread(() => vk.WriteFeedImg(5000));
            t3 = new Thread(() => vk.WriteFeedHref(5000));
            t4 = new Thread(() => vk.ReadFeed(25000));
            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();
        }
    }
}
