using System.Text.Json;
using Zaphira.Application;
using Zaphira.Application.Providers;
using Zaphira.Contracts;
using Zaphira.Domain;

namespace Zaphira.Server.Chat;

internal static class ChatApiEndpoints
{
    private static readonly JsonSerializerOptions StreamSerializerOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapChatApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api");

        group.MapPost("/conversations", CreateConversationAsync);
        group.MapGet("/conversations", GetConversationsAsync);
        group.MapPatch("/conversations/{conversationId:guid}", RenameConversationAsync);
        group.MapDelete("/conversations/{conversationId:guid}", DeleteConversationAsync);
        group.MapGet("/conversations/{conversationId:guid}/messages", GetMessagesAsync);
        group.MapPost("/conversations/{conversationId:guid}/messages", SendMessageAsync);
        group.MapPost("/conversations/{conversationId:guid}/messages/{assistantMessageId:guid}/stream", StreamMessageAsync);
        group.MapPost("/conversations/{conversationId:guid}/messages/{assistantMessageId:guid}/cancel", CancelMessageAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateConversationAsync(
        CreateConversationRequest request,
        IConversationRepository conversationRepository,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ConversationSummary summary = new(
            ConversationId.New(),
            request.Title,
            ConversationPreview.Empty(),
            messageCount: 0,
            now,
            now);

        await conversationRepository.SaveAsync(summary, cancellationToken);

        return Results.Created($"/api/conversations/{summary.Id.Value}", ToResponse(summary));
    }

    private static async Task<IResult> GetConversationsAsync(
        IConversationRepository conversationRepository,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ConversationSummary> summaries = await conversationRepository.GetSummariesAsync(cancellationToken);

        return Results.Ok(new ConversationListResponse(summaries.Select(ToResponse).ToArray()));
    }

    private static async Task<IResult> RenameConversationAsync(
        Guid conversationId,
        UpdateConversationRequest request,
        IConversationRepository conversationRepository,
        CancellationToken cancellationToken)
    {
        ConversationId domainConversationId = new(conversationId);
        ConversationSummaryLookup lookup = await conversationRepository.GetSummaryAsync(domainConversationId, cancellationToken);
        if (!lookup.Exists)
        {
            return Results.NotFound(ErrorResponse.ConversationNotFound());
        }

        ConversationSummary updatedSummary = new(
            lookup.Summary.Id,
            request.Title,
            lookup.Summary.Preview,
            lookup.Summary.MessageCount,
            lookup.Summary.CreatedAt,
            DateTimeOffset.UtcNow);

        await conversationRepository.SaveAsync(updatedSummary, cancellationToken);

        return Results.Ok(ToResponse(updatedSummary));
    }

    private static async Task<IResult> DeleteConversationAsync(
        Guid conversationId,
        IConversationRepository conversationRepository,
        CancellationToken cancellationToken)
    {
        bool deleted = await conversationRepository.DeleteAsync(new ConversationId(conversationId), cancellationToken);

        return deleted
            ? Results.NoContent()
            : Results.NotFound(ErrorResponse.ConversationNotFound());
    }

    private static async Task<IResult> GetMessagesAsync(
        Guid conversationId,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        CancellationToken cancellationToken)
    {
        ConversationId domainConversationId = new(conversationId);
        if (!await ConversationExistsAsync(conversationRepository, domainConversationId, cancellationToken))
        {
            return Results.NotFound(ErrorResponse.ConversationNotFound());
        }

        IReadOnlyList<ChatMessage> messages = await messageRepository.GetMessagesAsync(domainConversationId, cancellationToken);

        return Results.Ok(new MessageListResponse(messages.Select(ToResponse).ToArray()));
    }

    private static async Task<IResult> SendMessageAsync(
        Guid conversationId,
        SendMessageRequest request,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IChatModelProvider provider,
        CancellationToken cancellationToken)
    {
        ConversationId domainConversationId = new(conversationId);
        IReadOnlyList<ConversationSummary> summaries = await conversationRepository.GetSummariesAsync(cancellationToken);
        ConversationSummary? existingSummary = summaries.FirstOrDefault(summary => summary.Id == domainConversationId);
        if (existingSummary is null)
        {
            return Results.NotFound(ErrorResponse.ConversationNotFound());
        }

        ModelId domainModelId = new(request.ModelId);
        ModelAvailability modelAvailability = await GetModelAvailabilityAsync(provider, domainModelId, cancellationToken);
        if (modelAvailability is not ModelAvailability.Available)
        {
            return ToModelAvailabilityError(modelAvailability);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        ChatMessage userMessage = new(
            MessageId.New(),
            domainConversationId,
            MessageRole.User,
            [new TextMessagePart(request.Text)],
            MessageStatus.Completed,
            now);
        ChatMessage assistantMessage = new(
            MessageId.New(),
            domainConversationId,
            MessageRole.Assistant,
            [new TextMessagePart("Response pending.")],
            MessageStatus.Pending,
            now);
        ConversationSummary updatedSummary = new(
            existingSummary.Id,
            existingSummary.Title,
            new ConversationPreview(request.Text),
            existingSummary.MessageCount + 2,
            existingSummary.CreatedAt,
            now);

        await messageRepository.SaveAsync(userMessage, cancellationToken);
        await messageRepository.SaveAsync(assistantMessage, cancellationToken);
        await conversationRepository.SaveAsync(updatedSummary, cancellationToken);

        return Results.Ok(new SendMessageResponse(userMessage.Id.Value, assistantMessage.Id.Value));
    }

    private static async Task StreamMessageAsync(
        Guid conversationId,
        Guid assistantMessageId,
        StreamMessageRequest request,
        HttpContext context,
        IChatModelProvider provider,
        IMessageRepository messageRepository,
        GenerationCancellationRegistry cancellationRegistry)
    {
        ConversationId domainConversationId = new(conversationId);
        MessageId domainAssistantMessageId = new(assistantMessageId);
        IReadOnlyList<ChatMessage> messages = await messageRepository.GetMessagesAsync(
            domainConversationId,
            context.RequestAborted);
        ChatMessage? assistantMessage = messages.FirstOrDefault(message => message.Id == domainAssistantMessageId);
        if (assistantMessage is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(ErrorResponse.MessageNotFound(), context.RequestAborted);
            return;
        }

        ModelId domainModelId = new(request.ModelId);
        ModelAvailability modelAvailability = await GetModelAvailabilityAsync(provider, domainModelId, context.RequestAborted);
        if (modelAvailability is not ModelAvailability.Available)
        {
            int statusCode = modelAvailability is ModelAvailability.ProviderUnavailable
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status400BadRequest;
            ErrorResponse error = modelAvailability is ModelAvailability.ProviderUnavailable
                ? ErrorResponse.ProviderUnavailable()
                : ErrorResponse.ModelNotFound();

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(error, context.RequestAborted);
            return;
        }

        string generatedText = string.Empty;
        CancellationToken generationCancellationToken = cancellationRegistry.Register(
            domainAssistantMessageId,
            context.RequestAborted);

        context.Response.ContentType = "application/x-ndjson";

        try
        {
            await messageRepository.SaveAsync(
                UpdateAssistantMessage(assistantMessage, assistantMessage.Parts, MessageStatus.Streaming),
                generationCancellationToken);
            ProviderGenerationRequest generationRequest = new(domainModelId, messages);

            await foreach (ProviderGenerationEvent generationEvent in provider.GenerateAsync(generationRequest, generationCancellationToken))
            {
                switch (generationEvent)
                {
                    case TextGenerationDeltaEvent textDelta:
                        generatedText += textDelta.Text;
                        await WriteStreamEventAsync(context, GenerationStreamResponse.TextDelta(textDelta.Text), generationCancellationToken);
                        await messageRepository.SaveAsync(
                            UpdateAssistantMessage(
                                assistantMessage,
                                [new TextMessagePart(ToPersistedAssistantText(generatedText))],
                                MessageStatus.Streaming),
                            generationCancellationToken);
                        break;
                    case GenerationCompletedEvent:
                        await WriteStreamEventAsync(context, GenerationStreamResponse.Completed(), generationCancellationToken);
                        await messageRepository.SaveAsync(
                            UpdateAssistantMessage(
                                assistantMessage,
                                [new TextMessagePart(ToPersistedAssistantText(generatedText))],
                                MessageStatus.Completed),
                            generationCancellationToken);
                        return;
                    case GenerationFailedEvent failed:
                        await WriteStreamEventAsync(context, GenerationStreamResponse.Failed(failed.Error.Message), generationCancellationToken);
                        await messageRepository.SaveAsync(
                            UpdateAssistantMessage(assistantMessage, assistantMessage.Parts, MessageStatus.Failed),
                            context.RequestAborted);
                        return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            await messageRepository.SaveAsync(
                UpdateAssistantMessage(assistantMessage, assistantMessage.Parts, MessageStatus.Cancelled),
                CancellationToken.None);
            await WriteStreamEventAsync(context, GenerationStreamResponse.Cancelled(), CancellationToken.None);
        }
        finally
        {
            cancellationRegistry.Complete(domainAssistantMessageId);
        }
    }

    private static async Task<IResult> CancelMessageAsync(
        Guid conversationId,
        Guid assistantMessageId,
        IMessageRepository messageRepository,
        GenerationCancellationRegistry cancellationRegistry,
        CancellationToken cancellationToken)
    {
        ConversationId domainConversationId = new(conversationId);
        MessageId domainAssistantMessageId = new(assistantMessageId);
        cancellationRegistry.Cancel(domainAssistantMessageId);

        IReadOnlyList<ChatMessage> messages = await messageRepository.GetMessagesAsync(domainConversationId, cancellationToken);
        ChatMessage? assistantMessage = messages.FirstOrDefault(message => message.Id == domainAssistantMessageId);
        if (assistantMessage is null)
        {
            return Results.NotFound(ErrorResponse.MessageNotFound());
        }

        await messageRepository.SaveAsync(
            UpdateAssistantMessage(assistantMessage, assistantMessage.Parts, MessageStatus.Cancelled),
            cancellationToken);

        return Results.Ok();
    }

    private static async Task WriteStreamEventAsync(
        HttpContext context,
        GenerationStreamResponse response,
        CancellationToken cancellationToken)
    {
        await JsonSerializer.SerializeAsync(context.Response.Body, response, StreamSerializerOptions, cancellationToken);
        await context.Response.WriteAsync("\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }

    private static string ToPersistedAssistantText(string generatedText) =>
        string.IsNullOrWhiteSpace(generatedText) ? "No response content." : generatedText;

    private static ChatMessage UpdateAssistantMessage(
        ChatMessage assistantMessage,
        IEnumerable<IMessagePart> parts,
        MessageStatus status) =>
        new(
            assistantMessage.Id,
            assistantMessage.ConversationId,
            assistantMessage.Role,
            parts,
            status,
            assistantMessage.CreatedAt);

    private static ConversationResponse ToResponse(ConversationSummary summary) =>
        new(summary.Id.Value, summary.Title, summary.Preview.Text, summary.MessageCount, summary.CreatedAt, summary.UpdatedAt);

    private static async Task<bool> ConversationExistsAsync(
        IConversationRepository conversationRepository,
        ConversationId conversationId,
        CancellationToken cancellationToken)
    {
        ConversationSummaryLookup lookup = await conversationRepository.GetSummaryAsync(conversationId, cancellationToken);

        return lookup.Exists;
    }

    private static async Task<ModelAvailability> GetModelAvailabilityAsync(
        IChatModelProvider provider,
        ModelId modelId,
        CancellationToken cancellationToken)
    {
        ProviderModelCatalog catalog;
        try
        {
            catalog = await provider.ListModelsAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ModelAvailability.ProviderUnavailable;
        }

        return catalog.Models.Any(model => model.Id == modelId)
            ? ModelAvailability.Available
            : ModelAvailability.ModelNotFound;
    }

    private static IResult ToModelAvailabilityError(ModelAvailability modelAvailability) =>
        modelAvailability is ModelAvailability.ProviderUnavailable
            ? Results.Json(ErrorResponse.ProviderUnavailable(), statusCode: StatusCodes.Status503ServiceUnavailable)
            : Results.BadRequest(ErrorResponse.ModelNotFound());

    private static ChatMessageResponse ToResponse(ChatMessage message) =>
        new(
            message.Id.Value,
            message.ConversationId.Value,
            message.Role.ToString(),
            message.Status.ToString(),
            message.Parts.Select(ToResponse).ToArray(),
            message.CreatedAt);

    private static MessagePartResponse ToResponse(IMessagePart part) =>
        part switch
        {
            TextMessagePart text => new MessagePartResponse("text", text.Text),
            FileMessagePart file => new MessagePartResponse("file", file.Reference.DisplayName),
            ReasoningMessagePart reasoning => new MessagePartResponse("reasoning", reasoning.Summary),
            UnknownMessagePart unknown => new MessagePartResponse("unknown", unknown.OriginalKind),
            _ => new MessagePartResponse("unknown", part.GetType().Name)
        };

    private enum ModelAvailability
    {
        Available,
        ModelNotFound,
        ProviderUnavailable
    }
}
