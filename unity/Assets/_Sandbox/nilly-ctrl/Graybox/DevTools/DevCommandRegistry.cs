#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Thrown by a command handler for bad input. The message is shown to the user as the
    /// command's result; no stack trace is logged.
    /// </summary>
    public sealed class DevCommandException : Exception
    {
        public DevCommandException(string message) : base(message)
        {
        }
    }

    /// <summary>What a command line did: whether it worked and the text to show.</summary>
    public sealed class DevCommandResult
    {
        public DevCommandResult(bool ok, string message)
        {
            Ok = ok;
            Message = message ?? string.Empty;
        }

        public bool Ok { get; }

        public string Message { get; }
    }

    /// <summary>The words after the command name, with typed accessors that explain what was wrong.</summary>
    public sealed class DevCommandArgs
    {
        private readonly string[] _words;

        public DevCommandArgs(string[] words, string line)
        {
            _words = words ?? new string[0];
            Line = line ?? string.Empty;
        }

        /// <summary>The whole line as typed.</summary>
        public string Line { get; }

        public int Count => _words.Length;

        public string this[int index] => _words[index];

        public bool Has(int index) => index >= 0 && index < _words.Length;

        /// <summary>The word at <paramref name="index"/>, or an error naming <paramref name="what"/>.</summary>
        public string String(int index, string what)
        {
            if (!Has(index)) throw new DevCommandException("Missing <" + what + ">.");
            return _words[index];
        }

        public int Int(int index, string what)
        {
            string word = String(index, what);
            if (!int.TryParse(word, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                throw new DevCommandException("<" + what + "> must be a whole number, got '" + word + "'.");
            }

            return value;
        }

        public int OptionalInt(int index, int fallback, string what)
        {
            return Has(index) ? Int(index, what) : fallback;
        }

        public float Float(int index, string what)
        {
            string word = String(index, what);
            if (!float.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                throw new DevCommandException("<" + what + "> must be a number, got '" + word + "'.");
            }

            return value;
        }

        /// <summary>The words from <paramref name="index"/> on, joined with single spaces.</summary>
        public string Rest(int index)
        {
            if (index >= _words.Length) return string.Empty;
            return string.Join(" ", _words, index, _words.Length - index);
        }
    }

    /// <summary>Runs a command. The returned text is shown to the user; null means just "ok".</summary>
    public delegate string DevCommandHandler(DevCommandArgs args);

    /// <summary>One registered command.</summary>
    public sealed class DevCommand
    {
        public DevCommand(string name, string usage, string help, DevCommandHandler handler, string[] aliases)
        {
            Name = name;
            Usage = string.IsNullOrEmpty(usage) ? name : usage;
            Help = help ?? string.Empty;
            Handler = handler;
            Aliases = aliases ?? new string[0];
        }

        public string Name { get; }

        public string Usage { get; }

        public string Help { get; }

        public string[] Aliases { get; }

        public DevCommandHandler Handler { get; }
    }

    /// <summary>
    /// The set of console commands. Other code adds its own with <see cref="Register"/> on
    /// <see cref="Default"/>; the built-in ones come from <see cref="DevBuiltinCommands"/>.
    /// </summary>
    /// <remarks>
    /// Names are case-insensitive. Registering a name again replaces the earlier command. This
    /// class has no Unity dependencies, so it is covered by plain EditMode tests.
    /// </remarks>
    public sealed class DevCommandRegistry
    {
        /// <summary>The registry the console and the replay recorder use.</summary>
        public static readonly DevCommandRegistry Default = new DevCommandRegistry();

        private readonly Dictionary<string, DevCommand> _byName =
            new Dictionary<string, DevCommand>(StringComparer.OrdinalIgnoreCase);

        private readonly List<DevCommand> _commands = new List<DevCommand>();

        /// <summary>Raised after a line named a known command, with the line and what happened.</summary>
        public event Action<string, DevCommandResult> Executed;

        /// <summary>The commands, in the order they were first registered.</summary>
        public IReadOnlyList<DevCommand> Commands => _commands;

        /// <summary>Adds a command (and its aliases), replacing any command of the same name.</summary>
        public DevCommand Register(string name, string usage, string help, DevCommandHandler handler, params string[] aliases)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A command needs a name.", nameof(name));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            Unregister(name);
            var command = new DevCommand(name.Trim(), usage, help, handler, aliases);
            _commands.Add(command);
            _byName[command.Name] = command;
            foreach (string alias in command.Aliases)
            {
                if (!string.IsNullOrWhiteSpace(alias)) _byName[alias.Trim()] = command;
            }

            return command;
        }

        /// <summary>Removes a command by its name or an alias. False if there was none.</summary>
        public bool Unregister(string name)
        {
            if (name == null || !_byName.TryGetValue(name.Trim(), out DevCommand command)) return false;

            _commands.Remove(command);
            var stale = new List<string>();
            foreach (KeyValuePair<string, DevCommand> pair in _byName)
            {
                if (pair.Value == command) stale.Add(pair.Key);
            }

            foreach (string key in stale) _byName.Remove(key);
            return true;
        }

        public void Clear()
        {
            _byName.Clear();
            _commands.Clear();
        }

        public bool TryGet(string name, out DevCommand command)
        {
            command = null;
            return name != null && _byName.TryGetValue(name, out command);
        }

        /// <summary>Command names (not aliases) that start with <paramref name="prefix"/>, sorted.</summary>
        public List<string> Complete(string prefix)
        {
            var matches = new List<string>();
            prefix = prefix ?? string.Empty;
            foreach (DevCommand command in _commands)
            {
                if (command.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) matches.Add(command.Name);
            }

            matches.Sort(StringComparer.OrdinalIgnoreCase);
            return matches;
        }

        /// <summary>Parses and runs one line. Never throws.</summary>
        public DevCommandResult Execute(string line)
        {
            List<string> words = Tokenize(line);
            if (words.Count == 0) return new DevCommandResult(true, string.Empty);

            if (!TryGet(words[0], out DevCommand command))
            {
                return new DevCommandResult(false, "Unknown command '" + words[0] + "'. Type help.");
            }

            words.RemoveAt(0);
            DevCommandResult result;
            try
            {
                string text = command.Handler(new DevCommandArgs(words.ToArray(), line));
                result = new DevCommandResult(true, text ?? "ok");
            }
            catch (DevCommandException ex)
            {
                result = new DevCommandResult(false, ex.Message + "  Usage: " + command.Usage);
            }
            catch (Exception ex)
            {
                // A handler that touches the scene can fail in many ways; say so and keep going.
                result = new DevCommandResult(false, command.Name + " failed: " + ex.GetType().Name + ": " + ex.Message);
            }

            Executed?.Invoke(line, result);
            return result;
        }

        /// <summary>
        /// Splits a line into words on whitespace. Single or double quotes group words that
        /// contain spaces; a quote that is never closed runs to the end of the line.
        /// </summary>
        public static List<string> Tokenize(string line)
        {
            var words = new List<string>();
            if (string.IsNullOrEmpty(line)) return words;

            var current = new StringBuilder();
            char quote = '\0';
            bool inWord = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quote != '\0')
                {
                    if (c == quote) quote = '\0';
                    else current.Append(c);
                }
                else if (c == '"' || c == '\'')
                {
                    quote = c;
                    inWord = true;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (inWord)
                    {
                        words.Add(current.ToString());
                        current.Length = 0;
                        inWord = false;
                    }
                }
                else
                {
                    current.Append(c);
                    inWord = true;
                }
            }

            if (inWord) words.Add(current.ToString());
            return words;
        }
    }
}
#endif
