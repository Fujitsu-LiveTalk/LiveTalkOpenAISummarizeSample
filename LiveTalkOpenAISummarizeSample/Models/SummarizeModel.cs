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
        private const string UrlString = "https://{0}.openai.azure.com/openai/deployments/{1}/chat/completions?api-version={2}";
        private HttpClient SendClient = null;

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
        /// DeploymentName
        /// </summary>
        private string _DeploymentName = Common.Config.GetConfig("DeploymentName");
        public string DeploymentName
        {
            get { return this._DeploymentName; }
            internal set
            {
                if (this._DeploymentName != value)
                {
                    this._DeploymentName = value;
                    OnPropertyChanged();
                    Common.Config.SetConfig("DeploymentName", value);
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
        /// APIResourceName
        /// </summary>
        private string _APIResourceName = Common.Config.GetConfig("APIResourceName");
        public string APIResourceName
        {
            get { return this._APIResourceName; }
            internal set
            {
                if (this._APIResourceName != value)
                {
                    this._APIResourceName = value;
                    OnPropertyChanged();
                    Common.Config.SetConfig("APIResourceName", value);
                }
            }
        }

        /// <summary>
        /// APIKey
        /// </summary>
        private string _APIKey = Common.Config.GetConfig("APIKey");
        public string APIKey
        {
            get { return this._APIKey; }
            internal set
            {
                if (this._APIKey != value)
                {
                    this._APIKey = value;
                    OnPropertyChanged();
                    Common.Config.SetConfig("APIKey", value);
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
        }

        internal async Task Convert()
        {
            var source = this.FileName;

            try
            {
                this.FileName = Common.Config.GetConfig("FileName");
                this.DeploymentName = Common.Config.GetConfig("DeploymentName");
                this.APIResourceName = Common.Config.GetConfig("APIResourceName");
                this.APIKey = Common.Config.GetConfig("APIKey");
                this.Result = string.Empty;

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
            var body = new TRequest()
            {
                messages = new TMessage[]
                {
                    new TMessage()
                    {
                        role = "system",
                        content = this.Prompt,
                    },
                    new TMessage()
                    {
                        role = "user",
                        content = inputData,
                    },
                },
                temperature = (float)0.3,       // Temperature(出力の多様性を30%に指定)
                max_tokens = 1000,              // MaxTokens(生成されるレスポンスのトークンの最大数)
                top_p = (float)1,               // NucleusSamplingFactor(上位１00%の結果を利用)
                frequency_penalty = (float)0,   // FrequencyPenalty
                presence_penalty = (float)0,    // PresencePenalty
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
                        request.RequestUri = new Uri(string.Format(UrlString, this.APIResourceName, this.DeploymentName, version));
                        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                        request.Headers.Add("api-key", this.APIKey);

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
                                            summarizeText += choice.message.content + Environment.NewLine;
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
            public TMessage[] messages { get; set; }
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
        }

        [DataContract]
        public class TMessage
        {
            [DataMember]
            public string role { get; set; }
            [DataMember]
            public string content { get; set; }
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
            public TChoice[] choices { get; set; }
            [DataMember]
            public TUsage usage { get; set; }
        }

        [DataContract]
        public class TUsage
        {
            [DataMember]
            public int completion_tokens { get; set; }
            [DataMember]
            public int prompt_tokens { get; set; }
            [DataMember]
            public int total_tokens { get; set; }
        }

        [DataContract]
        public class TChoice
        {
            [DataMember]
            public TMessage message { get; set; }
            [DataMember]
            public int index { get; set; }
            [DataMember]
            public string finish_reason { get; set; }
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
