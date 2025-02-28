using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.ChatOpenAI.UnitTests
{
    [TestClass]
    public class MainTests
    {
        private Main main;

        [TestInitialize]
        public void TestInitialize()
        {
            main = new Main();
        }

        [TestMethod]
        public void AdditionalOptions_should_return_option_for_openai_config()
        {
            var options = main.AdditionalOptions;
            Assert.AreEqual("OpenAIBaseURL", options.ElementAt(0).Key);
            Assert.AreEqual("OpenAIAPIKey", options.ElementAt(1).Key);
            Assert.AreEqual("ModelName", options.ElementAt(2).Key);
            Assert.AreEqual("EndCharacter", options.ElementAt(3).Key);
        }

        [TestMethod]
        public void UpdateSettings_should_set_OpenAI_config()
        {
            main.UpdateSettings(new() { AdditionalOptions = [
                    new() { Key = "OpenAIBaseURL", TextValue = "https://api.siliconflow.cn/v1" },
                    new() { Key = "OpenAIAPIKey", TextValue = "sk-1234" },
                    new() { Key= "ModelName", TextValue = "Pro/deepseek-ai/DeepSeek-R1-Distill-Qwen-1.5B" },
                    new() { Key = "EndCharacter", TextValue = "#", Value = true }
                ] });

            var options = main.AdditionalOptions;
            Assert.AreEqual("https://api.siliconflow.cn/v1", options.ElementAt(0).TextValue);
            Assert.AreEqual("sk-1234", options.ElementAt(1).TextValue);
            Assert.AreEqual("Pro/deepseek-ai/DeepSeek-R1-Distill-Qwen-1.5B", options.ElementAt(2).TextValue);
            Assert.AreEqual(true, options.ElementAt(3).Value);
            Assert.AreEqual("#", options.ElementAt(3).TextValue);
        }

        [TestMethod]
        public void Query_OpenAI_API_should_return_results()
        {
            // 调用设置接口，设置OpenAI的BaseURL和API Key
            main.UpdateSettings(new()
            {
                AdditionalOptions = [
                    new() { Key = "OpenAIBaseURL", TextValue = "https://api.siliconflow.cn/v1" },
                    new() { Key = "OpenAIAPIKey", TextValue = "sk-1234" },
                    new() { Key= "ModelName", TextValue = "Pro/deepseek-ai/DeepSeek-R1-Distill-Qwen-1.5B" },
                    new() { Key = "EndCharacter", TextValue = ".", Value = true }
                ]
            });
            var results = main.Query(new("ai hello.", "ai"), true);

            Assert.IsNotNull(results.First());
        }
    }
}
