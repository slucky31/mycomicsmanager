using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Web.Infrastructure;
using Web.Tests.Infrastructure.LogLevelSwitchesFixtures;
using Xunit;

namespace Web.Tests.Infrastructure
{
    public sealed class LogLevelSwitchesTests
    {
        private const string ServicesNamespace = "Web.Tests.Infrastructure.LogLevelSwitchesFixtures";
        private static readonly string s_ctorLogger = typeof(ServiceWithCtorLogger).FullName!;
        private static readonly string s_injectedLogger = typeof(ComponentWithInjectedLogger).FullName!;

        private static LogLevelSwitches CreateSwitches(IReadOnlyDictionary<string, LogEventLevel>? overrides = null) =>
            new(LogEventLevel.Information, overrides ?? new Dictionary<string, LogEventLevel>(), [s_ctorLogger, s_injectedLogger]);

        private static Logger CreateLogger(LogLevelSwitches switches) =>
            switches.ApplyTo(new LoggerConfiguration()).CreateLogger();

        private static bool IsEnabled(Logger logger, string sourceContext, LogEventLevel level) =>
            logger.ForContext(Constants.SourceContextPropertyName, sourceContext).IsEnabled(level);

        private static LoggerLevel Get(LogLevelSwitches switches, string name) =>
            switches.GetLoggers().Single(l => l.Name == name);

        [Fact]
        public void DiscoverLoggerNames_Should_FindCtorAndInjectedPropertyLoggers_AndIgnoreOtherTypes()
        {
            var names = LogLevelSwitches.DiscoverLoggerNames([typeof(LogLevelSwitchesTests).Assembly]).ToList();

            names.Should().Contain([s_ctorLogger, s_injectedLogger]);
            names.Should().NotContain(typeof(ServiceWithoutLogger).FullName!);
        }

        [Fact]
        public void GetLoggers_Should_ListRootFirst_AndParentNamespaces()
        {
            var names = CreateSwitches().GetLoggers().Select(l => l.Name).ToList();

            names[0].Should().Be(LogLevelSwitches.RootLoggerName);
            names.Should().Contain([ServicesNamespace, "Web.Tests.Infrastructure", "Web.Tests", "Web"]);
        }

        [Fact]
        public void Set_Should_EnableDebug_ForAClassLogger()
        {
            var switches = CreateSwitches();
            using var logger = CreateLogger(switches);
            IsEnabled(logger, s_ctorLogger, LogEventLevel.Debug).Should().BeFalse();

            switches.Set(s_ctorLogger, LogEventLevel.Debug).Should().BeTrue();

            IsEnabled(logger, s_ctorLogger, LogEventLevel.Debug).Should().BeTrue();
            IsEnabled(logger, s_injectedLogger, LogEventLevel.Debug).Should().BeFalse();
        }

