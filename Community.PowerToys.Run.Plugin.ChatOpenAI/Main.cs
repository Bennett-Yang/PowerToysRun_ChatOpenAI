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

        // 发送用户输入的字符串给OpenAI，返回AI的回复
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

        // iPlugin: Query函数为主逻辑，
        // 根据用户输入的Query对象（其中Search成员即用户输入的字符串）进行处理并返回结果,
        // 结果包含标题、副标题、图标、Action等信息
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

        // iPlugin: 插件初始化函数，这里仅注册了主题变更事件（跟随系统主题light还是dark更换图标）
        public void Init(PluginInitContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(Context.API.GetCurrentTheme());
        }

        // ISettingProvider: 创建设置面板，一般不需要，直接返回未实现
        public Control CreateSettingPanel() => throw new NotImplementedException();

        // ISettingProvider: 更新设置
        // 从AdditionalOptions查找对应的设置，如果找到了则判断其值是否为null，如果不是null就返回值，找不到就返回false
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