# BoneNet Project Overview

## Project Structure
The project follows Clean Architecture principles with clear separation of concerns:

```
BoneNet/
├── Application/        - Core business logic and use cases
│   ├── Categories/    - Category management CQRS
│   ├── Common/        - Shared infrastructure (behaviors, models, jobs)
│   ├── ConfigCart/    - Configuration cart system
│   ├── Configurations/- Configuration management
│   └── Deployments/   - Deployment workflows
├── Domain/            - Core domain models and business rules
│   ├── Common/        - Base entities and value objects
│   ├── Entities/      - Aggregate roots and domain entities
│   ├── Enums/         - Domain-specific enums
│   ├── Exceptions/    - Custom domain exceptions
│   └── ValueObjects/  - Domain value objects
├── Infrastructure/    - Implementation details
│   ├── Caching/       - In-memory caching system
│   ├── Persistence/   - EF Core data access
│   └── Services/      - External service integrations
└── Web/               - Presentation layer (partial)
    └── Admin/         - Blazor components
```

## Key Patterns & Concepts

1. **CQRS Pattern**:
   - Commands (CreateCategoryCommand) and Queries (GetAllCategoriesQuery) are separated
   - MediatR is used for command/query dispatching
   
```7:34:BoneNet.Application/Categories/Commands/CreateCategory/CreateCategoryCommand.cs
public record CreateCategoryCommand(string Name, string? Description) : IRequest<Result<Guid>>;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var existingCategory = await _unitOfWork.BonfigurationRepository
            .GetCategoryByNameAsync(command.Name);

        if (existingCategory != null)
        {
            return Result<Guid>.Failure($"Category with name '{command.Name}' already exists");
        }

        var category = Domain.Entities.BonfigurationCategory.Create(
            command.Name,
            command.Description);

        await _unitOfWork.BonfigurationRepository.AddCategoryAsync(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

```


2. **Domain-Driven Design**:
   - Rich domain models with encapsulated business logic
   - Value Objects for type safety:
   
```5:35:BoneNet.Domain/ValueObjects/DataType.cs

public class DataType : ValueObject
{
    public string Value { get; private set; }

    private DataType(string value)
    {
        Value = value;
    }

    public static DataType String => new("string");
    public static DataType Number => new("number");
    public static DataType Boolean => new("boolean");
    public static DataType Json => new("json");
    public static DataType Date => new("date");

    public static DataType From(string value)
    {
        var normalizedValue = value.ToLowerInvariant();
        return normalizedValue switch
        {
            "string" => String,
            "number" => Number,
            "boolean" => Boolean,
            "json" => Json,
            "date" => Date,
            _ => throw new DomainException($"Invalid data type: {value}")
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
```


3. **Auditing**:
   - BaseAuditableEntity tracks creation/modification dates
   
```3:10:BoneNet.Domain/Common/BaseAuditableEntity.cs
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public abstract class BaseEntity
{
```


4. **Circuit Breaker Pattern**:
   - Implements fault tolerance for external services
   
