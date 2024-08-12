using Kernel;
using System.Net.Http.Json;

namespace FnsKernel
{
    /// <summary>
    /// Реализация интерфейса для получения данных о компании с сайта vbankcenter.ru
    /// </summary>
    public class FnsKernel : IKernel
    {
        /// <summary>
        /// Фабрика для HttpClient'ов
        /// </summary>
        private IHttpClientFactory httpFactory;

        public FnsKernel(IHttpClientFactory httpClient)
        {
            httpFactory = httpClient;
        }

        /// <summary>
        /// Метод для получения информации о названии и адресе компании по ИНН
        /// </summary>
        /// <param name="inn">ИНН компании</param>
        /// <returns>Данные о компании</returns>
        public async Task<CompanyInfo> GetCompanyInfo(string inn)
        {
            Content shortCompanyInfo = await GetCompanyShortInfo(inn);
            return new(shortCompanyInfo.fullName, shortCompanyInfo.address);
        }

        /// <summary>
        /// Метод для получения выписки из ЕГРЮЛ по ИНН компании
        /// </summary>
        /// <param name="inn">ИНН компании</param>
        /// <returns>Поток с файлом, пришедшим с сервера</returns>
        public async Task<Stream> GetEgrul(string inn)
        {
            var client = httpFactory.CreateClient();
            var response = await client.GetAsync($"https://vbankcenter.ru/contragent/api/web/counterparty/type/legal/uuid/{(await GetCompanyShortInfo(inn, client)).partnerUuid}/pdf");
            return await response.Content.ReadAsStreamAsync();
        }

        /// <summary>
        /// Метод для получения информации о видах деятельности компании по ИНН
        /// </summary>
        /// <param name="inn">ИНН компании</param>
        /// <returns>Список кодов деятельности</returns>
        public async Task<OkvedInfo[]> GetOkved(string inn)
        {
            var client = httpFactory.CreateClient();
            var response = await client.GetAsync($"https://vbankcenter.ru/contragent/api/web/counterparty/type/legal/uuid/{(await GetCompanyShortInfo(inn, client)).partnerUuid}/activityTypes?page=0&size=100&inn={inn}");
            return (await response.Content.ReadFromJsonAsync<OkvedResult>()).content
                .OrderByDescending(x => x.name).Select(x => new OkvedInfo(x.code, x.name)).ToArray();
        }

        /// <summary>
        /// Метод для получения краткой информации о компании
        /// </summary>
        /// <param name="inn">ИНН компании</param>
        /// <param name="httpClient">HttpClient</param>
        /// <returns>Краткие данные о компании</returns>
        /// <exception cref="ArgumentException">Не найдена компания/надено несколько компаний</exception>
        private async Task<Content> GetCompanyShortInfo(string inn, HttpClient? httpClient = null)
        {
            var client = httpClient ?? httpFactory.CreateClient();
            var response = await client.GetAsync($"https://vbankcenter.ru/contragent/api/web/counterparty/filter?page=0&size=20&searchStr={inn}&withCounter=true");
            SearchInfo info = await response.Content.ReadFromJsonAsync<SearchInfo>();
            Content? company = info.content.FirstOrDefault(x => x.inn == $"<em>{inn}</em>");
            if (company == null)
            {
                throw new ArgumentException("Не найдена компания с данным ИНН");
            }
            return company;
        }
    }
}
