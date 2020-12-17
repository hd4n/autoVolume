using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using AudioSwitcher.AudioApi.CoreAudio;
using System.Threading;

namespace autoVolume
{
    struct Config
    {
        public string name;
        public int volume;
        public int match;
    }
    class Program
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        static void Main(string[] args)
        {
            CoreAudioDevice defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
            Program program = new Program();
            List<Config> events = new List<Config>();
            int defaultVolume = 10;

            StreamReader sr = new StreamReader("config.txt");
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (line[0] != '#')
                {
                    string[] currLine = line.Split(':');
                    if (currLine[0] == "defaultVolume")
                    {
                        defaultVolume = Convert.ToInt32(currLine[1]);
                    }
                    else
                    {
                        Config newConfig = new Config();
                        newConfig.name = currLine[0];
                        newConfig.volume = Convert.ToInt32(currLine[1]);
                        newConfig.match = Convert.ToInt32(currLine[2]);
                        events.Add(newConfig);
                    }
                }
            }
            sr.Close();

            Thread thread = new Thread(() =>
            {
                bool changed = false;
                bool newWindow = true;
                while (true)
                {
                    string nameprev = program.GetActiveWindowTitle();
                    System.Threading.Thread.Sleep(500);
                    string newname = program.GetActiveWindowTitle();
                    if (nameprev != newname && newname != null)
                    {
                        //Console.WriteLine(newname);
                        newWindow = true;
                        for (int i = 0; i < events.Count(); i++)
                        {
                            if (events[i].match == 0)
                            {
                                if (newname.ToLower() == events[i].name.ToLower())
                                {
                                    defaultPlaybackDevice.Volume = events[i].volume;
                                    changed = true;
                                    newWindow = false;
                                }
                            }
                            else
                            {
                                if (newname.ToLower().Contains(events[i].name.ToLower()))
                                {
                                    defaultPlaybackDevice.Volume = events[i].volume;
                                    changed = true;
                                    newWindow = false;
                                }
                            }
                        }
                        if (changed && newWindow)
                        {
                            defaultPlaybackDevice.Volume = defaultVolume;
                            changed = false;
                        }
                    }
                }
            });
            thread.Start();

            bool quit = false;
            Console.WriteLine("Default volume: " + defaultVolume);
            Console.WriteLine("---------------------");
            Console.WriteLine("TRIGGER\tVOLUME\tMATCH");
            Console.WriteLine("---------------------");
            for (int i = 0; i < events.Count(); i++)
            {
                Console.Write(events[i].name + "\t" + events[i].volume + "\t");
                if (events[i].match == 0)
                {
                    Console.Write("equals\n");
                }
                else
                {
                    Console.Write("contains\n");
                }
            }
            Console.WriteLine("---------------------");
            Console.WriteLine("(q) Quit");
            while (!quit)
            {
                string input = Console.ReadLine();
                if (input == "q" || input == "Q")
                {
                    quit = true;
                    thread.Abort();
                    defaultPlaybackDevice.Volume = defaultVolume;
                }
            }

        }
    }
}
