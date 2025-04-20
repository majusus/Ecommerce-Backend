using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface ITextSummarizationService
    {
        Task<string> SummarizeTextAsync(string text);
    }
}