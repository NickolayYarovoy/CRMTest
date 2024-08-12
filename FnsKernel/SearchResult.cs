namespace FnsKernel
{
    /// <summary>
    /// Результат поиска краткой информации о компании
    /// </summary>
    public class SearchInfo
    {
        /// <summary>
        /// Список найденных компаний
        /// </summary>
        public Content[] content { get; set; }
    }
    
    /// <summary>
    /// Краткая информация о найденной компании
    /// </summary>
    public class Content
    {
        /// <summary>
        /// Служебное поле сайта
        /// </summary>
        public string partnerUuid { get; set; }
        /// <summary>
        /// Название компании
        /// </summary>
        public string fullName { get; set; }
        /// <summary>
        /// Адрес компании
        /// </summary>
        public string address { get; set; }
    }
}
