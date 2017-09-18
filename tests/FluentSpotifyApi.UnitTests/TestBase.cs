﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentSpotifyApi.Client;
using FluentSpotifyApi.Core.Internal;
using FluentSpotifyApi.Core.Internal.Extensions;
using FluentSpotifyApi.Core.Model;
using FluentSpotifyApi.UnitTesting.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FluentSpotifyApi.UnitTests
{
    [TestClass]
    [SuppressMessage("Microsoft.StyleCop.CSharp.SpacingRules", "SA1000:KeywordsMustBeSpacedCorrectly", Justification = "C# 7 Tuples")]
    public class TestBase
    {
        protected const string UserId = "TestUser";

        private IServiceProvider serviceProvider;

        protected Mock<ISpotifyHttpClient> SpotifyHttpClientMock { get; private set; }

        protected IFluentSpotifyClient Client { get; private set; }

        [TestInitialize]
        public virtual void TestInitialize()
        {
            var services = new ServiceCollection();
            services.AddFluentSpotifyClientForUnitTesting();

            this.SpotifyHttpClientMock = new Mock<ISpotifyHttpClient>();
            services.Replace(ServiceDescriptor.Singleton(this.SpotifyHttpClientMock.Object));

            this.serviceProvider = services.BuildServiceProvider();

            this.Client = this.serviceProvider.GetRequiredService<IFluentSpotifyClient>();
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            if (this.serviceProvider != null)
            {
                (this.serviceProvider as IDisposable).Dispose();
            }
        }

        protected IList<MockResult<T>> MockGet<T>(Func<int, T> factory = null)
        {
            return this.MockSend<T>(HttpMethod.Get, factory);
        }

        protected IList<MockResult<object>> MockPost()
        {
            return this.MockSend<object>(HttpMethod.Post);
        }

        protected IList<MockResult<T>> MockPost<T>(Func<int, T> factory = null)
        {
            return this.MockSend<T>(HttpMethod.Post, factory);
        }

        protected IList<MockResult<object>> MockPut()
        {
            return this.MockSend<object>(HttpMethod.Put);
        }

        protected IList<MockResult<T>> MockPut<T>(Func<int, T> factory = null)
        {
            return this.MockSend<T>(HttpMethod.Put, factory);
        }

        protected IList<MockResult<object>> MockDelete()
        {
            return this.MockSend<object>(HttpMethod.Delete);
        }

        protected IList<MockResult<T>> MockDelete<T>(Func<int, T> factory = null)
        {
            return this.MockSend<T>(HttpMethod.Delete);
        }

        protected IList<MockResult<T>> MockSend<T>(HttpMethod httpMethod,  Func<int, T> factory = null)
        {
            int i = 0;
            var mockResults = new List<MockResult<T>>();

            this.SpotifyHttpClientMock
                .Setup(x => x.SendWithJsonBodyAsync<T, object>(
                    It.IsAny<Uri>(),
                    httpMethod,
                    It.IsAny<object>(),                   
                    It.IsAny<object>(),
                    It.IsAny<IEnumerable<KeyValuePair<string, string>>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object[]>()))
                .Returns((Func<Uri, HttpMethod, object, object, IEnumerable<KeyValuePair<string, string>>, CancellationToken, object[], Task<T>>)((uri, innerHttpMehod, queryParameters, requestBody, requestHeaders, cancellationToken, routeValues) =>
                {
                    var result = factory == null ? (T)Activator.CreateInstance(typeof(T)) : factory(i++);

                    mockResults.Add(new MockResult<T>()
                    {
                        QueryParameters = ProcessQueryStringParameters(SpotifyObjectHelpers.GetPropertyBag(queryParameters)).Select(item => (item.Key, item.Value)).ToList(),
                        RouteValues = ProcessRouteValues(routeValues),
                        RequestPayload = JObject.Parse(JsonConvert.SerializeObject(requestBody)),
                        Result = result
                    });

                    return Task.FromResult(result);
                }));

            this.SpotifyHttpClientMock
                .Setup(x => x.SendAsync<T>(
                    It.IsAny<Uri>(),
                    httpMethod,                    
                    It.IsAny<object>(),
                    It.IsAny<object>(),
                    It.IsAny<IEnumerable<KeyValuePair<string, string>>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object[]>()))
                .Returns((Func<Uri, HttpMethod, object, object, IEnumerable<KeyValuePair<string, string>>, CancellationToken, object[], Task<T>>)((uri, innerHttpMehod, queryParameters, requestBodyParameters, requestHeaders, cancellationToken, routeValues) =>
                {
                    var result = factory == null ? (T)Activator.CreateInstance(typeof(T)) : factory(i++);

                    mockResults.Add(new MockResult<T>()
                    {
                        QueryParameters = ProcessQueryStringParameters(SpotifyObjectHelpers.GetPropertyBag(queryParameters)).Select(item => (item.Key, item.Value)).ToList(),
                        RouteValues = ProcessRouteValues(routeValues),
                        BodyParameters = SpotifyObjectHelpers.GetPropertyBag(requestBodyParameters).Select(item => (item.Key, item.Value)).ToList(),
                        Result = result
                    });

                    return Task.FromResult<T>(result);
                }));

            return mockResults;
        }

        private static IList<object> ProcessRouteValues(IList<object> routeValues)
        {
            return routeValues.EmptyIfNull().Select(item => (item is ITransformer transformer) && transformer.SourceType == typeof(IUser) ? UserId : item).ToList();
        }

        private static IEnumerable<KeyValuePair<string, object>> ProcessQueryStringParameters(IEnumerable<KeyValuePair<string, object>> queryStringParameters)
        {
            return queryStringParameters.EmptyIfNull()
                .Select(item => (item.Value is ITransformer transformer) && transformer.SourceType == typeof(IUser) 
                ? 
                new KeyValuePair<string, object>(item.Key, UserId)
                : 
                item).ToList();
        }

        public class MockResult<T>
        {
            [SuppressMessage("Microsoft.StyleCop.CSharp.SpacingRules", "SA1009:ClosingParenthesisMustBeSpacedCorrectly", Justification = "C# 7 Tuples")]
            public IList<(string Key, object Value)> QueryParameters { get; set; }

            [SuppressMessage("Microsoft.StyleCop.CSharp.SpacingRules", "SA1009:ClosingParenthesisMustBeSpacedCorrectly", Justification = "C# 7 Tuples")]
            public IList<(string Key, object Value)> BodyParameters { get; set; }

            public IList<object> RouteValues { get; set; }

            public JObject RequestPayload { get; set; }

            public T Result { get; set; }
        }
    }
}
