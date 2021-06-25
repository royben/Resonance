using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.ExtensionMethods
{
    public static class ILoggerExtensions
    {
        public static Exception LogErrorThrow(this ILogger logger, Exception ex, String message)
        {
            logger.LogError(ex, message);
            return ex;
        }

        public static Exception LogErrorThrow(this ILogger logger, Exception ex)
        {
            logger.LogError(ex, ex.Message);
            return ex;
        }

        public static void LogError(this ILogger logger, Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }

        public static void LogCritical(this ILogger logger, Exception ex)
        {
            logger.LogCritical(ex, ex.Message);
        }

        public static IDisposable BeginScopeToken(this ILogger logger, String token)
        {
            return logger.BeginScope(new Dictionary<String, Object>()
            {
                ["Token"] = token
            });
        }

        public static void LogDebugToken(this ILogger logger, String token, String message, params object[] args)
        {
            using (logger.BeginScopeToken(token))
            {
                logger.LogDebug(message, args);
            }
        }

        public static void LogInformationToken(this ILogger logger, String token, String message, params object[] args)
        {
            using (logger.BeginScopeToken(token))
            {
                logger.LogInformation(message, args);
            }
        }

        public static void LogWarningToken(this ILogger logger, String token, String message, params object[] args)
        {
            using (logger.BeginScopeToken(token))
            {
                logger.LogWarning(message, args);
            }
        }

        public static void LogWarningToken(this ILogger logger, String token, Exception ex, String message, params object[] args)
        {
            using (logger.BeginScopeToken(token))
            {
                logger.LogWarning(ex, message, args);
            }
        }

        public static void LogErrorToken(this ILogger logger, String token, Exception ex, String message, params object[] args)
        {
            using (logger.BeginScopeToken(token))
            {
                logger.LogError(ex, message, args);
            }
        }

        public static void LogErrorTokenNoMessage(this ILogger logger, String token, Exception ex, params object[] args)
        {
            using (logger.BeginScopeToken(token))
            {
                logger.LogError(ex, ex.Message, args);
            }
        }

        public static Exception LogErrorThrowToken(this ILogger logger, String token, Exception ex, String message)
        {
            using (logger.BeginScopeToken(token))
            {
                return logger.LogErrorThrow(ex, message);
            }
        }

        public static Exception LogErrorThrowToken(this ILogger logger, String token, Exception ex)
        {
            using (logger.BeginScopeToken(token))
            {
                return logger.LogErrorThrow(ex);
            }
        }
    }
}
