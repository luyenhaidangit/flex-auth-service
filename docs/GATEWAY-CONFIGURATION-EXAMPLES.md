# Gateway Configuration Examples

## 📘 Các ví dụ cấu hình thực tế cho API Gateway

---

## 1. Multi-Service Configuration

### Scenario: Gateway route đến nhiều services với config khác nhau

```json
{
  "ReverseProxy": {
    "Routes": {
      "branch-api": {
        "ClusterId": "branch-service",
        "Match": {
          "Path": "/api/branches/{**catch-all}"
        }
      },
      "approval-api": {
        "ClusterId": "approval-service",
        "Match": {
          "Path": "/api/approvals/{**catch-all}"
        }
      },
      "report-api": {
        "ClusterId": "report-service",
        "Match": {
          "Path": "/api/reports/{**catch-all}"
        },
        "Metadata": {
          "Timeout": "10"
        }
      }
    },
    "Clusters": {
      "branch-service": {
        "LoadBalancingPolicy": "PowerOfTwoChoices",
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:10",
            "Timeout": "00:00:02",
            "Policy": "ConsecutiveFailures",
            "Path": "/health"
          }
        },
        "HttpClient": {
          "RequestTimeout": "00:00:04"
        },
        "Destinations": {
          "primary": {
            "Address": "https://branch-svc-1.internal/",
            "Health": "https://branch-svc-1.internal/health"
          },
          "secondary": {
            "Address": "https://branch-svc-2.internal/",
            "Health": "https://branch-svc-2.internal/health"
          }
        }
      },
      "approval-service": {
        "LoadBalancingPolicy": "RoundRobin",
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:15",
            "Timeout": "00:00:03",
            "Policy": "ConsecutiveFailures",
            "Path": "/health"
          }
        },
        "HttpClient": {
          "RequestTimeout": "00:00:05"
        },
        "Destinations": {
          "d1": {
            "Address": "https://approval-svc.internal/",
            "Health": "https://approval-svc.internal/health"
          }
        }
      },
      "report-service": {
        "LoadBalancingPolicy": "FirstAlphabetical",
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:20",
            "Timeout": "00:00:05",
            "Policy": "ConsecutiveFailures",
            "Path": "/health"
          }
        },
        "HttpClient": {
          "RequestTimeout": "00:00:10"
        },
        "Destinations": {
          "d1": {
            "Address": "https://report-svc.internal/",
            "Health": "https://report-svc.internal/health"
          }
        }
      }
    }
  }
}
```

**Giải thích:**
- **Branch Service**: Timeout 4s, 2 destinations (HA)
- **Approval Service**: Timeout 5s, RoundRobin LB
- **Report Service**: Timeout 10s (vì report thường chậm hơn)

---

## 2. Environment-Specific Configuration

### Development (`yarp.Development.json`)

```json
{
  "ReverseProxy": {
    "Routes": {
      "branch-api": {
        "ClusterId": "branch-service",
        "Match": {
          "Path": "/api/branches/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "branch-service": {
        "LoadBalancingPolicy": "First",
        "HealthCheck": {
          "Active": {
            "Enabled": false
          }
        },
        "HttpClient": {
          "RequestTimeout": "00:00:30",
          "DangerousAcceptAnyServerCertificate": true
        },
        "Destinations": {
          "local": {
            "Address": "http://localhost:5001/"
          }
        }
      }
    }
  }
}
```

### Production (`yarp.Production.json`)

```json
{
  "ReverseProxy": {
    "Routes": {
      "branch-api": {
        "ClusterId": "branch-service",
        "Match": {
          "Path": "/api/branches/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "branch-service": {
        "LoadBalancingPolicy": "PowerOfTwoChoices",
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:10",
            "Timeout": "00:00:02",
            "Policy": "ConsecutiveFailures",
            "Path": "/health"
          },
          "Passive": {
            "Enabled": true,
            "Policy": "TransportFailureRate",
            "ReactivationPeriod": "00:01:00"
          }
        },
        "HttpClient": {
          "RequestTimeout": "00:00:04",
          "DangerousAcceptAnyServerCertificate": false
        },
        "Destinations": {
          "pod-1": {
            "Address": "https://branch-svc-1.prod.internal/",
            "Health": "https://branch-svc-1.prod.internal/health"
          },
          "pod-2": {
            "Address": "https://branch-svc-2.prod.internal/",
            "Health": "https://branch-svc-2.prod.internal/health"
          },
          "pod-3": {
            "Address": "https://branch-svc-3.prod.internal/",
            "Health": "https://branch-svc-3.prod.internal/health"
          }
        }
      }
    }
  }
}
```

