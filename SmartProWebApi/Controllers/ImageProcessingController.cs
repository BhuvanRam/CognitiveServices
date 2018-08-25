using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartProWebApi.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace SmartProWebApi.Controllers
{

    public class ImageData
    {
        public string base64image;
    }
    public class ImageProcessingController : ApiController
    {

        [HttpPost]
        public string PostImageData(ImageData base64image)
        {
            var t = base64image.base64image.Substring(22);
            byte[] byteData = Convert.FromBase64String(t);
            var result = Task.Run(async () => { return await MakeAnalysisRequest(byteData); }).Result;            
            return result;
        }

        const string subscriptionKey = "e70946f2ab0a4b57825109406586046f";        
        const string uriBase = "https://southeastasia.api.cognitive.microsoft.com/vision/v2.0/recognizeText";


        async Task<string> MakeAnalysisRequest(byte[] byteData)
        {
            string recognizedText = string.Empty;
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                string requestParameters =
                   "mode=Printed";

                string uri = uriBase + "?" + requestParameters;
                HttpResponseMessage response;

                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {

                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response = await client.PostAsync(uri, content);
                }

                if (response.IsSuccessStatusCode)
                    recognizedText =
                        response.Headers.GetValues("Operation-Location").FirstOrDefault();
                else
                {
                    // Display the JSON error data.
                    recognizedText = await response.Content.ReadAsStringAsync();

                }

                string contentString;
                int i = 0;
                do
                {
                    System.Threading.Thread.Sleep(1000);
                    response = await client.GetAsync(recognizedText);
                    contentString = await response.Content.ReadAsStringAsync();
                    ++i;
                }
                while (i < 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1);

                if (i == 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1)
                {
                    ;
                }


                RootObject result = JsonConvert.DeserializeObject<RootObject>(JToken.Parse(contentString).ToString());
                List<string> words = new List<string>();


                foreach (Line line in result.recognitionResult.lines)
                {
                    words.AddRange(line.words.Select(p => p.text).ToList());
                }

                recognizedText = string.Join(" ", words);

            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message);
            }
            return recognizedText;
        }

    }
}
