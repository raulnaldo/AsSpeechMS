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
using CognitiveServicesTTS;
using System.Net;
using Newtonsoft.Json;
using System.Xml;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AsAsrMicrosoft
{
    public class AsAsrMicrosoft
    {
        private static DataRecognitionClient dataClient;
        private static ILog log;

        private static string AuthenticationUri = "";
        private static string SubscriptionKey = string.Empty;        
        private static SpeechRecognitionMode Mode = SpeechRecognitionMode.ShortPhrase;
        private static string DefaultLocale = "es-ES";
        private string Logs4NetDir;
        private string UserName;
        private string DebugDir;
        private static string FileNameTTS;
        private static string RecognizedStatement = string.Empty;
        private static Boolean PlayTtsStatus;

        public AsAsrMicrosoft(string pLogs4NetDir, string pDebugDir, string pUserName, string pSubscriptionKey)
        {
            try
            {
                this.Logs4NetDir = pLogs4NetDir;
                this.UserName = pUserName;
                this.DebugDir = pDebugDir;
                SubscriptionKey = pSubscriptionKey;
                if (log == null)
                {
                    //log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(Logs4NetDir + "\\MWDDB_logs4netConfig"));
                    log4net.GlobalContext.Properties["agent"] = UserName;
                    log4net.GlobalContext.Properties["debugDir"] = DebugDir;
                    log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(pLogs4NetDir + "//logs4netConfig.xml"));
                    AsAsrMicrosoft.log = LogManager.GetLogger(typeof(AsAsrMicrosoft));
                }
                log.Info(string.Format("-->[Constructor] public AsAsrMicrosoft(Version:{0})", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
                log.Debug(string.Format("    --> pLogs4NetDir:[{0}]", Logs4NetDir));
                log.Debug(string.Format("    --> pUserName:----------[{0}]", UserName));
                log.Debug(string.Format("    --> pDebugDir:----------[{0}]", DebugDir));
                log.Debug(string.Format("    --> pSubscriptionKey:--[{0}]", pSubscriptionKey));
                log.Info("<--[Constructor] public AsAsrMicrosoft()");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
        #region ASR #######################################################################
        public string Recognice(string pFile, int pTimeOut)
        {
            try
            {
                log.Info(string.Format("-->Recognice(File:{0},TimeOut:{1})", pFile, pTimeOut.ToString()));
                RecognizedStatement = string.Empty;
                log.Info("  --> new ParameterizedThreadStart(StartProcess)()");
                Thread ThInvoke = new Thread(new ParameterizedThreadStart(StartProcess));
                log.Info("  <-- new ParameterizedThreadStart(StartProcess)()");
                ThInvoke.IsBackground = true;
                log.Info("  --> ThInvoke.Start()");
                ThInvoke.Start(pFile);
                bool final = ThInvoke.Join(pTimeOut * 1000);
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
                    log.Debug(string.Format(" -- Waiting[{0}]", (pTimeOut - i).ToString()));
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
            finally
            {
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

        #endregion -----------------------------------------------------------------

        #region TTS ###########################################################
        
        public Boolean PlayTTS(string pText, string pVoice, string pFile)
        {
            PlayTtsStatus = false;
            string TTsVoiceLocale = "es-ES";
            CognitiveServicesTTS.Gender TTsVoiceGender=Gender.Female;
            string TTsVoiceName = "Microsoft Server Speech Text to Speech Voice (es-ES, Laura, Apollo)";
            try
            {
                FileNameTTS = pFile;
                log.Debug("###########################################################################");
                log.Debug("-->PlayTTS()");
                log.Debug(string.Format("  >TTS Text--[{0}]", pText));
                log.Debug(string.Format("  >TTS Voice-[{0}]", pVoice));
                log.Debug(string.Format("  >TTS File--[{0}]", pFile));

                log.Debug(string.Format(" ->Selecting Voice params from pVoice--[{0}]", pVoice));
                switch (pVoice)
                {
                    case "es-Es-Laura":
                        TTsVoiceLocale = "es-ES";
                        TTsVoiceGender=Gender.Female;
                        TTsVoiceName = "Microsoft Server Speech Text to Speech Voice (es-ES, Laura, Apollo)";
                        break;
                    case "es-Es-Helena":
                        TTsVoiceLocale = "es-ES";
                        TTsVoiceGender=Gender.Female;
                        TTsVoiceName = "Microsoft Server Speech Text to Speech Voice (es-ES, HelenaRUS)";
                        break;
                    case "es-Es-Pablo":
                        TTsVoiceLocale = "es-ES";
                        TTsVoiceGender=Gender.Male;
                        TTsVoiceName = "Microsoft Server Speech Text to Speech Voice (es-ES, Pablo, Apollo)";
                        break;
                    case "en-US-Zira":
                        TTsVoiceLocale = "en-US";
                        TTsVoiceGender = Gender.Female;
                        TTsVoiceName = "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)";
                        break;
                    case "en-US-Benjamin":
                        TTsVoiceLocale = "en-US";
                        TTsVoiceGender = Gender.Male;
                        TTsVoiceName = "Microsoft Server Speech Text to Speech Voice (en-US, BenjaminRUS)";
                        break;
                    default:
                        log.Debug(string.Format("   - Default Case, getting default values"));
                        break;
                }
                log.Debug(string.Format(" <-Selecting Voice params from pVoice--[{0}-{1}-{2}]", TTsVoiceLocale, TTsVoiceGender.ToString(), TTsVoiceName));

                string accessToken;

                // Note: The way to get api key:
                // Free: https://www.microsoft.com/cognitive-services/en-us/subscriptions?productId=/products/Bing.Speech.Preview
                // Paid: https://portal.azure.com/#create/Microsoft.CognitiveServices/apitype/Bing.Speech/pricingtier/S0
                Authentication auth = new Authentication(SubscriptionKey);

                try
                {
                    accessToken = auth.GetAccessToken();
                    log.Debug(string.Format("Token: {0}\n", accessToken));
                }
                catch (Exception ex)
                {
                    log.Debug("Failed authentication.");
                    log.Error(ex);
                    return PlayTtsStatus;
                }

                string requestUri = "https://speech.platform.bing.com/synthesize";                
                log.Debug(string.Format(" <--> Request Uri[{0}]", requestUri));

                log.Debug(string.Format(" --> Creating Cortana()"));
                var cortana = new Synthesize();
                log.Debug(string.Format(" <-- Creating Cortana()"));

                log.Debug(string.Format(" --> Registering cortana.OnAudioAvailable"));
                cortana.OnAudioAvailable += PlayAudio;
                log.Debug(string.Format(" <-- Registering cortana.OnAudioAvailable"));
                log.Debug(string.Format(" --> Registering cortana.OnError"));
                cortana.OnError += ErrorHandler;
                log.Debug(string.Format(" <-- Registering cortana.OnError"));

                // Reuse Synthesize object to minimize latency
                log.Debug(string.Format(" --> cortana.Speak()"));
                log.Debug(string.Format("**************************"));
                log.Debug(string.Format(" <--> Request Uri--------[{0}]", requestUri));
                log.Debug(string.Format(" <--> Text---------------[{0}]", pText));
                log.Debug(string.Format(" <--> VoiceType----------[{0}]", TTsVoiceGender.ToString()));
                log.Debug(string.Format(" <--> Locale-------------[{0}]", TTsVoiceLocale));
                log.Debug(string.Format(" <--> VoiceName----------[{0}]", TTsVoiceName));
                log.Debug(string.Format(" <--> OutputFormat-------[{0}]", AudioOutputFormat.Riff16Khz16BitMonoPcm.ToString()));
                log.Debug(string.Format(" <--> AuthorizationToken-[Bearer {0}]", accessToken));
                log.Debug(string.Format("**************************"));
                log.Debug(string.Format(" <--> Request Uri[{0}]", requestUri));
                cortana.Speak(CancellationToken.None, new Synthesize.InputOptions()
                {
                    RequestUri = new Uri(requestUri),
                    // Text to be spoken.
                    Text = pText,
                    VoiceType = TTsVoiceGender,
                    //VoiceType = Gender.Male,
                    // Refer to the documentation for complete list of supported locales.
                    Locale = TTsVoiceLocale,
                    // You can also customize the output voice. Refer to the documentation to view the different
                    // voices that the TTS service can output.
                    //VoiceName = "Microsoft Server Speech Text to Speech Voice (es-ES, HelenaRUS)",
                    //VoiceName = "Microsoft Server Speech Text to Speech Voice (es-ES, Pablo, Apollo)"
                    //VoiceName = "Microsoft Server Speech Text to Speech Voice (es-ES, Laura, Apollo)"
                    VoiceName = TTsVoiceName,
                    // Service can return audio in different output format.
                    OutputFormat = AudioOutputFormat.Riff16Khz16BitMonoPcm,
                    AuthorizationToken = "Bearer " + accessToken,
                }).Wait();
                log.Debug(string.Format(" <-- cortana.Speak()"));
            }
            catch (Exception exc)
            {
                log.Error(exc);
                if (exc.GetType().ToString() == "System.Net.WebException")
                {
                    log.Error(
                        ((System.Net.WebException)exc).Response.ToString()
                        );
                }
            }
            finally
            {
                log.Debug(string.Format("<--PlayTTS({0})", PlayTtsStatus.ToString()));
                log.Debug("+++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            }
            return PlayTtsStatus;
        }

        private static void PlayAudio(object sender, GenericEventArgs<Stream> args)
        {
            try
            {
                log.Debug("  --> PlayAudio()");
                log.Debug(string.Format("Evt arg[{0}]", args.EventData.ToString()));

                // For SoundPlayer to be able to play the wav file, it has to be encoded in PCM.
                // Use output audio format AudioOutputFormat.Riff16Khz16BitMonoPcm to do that.
                log.Debug("   --> Building Stream Object()");
                Stream FileToWriteStream = args.EventData;
                log.Debug("   <-- Building Stream Object()");

                log.Debug(string.Format("   --> Saving into file({0})", FileNameTTS));
                using (Stream output = File.OpenWrite(FileNameTTS))
                using (Stream input = FileToWriteStream)
                {
                    input.CopyTo(output);
                }
                log.Debug(string.Format("   <-- Saving into file()"));
                log.Debug("   --> args.EventData.Dispose()");
                args.EventData.Dispose();
                log.Debug("   <-- args.EventData.Dispose()");
                PlayTtsStatus = true;
            }
            catch (Exception ex)
            {
                log.Error(ex);                
            }
            finally
            {
                log.Debug("  <-- PlayAudio()");
            }
        }

        /// <summary>
        /// Handler an error when a TTS request failed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="GenericEventArgs{Exception}"/> instance containing the event data.</param>
        private static void ErrorHandler(object sender, GenericEventArgs<Exception> e)
        {
            log.Error(string.Format("ErrorHandler [{0}]", e.ToString()));
        }

        #endregion ------------------------------------------------------------
        #region call WebService Rest LUIS

        public bool GetLuisAnswer(string pText, ref LuisOutputResponse pResponse)
        {
            log.Debug("-->GetLuisAnswer()");
            pResponse = new LuisOutputResponse();
            bool Status = false;
            string Json = string.Empty;            
            try
            {
                log.Debug(string.Format("************************"));                
                log.Debug(string.Format("   > pText[{0}]", pText));                
                var url = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/bb0c5997-06bf-49d6-8499-f2782d4c2ec6?subscription-key=2797d44c476a440fb259a71880564f4f&staging=true&verbose=true&timezoneOffset=1.0&q=";
                log.Debug(string.Format("   > url[{0}]", url));
                url = string.Format("{0}{1}", url, pText);
                log.Debug(string.Format("   > Req[{0}]", url));
                log.Debug(string.Format("************************"));

                log.Debug(string.Format("   --> System.Net.WebRequest.Create({0})", url));
                var webrequest = (HttpWebRequest)System.Net.WebRequest.Create(url);
                log.Debug(string.Format("   <-- System.Net.WebRequest.Create()" ));
                log.Debug(string.Format("   --> webrequest.GetResponse()"));
                using (var response = webrequest.GetResponse())                
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    log.Debug(string.Format("   --> reader.ReadToEnd()"));
                    var result = reader.ReadToEnd();
                    log.Debug(string.Format("   <-- reader.ReadToEnd()"));
                    log.Debug(string.Format("   --> Convert.ToString from Json()"));
                    Json = Convert.ToString(result);
                    log.Debug(string.Format("   <-- Convert.ToString from Json()"));
                    log.Debug(Json);
                    log.Debug(string.Format("   --> ParseFromLuis.FromJson(Json)"));
                    ParseFromLuis LuisResponse = new ParseFromLuis();
                    LuisResponse = ParseFromLuis.FromJson(Json);
                    log.Debug(string.Format("   <-- ParseFromLuis.FromJson(Json)"));
                    log.Debug(string.Format("   <-- LuisResponse.Query({0})", LuisResponse.Query));
                    pResponse.query = LuisResponse.Query;
                    log.Debug(string.Format("   --> pResponse.topScoringIntent()"));
                    pResponse.topScoringIntent = LuisResponse.TopScoringIntent.Intent;
                    log.Debug(string.Format("   <-- pResponse.topScoringIntent({0})", pResponse.topScoringIntent.ToString()));

                    log.Debug(string.Format("   --> pResponse.topScoringScore()"));
                    pResponse.topScoringScore= LuisResponse.TopScoringIntent.Score;
                    log.Debug(string.Format("   <-- pResponse.topScoringScore()", pResponse.topScoringScore));
                    
                    foreach (Entity MyEntity in LuisResponse.Entities)
                    {
                        if (MyEntity.Type == "Localizacion::Origen")
                        {
                            pResponse.EntityFrom = MyEntity.PurpleEntity;
                            pResponse.EntityFromScore = MyEntity.Score;
                            pResponse.EntityFromType = MyEntity.Type;
                        }
                        if (MyEntity.Type == "Localizacion::Destino")
                        {
                            pResponse.EntityTo = MyEntity.PurpleEntity;
                            pResponse.EntityToScore= MyEntity.Score;
                            pResponse.EntityType= MyEntity.Type;
                        }
                    }
                    Status = true;
                }
                log.Debug(string.Format("   <-- webrequest.GetResponse()"));
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                log.Debug(string.Format("<--GetLuisAnswer({0})", Status.ToString()));
            }            
            return Status;
        }

        #endregion
    }
}
