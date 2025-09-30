using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;

namespace Xams.Firebase;

public static class FirebaseApiExtensions
{
    public class AddXamsFirebaseApiOptions
    {
        public bool RequireAuthorization { get; set; } = false;
        public string UrlPath { get; set; } = "xams";
    }

    public static IEndpointRouteBuilder AddXamsFirebaseApi(
        this IEndpointRouteBuilder app,
        Action<AddXamsFirebaseApiOptions>? options = null)
    {
        var opts = new AddXamsFirebaseApiOptions();
        options?.Invoke(opts);

        var group = app.MapGroup(opts.UrlPath);

        var verifyEmail = group.MapPost("verifyemail",
            async ([FromBody] VerifyEmailRequest request) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(request.Email))
                    {
                        return Results.BadRequest(new ApiResponse
                        {
                            Succeeded = false,
                            FriendlyMessage = "Email address is required",
                            LogMessage = "Email address is required"
                        });
                    }

                    // Get user by email
                    UserRecord userRecord;
                    try
                    {
                        userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(request.Email);
                    }
                    catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
                    {
                        return Results.NotFound(new ApiResponse
                        {
                            Succeeded = false,
                            FriendlyMessage = "User not found",
                            LogMessage = $"No user found with email: {request.Email}"
                        });
                    }

                    // Check if user has provider data (OAuth providers like Google, Facebook, Microsoft, etc.)
                    var hasOAuthProvider = userRecord.ProviderData
                        .Any(provider => provider.ProviderId != "password" && provider.ProviderId != "phone");

                    if (!hasOAuthProvider)
                    {
                        return Results.Ok(new ApiResponse
                        {
                            Succeeded = false,
                            FriendlyMessage = "User has not used an OAuth provider to login",
                            LogMessage = $"User {request.Email} has not used an OAuth provider (Google, Facebook, Microsoft, etc.)"
                        });
                    }

                    // If already verified, return success
                    if (userRecord.EmailVerified)
                    {
                        return Results.Ok(new ApiResponse
                        {
                            Succeeded = true,
                            FriendlyMessage = "Email is already verified",
                            LogMessage = $"Email {request.Email} is already verified"
                        });
                    }

                    // Mark email as verified
                    var args = new UserRecordArgs
                    {
                        Uid = userRecord.Uid,
                        EmailVerified = true
                    };
                    await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);

                    return Results.Ok(new ApiResponse
                    {
                        Succeeded = true,
                        FriendlyMessage = "Email verified successfully",
                        LogMessage = $"Email {request.Email} has been marked as verified"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "An error occurred while verifying email",
                        Detail = ex.Message
                    });
                }
            });

        if (opts.RequireAuthorization)
        {
            verifyEmail.RequireAuthorization();
        }

        return app;
    }

    public record VerifyEmailRequest(string Email);

    public record ApiResponse
    {
        public bool Succeeded { get; set; }
        public string? FriendlyMessage { get; set; }
        public string? LogMessage { get; set; }
    }

    /// <summary>
    /// Configures JWT Bearer authentication for Firebase.
    /// </summary>
    /// <param name="options">The JWT Bearer options to configure</param>
    /// <param name="firebaseProjectId">The Firebase project ID</param>
    public static void AddXamsFirebaseAuth(this JwtBearerOptions options, string firebaseProjectId)
    {
        options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
            ValidateAudience = true,
            ValidAudience = firebaseProjectId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    }
}