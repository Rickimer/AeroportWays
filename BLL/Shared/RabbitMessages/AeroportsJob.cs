namespace BLL.Shared.RabbitMessages
{
    public class AeroportsJob
    {
        public string Id { get; set; }
        public string FromIATACode { get; set; }
        public string ToIATACode { get; set; }
        public double? Distance { get; set; }
        public bool isError { get; set; }
    }
}
