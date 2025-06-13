# Coding Standards

This document outlines the coding standards adopted for the UPortal project, focusing on commenting, logging, and error handling to ensure code clarity, maintainability, and robustness.

## 1. Commenting

Consistent commenting helps in understanding the codebase and facilitates easier maintenance and collaboration.

### 1.1. C# XML Documentation Comments

All public and protected members (classes, methods, properties, events, enums, interfaces, delegates) in C# files (`.cs`) **must** be documented using XML documentation comments.

*   **`<summary>`**: Provide a concise description of the member's purpose.
*   **`<param name="paramName">`**: Describe each parameter for methods and constructors.
*   **`<returns>`**: Describe the value returned by a method.
*   **`<exception cref="ExceptionType">`**: Document any exceptions that the method can explicitly throw.
*   **`<remarks>`**: (Optional) Use for providing additional information beyond the summary.
*   **`<typeparam name="typeName">`**: Describe generic type parameters.

**Example:**
```csharp
/// <summary>
/// Retrieves a user by their Azure Active Directory Object ID.
/// </summary>
/// <param name="azureAdObjectId">The Azure AD Object ID of the user.</param>
/// <returns>A <see cref="AppUserDto"/> if found; otherwise, null.</returns>
/// <exception cref="ArgumentNullException">Thrown if <paramref name="azureAdObjectId"/> is null or empty.</exception>
public async Task<AppUserDto?> GetByAzureAdObjectIdAsync(string azureAdObjectId)
{
    // ... implementation ...
}
```

### 1.2. Inline Comments

Use inline comments (`//`) to explain complex, non-obvious, or critical sections of code within method bodies. Avoid over-commenting simple or self-explanatory code.

**Example:**
```csharp
// If the user is new, set default values before adding to the context.
if (appUser == null)
{
    appUser = new AppUser
    {
        // ...
    };
    context.AppUsers.Add(appUser);
}
```

### 1.3. Razor File Comments

*   For C# code within `@code` blocks in `.razor` files, follow the same XML documentation and inline commenting standards as for `.cs` files.
*   For explaining UI structures, conditional rendering logic, or specific component states within the HTML markup part of `.razor` files, use Razor comments (`@* ... *@`).

**Example:**
```razor
@* Show loading indicator while applications are being fetched *@
@if (_applications == null)
{
    <FluentProgressRing />
}
```

## 2. Logging Practices

Structured logging is crucial for diagnostics and monitoring. Serilog is used as the primary logging framework.

*   **Use `ILogger<TCategoryName>`**: Inject `ILogger` instances for logging within services and components.
*   **Log Levels**:
    *   `LogError`: For errors and exceptions that disrupt an operation. Always include the exception object.
    *   `LogWarning`: For unexpected or potentially harmful situations that do not (yet) cause an operation to fail but might indicate a problem.
    *   `LogInformation`: For tracking the general flow of the application, key events, and successful operations.
    *   `LogDebug`: For detailed diagnostic information useful during development and troubleshooting.
    *   `LogTrace`: For highly detailed diagnostic information, usually disabled in production.
*   **Contextual Information**: Log relevant context, such as method parameters, operation IDs, or entity identifiers, to aid in troubleshooting.
*   **Method Entry/Exit**: Consider logging entry and exit points for public methods in services, especially if they perform critical operations (use `LogDebug` or `LogTrace` generally, `LogInformation` for very high-level operations).
*   **Avoid Sensitive Data**: Do not log sensitive information like passwords, personal identifiable information (unless explicitly required and secured), or security tokens.

**Example:**
```csharp
_logger.LogInformation("Attempting to save application {AppName}", appFromForm.AppName);
try
{
    // ... save operation ...
    _logger.LogInformation("Application {AppName} saved successfully", appFromForm.AppName);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error saving application {AppName}", appFromForm.AppName);
    // ... handle error ...
}
```

## 3. Error Handling Practices

Robust error handling improves application stability and user experience.

*   **Service-Level Handling**:
    *   Services should catch exceptions that occur during their operations (e.g., `DbUpdateException` from Entity Framework).
    *   Log these exceptions with appropriate context before re-throwing, wrapping in a custom exception, or returning an error result.
    *   Validate input parameters and throw argument exceptions (e.g., `ArgumentNullException`, `ArgumentOutOfRangeException`) if inputs are invalid.

*   **UI-Level Handling (Razor Components)**:
    *   Wrap calls to services or other operations that might fail in `try-catch` blocks.
    *   Log any caught exceptions using `ILogger`.
    *   Provide clear, user-friendly error messages to the user (e.g., using a `ToastService`). Avoid exposing raw exception details.
    *   Guide the user on what to do next if possible (e.g., "try again," "contact support").

*   **Global Error Handler**:
    *   A global exception handler (`/Error` page) is configured to catch any unhandled exceptions.
    *   This handler logs the error with a unique Error ID and presents a generic error message to the user, including the Error ID for support reference.

```
