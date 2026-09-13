using Robust.Server;
using System.IO;
using System.Linq;

namespace Content.Server
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            // Networking starts before content Init. Supply the safe preset before engine startup;
            // an explicit later --config-file argument still takes precedence.
            var preset = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../Resources/ConfigPresets/server_config.toml"));
            if (!File.Exists(preset) && !args.Contains("--config-file"))
                throw new FileNotFoundException("Local development configuration is missing. Run a packaged server or supply --config-file.", preset);
            ContentStart.Start(new[] { "--config-file", preset }.Concat(args).ToArray());
        }
    }
}
