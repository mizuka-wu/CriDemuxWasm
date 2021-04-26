using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace CriDemuxer
{
    public class Program
    {
        [JSInvokable]
        public static Task<int[]> Demux()
        {
            return Task.FromResult(new int[] { 1, 2, 3 });
        }
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            await builder.Build().RunAsync();
        }
    }
}
