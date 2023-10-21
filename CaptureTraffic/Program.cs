/*
* This demo program shows how to use the FiddlerCore library.
*/
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Fiddler;
using Telerik.NetworkConnections;
using BCCertMaker;
using System.Web;
using System.Xml.Serialization;

namespace CaptureTraffic
{
    internal static class Program
    {
        // NOTE: In the next line, you can pass 0 for the port (instead of 8877) to have FiddlerCore auto-select an available port
        private const ushort fiddlerCoreListenPort = 0;

        private static readonly ICollection<Session> sessions = new List<Session>();
        private static AlertCollection alerts = new AlertCollection();
        private static readonly ReaderWriterLockSlim sessionsLock = new ReaderWriterLockSlim();

        private static readonly string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static void Main()
        {
            DeserializeAlertList();
            AttachEventListeners();

            EnsureRootCertificate();

            StartupFiddlerCore();

            ExecuteUserCommands();

            Quit();
        }

        private static void DeserializeAlertList()
        {
            var file = File.Open("C:\\Guardian\\alertList.xml", FileMode.OpenOrCreate);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(AlertCollection));
            try
            {
                alerts = (AlertCollection)xmlSerializer.Deserialize(file);
            }
            catch (Exception) { }
            file.Close();
        }

        private static void AttachEventListeners()
        {
            //
            // It is important to understand that FiddlerCore calls event handlers on session-handling
            // background threads.  If you need to properly synchronize to the UI-thread (say, because
            // you're adding the sessions to a list view) you must call .Invoke on a delegate on the 
            // window handle.
            // 
            // If you are writing to a non-threadsafe data structure (e.g. List<T>) you must
            // use a Monitor or other mechanism to ensure safety.
            //

            FiddlerApplication.Log.OnLogString += (o, lea) => Console.WriteLine($"** LogString: {lea.LogString}");

            FiddlerApplication.BeforeRequest += session =>
            {
                // Console.WriteLine(session.id);
                // Console.WriteLine("------------------------------------------------------------------------------");
                if (session.fullUrl.Contains("search?") && !session.fullUrl.Contains("/complete/"))
                {
                    string searchTerm = GetSearchTerm(session.fullUrl);
                    Console.WriteLine("------------------------------------------------------------------------------");
                    Console.WriteLine("Search term: " + searchTerm);

                    SentimentAnalyzer sentimentAnalyzer = new SentimentAnalyzer();
                    string sentiment = SentimentAnalyzer.SyncSentimentAnalyze(searchTerm);
                    Console.WriteLine("Sentiment: " + sentiment);
                    Console.WriteLine("------------------------------------------------------------------------------");
                    WriteAlert(sentiment);
                }
                // Console.WriteLine("------------------------------------------------------------------------------");
                // In order to enable response tampering, buffering mode MUST
                // be enabled; this allows FiddlerCore to permit modification of
                // the response in the BeforeResponse handler rather than streaming
                // the response to the client as the response comes in.
                session.bBufferResponse = false;

                // Set this property if you want FiddlerCore to automatically authenticate by
                // answering Digest/Negotiate/NTLM/Kerberos challenges itself
                // session["X-AutoAuth"] = "(default)";

                try
                {
                    sessionsLock.EnterWriteLock();
                    sessions.Add(session);
                }
                finally
                {
                    sessionsLock.ExitWriteLock();
                }
            };

            /*
            // The following event allows you to examine every response buffer read by Fiddler. Note that this isn't useful for the vast majority of
            // applications because the raw buffer is nearly useless; it's not decompressed, it includes both headers and body bytes, etc.
            //
            // This event is only useful for a handful of applications which need access to a raw, unprocessed byte-stream
            Fiddler.FiddlerApplication.OnReadResponseBuffer += (o, rrea) =>
            {
                // NOTE: arrDataBuffer is a fixed-size array. Only bytes 0 to iCountOfBytes should be read/manipulated.
                //
                // Just for kicks, lowercase every byte. Note that this will obviously break any binary content.
                for (int i = 0; i < e.iCountOfBytes; i++)
                {
                    if ((e.arrDataBuffer[i] > 0x40) && (e.arrDataBuffer[i] < 0x5b))
                    {
                        e.arrDataBuffer[i] = (byte)(e.arrDataBuffer[i] + (byte)0x20);
                    }
                }
                Console.WriteLine(String.Format("Read {0} response bytes for session {1}", e.iCountOfBytes, e.sessionOwner.id));
            }
            */


            Fiddler.FiddlerApplication.BeforeResponse += session =>
            {
                //Console.WriteLine($"{session.id}:HTTP {session.responseCode} for {session.fullUrl}");

                // Uncomment the following two statements to decompress/unchunk the
                // HTTP response and subsequently modify any HTTP responses to replace 
                // instances of the word "Telerik" with "Progress". You MUST also
                // set session.bBufferResponse = true inside the BeforeRequest event handler above.
                //
                session.utilDecodeResponse();

            };

            FiddlerApplication.AfterSessionComplete += session =>
            {
                //Console.WriteLine($"Finished session: {oS.fullUrl}");

                int sessionsCount = 0;
                try
                {
                    sessionsLock.EnterReadLock();
                    sessionsCount = sessions.Count;
                }
                finally
                {
                    sessionsLock.ExitReadLock();
                }

                if (sessionsCount == 0)
                    return;

                Console.Title = $"Session list contains: {sessionsCount} sessions";
            };

            // Tell the system console to handle CTRL+C by calling our method that
            // gracefully shuts down the FiddlerCore.
            //
            // Note, this doesn't handle the case where the user closes the window with the close button.
            Console.CancelKeyPress += (o, ccea) =>
            {
                Quit();
            };
        }

