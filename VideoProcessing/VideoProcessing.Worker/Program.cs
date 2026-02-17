using VideoProcessing.Application.UseCases;
using VideoProcessing.Domain.Ports.In;
using VideoProcessing.Infrastructure.Messaging.Configuration;
using VideoProcessing.Domain.Ports.On;
using VideoProcessing.Infrastructure.Providers;
using Azure.Storage.Blobs;
using System.Net.Http.Headers;;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<VideoProcessingMessageConsumer>();

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMq")
);

builder.Services.AddScoped<IProcessVideoUseCase, ProcessVideoUseCase>();
builder.Services.AddSingleton<RabbitMqConnectionFactory>();

var userApiSection = builder.Configuration.GetSection("UserApi");
var userApiBaseUrl = userApiSection.GetValue<string>("BaseUrl");
var userApiKey = userApiSection.GetValue<string>("apiKey");

builder.Services.AddHttpClient<IUserPlanProvider, UserPlanProvider>(client =>
{
    if (!string.IsNullOrWhiteSpace(userApiBaseUrl))
    {
        client.BaseAddress = new Uri(userApiBaseUrl);
    }

    if (!string.IsNullOrWhiteSpace(userApiKey))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userApiKey);
    }
});

var blobConnectionString = builder.Configuration.GetValue<string>("AzureBlob:ConnectionString");

var blobServiceClient = new BlobServiceClient(blobConnectionString);
builder.Services.AddSingleton(blobServiceClient);

builder.Services.AddScoped<IVideoDownloader, BlobVideoDownloader>();
builder.Services.AddScoped<IFrameExtractor, FfmpegFrameExtractor>();

builder.Services.AddScoped<IZipService, ZipService>();
builder.Services.AddScoped<IFileStorage, BlobFileStorage>();
builder.Services.AddScoped<IProcessingPublisher, LoggingProcessingPublisher>();

var host = builder.Build();
host.Run();
