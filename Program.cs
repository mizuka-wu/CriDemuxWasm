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
using VGMToolbox.format;

namespace CriDemuxer
{
    public class Program
    {
        [JSInvokable]
        public static async Task<string[]> Demux(string usmFilePath)
        {
            MpegStream.DemuxOptionsStruct demuxOptions = new MpegStream.DemuxOptionsStruct();
            demuxOptions.ExtractAudio = false;
            demuxOptions.ExtractVideo = true;

            CriUsmStream demuxer = new CriUsmStream(usmFilePath);
            return await Task.Run(() =>
            {
                return demuxer.DemultiplexStreams(demuxOptions);
            });
        }
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            await builder.Build().RunAsync();
        }
    }
}