        private static string GetSearchTerm(string fullUrl)
        {
            string[] reqParams = fullUrl.Split(new char[] { '?', '&' });
            foreach (string param in reqParams)
            {
                if (param.StartsWith("q="))
                {
                    string filteredParam = String.Concat(param.Skip(2));
                    filteredParam = HttpUtility.UrlDecode(filteredParam);
                    return filteredParam;
                }
            }
            return "";
        }

        private static void EnsureRootCertificate()
        {
            BCCertMaker.BCCertMaker certProvider = new BCCertMaker.BCCertMaker();
            CertMaker.oCertProvider = certProvider;

            // On first run generate root certificate using the loaded provider, then re-use it for subsequent runs.
            string rootCertificatePath = Path.Combine(assemblyDirectory, "..", "..", "RootCertificate.p12");
            string rootCertificatePassword = "S0m3T0pS3cr3tP4ssw0rd";
            if (!File.Exists(rootCertificatePath))
            {
                certProvider.CreateRootCertificate();
                certProvider.WriteRootCertificateAndPrivateKeyToPkcs12File(rootCertificatePath, rootCertificatePassword);
            }
            else
            {
                certProvider.ReadRootCertificateAndPrivateKeyFromPkcs12File(rootCertificatePath, rootCertificatePassword);
            }

            // Once the root certificate is set up, ensure it's trusted.
            if (!CertMaker.rootCertIsTrusted())
            {
                CertMaker.trustRootCert();
            }
        }

        private static void StartupFiddlerCore()
        {
            FiddlerCoreStartupSettings startupSettings =
                new FiddlerCoreStartupSettingsBuilder()
                    .ListenOnPort(fiddlerCoreListenPort)
                    .RegisterAsSystemProxy()
                    .ChainToUpstreamGateway()
                    .DecryptSSL()
                    .OptimizeThreadPool()
                    .Build();

            FiddlerApplication.Startup(startupSettings);

            FiddlerApplication.Log.LogString($"Created endpoint listening on port {CONFIG.ListenPort}");
        }

        private static void ExecuteUserCommands()
        {
            bool done = false;
            do
            {
                ConsoleKeyInfo cki = Console.ReadKey();
                switch (char.ToLower(cki.KeyChar))
                {
                    case 'q':
                        done = true;
                        break;
                }
            } while (!done);
        }

        private static void Quit()
        {
            WriteCommandResponse("Shutting down...");

            FiddlerApplication.Shutdown();
        }
        private static void WriteCommandResponse(string s)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(s);
            Console.ForegroundColor = oldColor;
        }

        private static void WriteAlert(string sentiment)
        {
            var file = File.Create("C:\\Guardian\\alertList.xml");
            AlertRecord record = new AlertRecord();
            record.alertTime = DateTime.Now;
            record.alertText = sentiment;
            alerts.AlertRecords.Add(record);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(AlertCollection));
            xmlSerializer.Serialize(file, alerts);
            file.Close();
        }
    }
}

