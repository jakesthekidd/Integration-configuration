using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http.Json;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Services.CustomerService;

namespace Transflo.Platform.Transformer.Core.Tests.Services
{
    public class CustomerServiceTests
    {
        private readonly Mock<ILogger<CustomerService>> _loggerMock = new();

        private CustomerService CreateService(MockHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://test.com/")
            };

            return new CustomerService(httpClient, _loggerMock.Object);
        }

        [Fact]
        public async Task GetCustomersAsync_ShouldReturnAllCustomers_WhenActiveOnlyIsNull()
        {
            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Get, "http://test.com/customers")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new List<CustomerResponse>
                   {
                       new() { CustomerId = "1", Enabled = true, IsDeleted = false },
                       new() { CustomerId = "2", Enabled = false, IsDeleted = false }
                   }));

            var service = CreateService(handler);
            var result = await service.GetCustomersAsync(null);

            result.Success.Should().BeTrue();
            result.Data!.Customers.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetCustomersAsync_ShouldReturnOnlyEnabled_WhenActiveOnlyTrue()
        {
            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Get, "http://test.com/customers")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new List<CustomerResponse>
                   {
                       new() { CustomerId = "1", Enabled = true },
                       new() { CustomerId = "2", Enabled = false }
                   }));

            var service = CreateService(handler);
            var result = await service.GetCustomersAsync(true);

            result.Success.Should().BeTrue();
            result.Data!.Customers.Should().ContainSingle(c => c.Enabled);
        }

        [Fact]
        public async Task GetCustomersAsync_ShouldReturnOnlyDisabled_WhenActiveOnlyFalse()
        {
            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Get, "http://test.com/customers")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new List<CustomerResponse>
                   {
                       new() { CustomerId = "1", Enabled = true },
                       new() { CustomerId = "2", Enabled = false }
                   }));

            var service = CreateService(handler);
            var result = await service.GetCustomersAsync(false);

            result.Success.Should().BeTrue();
            result.Data!.Customers.Should().ContainSingle(c => !c.Enabled);
        }

        [Fact]
        public async Task GetCustomersAsync_ShouldFilterDeletedCustomers()
        {
            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Get, "http://test.com/customers")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new List<CustomerResponse>
                   {
                       new() { CustomerId = "1", IsDeleted = false },
                       new() { CustomerId = "2", IsDeleted = true }
                   }));

            var service = CreateService(handler);
            var result = await service.GetCustomersAsync(null);

            result.Success.Should().BeTrue();
            result.Data!.Customers.Should().ContainSingle(c => c.CustomerId == "1");
        }

        [Fact]
        public async Task GetCustomersAsync_ShouldReturnEmptyList_WhenApiReturnsNull()
        {
            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Get, "http://test.com/customers")
                   .Respond(HttpStatusCode.OK, "application/json", "null");

            var service = CreateService(handler);
            var result = await service.GetCustomersAsync(null);

            result.Success.Should().BeTrue();
            result.Data!.Customers.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetCustomersAsync_ShouldReturnError_WhenApiFails()
        {
            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Get, "http://test.com/customers")
                   .Respond(HttpStatusCode.InternalServerError);

            var service = CreateService(handler);
            var result = await service.GetCustomersAsync(null);

            result.Success.Should().BeFalse();
            result.Data.Should().BeNull();
            result.Message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetCustomerByIdAsync_ShouldReturnCustomer_WhenFound()
        {
            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Get, "http://test.com/customers/123")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new CustomerResponse
                   {
                       CustomerId = "123",
                       Enabled = true
                   }));

            var service = CreateService(handler);
            var result = await service.GetCustomerByIdAsync("123");

            result.Success.Should().BeTrue();
            result.Data!.CustomerId.Should().Be("123");
            result.Data.Enabled.Should().BeTrue();
        }

        [Fact]
        public async Task GetCustomerByIdAsync_ShouldReturnError_WhenApiReturnsNull()
        {
            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Get, "http://test.com/customers/123")
                   .Respond(HttpStatusCode.OK, "application/json", "null");

            var service = CreateService(handler);
            var result = await service.GetCustomerByIdAsync("123");

            result.Success.Should().BeFalse();
            result.Data.Should().BeNull();
            result.Message.Should().Be("Customer not found");
        }

        [Fact]
        public async Task CreateCustomerAsync_ShouldReturnCreatedCustomer()
        {
            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Post, "http://test.com/customers")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new CustomerResponse
                   {
                       CustomerId = "10"
                   }));

            var service = CreateService(handler);
            var result = await service.CreateCustomerAsync(new CustomerRequest());

            result.Success.Should().BeTrue();
            result.Data!.CustomerId.Should().Be("10");
        }

        [Fact]
        public async Task CreateCustomerAsync_ShouldReturnDefaultCustomer_WhenApiReturnsNull()
        {
            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Post, "http://test.com/customers")
                   .Respond(HttpStatusCode.OK, "application/json", "null");

            var service = CreateService(handler);
            var result = await service.CreateCustomerAsync(new CustomerRequest());

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.CustomerId.Should().BeNull();
        }

        [Fact]
        public async Task UpdateCustomerAsync_ShouldReturnUpdatedCustomer()
        {
            var handler = new MockHttpMessageHandler();

            handler.When(HttpMethod.Get, "http://test.com/customers/5")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new CustomerResponse
                   {
                       CustomerId = "5",
                       CustomerName = "Old Name"
                   }));

            handler.When(HttpMethod.Put, "http://test.com/customers/5")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new CustomerResponse
                   {
                       CustomerId = "5",
                       CustomerName = "Updated"
                   }));

            var service = CreateService(handler);

            var request = new CustomerRequest
            {
                CustomerId = "5",
                CustomerName = "Updated"
            };

            var result = await service.UpdateCustomerAsync("5", request);

            result.Success.Should().BeTrue();
            result.Data!.CustomerName.Should().Be("Updated");
        }

        [Fact]
        public async Task UpdateCustomerAsync_ShouldReturnDefaultCustomer_WhenApiReturnsNull()
        {
            var handler = new MockHttpMessageHandler();

            handler.When(HttpMethod.Get, "http://test.com/customers/5")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new CustomerResponse
                   {
                       CustomerId = "5",
                       CustomerName = "Old Name"
                   }));

            handler.When(HttpMethod.Put, "http://test.com/customers/5")
                   .Respond(HttpStatusCode.OK, "application/json", "null");

            var service = CreateService(handler);

            var request = new CustomerRequest
            {
                CustomerId = "5"
            };

            var result = await service.UpdateCustomerAsync("5", request);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.CustomerId.Should().BeNull();
        }



        [Fact]
        public async Task SoftDeleteCustomerAsync_ShouldReturnTrue_WhenSuccessful()
        {
            var handler = new MockHttpMessageHandler();

            handler.When(HttpMethod.Get, "http://test.com/customers/7")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new CustomerResponse
                   {
                       CustomerId = "7",
                       IsDeleted = false
                   }));

            handler.When(HttpMethod.Put, "http://test.com/customers/7")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new CustomerResponse
                   {
                       CustomerId = "7",
                       IsDeleted = true
                   }));

            var service = CreateService(handler);
            var result = await service.SoftDeleteCustomerAsync("7");

            result.Success.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Fact]
        public async Task SoftDeleteCustomerAsync_ShouldReturnError_WhenCustomerNotFound()
        {
            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Get, "http://test.com/customers/7")
                   .Respond(HttpStatusCode.OK, "application/json", "null");

            var service = CreateService(handler);
            var result = await service.SoftDeleteCustomerAsync("7");

            result.Success.Should().BeFalse();
            result.Data.Should().BeFalse();
            result.Message.Should().Be("Customer not found");
        }

        [Fact]
        public async Task SetCustomerStatusAsync_ShouldReturnError_WhenCustomerNotFound()
        {
            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Get, "http://test.com/customers/9")
                   .Respond(HttpStatusCode.OK, "application/json", "null");

            var service = CreateService(handler);
            var result = await service.SetCustomerStatusAsync("9", true);

            result.Success.Should().BeFalse();
            result.Data.Should().BeNull();
            result.Message.Should().Be("Customer not found");
        }

        [Fact]
        public async Task SetCustomerStatusAsync_ShouldUpdateStatus()
        {
            var handler = new MockHttpMessageHandler();

            handler.When(HttpMethod.Get, "http://test.com/customers/9")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new CustomerResponse
                   {
                       CustomerId = "9",
                       Enabled = false
                   }));

            handler.When(HttpMethod.Put, "http://test.com/customers/9")
                   .Respond(HttpStatusCode.OK, JsonContent.Create(new CustomerResponse
                   {
                       CustomerId = "9",
                       Enabled = true
                   }));

            var service = CreateService(handler);
            var result = await service.SetCustomerStatusAsync("9", true);

            result.Success.Should().BeTrue();
            result.Data!.Enabled.Should().BeTrue();
        }
    }
}