        [Fact]
        public void Set_Should_ApplyToMicrosoftLoggers_WhenAppliedAfterTheConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Serilog:MinimumLevel:Default"] = "Information",
                    ["Serilog:MinimumLevel:Override:" + ServicesNamespace] = "Warning",
                })
                .Build();
            var switches = LogLevelSwitches.FromConfiguration(configuration, [typeof(LogLevelSwitchesTests).Assembly]);
            using var serilogLogger = switches.ApplyTo(new LoggerConfiguration().ReadFrom.Configuration(configuration)).CreateLogger();
            using var factory = new SerilogLoggerFactory(serilogLogger);
            var logger = factory.CreateLogger<ServiceWithCtorLogger>();
            logger.IsEnabled(LogLevel.Information).Should().BeFalse();

            switches.Set(s_ctorLogger, LogEventLevel.Debug);

            logger.IsEnabled(LogLevel.Debug).Should().BeTrue();
        }

        [Fact]
        public void Set_Should_ApplyToChildClasses_WhenSetOnAParentNamespace()
        {
            var switches = CreateSwitches();
            using var logger = CreateLogger(switches);

            switches.Set(ServicesNamespace, LogEventLevel.Debug);

            IsEnabled(logger, s_ctorLogger, LogEventLevel.Debug).Should().BeTrue();
            Get(switches, s_ctorLogger).Should().Be(new LoggerLevel(s_ctorLogger, null, LogEventLevel.Debug));
        }

        [Fact]
        public void Set_Should_KeepTheChildLevel_WhenTheChildHasItsOwnLevel()
        {
            var switches = CreateSwitches(new Dictionary<string, LogEventLevel> { [s_ctorLogger] = LogEventLevel.Error });

            switches.Set(ServicesNamespace, LogEventLevel.Debug);

            Get(switches, s_ctorLogger).EffectiveLevel.Should().Be(LogEventLevel.Error);
            Get(switches, s_injectedLogger).EffectiveLevel.Should().Be(LogEventLevel.Debug);
        }

        [Fact]
        public void Set_Should_InheritFromTheParent_WhenLevelIsNull()
        {
            var switches = CreateSwitches(new Dictionary<string, LogEventLevel> { [s_ctorLogger] = LogEventLevel.Error });
            switches.Set(ServicesNamespace, LogEventLevel.Warning);

            switches.Set(s_ctorLogger, null);

            Get(switches, s_ctorLogger).Should().Be(new LoggerLevel(s_ctorLogger, null, LogEventLevel.Warning));
        }

        [Fact]
        public void Set_Should_SilenceTheLogger_WhenLevelIsOff()
        {
            var switches = CreateSwitches();
            using var logger = CreateLogger(switches);

            switches.Set(s_ctorLogger, LogLevelSwitches.Off);

            IsEnabled(logger, s_ctorLogger, LogEventLevel.Fatal).Should().BeFalse();
        }

        [Fact]
        public void Set_Should_DriveLoggersWithoutSwitch_WhenSetOnRoot()
        {
            var switches = CreateSwitches();
            using var logger = CreateLogger(switches);

            switches.Set(LogLevelSwitches.RootLoggerName, LogEventLevel.Debug);

            IsEnabled(logger, "Some.Undeclared.Library", LogEventLevel.Debug).Should().BeTrue();
            IsEnabled(logger, s_ctorLogger, LogEventLevel.Debug).Should().BeTrue();
        }

        [Fact]
        public void Set_Should_RestoreTheInitialRootLevel_WhenRootLevelIsNull()
        {
            var switches = CreateSwitches();
            switches.Set(LogLevelSwitches.RootLoggerName, LogEventLevel.Error);

            switches.Set(LogLevelSwitches.RootLoggerName, null);

            Get(switches, LogLevelSwitches.RootLoggerName).ConfiguredLevel.Should().Be(LogEventLevel.Information);
        }

        [Fact]
        public void Set_Should_ReturnFalse_WhenTheLoggerIsUnknown()
        {
            var switches = CreateSwitches();

            switches.Set("Unknown.Logger", LogEventLevel.Debug).Should().BeFalse();
        }

        [Fact]
        public void ResetAll_Should_RestoreTheConfiguredLevels()
        {
            var switches = CreateSwitches(new Dictionary<string, LogEventLevel> { [s_ctorLogger] = LogEventLevel.Warning });
            switches.Set(s_ctorLogger, LogEventLevel.Debug);
            switches.Set(ServicesNamespace, LogEventLevel.Error);

            switches.ResetAll();

            Get(switches, s_ctorLogger).Should().Be(new LoggerLevel(s_ctorLogger, LogEventLevel.Warning, LogEventLevel.Warning));
            Get(switches, ServicesNamespace).Should().Be(new LoggerLevel(ServicesNamespace, null, LogEventLevel.Information));
        }

        [Fact]
        public void FromConfiguration_Should_UseTheSerilogMinimumLevels()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Serilog:MinimumLevel:Default"] = "Warning",
                    ["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore.Database.Command"] = "Error",
                    ["Serilog:MinimumLevel:Override:Custom.Library"] = "Debug",
                })
                .Build();

            var switches = LogLevelSwitches.FromConfiguration(configuration, [typeof(LogLevelSwitchesTests).Assembly]);

            Get(switches, LogLevelSwitches.RootLoggerName).EffectiveLevel.Should().Be(LogEventLevel.Warning);
            Get(switches, "Microsoft.EntityFrameworkCore.Database.Command").ConfiguredLevel.Should().Be(LogEventLevel.Error);
            Get(switches, "Microsoft.EntityFrameworkCore").Should().Be(new LoggerLevel("Microsoft.EntityFrameworkCore", null, LogEventLevel.Warning));
            Get(switches, "Custom.Library").EffectiveLevel.Should().Be(LogEventLevel.Debug);
            Get(switches, s_injectedLogger).EffectiveLevel.Should().Be(LogEventLevel.Warning);
        }
    }
}

namespace Web.Tests.Infrastructure.LogLevelSwitchesFixtures
{
#pragma warning disable CA1812 // Only inspected by reflection
    internal sealed class ServiceWithCtorLogger(ILogger<ServiceWithCtorLogger> logger)
    {
        public Microsoft.Extensions.Logging.ILogger Logger => logger;
    }

    internal sealed class ComponentWithInjectedLogger
    {
        private ILogger<ComponentWithInjectedLogger>? Logger { get; set; }

        public bool HasLogger => Logger is not null;
    }

    internal sealed class ServiceWithoutLogger(string name)
    {
        public string Name => name;
    }
#pragma warning restore CA1812
}
