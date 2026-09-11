using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq.AutoMock;

namespace Jellyfin.Plugin.ListenBrainz.Tests.TestKit;

/// <summary>
/// Base class for tests of a service.
/// </summary>
/// <typeparam name="TService">Tested service.</typeparam>
public abstract class ServiceTest<TService>
    where TService : class
{
    private readonly AutoMocker _mocker;
    private readonly Lazy<TService> _subject;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceTest{TService}"/> class.
    /// </summary>
    protected ServiceTest()
    {
        _mocker = new AutoMocker();
        _mocker.Use<ILogger>(NullLogger.Instance);
        _mocker.Use<ILoggerFactory>(NullLoggerFactory.Instance);
        _subject = new Lazy<TService>(() => _mocker.CreateInstance<TService>());
    }

    /// <summary>
    /// Gets the service being tested.
    /// </summary>
    protected TService TestedService => _subject.Value;

    /// <summary>
    /// Gets the mock for a dependency.
    /// </summary>
    /// <typeparam name="TDependency">The dependency type.</typeparam>
    /// <returns>The mock for the given dependency.</returns>
    protected Mock<TDependency> MockOf<TDependency>()
        where TDependency : class => _mocker.GetMock<TDependency>();
}
