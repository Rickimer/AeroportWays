using BLL.Shared;

namespace BLL.Services
{
    public interface IMainService
    {
        string Post(string FromIATACode, string ToIATACode);
        void TaskProcessing();
        void RepeatedCallProcessing();
        GetDistanceResultDto Get(string id);
    }
}
