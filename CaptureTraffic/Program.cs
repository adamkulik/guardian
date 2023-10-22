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
using HtmlAgilityPack;

namespace CaptureTraffic
{
    internal static class Program
    {
        // NOTE: In the next line, you can pass 0 for the port (instead of 8877) to have FiddlerCore auto-select an available port
        private const ushort fiddlerCoreListenPort = 0;

        private static readonly ICollection<Session> sessions = new List<Session>();
        private static AlertCollection alerts = new AlertCollection();
        private static readonly ReaderWriterLockSlim sessionsLock = new ReaderWriterLockSlim();
        public static List<int> flaggedSessionIds = new List<int>();
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
                    if(sentiment != "neutral") flaggedSessionIds.Add(session.id);
                }
                // Console.WriteLine("------------------------------------------------------------------------------");
                // In order to enable response tampering, buffering mode MUST
                // be enabled; this allows FiddlerCore to permit modification of
                // the response in the BeforeResponse handler rather than streaming
                // the response to the client as the response comes in.
                session.oRequest["Accept-Encoding"] = "gzip, deflate";
                session.bBufferResponse = true;

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
                if (flaggedSessionIds.Contains(session.id))
                {
                    session.utilDecodeResponse();
                    string nonce = "8IBTHwOdqNKAWeKl7plt8g==";
                    var header = session.ResponseHeaders.Where(x => x.Name.Contains("Content-Security-Policy")).FirstOrDefault();
                    if (header != null)
                    {
                        int scriptSrcPos = header.Value.IndexOf("nonce-");
                        nonce = String.Concat(header.Value.Skip(scriptSrcPos + 6));
                        int apostrophePos = nonce.IndexOf('\'');
                        nonce = nonce.Substring(0, apostrophePos);
                    }
                    var doc = new HtmlDocument();
                    doc.LoadHtml(Encoding.UTF8.GetString(session.ResponseBody));
                    var body = doc.DocumentNode.SelectSingleNode("//body");
                    body.InnerHtml = "<script nonce=\"" + nonce + "\">\r\n  window.watsonAssistantChatOptions = {\r\n    integrationID: \"9693c7bd-c717-4be4-8305-e0e735d16e8a\", // The ID of this integration.\r\n    region: \"eu-de\", // The region your integration is hosted in.\r\n    serviceInstanceID: \"41448759-31dd-4ac8-b39a-ed05bde7ad72\", // The ID of your service instance.\r\n    openChatByDefault: true,\r\n    onLoad: async (instance) => {\r\n      // The instance returned here has many methods on it that are documented on this page. You can assign it to any\r\n      // global window variable you like if you need to access it in other functions in your application. This instance\r\n      // is also passed as an argument to all event handlers when web chat fires an event.\r\n      window.webChatInstance = instance;\r\n      instance.updateHomeScreenConfig({\r\n  is_on: true,\r\n  greeting: 'Hi, is everything ok?',\r\n  starters: {\r\n    is_on: true,\r\n    buttons: [\r\n      {\r\n        label: 'Please, contact me with therapist'\r\n      }\r\n      \r\n    ]\r\n  }\r\n\r\n\r\n});\r\n\r\n      await instance.render();\r\n    }\r\n  };\r\n \r\n  setTimeout(function(){\r\n    const t=document.createElement('script');\r\n    t.src=\"https://web-chat.global.assistant.watson.appdomain.cloud/versions/\" + (window.watsonAssistantChatOptions.clientVersion || 'latest') + \"/WatsonAssistantChatEntry.js\";\r\n    document.head.appendChild(t);\r\n  });\r\n</script>" + body.InnerHtml;
                    StringWriter modifiedHtmlWriter = new StringWriter();
                    doc.DocumentNode.WriteTo(modifiedHtmlWriter, 0);
                    string modifiedHtml = modifiedHtmlWriter.ToString();
                    session.ResponseBody = Encoding.UTF8.GetBytes(modifiedHtml);
                    //session.utilSetResponseBody(" <!DOCTYPE html>\r\n<html>\r\n\r\n<head>\r\n<title>Page Title</title>\r\n  <meta charset=\"UTF-8\">\r\n  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\r\n  <meta https-equiv=\"Content-Security-Policy\" content=\"script-src 'nonce-ab49Fdd' 'unsafe-inline';\" />\r\n  <title>Custom elements - IBM watsonx Assistant web chat toolkit</title>\r\n  <style>\r\n    body, html {\r\n      width: 100%;\r\n      height: 100%;\r\n      margin: 0;\r\n      padding: 0;\r\n    }\r\n\r\n    body {\r\n      overflow: hidden;\r\n    }\r\n\r\n    .WebChatContainer {\r\n      position: absolute;\r\n      width: 500px;\r\n      right: 0;\r\n      top: 16px;\r\n      bottom: 16px;\r\n    }\r\n\r\n    #WACContainer.WACContainer .WebChatStyles {\r\n      position: relative;\r\n      transition: right 500ms ease-in-out;\r\n    }\r\n\r\n    #WACContainer.WACContainer .HideWebChat {\r\n      display: none;\r\n    }\r\n\r\n    #WACContainer.WACContainer .StartOpenAnimation {\r\n      transition: none;\r\n      right: -500px;\r\n    }\r\n\r\n    #WACContainer.WACContainer .OpenAnimation {\r\n      right: 16px;\r\n    }\r\n\r\n    #WACContainer.WACContainer .CloseAnimation {\r\n      right: -500px;\r\n    }\r\n  </style>\r\n</head>\r\n<body>\r\n\r\n  <div class=\"WebChatContainer\"></div>\r\n\r\n  <script nonce=\""+nonce+"\">\r\n    const customElement = document.querySelector('.WebChatContainer');\r\n    let stylesInitialized = false;\r\n\r\n    /**\r\n     * This function is called after a view change has occurred. It will trigger the animation for the main window and\r\n     * then make the main window hidden or visible after the animation as needed.\r\n     */\r\n    function viewChangeHandler(event, instance) {\r\n      if (!stylesInitialized) {\r\n        // The first time we get this, set the styles to their initial, default state.\r\n        instance.elements.getMainWindow().addClassName('HideWebChat');\r\n        instance.elements.getMainWindow().addClassName('WebChatStyles');\r\n        stylesInitialized = true;\r\n      }\r\n\r\n      const mainWindowChanged = event.oldViewState.mainWindow !== event.newViewState.mainWindow;\r\n      if (mainWindowChanged) {\r\n        if (event.reason === 'sessionHistory') {\r\n          // If we're re-opening web chat from session history, skip the animation by leaving out \"StartOpenAnimation\".\r\n          if (event.newViewState.mainWindow) {\r\n            instance.elements.getMainWindow().addClassName('OpenAnimation');\r\n            instance.elements.getMainWindow().removeClassName('HideWebChat');\r\n          } else {\r\n            instance.elements.getMainWindow().addClassName('HideWebChat');\r\n          }\r\n        } else if (event.newViewState.mainWindow) {\r\n          // Move the main window to the off-screen position and then un-hide it.\r\n          instance.elements.getMainWindow().addClassName('StartOpenAnimation');\r\n          instance.elements.getMainWindow().removeClassName('HideWebChat');\r\n          setTimeout(() => {\r\n            // Give the browser a chance to render the off-screen state and then trigger the open animation.\r\n            instance.elements.getMainWindow().addClassName('OpenAnimation');\r\n            instance.elements.getMainWindow().removeClassName('StartOpenAnimation');\r\n          });\r\n        } else {\r\n          // Trigger the animation to slide the main window to the hidden position.\r\n          instance.elements.getMainWindow().addClassName('CloseAnimation');\r\n          instance.elements.getMainWindow().removeClassName('OpenAnimation');\r\n          setTimeout(() => {\r\n            // After the animation is complete, hide the main window.\r\n            instance.elements.getMainWindow().addClassName('HideWebChat');\r\n            instance.elements.getMainWindow().removeClassName('CloseAnimation');\r\n          }, 500);\r\n        }\r\n      }\r\n    }\r\n\r\n    /**\r\n     * This is the function that is called when the web chat code has been loaded and it is ready to be rendered.\r\n     */\r\n    async function onLoad(instance) {\r\n      // Add listeners so we know when web chat has been opened or closed.\r\n      // See https://web-chat.global.assistant.watson.cloud.ibm.com/docs.html?to=api-events#summary for more about our\r\n      // events.\r\n      instance.on({ type: 'view:change', handler: viewChangeHandler });\r\n\r\n      await instance.render();\r\n    }\r\n\r\n    // This is the standard web chat configuration object. You can modify these values with the embed code for your\r\n    // own assistant if you wish to try this example with your assistant. You can find the documentation for this at\r\n    // https://web-chat.global.assistant.watson.cloud.ibm.com/docs.html?to=api-configuration#configurationobject.\r\nwindow.watsonAssistantChatOptions = {\r\n    integrationID: \"45ed33ce-545f-44ef-b5af-4568ccaa26f6\", // The ID of this integration.\r\n    region: \"eu-de\", // The region your integration is hosted in.\r\n    serviceInstanceID: \"41448759-31dd-4ac8-b39a-ed05bde7ad72\", // The ID of your service instance.\r\n    openChatByDefault: true,\r\n    onLoad: function(instance) { instance.render(); }\r\n  };\r\n  setTimeout(function(){\r\n    const t=document.createElement('script');\r\n    t.src=\"https://web-chat.global.assistant.watson.appdomain.cloud/versions/\" + (window.watsonAssistantChatOptions.clientVersion || 'latest') + \"/WatsonAssistantChatEntry.js\";\r\n    document.head.appendChild(t);\r\n  });\r\n  </script>\r\n</body>\r\n</html> "); 

                }

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

