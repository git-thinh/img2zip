using System;
using System.Collections.Generic;
using System.Drawing;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;

class Program
{
    const int __PORT_WRITE = 1000;
    const int __PORT_READ = 1001;
    const string __SUBCRIBE_IN = "__IMG2ZIP_IN";
    const string __SUBCRIBE_OUT = "__IMG2ZIP_OUT";
    static RedisBase m_subcriber;
    static bool __running = true;

    static void __executeBackground(Tuple<string, byte[]> data)
    {
        if (data == null) return;
        var buf = data.Item2;
        //oTesseractRequest r = null;
        //string guid = Encoding.ASCII.GetString(buf);
        //var redis = new RedisBase(new RedisSetting(REDIS_TYPE.ONLY_READ, __PORT_WRITE));
        //try
        //{
        //    string json = redis.HGET("_OCR_REQUEST", guid);
        //    r = JsonConvert.DeserializeObject<oTesseractRequest>(json);
        //    Bitmap bitmap = redis.HGET_BITMAP(r.redis_key, r.redis_field);
        //    if (bitmap != null) r = __ocrExecute(r, bitmap);
        //}
        //catch (Exception ex)
        //{
        //    if (r != null)
        //    {
        //        string error = ex.Message + Environment.NewLine + ex.StackTrace
        //            + Environment.NewLine + "----------------" + Environment.NewLine +
        //           JsonConvert.SerializeObject(r);
        //        r.ok = -1;
        //        redis.HSET("_OCR_REQ_ERR", r.requestId, error);
        //    }
        //}

        //if (r != null)
        //{
        //    redis.HSET("_OCR_REQUEST", r.requestId, JsonConvert.SerializeObject(r, Formatting.Indented));
        //    redis.HSET("_OCR_REQ_LOG", r.requestId, r.ok == -1 ? "-1" : r.output_count.ToString());
        //    redis.PUBLISH("__TESSERACT_OUT", r.requestId);
        //}
    }

    static void __startApp()
    {
        m_subcriber = new RedisBase(new RedisSetting(REDIS_TYPE.ONLY_SUBCRIBE, __PORT_READ));
        m_subcriber.PSUBSCRIBE(__SUBCRIBE_IN);
        var bs = new List<byte>();
        while (__running)
        {
            if (!m_subcriber.m_stream.DataAvailable)
            {
                if (bs.Count > 0)
                {
                    var buf = m_subcriber.__getBodyPublish(bs.ToArray(), __SUBCRIBE_IN);
                    bs.Clear();
                    if (buf != null)
                        new Thread(new ParameterizedThreadStart((o) =>
                        __executeBackground((Tuple<string, byte[]>)o))).Start(buf);
                }
                Thread.Sleep(100);
                continue;
            }
            byte b = (byte)m_subcriber.m_stream.ReadByte();
            bs.Add(b);
        }
    }

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