**Khác biệt:**
- **Dev**: Health check OFF, timeout cao (30s), 1 destination
- **Prod**: Health check ON, timeout thấp (4s), multiple destinations, HTTPS strict

---

## 3. Per-Route Rate Limiting

### Trong `Program.cs` hoặc `ServiceExtensions.cs`

```csharp
builder.Services.AddRateLimiter(options =>
{
    // Policy cho API public (rate limit thấp)
    options.AddFixedWindowLimiter("public-api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromSeconds(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // Policy cho API nội bộ (rate limit cao)
    options.AddFixedWindowLimiter("internal-api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromSeconds(1);
        limiterOptions.QueueLimit = 0;
    });

    // Policy cho admin (không limit)
    options.AddConcurrencyLimiter("admin-api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 1000;
        limiterOptions.QueueLimit = 0;
    });
});
```

### Apply policy cho route

```json
{
  "Routes": {
    "public-branch-api": {
      "ClusterId": "branch-service",
      "Match": {
        "Path": "/api/public/branches/{**catch-all}"
      },
      "Metadata": {
        "RateLimitPolicy": "public-api"
      }
    },
    "internal-branch-api": {
      "ClusterId": "branch-service",
      "Match": {
        "Path": "/api/internal/branches/{**catch-all}"
      },
      "Metadata": {
        "RateLimitPolicy": "internal-api"
      }
    }
  }
}
```

---

## 4. Custom Transforms

### Remove sensitive headers

```csharp
// Trong HeaderTransform.cs
public void Apply(TransformBuilderContext context)
{
    context.AddRequestTransform(ctx =>
    {
        var headers = ctx.ProxyRequest.Headers;

        // Remove sensitive headers
        headers.Remove("Cookie");
        headers.Remove("Authorization"); // Nếu dùng internal auth
        headers.Remove("X-Api-Key");
        headers.Remove("Referer");

        // Add gateway info
        headers.TryAddWithoutValidation("X-Gateway-Version", "1.0.0");
        headers.TryAddWithoutValidation("X-Forwarded-By", "FlexGateway");

        return ValueTask.CompletedTask;
    });
}
```

### Add authentication headers

```csharp
// Custom transform cho internal authentication
public class InternalAuthTransform : ITransformProvider
{
    public void Apply(TransformBuilderContext context)
    {
        context.AddRequestTransform(async ctx =>
        {
            // Add internal API key
            ctx.ProxyRequest.Headers.TryAddWithoutValidation(
                "X-Internal-Api-Key",
                "your-secure-internal-key"
            );

            // Or use certificate authentication
            // ctx.ProxyRequest.Headers.TryAddWithoutValidation(
            //     "X-Client-Cert",
            //     GetClientCertificate()
            // );

            await Task.CompletedTask;
        });
    }
}
```

---

## 5. Advanced Resilience Configuration

### Custom resilience cho từng service

```csharp
// Trong ServiceExtensions.cs
public static IServiceCollection AddServiceSpecificResilience(
    this IServiceCollection services)
{
    // Fast service: Timeout thấp, retry nhanh
    services.AddHttpClient("branch-client")
        .AddHttpMessageHandler<CorrelationIdHandler>()
        .AddCustomResilience(
            timeout: TimeSpan.FromSeconds(3),
            maxRetryAttempts: 1
        );

    // Slow service: Timeout cao, không retry
    services.AddHttpClient("report-client")
        .AddHttpMessageHandler<CorrelationIdHandler>()
        .AddCustomResilience(
            timeout: TimeSpan.FromSeconds(15),
            maxRetryAttempts: 0
        );

    // Critical service: Circuit breaker khắt khe
    services.AddHttpClient("payment-client")
        .AddHttpMessageHandler<CorrelationIdHandler>()
        .AddCustomResilience(
            timeout: TimeSpan.FromSeconds(5),
            maxRetryAttempts: 1,
            circuitBreakerFailureRatio: 0.3 // Open nếu 30% fail
        );

    return services;
}
```

---

## 6. Authentication & Authorization per Route

### Trong `yarp.json`

```json
{
  "Routes": {
    "public-api": {
      "ClusterId": "branch-service",
      "Match": {
        "Path": "/api/public/{**catch-all}"
      },
      "Metadata": {
        "Authorization": "Anonymous"
      }
    },
    "user-api": {
      "ClusterId": "branch-service",
      "Match": {
        "Path": "/api/user/{**catch-all}"
      },
      "Metadata": {
        "Authorization": "RequireAuthenticatedUser"
      }
    },
    "admin-api": {
      "ClusterId": "branch-service",
      "Match": {
        "Path": "/api/admin/{**catch-all}"
      },
      "Metadata": {
        "Authorization": "RequireAdminRole"
      }
    }
  }
}
```

