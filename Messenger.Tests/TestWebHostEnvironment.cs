using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Messenger.Tests;

internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Messenger.Tests";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = Path.GetTempPath();
    public string EnvironmentName { get; set; } = "Testing";
    public string WebRootPath { get; set; } = Path.GetTempPath();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
