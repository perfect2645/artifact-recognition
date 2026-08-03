using Microsoft.Extensions.Options;
using Utils.Ioc;

namespace artifact.desktop.Configurations;

public record AppSettings
{
    public string? ApplicationName { get; set; }
}