### Apply authorization

```csharp
// Trong ApplicationExtensions.cs
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use((context, next) =>
    {
        var endpoint = context.GetEndpoint();
        var authPolicy = endpoint?.Metadata
            .GetMetadata<string>("Authorization");

        if (authPolicy == "Anonymous")
        {
            return next();
        }

        // Apply authorization
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        if (authPolicy == "RequireAdminRole" 
            && !context.User.IsInRole("Admin"))
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        return next();
    });
});
```

---

## 7. Canary Deployment / A/B Testing

### Config canary deployment

```json
{
  "Routes": {
    "branch-api-canary": {
      "ClusterId": "branch-service-canary",
      "Match": {
        "Path": "/api/branches/{**catch-all}",
        "Headers": [
          {
            "Name": "X-Canary",
            "Values": ["true"]
          }
        ]
      }
    },
    "branch-api-stable": {
      "ClusterId": "branch-service-stable",
      "Match": {
        "Path": "/api/branches/{**catch-all}"
      }
    }
  },
  "Clusters": {
    "branch-service-canary": {
      "Destinations": {
        "canary": {
          "Address": "https://branch-svc-canary.internal/"
        }
      }
    },
    "branch-service-stable": {
      "Destinations": {
        "stable": {
          "Address": "https://branch-svc-stable.internal/"
        }
      }
    }
  }
}
```

**Sử dụng:**
- Request với header `X-Canary: true` → route đến canary version
- Request bình thường → route đến stable version

---

## 8. Request/Response Logging

### Custom logging middleware

```csharp
// Trong ApplicationExtensions.cs
app.Use(async (context, next) =>
{
    var logger = context.RequestServices
        .GetRequiredService<ILogger<Program>>();

    var correlationId = context.Request.Headers["X-Correlation-Id"]
        .FirstOrDefault();

    logger.LogInformation(
        "Gateway Request: {Method} {Path} | CorrelationId: {CorrelationId}",
        context.Request.Method,
        context.Request.Path,
        correlationId
    );

    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();

    logger.LogInformation(
        "Gateway Response: {StatusCode} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
        context.Response.StatusCode,
        sw.ElapsedMilliseconds,
        correlationId
    );
});
```

---

## 9. Blue-Green Deployment

### Config blue-green với weighted destinations

```json
{
  "Clusters": {
    "branch-service": {
      "LoadBalancingPolicy": "Random",
      "Destinations": {
        "blue": {
          "Address": "https://branch-blue.internal/",
          "Metadata": {
            "Weight": "90"
          }
        },
        "green": {
          "Address": "https://branch-green.internal/",
          "Metadata": {
            "Weight": "10"
          }
        }
      }
    }
  }
}
```

**Giải thích:**
- 90% traffic → blue (stable)
- 10% traffic → green (new version)
- Sau khi green ổn định → swap weight

---

## 10. CORS Configuration per Route

### Trong `ServiceExtensions.cs`

```csharp
builder.Services.AddCors(options =>
{
    // Public API: Allow all
    options.AddPolicy("PublicApi", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Internal API: Restrict origins
    options.AddPolicy("InternalApi", policy =>
    {
        policy.WithOrigins(
                "https://admin.example.com",
                "https://dashboard.example.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
```

### Apply CORS per route

```csharp
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        var endpoint = context.GetEndpoint();
        var corsPolicy = endpoint?.Metadata
            .GetMetadata<string>("CorsPolicy");

        if (!string.IsNullOrEmpty(corsPolicy))
        {
            var cors = context.RequestServices
                .GetRequiredService<ICorsService>();
            var policy = context.RequestServices
                .GetRequiredService<ICorsPolicyProvider>()
                .GetPolicyAsync(context, corsPolicy)
                .Result;

            if (policy != null)
            {
                var corsResult = cors.EvaluatePolicy(context, policy);
                cors.ApplyResult(corsResult, context.Response);
            }
        }

        await next();
    });
});
```

---

## Tổng kết

Các ví dụ trên bao gồm:
- ✅ Multi-service routing
- ✅ Environment-specific config
- ✅ Per-route rate limiting
- ✅ Custom transforms
- ✅ Advanced resilience
- ✅ Authentication per route
- ✅ Canary deployment
- ✅ Request/response logging
- ✅ Blue-green deployment
- ✅ CORS per route

Chọn pattern phù hợp với use case của bạn!

