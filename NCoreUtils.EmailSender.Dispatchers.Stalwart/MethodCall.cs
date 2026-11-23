using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.EmailSender.Dispatchers.Stalwart;

[JsonConverter(typeof(MethodCallConverter))]
internal abstract class MethodCall(string name, string clientId)
{
    private sealed class MethodCallWithArguments<T>(string name, T arguments, string clientId)
        : MethodCall(name, clientId)
    {
        public T Arguments { get; } = arguments;

        public override void SerializeArguments(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            var typeInfo = options.GetTypeInfo(typeof(T)) switch
            {
                null => throw new InvalidOperationException($"No JSON type info for {typeof(T)}."),
                JsonTypeInfo<T> instance => instance,
                _ => throw new InvalidOperationException($"Invalid JSON type info for {typeof(T)}.")
            };
            JsonSerializer.Serialize(writer, Arguments, typeInfo);
        }
    }

    private sealed class MethodCallWithEmptyArguments(string name, string clientId)
        : MethodCall(name, clientId)
    {
        public override void SerializeArguments(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }

    public static MethodCall Create<T>(string name, T arguments, string clientId)
        => new MethodCallWithArguments<T>(name, arguments, clientId);

    public static MethodCall Create(string name, string clientId)
        => new MethodCallWithEmptyArguments(name, clientId);

    public string Name { get; } = name;

    public string ClientId { get; } = clientId;

    public abstract void SerializeArguments(Utf8JsonWriter writer, JsonSerializerOptions options);
}

internal sealed class MethodCallConverter : JsonConverter<MethodCall>
{
    public override MethodCall? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException("Deseerializing methodCall is not supported.");

    public override void Write(Utf8JsonWriter writer, MethodCall value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.Name);
        value.SerializeArguments(writer, options);
        writer.WriteStringValue(value.ClientId);
        writer.WriteEndArray();
    }
}

[method: JsonConstructor]
internal readonly struct EmailSetMessageFrom(string email, string? name = default)
{
    [JsonPropertyName("email")]
    public string Email { get; } = email;

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Name { get; } = name;
}

