using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading;

class Program
{
    static int m_port_write = 0;
    static int m_port_read = 0;

    #region [ CODE ]

    static RedisBase m_subcriber;
    static bool _subscribe(string channel)
    {
        if (string.IsNullOrEmpty(channel)) return false;
        channel = "<{" + channel + "}>";
        try
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("*2\r\n");
            sb.Append("$10\r\nPSUBSCRIBE\r\n");
            sb.AppendFormat("${0}\r\n{1}\r\n", channel.Length, channel);

            byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
            var ok = m_subcriber.SendBuffer(buf);
            var lines = m_subcriber.ReadMultiString();
            //Console.WriteLine("\r\n\r\n{0}\r\n\r\n", string.Join(Environment.NewLine, lines));
            return ok;
        }
        catch (Exception ex)
        {
        }
        return false;
    }

    #endregion

    static void __startApp()
    {
        var redis = new RedisBase(new RedisSetting(REDIS_TYPE.ONLY_READ, 1001));


        return;

        //File.WriteAllText(@"C:\___.txt", m_port_write.ToString());

        if (m_port_write == 0) m_port_write = 1000;
        if (m_port_read == 0) m_port_read = 1001;
        m_subcriber = new RedisBase(new RedisSetting(REDIS_TYPE.ONLY_SUBCRIBE, 1001));
        _subscribe("__IMG2ZIP_IN");

        string[] a;
        string s;
        var bs = new List<byte>();
        while (__running)
        {
            if (!m_subcriber.m_stream.DataAvailable)
            {
                if (bs.Count > 0)
                {
                    s = Encoding.UTF8.GetString(bs.ToArray()).Trim();
                    bs.Clear();
                    a = s.Split('\r');
                    s = a[a.Length - 1].Trim();
                    if (File.Exists(s))
                    {
                        //new Thread(new ParameterizedThreadStart((o)
                        //    => __createDocumentBackground(o.ToString()))).Start(s);
                    }
                }

                Thread.Sleep(100);
                continue;
            }

            byte b = (byte)m_subcriber.m_stream.ReadByte();
            bs.Add(b);
        }
    }

    static bool __running = true;
    static void __stopApp() => __running = false;

    #region [ SETUP WINDOWS SERVICE ]

    static Thread __threadWS = null;
    static void Main(string[] args)
    {
        if (Environment.UserInteractive)
        {
            StartOnConsoleApp(args);
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey(true);
            Stop();
        }
        else using (var service = new MyService())
                ServiceBase.Run(service);
    }

    public static void StartOnConsoleApp(string[] args) => __startApp();
    public static void StartOnWindowService(string[] args)
    {
        __threadWS = new Thread(new ThreadStart(() => __startApp()));
        __threadWS.IsBackground = true;
        __threadWS.Start();
    }

    public static void Stop()
    {
        __stopApp();
        if (__threadWS != null) __threadWS.Abort();
    }

    #endregion;
}

