using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;
using MongoDB.Driver;
using StandardDependencies.Injection;
using StandardDependencies.Models;
using VideoProcessing.Application.UseCases;
using VideoProcessing.Domain.Ports.In;
using VideoProcessing.Domain.Ports.On;
using VideoProcessing.Infrastructure.Messaging;
using VideoProcessing.Infrastructure.Messaging.Configuration;
using VideoProcessing.Infrastructure.Providers;

namespace VideoProcessing.Worker;

[ExcludeFromCodeCoverage]
public class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var openTelemetryOptions = builder
            .Configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>();

        builder.ConfigureCommonElements(openTelemetryOptions);
        
        builder.Services.AddHostedService<VideoProcessingMessageConsumer>();

        builder.Services.Configure<RabbitMqSettings>(
            builder.Configuration.GetSection("RabbitMqSettings")
        );

        builder.Services.AddScoped<IProcessVideoUseCase, ProcessVideoUseCase>();
        builder.Services.AddSingleton<RabbitMqConnectionFactory>();
        builder.Services.AddScoped<IVideoProcessedMessageProducer, VideoProcessedMessageProducer>();

        var userApiSection = builder.Configuration.GetSection("UserApi");
        var userApiBaseUrl = userApiSection.GetValue<string>("BaseUrl");
        var userApiKey = userApiSection.GetValue<string>("ApiKey");

        builder.Services.AddHttpClient<IUserPlanProvider, UserPlanProvider>(client =>
        {
            if (!string.IsNullOrWhiteSpace(userApiBaseUrl))
            {
                client.BaseAddress = new Uri(userApiBaseUrl);
            }

            if (!string.IsNullOrWhiteSpace(userApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Api-Key", userApiKey);
            }
        });

        var blobConnectionString = builder.Configuration.GetValue<string>("AzureBlob:ConnectionString");

        var blobServiceClient = new BlobServiceClient(blobConnectionString);
        builder.Services.AddSingleton(blobServiceClient);

        builder.Services.AddScoped<IVideoDownloader, BlobVideoDownloader>();
        builder.Services.AddScoped<IFrameExtractor, FfmpegFrameExtractor>();

        builder.Services.AddScoped<IZipService, ZipService>();
        builder.Services.AddScoped<IFileStorage, BlobFileStorage>();

        // MongoDB client and processing repository
        var mongoConnectionString = builder.Configuration.GetValue<string>("MongoDb:ConnectionString");
        builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));
        builder.Services.AddScoped<IProcessingRepository, MongoProcessingRepository>();

        var host = builder.Build();

        await host.RunAsync();
    }
}