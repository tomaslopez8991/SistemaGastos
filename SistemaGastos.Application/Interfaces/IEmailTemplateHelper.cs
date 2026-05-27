namespace SistemaGastos.Application.Interfaces;

public interface IEmailTemplateHelper
{
    Task<string> GetTemplateAsync(string templateName, Dictionary<string, string> replacements);
}