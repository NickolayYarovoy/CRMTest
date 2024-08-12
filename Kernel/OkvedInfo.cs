namespace Kernel
{
    /// <summary>
    /// Информация о виде деятельности по ОКВЭД
    /// </summary>
    public class OkvedInfo
    {
        /// <summary>
        /// Код по ОКВЭД
        /// </summary>
        public string Code { get; }
        /// <summary>
        /// Тип деятельности
        /// </summary>
        public string ActivityType { get; }

        public OkvedInfo(string code, string activityType)
        {
            Code = code;
            ActivityType = activityType;
        }
    }
}
