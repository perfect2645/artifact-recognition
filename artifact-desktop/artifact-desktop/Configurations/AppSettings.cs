using Microsoft.Extensions.Options;
using Utils.Ioc;

namespace artifact_automation_desktop.Configurations;

public record AppSettings
{
    public string? ApplicationName { get; set; }
}