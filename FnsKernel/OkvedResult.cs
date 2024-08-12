namespace FnsKernel
{
    /// <summary>
    /// Результат поиска кодов деятельности компании
    /// </summary>
    public class OkvedResult
    {
        /// <summary>
        /// Список кодов деятельности компании
        /// </summary>
        public OkvedContent[] content { get; set; }
    }

    /// <summary>
    /// Информация о коде деятельности компании
    /// </summary>
    public class OkvedContent
    {
        /// <summary>
        /// Направление деятельности
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// Код направления деятельности
        /// </summary>
        public string code { get; set; }
    }

}
