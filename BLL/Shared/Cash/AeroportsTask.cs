using System.Collections.Generic;

namespace BLL.Shared.Cash
{
    public class AeroportsTask
    {
        public string IataCode { get; set; }
        public IList<string> DependedJobs { get; set; }
        public Location Location { get; set; }
        /// <summary>
        /// Таймаут для следующего вызова реквеста
        /// </summary>
        public ushort Timeout { get; set; } = 0; 
    }
}
