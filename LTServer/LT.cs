using System;
using System.Collections.Generic;

using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;

using System.IO;
using System.Security.Cryptography;

using System.Threading;
using System.Text;
using System.Diagnostics;

namespace LTServer
{
    class LicznikTransferu
    {
        public class Variable
        {
            public string name;
            public int value;

            public Variable(string name, int value)
            {
                this.name = name;
                this.value = value;
            }
        }


        public struct DownloadLimits
        {
            public int download;
            public int upload;
            public int downloadLimit;
            public int uploadLimit;
            public int cost;

            public DownloadLimits(int download, int upload, int downloadLimit, int uploadLimit, int cost)
            {
                this.download = download;
                this.upload = upload;
                this.downloadLimit = downloadLimit;
                this.uploadLimit = uploadLimit;
                this.cost = cost;
            }

            public override string ToString()
            {
                return ToString(';');
            }

            public string ToString(char separator)
            {
                return download.ToString() + separator + upload.ToString() + separator + downloadLimit.ToString() + separator + uploadLimit.ToString() + separator + cost.ToString();
            }

            public static DownloadLimits EMPTY = new DownloadLimits() { cost = -1 };
        }

        static int? GetVariableValue(List<Variable> variables, string name)
        {
            for (int i = 0; i < variables.Count; i++)
            {
                if (variables[i].name == name)
                {
                    return variables[i].value;
                }
            }

            return null;
        }

        ChromeDriver driver;

        public DownloadLimits GetDownloadLimits(string email, string password)
        {
            List<Variable> variables = new List<Variable>();
            Console.WriteLine("Connecting...");
            driver.Navigate().GoToUrl("http://minik.ml/agh/transfer");
            //Console.WriteLine("Title: " + driver.Title);
            if (driver.Url == "https://panel.dsnet.agh.edu.pl/login")
            {



                Console.WriteLine("Extracting...");
                var form = driver.FindElement(By.ClassName("form-horizontal"));
                if (form == null)
                {
                    Console.WriteLine("Form is null");
                    return DownloadLimits.EMPTY;
                }
                else
                {
                    var elements = form.FindElements(By.ClassName("form-control"));
                    int it = 0;
                    Console.WriteLine("Logging in...");
                    foreach (var item in elements)
                    {
                        if (it == 0)
                        {
                            item.SendKeys(email);
                        }
                        else if (it == 1)
                        {
                            item.SendKeys(password);
                        }
                        else
                        {
                            break;
                        }
                        it++;
                    }
                    var button = form.FindElement(By.ClassName("btn"));
                    button.Click();
                }
                if (!driver.Url.Contains("minik.ml/agh/transfer"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Nie udalo sie zalogowac, bledny email / haslo?");
                    Console.ResetColor();
                    return DownloadLimits.EMPTY;
                }
            }
            else if (!driver.Url.StartsWith("http://minik.ml/agh/transfer"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Not AGH login website or service website - \"" + driver.Url + "\"");
                Console.ForegroundColor = ConsoleColor.Gray;

                //Console.ReadKey();
                return DownloadLimits.EMPTY;
            }

            Console.WriteLine("Parsing data...");
            string source = driver.PageSource.Replace("&gt", ">").Replace("<html>", "").Replace("</html>", "").Replace("<head>", "").Replace("</head>", "").Replace("<body>", "").Replace("</body>", "");
            string[] lines = source.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Replace("\n", "").Replace("\r", "");
                if (lines[i].StartsWith("  [\"") && lines[i].EndsWith("\"]=>;"))
                {
                    if (i + 1 < lines.Length)
                    {
                        string name = lines[i].Replace("  [\"", "").Replace("\"]=>;", "");
                        int value;
                        if (!int.TryParse(lines[i + 1].Replace(" ", "").Replace("int(", "").Replace(")", ""), out value))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error parsing value, line: \"" + lines[i + 1] + "\"");
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }

                        variables.Add(new Variable(name, value));
                        i++;
                    }
                }
            }

            driver.ExecuteChromeCommand("Storage.clearCookies", new Dictionary<string, object>());

            return new DownloadLimits((int)GetVariableValue(variables, "download_limited"), (int)GetVariableValue(variables, "upload_limited"), (int)GetVariableValue(variables, "download_limit"), (int)GetVariableValue(variables, "upload_limit"), (int)GetVariableValue(variables, "cost"));
        }

        public LicznikTransferu()
        {


            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.EnableVerboseLogging = false;
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;

            

            ChromeOptions options = new ChromeOptions();
            options.AddArguments("--headless");
            options.AddArguments("--silent");
            options.AddArguments("--disable-gpu");
            options.AddArguments("--disable-logging");
            options.AddArguments("--log-level=3");
            options.AddArguments("--output=/dev/null");
            options.AddArguments("--single-instance");
            options.AddArguments("--disable-dev-shm-usage");
            options.AddArguments("--no-sandbox");

            Console.WriteLine("Initializing...");
            //using (var driver = new ChromeDriver(service, options))
            //{

            
            try
            {
                driver = new ChromeDriver(service, options);
            }
            catch(InvalidOperationException e)
            {
                Debug.Exception(e, "[Nieprawidlowa wersja Chrome]");
                return;
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[CreatingDriverException]");
                return;
            }
           

            //var limits = GetDownloadLimits(email, password);

            Console.WriteLine("Ready");
        }

        void ParseLimits(DownloadLimits limits)
        {
            int download = limits.download;//(int)GetVariableValue(variables, "download_limited");
            int upload = limits.upload;//(int)GetVariableValue(variables, "upload_limited");
            int downloadLimit = limits.downloadLimit;//(int)GetVariableValue(variables, "download_limit");
            int uploadLimit = limits.uploadLimit;//(int)GetVariableValue(variables, "upload_limit");
            Console.WriteLine("\n\n\n\n");

            Console.Write("Download: ");
            double dp = download * 100.0 / downloadLimit;
            Console.ForegroundColor = ConsoleColor.Green;
            if (dp > 75)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            if (dp > 90)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            Console.WriteLine((download / 1024.0).ToString("0.00") + " GB / " + (downloadLimit / 1024.0).ToString("0.00") + " GB - " + dp.ToString("0.00") + "%");

            Console.ResetColor();
            double up = upload * 100.0 / uploadLimit;
            Console.Write("Upload: ");
            Console.ForegroundColor = ConsoleColor.Green;
            if (up > 75)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            if (up > 90)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine((upload / 1024.0).ToString("0.00") + " GB / " + (uploadLimit / 1024.0).ToString("0.00") + " GB - " + up.ToString("0.00") + "%");
            Console.ResetColor();

            Console.Write("Cost: ");
            int cost = limits.cost;//(int)GetVariableValue(variables, "cost");
            Console.ForegroundColor = ConsoleColor.Cyan;
            if (cost >= 10)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
            }
            if (cost >= 50)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            if (cost >= 100)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            if (cost > 100)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            }
            if (cost >= 150)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            Console.WriteLine(cost.ToString() + "%");

            Console.ResetColor();
            Console.WriteLine("\n\nWaiting for a keypress");
            ConsoleKeyInfo info = Console.ReadKey();
            if (info.Key == ConsoleKey.F9)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("Dl'yl dhajopun Fvb");
                Thread.Sleep(5000);
                Console.ResetColor();
            }
        }

    }

}