```7:110:BoneNet.Infrastructure/Services/CircuitBreakerService.cs
public class CircuitBreakerService : ICircuitBreakerService
{
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitStates;
    private readonly ConcurrentDictionary<string, int> _failureCounters;
    private readonly ConcurrentDictionary<string, DateTime> _lastFailureTime;
    
    private const int FailureThreshold = 5;           // Number of failures before opening circuit
    private const int RetryTimeoutSeconds = 60;       // Time to wait before attempting to close circuit
    private const int HalfOpenMaxAttempts = 3;        // Max attempts in half-open state

    public CircuitBreakerService(ILogger<CircuitBreakerService> logger)
    {
        _logger = logger;
        _circuitStates = new ConcurrentDictionary<string, CircuitBreakerState>();
        _failureCounters = new ConcurrentDictionary<string, int>();
        _lastFailureTime = new ConcurrentDictionary<string, DateTime>();
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationKey)
    {
        EnsureCircuitInitialized(operationKey);

        var currentState = GetCircuitState(operationKey);
        
        if (currentState == CircuitBreakerState.Open)
        {
            if (ShouldAttemptReset(operationKey))
            {
                TransitionToHalfOpen(operationKey);
            }
            else
            {
                _logger.LogWarning("Circuit breaker is open for operation: {OperationKey}", operationKey);
                throw new CircuitBreakerOpenException($"Circuit breaker is open for operation: {operationKey}");
            }
        }

        try
        {
            var result = await operation();
            
            if (currentState == CircuitBreakerState.HalfOpen)
            {
                ResetCircuit(operationKey);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            await HandleFailure(operationKey, ex);
            throw;
        }
    }

    public CircuitBreakerState GetCircuitState(string operationKey)
    {
        return _circuitStates.GetOrAdd(operationKey, CircuitBreakerState.Closed);
    }

    private void EnsureCircuitInitialized(string operationKey)
    {
        _circuitStates.GetOrAdd(operationKey, CircuitBreakerState.Closed);
        _failureCounters.GetOrAdd(operationKey, 0);
    }

    private async Task HandleFailure(string operationKey, Exception exception)
    {
        _lastFailureTime.AddOrUpdate(operationKey, DateTime.UtcNow, (_, __) => DateTime.UtcNow);
        
        var failures = _failureCounters.AddOrUpdate(operationKey, 1, (_, count) => count + 1);
        
        _logger.LogWarning(exception, 
            "Operation {OperationKey} failed. Failure count: {FailureCount}", 
            operationKey, failures);

        if (failures >= FailureThreshold)
        {
            TransitionToOpen(operationKey);
        }

        await Task.CompletedTask;
    }

    private void TransitionToOpen(string operationKey)
    {
        _circuitStates.TryUpdate(operationKey, CircuitBreakerState.Open, CircuitBreakerState.Closed);
        _logger.LogWarning("Circuit breaker transitioned to Open for operation: {OperationKey}", operationKey);
    }

    private void TransitionToHalfOpen(string operationKey)
    {
        _circuitStates.TryUpdate(operationKey, CircuitBreakerState.HalfOpen, CircuitBreakerState.Open);
        _logger.LogInformation("Circuit breaker transitioned to HalfOpen for operation: {OperationKey}", operationKey);
    }

    private void ResetCircuit(string operationKey)
    {
        _circuitStates.TryUpdate(operationKey, CircuitBreakerState.Closed, CircuitBreakerState.HalfOpen);
        _failureCounters.TryUpdate(operationKey, 0, _failureCounters[operationKey]);
        _logger.LogInformation("Circuit breaker reset for operation: {OperationKey}", operationKey);
    }

```


## Core Domain Models

1. **Configuration Management**:
   - `BonfigurationCategory` > `BonfigurationItem` > `BonfigurationValue` hierarchy
   - Versioning and draft/live states for configuration values
   
```7:60:BoneNet.Domain/Entities/BonfigurationItem.cs

public class BonfigurationItem : BaseAuditableEntity
{
    private readonly List<BonfigurationValue> _values = new();

    public string Key { get; private set; }
    public string? Description { get; private set; }
    public DataType DataType { get; private set; }
    public bool IsEncrypted { get; private set; }
    public JsonDocument? ValidationRules { get; private set; }
    public Guid CategoryId { get; private set; }
    public virtual BonfigurationCategory Category { get; private set; }
    public IReadOnlyCollection<BonfigurationValue> Values => _values.AsReadOnly();
    
    public Guid? CurrentLiveVersionId { get; private set; }
    public virtual BonfigurationValue? CurrentLiveVersion { get; private set; }
    
    public Guid? CurrentDraftVersionId { get; private set; }
    public virtual BonfigurationValue? CurrentDraftVersion { get; private set; }

    private BonfigurationItem() { } // For EF Core

    public static BonfigurationItem Create(
        string key,
        DataType dataType,
        Guid categoryId,
        string? description = null,
        bool isEncrypted = false,
        JsonDocument? validationRules = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Item key cannot be empty");

        return new BonfigurationItem
        {
            Key = key,
            DataType = dataType,
            CategoryId = categoryId,
            Description = description,
            IsEncrypted = isEncrypted,
            ValidationRules = validationRules
        };
    }

    public void Update(string key, string? description, JsonDocument? validationRules)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Item key cannot be empty");

        Key = key;
        Description = description;
        ValidationRules = validationRules;
    }

```


