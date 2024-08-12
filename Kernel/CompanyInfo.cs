namespace Kernel
{
    /// <summary>
    /// Информация о названии и адресе компании
    /// </summary>
    public class CompanyInfo
    {
        /// <summary>
        /// Название компании
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Адрес компании
        /// </summary>
        public string Address { get; }

        public CompanyInfo(string name, string address)
        {
            Name = name;
            Address = address;
        }
    }
}