[method: JsonConstructor]
internal readonly struct EmailSetMessageBodyValue(string value, bool? isEncodingProblem = default, bool? isTruncated = default)
{
    [JsonPropertyName("value")]
    public string Value { get; } = value;

    [JsonPropertyName("isEncodingProblem")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? IsEncodingProblem { get; } = isEncodingProblem;

    [JsonPropertyName("isTruncated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? IsTruncated { get; } = isTruncated;
}

[method: JsonConstructor]
internal readonly struct EmailSetMessageBodyPart(string type, string partId)
{
    [JsonPropertyName("type")]
    public string Type { get; } = type;

    [JsonPropertyName("partId")]
    public string PartId { get; } = partId;
}

internal sealed class EmailSetMessageAttachment(string blobId, string type, string name, string? cid, string? disposition, bool? isInline)
{
    [JsonPropertyName("blobId")]
    public string BlobId { get; } = blobId;

    [JsonPropertyName("type")]
    public string Type { get; } = type;

    [JsonPropertyName("name")]
    public string Name { get; } = name;

    [JsonPropertyName("cid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Cid { get; } = cid;

    [JsonPropertyName("disposition")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Disposition { get; } = disposition;

    [JsonPropertyName("isInline")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? IsInline { get; } = isInline;
}

internal sealed class EmailSetMessage(
    IReadOnlyDictionary<string, bool> mailboxIds,
    IReadOnlyList<EmailSetMessageFrom> from,
    IReadOnlyList<EmailSetMessageFrom> to,
    string subject,
    IReadOnlyDictionary<string, EmailSetMessageBodyValue> bodyValues,
    IReadOnlyList<EmailSetMessageBodyPart> htmlBody,
    IReadOnlyList<EmailSetMessageAttachment> attachments,
    IReadOnlyList<EmailSetMessageFrom>? replyTo = default,
    IReadOnlyList<EmailSetMessageFrom>? cc = default,
    IReadOnlyList<EmailSetMessageFrom>? bcc = default)
{
    [JsonPropertyName("mailboxIds")]
    public IReadOnlyDictionary<string, bool> MailboxIds { get; } = mailboxIds;

    [JsonPropertyName("from")]
    public IReadOnlyList<EmailSetMessageFrom> From { get; } = from;

    [JsonPropertyName("to")]
    public IReadOnlyList<EmailSetMessageFrom> To { get; } = to;

    [JsonPropertyName("subject")]
    public string Subject { get; } = subject;

    [JsonPropertyName("bodyValues")]
    public IReadOnlyDictionary<string, EmailSetMessageBodyValue> BodyValues { get; } = bodyValues;

    [JsonPropertyName("htmlBody")]
    public IReadOnlyList<EmailSetMessageBodyPart> HtmlBody { get; } = htmlBody;

    [JsonPropertyName("attachments")]
    public IReadOnlyList<EmailSetMessageAttachment> Attachments { get; } = attachments;

    [JsonPropertyName("replyTo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyList<EmailSetMessageFrom>? ReplyTo { get; } = replyTo;

    [JsonPropertyName("cc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyList<EmailSetMessageFrom>? Cc { get; } = cc;

    [JsonPropertyName("bcc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyList<EmailSetMessageFrom>? Bcc { get; } = bcc;

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Additional { get; set; } = [];
}

internal sealed class SingletonDictionary<TKey, TValue>(TKey key, TValue value, IEqualityComparer<TKey>? keyEqualityComparer = default)
    : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Eq(TKey a, TKey b)
        => (keyEqualityComparer ?? EqualityComparer<TKey>.Default).Equals(a, b);

    private TKey Key { get; } = key;

    public TValue Value { get; } = value;


    public TValue this[TKey key] => TryGetValue(key, out var value)
        ? value
        : throw new KeyNotFoundException();

    public IEnumerable<TKey> Keys => [Key];

    public IEnumerable<TValue> Values => [Value];

    public int Count => 1;

    public bool ContainsKey(TKey key) => Eq(key, Key);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        yield return new(Key, Value);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (Eq(key, Key))
        {
            value = Value;
            return true;
        }
        value = default;
        return false;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class EmailSubmissionSetSubmission(string emailId, string identityId)
{
    [JsonPropertyName("emailId")]
    public string EmailId { get; } = emailId;

    [JsonPropertyName("identityId")]
    public string IdentityId { get; } = identityId;
}

[method: JsonConstructor]
internal sealed class EmailSetArguments(IReadOnlyDictionary<string, EmailSetMessage> create)
{
    [JsonPropertyName("create")]
    public IReadOnlyDictionary<string, EmailSetMessage> Create { get; } = create;

    public EmailSetArguments(string messageId, EmailSetMessage message)
        : this(new SingletonDictionary<string, EmailSetMessage>(messageId, message))
    { }
}

[method: JsonConstructor]
internal sealed class EmailSubmissionSetArguments(IReadOnlyDictionary<string, EmailSubmissionSetSubmission> create)
{
    [JsonPropertyName("create")]
    public IReadOnlyDictionary<string, EmailSubmissionSetSubmission> Create { get; } = create;

    public EmailSubmissionSetArguments(string messageId, EmailSubmissionSetSubmission message)
        : this(new SingletonDictionary<string, EmailSubmissionSetSubmission>(messageId, message))
    { }
}

internal sealed class MailboxQueryFilterCondition(
    Maybe<string?> parentId = default,
    string? name = default,
    Maybe<string?> role = default,
    bool? hasAnyRole = default,
    bool? isSubscribed = default)
{
    public static MailboxQueryFilterCondition ForRole(string role)
        => new(role: role.Just<string?>());

    [JsonPropertyName("parentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Maybe<string?> ParentId { get; } = parentId;

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Name { get; } = name;

    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Maybe<string?> Role { get; } = role;

    [JsonPropertyName("hasAnyRole")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? HasAnyRole { get; } = hasAnyRole;

    [JsonPropertyName("isSubscribed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? IsSubscribed { get; } = isSubscribed;
}

internal sealed class Comparator(
    string property,
    bool? isAscending = default,
    string? collation = default,
    int? position = default,
    uint? limit = default,
    bool? calculateTotal = default)
{
    [JsonPropertyName("property")]
    public string Property { get; } = property;

    [JsonPropertyName("isAscending")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? IsAscending { get; } = isAscending;

    [JsonPropertyName("collation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Collation { get; } = collation;

    [JsonPropertyName("position")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Position { get; } = position;

    [JsonPropertyName("limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint? Limit { get; } = limit;

    [JsonPropertyName("calculateTotal")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? CalculateTotal { get; } = calculateTotal;
}

internal sealed class MailboxQueryArguments(
    string? accountId = default,
    MailboxQueryFilterCondition? filter = default,
    IReadOnlyList<Comparator>? sort = default,
    bool? sortAsTree = default,
    bool? filterAsTree = default)
{
    [JsonPropertyName("accountId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? AccountId { get; } = accountId;

    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public MailboxQueryFilterCondition? Filter { get; } = filter;

    [JsonPropertyName("sort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyList<Comparator>? Sort { get; } = sort;

    [JsonPropertyName("sortAsTree")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? SortAsTree { get; } = sortAsTree;

    [JsonPropertyName("filterAsTree")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? FilterAsTree { get; } = filterAsTree;
}

internal sealed class MailboxQueryResult(
    string? accountId = default,
    bool? canCalculateChanges = default,
    uint? position = default,
    IReadOnlyList<string>? ids = default,
    uint? total = default,
    uint? limit = default)
{
    [JsonPropertyName("accountId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? AccountId { get; } = accountId;

    [JsonPropertyName("canCalculateChanges")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? CanCalculateChanges { get; } = canCalculateChanges;

    [JsonPropertyName("position")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint? Position { get; } = position;

    [JsonPropertyName("ids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyList<string>? Ids { get; } = ids;

    [JsonPropertyName("total")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint? Total { get; } = total;

    [JsonPropertyName("limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint? Limit { get; } = limit;
}

internal sealed class EmailSetResultCreatedItem(
    string id,
    string? threadId,
    string? blobId,
    long? size)
{
    [JsonPropertyName("id")]
    public string Id { get; } = id;

    [JsonPropertyName("threadId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ThreadId { get; } = threadId;

    [JsonPropertyName("blobId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? BlobId { get; } = blobId;

    [JsonPropertyName("size")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long? Size { get; } = size;
}

internal sealed class EmailSetResultNotCreatedItem(
    string type,
    string? description,
    IReadOnlyList<string>? properties)
{
    [JsonPropertyName("type")]
    public string Type { get; } = type;

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; } = description;

    [JsonPropertyName("properties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyList<string>? Properties { get; } = properties;
}

internal sealed class EmailSetResult(
    string? accountId,
    string? oldState,
    string? newState,
    IReadOnlyDictionary<string, EmailSetResultCreatedItem>? created,
    IReadOnlyDictionary<string, EmailSetResultNotCreatedItem>? notCreated)
{
    [JsonPropertyName("accountId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? AccountId { get; } = accountId;

    [JsonPropertyName("oldState")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? OldState { get; } = oldState;

    [JsonPropertyName("newState")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? NewState { get; } = newState;

    [JsonPropertyName("created")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyDictionary<string, EmailSetResultCreatedItem>? Created { get; } = created;

    [JsonPropertyName("notCreated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyDictionary<string, EmailSetResultNotCreatedItem>? NotCreated { get; } = notCreated;
}

[method: JsonConstructor]
internal readonly struct IdBox(string id)
{
    [JsonPropertyName("id")]
    public string Id { get; } = id;
}

internal sealed class EmailSubmissionResult(
    string? accountId,
    string? oldState,
    string? newState,
    IReadOnlyDictionary<string, IdBox>? created,
    IReadOnlyDictionary<string, EmailSetResultNotCreatedItem>? notCreated)
{
    [JsonPropertyName("accountId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? AccountId { get; } = accountId;

    [JsonPropertyName("oldState")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? OldState { get; } = oldState;

    [JsonPropertyName("newState")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? NewState { get; } = newState;

    [JsonPropertyName("created")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyDictionary<string, IdBox>? Created { get; } = created;

    [JsonPropertyName("notCreated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyDictionary<string, EmailSetResultNotCreatedItem>? NotCreated { get; } = notCreated;
}

internal sealed class JmapCall(IReadOnlyList<string> @using, IReadOnlyList<MethodCall> methodCalls)
{
    [JsonPropertyName("using")]
    public IReadOnlyList<string> Using { get; } = @using;

    [JsonPropertyName("methodCalls")]
    public IReadOnlyList<MethodCall> MethodCalls { get; } = methodCalls;
}

[JsonConverter(typeof(MethodResponseConverter))]
internal sealed class MethodResponse(string name, JsonElement data, string clientId)
{
    [JsonPropertyName("name")]
    public string Name { get; } = name;

    [JsonPropertyName("data")]
    public JsonElement Data { get; } = data;

    [JsonPropertyName("clientId")]
    public string ClientId { get; } = clientId;

    public bool TryGetResponse<T>(JsonTypeInfo<T> typeInfo, [MaybeNullWhen(false)] out T response)
    {
        try
        {
            if (JsonSerializer.Deserialize(Data, typeInfo) is T resp)
            {
                response = resp;
                return true;
            }
            response = default;
            return false;
        }
        catch
        {
            response = default;
            return false;
        }
    }

    public T GetResponse<T>(JsonTypeInfo<T> typeInfo)
        => JsonSerializer.Deserialize(Data, typeInfo)
            ?? throw new InvalidOperationException("Deserializing response retuneed null.");
}

internal sealed class MethodResponseConverter : JsonConverter<MethodResponse>
{
    public override MethodResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Expect(JsonTokenType.StartArray);
        reader.ReadOrThrow();
        var ix = 0;
        string? name = default;
        JsonElement data = default;
        string? clientId = default;
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            switch (ix)
            {
                case 0:
                    name = reader.GetString();
                    break;
                case 1:
                    data = JsonElement.ParseValue(ref reader);
                    break;
                case 2:
                    clientId = reader.GetString();
                    break;
                default:
                    throw new JsonException("Method response must be an array with 3 items.");
            }
            reader.ReadOrThrow();
            ++ix;
        }
        return new(
            name ?? throw new JsonException("No name found while deserializing method response."),
            data,
            clientId ?? throw new JsonException("No name found while deserializing method response.")
        );
    }

    public override void Write(Utf8JsonWriter writer, MethodResponse value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.Name);
        value.Data.WriteTo(writer);
        writer.WriteStringValue(value.ClientId);
        writer.WriteEndArray();
    }
}

internal sealed class JmapResponse(IReadOnlyList<MethodResponse> methodResponses, string sessionState)
{
    [JsonPropertyName("methodResponses")]
    public IReadOnlyList<MethodResponse> MethodResponses { get; } = methodResponses;

    [JsonPropertyName("sessionState")]
    public string SessionState { get; } = sessionState;

    public MethodResponse this[string clientId]
    {
        get
        {
            return MethodResponses.FirstOrDefault(resp => resp.ClientId == clientId)
                ?? throw new KeyNotFoundException($"No method response found for \"{clientId}\".");
        }
    }
}

internal sealed class BlobUploadResponse(string accountId, string blobId, string type, long size)
{
    [JsonPropertyName("accountId")]
    public string AccountId { get; } = accountId;

    [JsonPropertyName("blobId")]
    public string BlobId { get; } = blobId;

    [JsonPropertyName("type")]
    public string Type { get; } = type;

    [JsonPropertyName("size")]
    public long Size { get; } = size;
}

internal sealed class IdentityGetResponseIdentity(
    string id,
    string? name,
    string email,
    IReadOnlyList<EmailSetMessageFrom>? replyTo,
    IReadOnlyList<EmailSetMessageFrom>? bcc,
    string? textSignature,
    string? htmlSignature,
    bool? mayDelete)
{
    [JsonPropertyName("id")]
    public string Id { get; } = id;

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Name { get; } = name;

    [JsonPropertyName("email")]
    public string Email { get; } = email;

    [JsonPropertyName("replyTo")]
    public IReadOnlyList<EmailSetMessageFrom>? ReplyTo { get; } = replyTo;

    [JsonPropertyName("bcc")]
    public IReadOnlyList<EmailSetMessageFrom>? Bcc { get; } = bcc;

    [JsonPropertyName("textSignature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? TextSignature { get; } = textSignature;

    [JsonPropertyName("htmlSignature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? HtmlSignature { get; } = htmlSignature;

    [JsonPropertyName("mayDelete")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? MayDelete { get; } = mayDelete;
}

internal sealed class IdentityGetResponse(
    string? accountId,
    string? state,
    IReadOnlyList<IdentityGetResponseIdentity> list,
    IReadOnlyList<string>? notFound)
{
    [JsonPropertyName("accountId")]
    public string? AccountId { get; } = accountId;

    [JsonPropertyName("state")]
    public string? State { get; } = state;

    [JsonPropertyName("list")]
    public IReadOnlyList<IdentityGetResponseIdentity> List { get; } = list;

    [JsonPropertyName("notFound")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyList<string>? NotFound { get; } = notFound;
}