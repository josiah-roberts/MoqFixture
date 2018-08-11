using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;

namespace MoqFixture
{
    /// <summary>
    /// Represents a set of tests for a class. Inherit from this class.
    /// See Common.Utility.Tests/readme.md for further documentation.
    /// </summary>
    /// <typeparam name="T">The type under test</typeparam>
    public class MoqFixture<T> where T : class
    {
        private readonly Dictionary<string, Mock> _mocks;
        private readonly Type _testObjectType = typeof(T);
        private readonly ConstructorInfo _testObjectConstructor;

        /// <summary>
        /// Initializes the fixture. We expect this to be called by the test runner.
        /// </summary>
        public MoqFixture()
        {
            var constructors = _testObjectType.GetConstructors();
            if (constructors.Length != 1)
            {
                throw new InvalidOperationException($"Expected the test object {_testObjectType.Name} to have exactly one constructor, but it has {constructors.Length}.");
            }

            _testObjectConstructor = constructors.Single();

            _mocks = new Dictionary<string, Mock>();

            InitMocks();
        }

        /// <summary>
        /// The object under test
        /// </summary>
        protected T TestObject => _testObject ?? (_testObject = InitTestObject());
        private T _testObject;

        /// <summary>
        /// Get the mock of a particular type that was injected into TestObject
        /// </summary>
        /// <typeparam name="TMock">The type of mock</typeparam>
        /// <returns>The requested mock</returns>
        public Mock<TMock> Mock<TMock>() where TMock : class
        {
            if (_mocks.TryGetValue(typeof(TMock).FullName, out var mock))
            {
                return (Mock<TMock>)mock;
            }

            throw new InvalidOperationException($"No mock of type {typeof(TMock).Name} is available, because test object's constructor did not request a dependency of type {typeof(TMock).Name}.");
        }

        private void InitMocks()
        {
            var mockTypes = _testObjectConstructor.GetParameters().Select(x => x.ParameterType);
            foreach (var argType in mockTypes)
            {
                if (_mocks.ContainsKey(argType.FullName))
                {
                    throw new InvalidOperationException($"Constructor for {_testObjectType.Name} has duplicate dependency {argType.Name}.");
                }

                try
                {
                    var mock = MoqExtensions.CreateMock(argType, DefaultValue.Empty);
                    _mocks.Add(argType.FullName, mock);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Encountered exception trying to initialize mock for type {argType.Name}", e);
                }
            }
        }

        private T InitTestObject()
        {
            var paramaters = _mocks.Values.Select(x => x.Object).ToArray();
            try
            {
                return (T)_testObjectConstructor.Invoke(paramaters);
            }
            catch (TargetInvocationException e)
            {
                throw new Exception($"Construction of TestObject of type {_testObjectType.Name} threw an exception.", e.InnerException);
            }
        }
    }
}
