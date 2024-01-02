using Discord.Shared.Activity;
using Discord.Shared.Logging;

using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace Discord.Server.Utilities
{
    public static class EnumConversion
    {
        public static ActivityStatus GetCustomStatus(this UserStatus status)
        {
            switch (status)
            {
                case UserStatus.Offline:
                    return ActivityStatus.Offline;

                case UserStatus.Online:
                    return ActivityStatus.Online;

                case UserStatus.Invisible:
                    return ActivityStatus.Invisible;

                case UserStatus.Idle:
                    return ActivityStatus.Idle;

                case UserStatus.DoNotDisturb:
                    return ActivityStatus.DoNotDisturb;
            }

            throw new Exception();
        }

        public static UserStatus GetStatus(this ActivityStatus status)
        {
            switch (status)
            {
                case ActivityStatus.Offline:
                    return UserStatus.Offline;

                case ActivityStatus.Online:
                    return UserStatus.Online;

                case ActivityStatus.Invisible:
                    return UserStatus.Invisible;

                case ActivityStatus.Idle:
                    return UserStatus.Idle;

                case ActivityStatus.DoNotDisturb:
                    return UserStatus.DoNotDisturb;
            }

            throw new Exception();
        }

        public static Shared.Activity.ActivityType GetCustomType(this DSharpPlus.Entities.ActivityType type)
        {
            switch (type)
            {
                case DSharpPlus.Entities.ActivityType.Playing:
                    return Shared.Activity.ActivityType.Playing;

                case DSharpPlus.Entities.ActivityType.Watching:
                    return Shared.Activity.ActivityType.Watching;

                case DSharpPlus.Entities.ActivityType.Streaming:
                    return Shared.Activity.ActivityType.Streaming;

                case DSharpPlus.Entities.ActivityType.Custom:
                    return Shared.Activity.ActivityType.Custom;

                case DSharpPlus.Entities.ActivityType.Competing:
                    return Shared.Activity.ActivityType.Competing;

                case DSharpPlus.Entities.ActivityType.ListeningTo:
                    return Shared.Activity.ActivityType.ListeningTo;
            }

            throw new Exception();
        }

        public static DSharpPlus.Entities.ActivityType GetDiscordType(this Shared.Activity.ActivityType type)
        {
            switch (type)
            {
                case Shared.Activity.ActivityType.Playing:
                    return DSharpPlus.Entities.ActivityType.Playing;

                case Shared.Activity.ActivityType.Watching:
                    return DSharpPlus.Entities.ActivityType.Watching;

                case Shared.Activity.ActivityType.Streaming:
                    return DSharpPlus.Entities.ActivityType.Streaming;

                case Shared.Activity.ActivityType.Custom:
                    return DSharpPlus.Entities.ActivityType.Custom;

                case Shared.Activity.ActivityType.Competing:
                    return DSharpPlus.Entities.ActivityType.Competing;

                case Shared.Activity.ActivityType.ListeningTo:
                    return DSharpPlus.Entities.ActivityType.ListeningTo;
            }

            throw new Exception();
        }

        public static LogLevel ToLogLevel(this DiscordLog.Severity severity)
        {
            switch (severity)
            {
                case DiscordLog.Severity.Warning:
                    return LogLevel.Warning;

                case DiscordLog.Severity.Information:
                    return LogLevel.Information;

                case DiscordLog.Severity.Error:
                    return LogLevel.Error;

                case DiscordLog.Severity.Trace:
                    return LogLevel.Trace;

                case DiscordLog.Severity.Verbose:
                case DiscordLog.Severity.Debug:
                    return LogLevel.Debug;
            }

            throw new Exception($"Invalid enum value: {severity}");
        }

        public static DiscordLog.Severity ToSeverity(this LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    return DiscordLog.Severity.Error;

                case LogLevel.Information:
                    return DiscordLog.Severity.Information;

                case LogLevel.Warning:
                    return DiscordLog.Severity.Warning;

                case LogLevel.Trace:
                    return DiscordLog.Severity.Trace;

                case LogLevel.Debug:
                    return DiscordLog.Severity.Debug;
            }

            throw new Exception($"Invalid enum value: {level}");
        }
    }
}
