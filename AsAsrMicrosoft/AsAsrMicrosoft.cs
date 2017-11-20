using System;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Threading;
using log4net;
using log4net.Config;
using Microsoft.CognitiveServices.SpeechRecognition;

namespace AsAsrMicrosoft
{
    public class AsAsrMicrosoft
    {
        private static DataRecognitionClient dataClient;
        private static ILog log;

        private static string AuthenticationUri = "";
        private static string SubscriptionKey = "4a1caba1da7446ffa7731d53e3026c9a";
        private static SpeechRecognitionMode Mode = SpeechRecognitionMode.ShortPhrase;
        private static string DefaultLocale = "es-ES";
        private string Logs4NetDir;
        private string UserName;
        private string DebugDir;
        private static string RecognizedStatement = string.Empty;

        public AsAsrMicrosoft(string pLogs4NetDir, string pDebugDir, string pUserName)
        {
            try
            {
                this.Logs4NetDir = pLogs4NetDir;
                this.UserName = pUserName;
                this.DebugDir = pDebugDir;
                if (log == null)
                {
                    //log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(Logs4NetDir + "\\MWDDB_logs4netConfig"));
                    log4net.GlobalContext.Properties["agent"] = UserName;
                    log4net.GlobalContext.Properties["debugDir"] = DebugDir;
                    log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(pLogs4NetDir +"//logs4netConfig.xml"));
                    AsAsrMicrosoft.log = LogManager.GetLogger(typeof(AsAsrMicrosoft));
                }
                log.Info(string.Format("-->[Constructor] public AsAsrMicrosoft(Version:{0})", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
                log.Debug(string.Format("    --> pLogs4NetDir:[{0}]", Logs4NetDir));
                log.Debug(string.Format("    --> pUserName:---[{0}]", UserName));
                log.Debug(string.Format("    --> pDebugDir:---[{0}]", DebugDir));
                log.Info("<--[Constructor] public AsAsrMicrosoft()");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
        public string Recognice(string pFile, int pTimeOut){
            try
            {                
                log.Info(string.Format("-->Recognice(File:{0},TimeOut:{1})",pFile,pTimeOut.ToString()));                                
                RecognizedStatement = string.Empty;
                log.Info("  --> new ParameterizedThreadStart(StartProcess)()");
                Thread ThInvoke = new Thread(new ParameterizedThreadStart(StartProcess));
                log.Info("  <-- new ParameterizedThreadStart(StartProcess)()");
                ThInvoke.IsBackground = true;
                log.Info("  --> ThInvoke.Start()");
                ThInvoke.Start(pFile);
                bool final = ThInvoke.Join(pTimeOut*1000);
                if (!final)
                {
                    log.Debug(" --> ThInvoke.Abort()");
                    ThInvoke.Abort();
                    log.Debug(" <-- ThInvoke.Abort()");
                    log.Info("  <-- ThInvoke.Start()");
                }
                else
                {
                    log.Info("  <-- ThInvoke.Start()");
                }
                int i = 0;
                log.Info(" --> WaitToProcessEnd()");
                while (string.IsNullOrEmpty(RecognizedStatement))
                {                    
                    log.Debug(string.Format(" -- Waiting[{0}]", (pTimeOut-i).ToString()));
                    Thread.Sleep(1000);
                    if (i >= pTimeOut)
                    {
                        log.Info("    <-- Exit For TimeOut()");
                        break;
                    }
                    i = i + 1;
                }
                log.Info(" <-- WaitToProcessEnd()");

            }
            catch (Exception ex)
            {
                log.Error(ex);                
            }
            finally{
                log.Info(string.Format("<--Recognice({0})", RecognizedStatement));
            }
            return RecognizedStatement;
        }

        private static void StartProcess(object pFile)
        {
            
            CreateDataRecoClient();
            SendAudioHelper((string)pFile);  
        }

        private static void SendAudioHelper(string wavFileName)
        {
            log.Debug(" --> SendAudioHelper()");
            using (FileStream fileStream = new FileStream(wavFileName, FileMode.Open, FileAccess.Read))
            {
                // Note for wave files, we can just send data from the file right to the server.
                // In the case you are not an audio file in wave format, and instead you have just
                // raw data (for example audio coming over bluetooth), then before sending up any 
                // audio data, you must first send up an SpeechAudioFormat descriptor to describe 
                // the layout and format of your raw audio data via DataRecognitionClient's sendAudioFormat() method.
                int bytesRead = 0;
                byte[] buffer = new byte[1024];

                try
                {
                    do
                    {
                        // Get more Audio data to send into byte buffer.
                        bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                        // Send of audio data to service. 
                        dataClient.SendAudio(buffer, bytesRead);
                    }
                    while (bytesRead > 0);
                }
                finally
                {
                    // We are done sending audio.  Final recognition results will arrive in OnResponseReceived event call.
                    dataClient.EndAudio();
                    log.Debug(" <-- SendAudioHelper()");
                }
            }
        }
        private static void CreateDataRecoClient()
        {
            dataClient = SpeechRecognitionServiceFactory.CreateDataClient(
                Mode,
                DefaultLocale,
                SubscriptionKey);
            dataClient.AuthenticationUri = AuthenticationUri;

            // Event handlers for speech recognition results
            if (Mode == SpeechRecognitionMode.ShortPhrase)
            {
                dataClient.OnResponseReceived += OnDataShortPhraseResponseReceivedHandler;
            }
            else
            {
                dataClient.OnResponseReceived += OnDataDictationResponseReceivedHandler;
            }

            dataClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            dataClient.OnConversationError += OnConversationErrorHandler;
        }
        private static void OnDataShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            log.Debug(string.Format("    --> OnDataShortPhraseResponseReceivedHandler:Results:[{0}]", e.PhraseResponse.Results.Length.ToString()));
            log.Debug(string.Format("    --> RecognitionStatus:[{0}]", e.PhraseResponse.RecognitionStatus.ToString()));            
            foreach (RecognizedPhrase frase in e.PhraseResponse.Results)
            {
                log.Debug(string.Format("    --> DisplayText:[{0}]", frase.DisplayText));
                log.Debug(string.Format("    --> Confidence:[{0}]", frase.Confidence.ToString()));
                if (frase.Confidence == Confidence.High)
                {
                    RecognizedStatement = frase.DisplayText;
                    break;
                }
            }
            log.Debug(string.Format("    <-- OnDataShortPhraseResponseReceivedHandler"));
        }
        /// Called when a final response is received;
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private static void OnDataDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            log.Debug(string.Format("    --> OnDataDictationResponseReceivedHandler:[{0}]", e.PhraseResponse.Results[0].DisplayText));
            
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            {

            }            
        }
        private static void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            log.Debug(string.Format("    --> OnPartialResponseReceivedHandler:[{0}]", e.PartialResult));
        }
        private static void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {
            log.Debug(string.Format("    --> OnConversationErrorHandler:[{0}]", e.SpeechErrorText));
        }
    }
}