2. **Deployment System**:
   - `ConfigCart` with validation states
   - Deployment tracking with audit history
   
```7:33:BoneNet.Domain/Entities/Deployment.cs
{
    public Guid PublishedBy { get; private set; }
    public DeploymentStatus Status { get; private set; }
    public Guid ConfigCartId { get; private set; }
    public string? Notes { get; private set; }

    private Deployment() { } // For EF Core

    public Deployment(Guid configCartId, Guid publishedBy, string? notes = null)
    {
        PublishedBy = publishedBy;
        Status = DeploymentStatus.Pending;
        ConfigCartId = configCartId;
        Notes = notes;
    }

    public void MarkAsSuccess()
    {
        Status = DeploymentStatus.Success;
    }

    public void MarkAsFailed()
    {
        Status = DeploymentStatus.Failed;
    }
}
```


## Infrastructure Highlights

1. **Persistence**:
   - Entity Framework Core with PostgreSQL
   - Repository pattern implementation
   
```7:70:BoneNet.Infrastructure/Persistence/UnitOfWork.cs

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BoneNetDbContext _context;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    public IBonfigurationRepository BonfigurationRepository { get; }
    
    public IConfigCartRepository ConfigCartRepository { get; }
    public IDeploymentRepository DeploymentRepository { get; }

    public UnitOfWork(BoneNetDbContext context)
    {
        _context = context;
        ConfigCartRepository = new ConfigCartRepository(context);
        BonfigurationRepository = new BonfigurationRepository(context);
        DeploymentRepository = new DeploymentRepository(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();

            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
```


2. **Caching**:
   - In-memory DataStore with concurrent access
   
```7:95:BoneNet.Infrastructure/Caching/DataStore.cs
public class DataStore
{
    private readonly ConcurrentDictionary<CategoryName, ConcurrentDictionary<string, object>> _data = new();

    public void Set(CategoryName categoryName, string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Key cannot be empty");

        if (value == null)
            throw new InvalidOperationException("Value cannot be null");

        _data.AddOrUpdate(
            categoryName,
            _ => new ConcurrentDictionary<string, object>(new[] { new KeyValuePair<string, object>(key, value) }),
            (_, dict) =>
            {
                dict.AddOrUpdate(key, value, (_, _) => value);
                return dict;
            });
    }

    public object Get(CategoryName categoryName, string key)
    {
        if (!_data.TryGetValue(categoryName, out var categoryData))
            throw new DataStoreKeyNotFoundException($"Category '{categoryName}' not found");

        if (!categoryData.TryGetValue(key, out var value))
            throw new DataStoreKeyNotFoundException($"Key '{key}' not found in category '{categoryName}'");

        return value;
    }

    public bool TryGet<T>(CategoryName categoryName, string key, out T result)
    {
        result = default!;

        if (!_data.TryGetValue(categoryName, out var categoryData))
            return false;

        if (!categoryData.TryGetValue(key, out var value))
            return false;

        if (value is not T typedValue)
            return false;

        result = typedValue;
        return true;
    }

    public IReadOnlyCollection<KeyValuePair<string, object>> GetAllFromCategory(CategoryName categoryName)
    {
        if (!_data.TryGetValue(categoryName, out var categoryData))
            return Array.Empty<KeyValuePair<string, object>>();

        return categoryData.ToList().AsReadOnly();
    }

    public IReadOnlyDictionary<string, object> GetMany(CategoryName categoryName, IEnumerable<string> keys)
    {
        if (!_data.TryGetValue(categoryName, out var categoryData))
            return new Dictionary<string, object>();

        return keys
            .Where(key => categoryData.ContainsKey(key))
            .ToDictionary(
                key => key,
                key => categoryData[key]);
    }

    public bool TryGetMany<T>(CategoryName categoryName, IEnumerable<string> keys, out Dictionary<string, T> results)
    {
        results = new Dictionary<string, T>();

        if (!_data.TryGetValue(categoryName, out var categoryData))
            return false;

        bool allFound = true;
        foreach (var key in keys)
        {
            if (categoryData.TryGetValue(key, out var value) && value is T typedValue)
            {
                results[key] = typedValue;
            }
            else
            {
                allFound = false;
            }
        }
```


