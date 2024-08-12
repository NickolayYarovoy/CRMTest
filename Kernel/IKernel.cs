namespace Kernel
{
    /// <summary>
    /// Интерфейс, предоставляющий доступ к данным о компании
    /// </summary>
    public interface IKernel
    {
        /// <summary>
        /// Метод для получения информации о названии и адресе компании по ИНН
        /// </summary>
        /// <param name="inn">ИНН компании</param>
        /// <returns>Данные о компании</returns>
        public Task<CompanyInfo> GetCompanyInfo(string inn);

        /// <summary>
        /// Метод для получения информации о видах деятельности компании по ИНН
        /// </summary>
        /// <param name="inn">ИНН компании</param>
        /// <returns>Список кодов деятельности</returns>
        public Task<OkvedInfo[]> GetOkved(string inn);

        /// <summary>
        /// Метод для получения выписки из ЕГРЮЛ по ИНН компании
        /// </summary>
        /// <param name="inn">ИНН компании</param>
        /// <returns>Поток с файлом, пришедшим с сервера</returns>
        public Task<Stream> GetEgrul(string inn);
    }
}
