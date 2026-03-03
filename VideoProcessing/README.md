# Video Processing Worker

Projeto .NET 8 que consome mensagens de processamento de vídeo via RabbitMQ, baixa o blob de vídeo do Azure Blob Storage (ou Azurite), extrai frames usando `ffmpeg`/`ffprobe`, compacta os frames em um ZIP, faz upload do ZIP para o blob storage e atualiza o estado do processamento em um documento MongoDB. Ao final, publica uma mensagem de notificação.

## Estrutura do repositório

- `VideoProcessing.Domain` - DTOs, eventos, portas (contratos) e enums.
- `VideoProcessing.Application` - Use case principal (`ProcessVideoUseCase`) que orquestra o processamento.
- `VideoProcessing.Infrastructure` - Implementações concretas: RabbitMQ, Azure Blob, ffmpeg, zip, MongoDB.
- `VideoProcessing.Worker` - Serviço do tipo Worker / BackgroundService que conecta tudo e consome filas.

## Pré-requisitos

- .NET 8 SDK
- RabbitMQ
- MongoDB
- Azure Blob Storage ou Azurite
- `ffmpeg` e `ffprobe` no PATH (usados para extrair frames)

## Configuração

O projeto lê configurações do arquivo `VideoProcessing.Worker/appsettings.json` (ou variáveis de ambiente). Principais chaves:

- `RabbitMqSettings:ConnectionUri` - URI de conexão com RabbitMQ (ex.: `amqp://user:pass@host:5672/`).
- `RabbitMqSettings:ProcessImagesQueue` - nome da fila de entrada (padrão `process-images`).
- `RabbitMqSettings:NotificationQueue` - fila de notificação (padrão `notification`).
- `UserApi:BaseUrl` e `UserApi:ApiKey` - para obter informações do plano do usuário.
- `AzureBlob:ConnectionString` - connection string do Blob Storage (ex.: `UseDevelopmentStorage=true` para Azurite).
- `AzureBlob:ContainerName` - container onde serão gravados os zips.
- `MongoDb:ConnectionString` - stringz de conexão do MongoDB.
- `MongoDb:Database` e `MongoDb:Collection` - database e collection usados para atualizar status.
- `QuantityFrames` - quantidade padrão de frames a extrair.

> Observação: ao usar Azure real, defina `AzureBlob:ConnectionString` com a connection string da sua conta.

## Executando localmente

1. Restaurar e build:

   dotnet restore
   dotnet build

2. Executar o Worker (diretório `VideoProcessing.Worker`):

   dotnet run --project VideoProcessing.Worker

O serviço abrirá conexão com RabbitMQ e aguardará mensagens na fila configurada. As mensagens esperadas têm o formato do tipo `VideoProcessing.Domain.Events.VideoProcessingEvent` (JSON):

{
  "UserId": "<user-id>",
  "PlanId": "<plan-id>",
  "ProcessingId": "<processing-id>",
  "BlobUrl": "https://...",
  "EventAt": "2024-01-01T00:00:00Z"
}

## Observações importantes

- `ffmpeg`/`ffprobe` precisam estar instalados e disponíveis no PATH. Sem eles, a extração de frames falhará.
- O downloader de blobs aceita URLs de Azurite (devstore) e URLs reais do Azure.
- Em caso de falha no processamento, o estado é atualizado para `Failed` no MongoDB e uma notificação de falha é publicada.