3. **Messaging**:
   - RabbitMQ integration for deployment notifications
   
```7:85:BoneNet.Infrastructure/Services/RabbitMqDeploymentQueueService.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace BoneNet.Infrastructure.Services;

public class RabbitMQDeploymentQueueService : IDeploymentQueueService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQDeploymentQueueService> _logger;
    private const string ExchangeName = "bonenet.deployments.fanout";
    private string _queueName;  // Unique queue name for each server instance

    public RabbitMQDeploymentQueueService(IConfiguration configuration, ILogger<RabbitMQDeploymentQueueService> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = configuration.GetValue<string>("RabbitMQ:Host") ?? "rabbitmq",
            Port = configuration.GetValue<int>("RabbitMQ:Port", 5672),
            UserName = configuration.GetValue<string>("RabbitMQ:Username") ?? "guest",
            Password = configuration.GetValue<string>("RabbitMQ:Password") ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare a fanout exchange
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);

        // Create a unique queue name for this server instance
        _queueName = $"bonenet.deployments.{Guid.NewGuid()}";
        
        // Declare a temporary queue that will be deleted when the connection closes
        _channel.QueueDeclare(
            queue: _queueName,
            durable: false,          // Not durable - queue will be deleted when server restarts
            exclusive: true,         // Exclusive to this connection
            autoDelete: true,        // Delete when no consumers
            arguments: null);

        // Bind the queue to the fanout exchange
        _channel.QueueBind(_queueName, ExchangeName, string.Empty);
        
        // Set QoS for this consumer
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
    }

    public Task PublishDeploymentNotification(CreateDeploymentCommand notification)
    {
        var message = JsonSerializer.Serialize(notification);
        var body = Encoding.UTF8.GetBytes(message);

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: string.Empty,  // Not needed for fanout exchange
            basicProperties: null,
            body: body);

        _logger.LogInformation("Published deployment notification: {DeploymentId}", notification.DeploymentId);
        return Task.CompletedTask;
    }

    public async Task SubscribeToDeploymentNotifications(Func<CreateDeploymentCommand, Task> handler)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var notification = JsonSerializer.Deserialize<CreateDeploymentCommand>(message);

                if (notification != null)
                {
```


## Key Flows

1. **Configuration Creation**:
   ```
   CreateCategoryCommand → Validation → Repository → Cache Update
   ```

2. **Deployment Workflow**:
   ```
   Create Deployment → Validate Cart → Queue Notification → Store Configurations
   ```

3. **Validation Process**:
   
```7:40:BoneNet.Application/Common/Behaviors/ValidationBehavior.cs
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

```


## Getting Started Tips

1. Start with Domain models to understand business rules
2. Follow the CQRS pattern in Application layer for new features
3. Use the UnitOfWork pattern for data access
4. Leverage existing value objects for type safety
5. Utilize circuit breaker for external service calls

## Important Dependencies
- MediatR (CQRS)
- FluentValidation
- Entity Framework Core
- RabbitMQ.Client
- Newtonsoft.Json

This structure provides a maintainable foundation for a configuration management system with robust auditing, versioning, and deployment capabilities.