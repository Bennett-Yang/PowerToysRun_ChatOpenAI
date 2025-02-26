using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Wox.Plugin.Common;
using System.Net.Http;
using Wox.Plugin;
using System.Configuration;
using Microsoft.PowerToys.Settings.UI.Library;
using System.Linq;
using System.IO;
using System.Windows.Input;
using ManagedCommon;
using System.Threading.Tasks;
using System.Text.Json;


namespace Community.PowerToys.Run.Plugin.ChatOpenAI
{
    public class Main : IPlugin, IDelayedExecutionPlugin, ISettingProvider, IDisposable
    {
        public string Name => "ChatOpenAI";
        public string Description => "Chat with your AI in PowerToys Run";
        public static string PluginID => "CF3C52EDC4314C059896FC403D6AFCDC";

        private string OpenAIBaseURL { get; set; }
        private string OpenAIAPIKey { get; set; }
        private bool isEndCharacterEnabled { get; set; }
        private string EndCharacter { get; set; }

#nullable enable
        private PluginInitContext? Context { get; set; }

        private string? IconPath { get; set; }
#nullable disable

        private bool Disposed { get; set; }

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => [
            new()
            {
                Key = nameof(OpenAIBaseURL),
                DisplayLabel = "OpenAI base URL",
                DisplayDescription = "Your OpenAI base URL",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = OpenAIBaseURL,
            },
            new()
            {
                Key = nameof(OpenAIAPIKey),
                DisplayLabel = "OpenAI API key",
                DisplayDescription = "Your OpenAI API key",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = OpenAIAPIKey,
            },
            new()
            {
                Key = nameof(EndCharacter),
                DisplayLabel = "End character",
                DisplayDescription = "End character. Only answer you after you type the end character. Period by Default.",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.CheckboxAndTextbox,
                TextValue = EndCharacter,
                Value = isEndCharacterEnabled
            }
        ];

        private HttpClient _httpClient = new HttpClient();

        // �����û�������ַ�����OpenAI������AI�Ļظ�
        private async Task<string> SendToOpenAI(string userInput)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{OpenAIBaseURL}/chat/completions")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {OpenAIAPIKey}" }
                },
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    model = "Pro/deepseek-ai/DeepSeek-R1-Distill-Qwen-1.5B",
                    messages = new[]
                    {
                        new { role = "user", content = userInput }
                    }
                }), System.Text.Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseBody);

            string aiResponse = responseJson
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return aiResponse;
        }

        // iPlugin: Query����Ϊ���߼���
        // �����û������Query��������Search��Ա���û�������ַ��������д������ؽ��,
        // ����������⡢�����⡢ͼ�ꡢAction����Ϣ
        public List<Result> Query(Query query) => Query(query, false);
        public List<Result> Query(Query query, bool delayedExecution)
        {
            ArgumentNullException.ThrowIfNull(query);
            var isGlobalQuery = string.IsNullOrEmpty(query.ActionKeyword);
            var isEmptySearch = string.IsNullOrEmpty(query.Search);
            var hasEndCharacter = query.Search.EndsWith(EndCharacter);
            if (isEmptySearch || isGlobalQuery || (isEndCharacterEnabled && !hasEndCharacter) || !delayedExecution)
            {
                return [];
            }

            var userInput = query.Search.TrimEnd(EndCharacter[0]);
            string aiResponse = Task.Run(() => SendToOpenAI(userInput)).Result;

            return [
                new Result{
                    Title = aiResponse,
                    SubTitle = "Click to copy the answer",
                    IcoPath = IconPath,
                    Action = _ =>
                    {
                        Clipboard.SetText(aiResponse);
                        return true;
                    },
                }
            ];
        }

        // iPlugin: �����ʼ�������������ע�����������¼�������ϵͳ����light����dark����ͼ�꣩
        public void Init(PluginInitContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(Context.API.GetCurrentTheme());
        }

        // ISettingProvider: ����������壬һ�㲻��Ҫ��ֱ�ӷ���δʵ��
        public Control CreateSettingPanel() => throw new NotImplementedException();

        // ISettingProvider: ��������
        // ��AdditionalOptions���Ҷ�Ӧ�����ã�����ҵ������ж���ֵ�Ƿ�Ϊnull���������null�ͷ���ֵ���Ҳ����ͷ���false
        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            OpenAIBaseURL = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(OpenAIBaseURL))?.TextValue ?? "null";
            OpenAIAPIKey = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(OpenAIAPIKey))?.TextValue ?? "null";
            isEndCharacterEnabled = settings.AdditionalOptions.SingleOrDefault(x => x.Key == nameof(EndCharacter))?.Value ?? false;
            EndCharacter = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(EndCharacter))?.TextValue ?? ".";
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed || !disposing)
            {
                return;
            }

            if (Context?.API != null)
            {
                Context.API.ThemeChanged -= OnThemeChanged;
            }

            Disposed = true;
        }

        private void UpdateIconPath(Theme theme) => IconPath = theme == Theme.Light || theme == Theme.HighContrastWhite ? Context?.CurrentPluginMetadata.IcoPathLight : Context?.CurrentPluginMetadata.IcoPathDark;

        private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);
    }
}