using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Attachments;

namespace OuchRBot.API.Extensions
{
    public static class VkApiExtensions
    {
        /// <summary>
        /// Загружает документ на сервер ВК.
        /// </summary>
        /// <param name="vkApi">Вк апи.</param>
        /// <param name="data">Аттачмент, байты которого будут отправлены на сервер</param>
        /// <param name="docMessageType">Тип документа - документ или аудиосообщение.</param>
        /// <param name="peerId">Идентификатор назначения</param>
        /// <param name="filename">Итоговое название документа</param>
        /// <returns>Аттачмент для отправки вместе с сообщением.</returns>
        public static async Task<MediaAttachment> LoadDocumentToChatAsync(this IVkApi vkApi, Stream data,
            DocMessageType docMessageType, long peerId, string filename)
        {
            var uploadServer = vkApi.Docs.GetMessagesUploadServer(peerId, docMessageType);

            var r = await UploadFile(uploadServer.UploadUrl, data);
            var documents = vkApi.Docs.Save(r, filename ?? Guid.NewGuid().ToString());

            if (documents.Count != 1)
                throw new ArgumentException($"Error while loading document attachment to {uploadServer.UploadUrl}");

            return documents[0].Instance;
        }

        /// <summary>
        /// Загружает массив байт на указанный url
        /// </summary>
        /// <param name="url">Адрес для загрузки</param>
        /// <param name="data">Массив данных для загрузки</param>
        /// <returns>Строка, которую вернул сервер.</returns>
        private static async Task<string> UploadFile(string url, Stream data)
        {
            using var client = new HttpClient();
            var requestContent = new MultipartFormDataContent();
            var documentContent = new StreamContent(data);
            documentContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/form-data");
            requestContent.Add(documentContent, "file", "file.pdf");

            var response = await client.PostAsync(url, requestContent);

            return Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
        }
    }
}
