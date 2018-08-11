using System;
using System.Linq;
using System.Reflection;
using Moq;

namespace MoqFixture
{
    public class MoqExtensions
    {
        public static Mock CreateMock(Type argType, DefaultValue defaultValue = DefaultValue.Mock)
        {
            var argConstructor = argType
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance)
                .Concat(argType.GetConstructors(BindingFlags.Public | BindingFlags.CreateInstance |
                                                BindingFlags.Instance))
                .OrderBy(x => x.GetParameters().Length).FirstOrDefault();
            var argCount = argConstructor?.GetParameters().Length ?? 0;

            var mockType = typeof(Mock<>).MakeGenericType(argType);
            var mockConstructor = mockType.GetConstructors().Single(x =>
            {
                var t = x.GetParameters().FirstOrDefault()?.ParameterType;
                return t != null && t.IsArray && t.GetElementType() == typeof(object);
            });

            var nullParams = new object[argCount];
            var mock = (Mock) mockConstructor.Invoke(new object[] {nullParams});
            mock.DefaultValue = defaultValue;
            return mock;
        }
    }
}