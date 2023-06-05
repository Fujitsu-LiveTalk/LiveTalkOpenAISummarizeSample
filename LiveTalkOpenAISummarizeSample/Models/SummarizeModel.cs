using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LiveTalkOpenAISummarizeSample.Models
{
    internal class SummarizeModel : INotifyPropertyChanged
    {
        private const string UrlString = "https://{0}.openai.azure.com/{1}";
        private HttpClient SendClient = null;
        private long LineCount = 0;
        private string Resource;
        private string AccessKey;

        /// <summary>
        /// 連携ファイル名
        /// </summary>
        private string _FileName = Common.Config.GetConfig("FileName");
        public string FileName
        {
            get { return this._FileName; }
            internal set
            {
                if (this._FileName != value)
                {
                    this._FileName = value;
                    OnPropertyChanged();
                    Common.Config.SetConfig("FileName", value);
                }
            }
        }

        /// <summary>
        /// プロンプト
        /// </summary>
        private string _Prompt = Common.Config.GetConfig("Prompt");
        public string Prompt
        {
            get { return this._Prompt; }
            internal set
            {
                if (this._Prompt != value)
                {
                    this._Prompt = value;
                    OnPropertyChanged();
                    Common.Config.SetConfig("Prompt", value);
                }
            }
        }

        /// <summary>
        /// 処理中メッセージ
        /// </summary>
        private string _Message = string.Empty;
        public string Message
        {
            get { return this._Message; }
            internal set
            {
                if (this._Message != value)
                {
                    this._Message = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 要約
        /// </summary>
        private string _Result = String.Empty;
        public string Result
        {
            get { return this._Result; }
            set
            {
                if (this._Result != value)
                {
                    this._Result = value;
                    OnPropertyChanged();
                }
            }
        }

        public SummarizeModel()
        {
            this.Resource = Common.Config.GetConfig("APIResourceName");
            this.AccessKey = Common.Config.GetConfig("APIKey");
        }

        internal async Task Convert()
        {
            var source = this.FileName;

            try
            {
                // ファイルからの入力は非同期に実施する
                await Task.Run(async () =>
                {
                    long seqNo = 0;
                    var reg = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    var context = string.Empty;

                    try
                    {
                        //　CSV入力
                        using (var fs = new System.IO.FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (var sr = new StreamReader(fs, Encoding.UTF8))
                            {

                                // ファイルの終わりまで入力する
                                while (!sr.EndOfStream)
                                {
                                    var s = sr.ReadLine();
                                    var items = reg.Split(s);
                                    var messageTime = DateTime.Parse(items[0].Substring(1, items[0].Length - 2));
                                    var name = items[1].Substring(1, items[1].Length - 2);
                                    var message = items[2].Substring(1, items[2].Length - 2);
                                    var translateText = items[3].Substring(1, items[3].Length - 2);

                                    // シーケンス番号
                                    this.Message = $"Read CSV File : SeqNo={++seqNo}";

                                    if (!string.IsNullOrEmpty(translateText)) message = translateText;
                                    context += message + "\n";
                                }
                                sr.Close();
                            }
                        }
                        this.Message = "Get Summarize";

                        // 要約
                        {
                            var result = await GetSummarizeAsync(context);

                            this.Result = !string.IsNullOrEmpty(result) ? result : this.Result;
                            this.Message = $"Summarized {seqNo} -> {this.Result.Split(Environment.NewLine).Count()}";
                        }
                    }
                    catch (Exception ex)
                    {
                        OnThrew(ex);
                    }
                });
            }
            catch { }
        }

        internal async Task<string> GetSummarizeAsync(string inputData)
        {
            var summarizeText = string.Empty;
            var version = "2023-05-15";
            var deploymentName = "text-davinci-003";
            var body = new TRequest()
            {
                prompt = $"{Prompt}\n\n{inputData}",
                temperature = (float)0.3,       // Temperature
                max_tokens = 250,               // MaxTokens
                top_p = (float)1,               // NucleusSamplingFactor
                frequency_penalty = (float)0,   // FrequencyPenalty
                presence_penalty = (float)0,    // PresencePenalty
                best_of = (float)1,             // GenerationSampleCount
            };

            try
            {
                if (this.SendClient == null)
                {
                    this.SendClient = new HttpClient();
                }
                if (this.SendClient != null)
                {
                    var requestBody = "";

                    using (var ms = new MemoryStream())
                    {
                        var serializer = new DataContractJsonSerializer(typeof(TRequest));
                        using (var sr = new StreamReader(ms))
                        {
                            serializer.WriteObject(ms, body);
                            ms.Position = 0;
                            requestBody = sr.ReadToEnd();
                        }
                    }

                    using (var request = new HttpRequestMessage())
                    {
                        request.Method = HttpMethod.Post;
                        request.RequestUri = new Uri(string.Format(UrlString, this.Resource, $"openai/deployments/{deploymentName}/completions?api-version={version}"));
                        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                        request.Headers.Add("api-key", AccessKey);

                        using (var response = await this.SendClient.SendAsync(request))
                        {
                            response.EnsureSuccessStatusCode();
                            var jsonString = await response.Content.ReadAsStringAsync();
                            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                            {
                                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TResult));
                                {
                                    var completionsResponse = ser.ReadObject(json) as TResult;
                                    if (completionsResponse is not null)
                                    {
                                        foreach (var choice in completionsResponse.choices)
                                        {
                                            summarizeText += choice.text + Environment.NewLine;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.SendClient = null;
                throw ex;
            }
            return summarizeText;
        }

        #region "TRequest"
        [DataContract]
        public class TRequest
        {
            [DataMember]
            public string prompt { get; set; }
            [DataMember]
            public int max_tokens { get; set; }
            [DataMember]
            public float temperature { get; set; }
            [DataMember]
            public float frequency_penalty { get; set; }
            [DataMember]
            public float presence_penalty { get; set; }
            [DataMember]
            public float top_p { get; set; }
            [DataMember]
            public float? best_of { get; set; }     // 
        }
        #endregion

        #region "TResult"
        [DataContract]
        public class TResult
        {
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public int created { get; set; }
            [DataMember]
            public string model { get; set; }
            [DataMember]
            public Choice[] choices { get; set; }
            [DataMember]
            public Usage usage { get; set; }
        }

        [DataContract]
        public class Usage
        {
            [DataMember]
            public int completion_tokens { get; set; }
            [DataMember]
            public int prompt_tokens { get; set; }
            [DataMember]
            public int total_tokens { get; set; }
        }

        [DataContract]
        public class Choice
        {
            [DataMember]
            public string text { get; set; }
            [DataMember]
            public int index { get; set; }
            [DataMember]
            public string finish_reason { get; set; }
            [DataMember]
            public object logprobs { get; set; }
        }
        #endregion

        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public event ErrorEventHandler Threw;
        protected virtual void OnThrew(Exception ex)
        {
            this.Threw?.Invoke(this, new ErrorEventArgs(ex));
        }

        /// <summary>
        /// プロパティ変更通知
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
