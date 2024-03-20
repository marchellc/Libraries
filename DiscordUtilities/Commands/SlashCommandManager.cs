using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;
using Common.Utilities;

using Discord.WebSocket;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordUtilities.Commands
{
    public class SlashCommandManager : Disposable
    {
        private readonly LockedDictionary<string, SlashCommandInfo> registeredCommands = new LockedDictionary<string, SlashCommandInfo>();

        public LogOutput Log { get; set; }
        public DiscordSocketClient Client { get; private set; }

        public bool IsInitialized { get; private set; }
        public bool LogUnknownCommands { get; set; } = true;

        public event Action<string, SocketUser> OnSlashCommandReceived;

        public event Action<SlashCommandInfo> OnSlashCommandFound;
        public event Action<SlashCommandInfo> OnSlashCommandExecuted;

        public SlashCommandManager(DiscordSocketClient baseDiscordClient, LogOutput log = null)
        {
            Client = baseDiscordClient;
            Log = log ?? new LogOutput("Slash Commands").Setup();
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                Log?.Warn($"Tried initializing twice.");
                return;
            }

            if (Client is null)
            {
                Log?.Warn($"Tried initializing with a null client.");
                return;
            }

            IsInitialized = true;

            Client.SlashCommandExecuted += OnSlashCommand;
        }

        public override void OnDispose()
        {
            base.OnDispose();

            IsInitialized = false;
            LogUnknownCommands = true;

            if (Client != null)
            {
                Client.SlashCommandExecuted -= OnSlashCommand;
                Client = null;
            }

            Log?.Dispose();
            Log = null;

            registeredCommands.Clear();
        }

        public void RegisterCommands()
        {
            Task.Run(async () =>
            {
                foreach (var cmd in registeredCommands)
                {
                    if (!cmd.Value.IsGlobal)
                        continue;

                    try
                    {
                        await Client.CreateGlobalApplicationCommandAsync(cmd.Value.GetApplicationCommandProperties());
                        Log?.Verbose($"Registered global command '{cmd.Key}'");
                    }
                    catch (Exception ex)
                    {
                        Log?.Error($"Failed to register global command '{cmd.Key}'!\n{ex}");
                    }
                }
            });
        }

        public void RegisterCommands(SocketGuild guild, bool onlyNonGlobal = false)
        {
            Task.Run(async () =>
            {
                foreach (var cmd in registeredCommands)
                {
                    if (onlyNonGlobal && cmd.Value.IsGlobal)
                        continue;

                    try
                    {
                        await guild.CreateApplicationCommandAsync(cmd.Value.GetApplicationCommandProperties());
                        Log?.Verbose($"Registered guild command '{cmd.Key}' in {guild.Name}");
                    }
                    catch (Exception ex)
                    {
                        Log?.Error($"Failed to register guild command '{cmd.Key}' in {guild.Name}!\n{ex}");
                    }
                }
            });
        }

        public int Register()
            => Register(Assembly.GetCallingAssembly());

        public int Register(Assembly assembly)
        {
            var counter = 0;

            foreach (var type in assembly.GetTypes())
                counter += Register(type, null);

            return counter;
        }

        public int Register(Type type, object instance = null)
        {
            var counter = 0;

            foreach (var method in type.GetAllMethods())
            {
                if (!method.HasAttribute<CommandAttribute>(out var commandAttribute))
                    continue;

                if (Register(method, commandAttribute.Name, commandAttribute.Description, commandAttribute.IsGlobal, instance))
                    counter++;
            }

            return counter;
        }

        public bool Register(MethodInfo method, string name, string description, bool isGlobal, object instance = null)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (!method.IsStatic && !method.DeclaringType.IsValidInstance(instance, false))
                throw new ArgumentNullException(nameof(instance));

            if (string.IsNullOrWhiteSpace(description))
                description = "No description.";

            if (registeredCommands.Any(p => p.Key.ToLower() == name.ToLower()))
            {
                Log?.Warn($"Attempted to register a duplicate command: {name}");
                return false;
            }

            if (!SlashCommandInfo.TryGet(method, name, description, instance, isGlobal, out var command))
            {
                Log?.Warn($"Failed to create a Slash Command from method '{method.ToName()}', perhaps one of it's parameters is not convertible.");
                return false;
            }

            registeredCommands[name] = command;
            Log?.Verbose($"Registered command '{name}': {method.ToName()}");
            return true;
        }

        public bool Unregister()
            => Unregister(Assembly.GetCallingAssembly());

        public bool Unregister(Assembly assembly)
        {
            var matchedValues = registeredCommands.Where(cmd => cmd.Value.Method.DeclaringType.Assembly == assembly);

            if (matchedValues.Count() < 1)
                return false;

            foreach (var value in matchedValues)
                registeredCommands.Remove(value.Key);

            return true;
        }

        public bool Unregister(Type type, object instance = null)
        {
            var matchedValues = registeredCommands.Where(cmd => cmd.Value.Method.DeclaringType == type && cmd.Value.Target.IsEqualTo(instance));

            if (matchedValues.Count() < 1)
                return false;

            foreach (var value in matchedValues)
                registeredCommands.Remove(value.Key);

            return true;
        }

        public bool Unregister(MethodInfo method, object instance = null)
        {
            var matchedValues = registeredCommands.Where(cmd => cmd.Value.Method == method && cmd.Value.Target.IsEqualTo(instance));

            if (matchedValues.Count() < 1)
                return false;

            foreach (var value in matchedValues)
                registeredCommands.Remove(value.Key);

            return true;
        }

        public bool Unregister(string name)
            => registeredCommands.Remove(name);

        private async Task OnSlashCommand(SocketSlashCommand arg)
        {
            try
            {
                if (!IsInitialized)
                {
                    Log?.Warn($"Received a slash command, but not initialized");
                    return;
                }

                Log?.Verbose($"Received a new slash command: {arg.Data.Name} in {arg.Channel.Name} by {arg.User.GlobalName} ({arg.User.Id})");

                OnSlashCommandReceived.Call(arg.CommandName, arg.User);

                if (!registeredCommands.TryGetValue(arg.Data.Name.ToLower(), out var foundCommand))
                {
                    if (LogUnknownCommands)
                        Log?.Warn($"Received an unknown slash command: {arg.Data.Name}");

                    return;
                }

                OnSlashCommandFound.Call(foundCommand);

                if (!SlashCommandUtils.ConvertArgs(arg, foundCommand))
                {
                    Log?.Warn($"Failed to convert arguments, perhaps some are missing.");
                    return;
                }

                var result = foundCommand.Method.CallUnsafe(foundCommand.Target, foundCommand.Buffer);

                if (result != null && result is Task task && !task.IsFinished())
                    await task.ConfigureAwait(false);

                OnSlashCommandExecuted.Call(foundCommand);
            }
            catch (Exception ex)
            {
                Log?.Error($"An error has occured while processing slash command '{arg.CommandName}'!\n{ex}");
            }
        }
    }
}