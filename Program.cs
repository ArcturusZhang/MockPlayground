using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Mocking;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Compute.Skus.Mocking;
using Azure.ResourceManager.Resources;
using Moq;

namespace MockPlayground
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Setting up mocks...");

            // 1. Create a mock for the resource that the extension method extends (SubscriptionResource)
            var subscriptionMock = new Mock<SubscriptionResource>();

            // 2. Create a mock for the "Mockable" resource that contains the implementation
            var mockableSubscription = new Mock<MockableComputeSkusSubscriptionResource>();

            // 3. Create the list of SKUs to return
            // Use ArmComputeModelFactory
            var sku1 = ArmComputeModelFactory.ComputeResourceSku(name: "Standard_D2s_v3", tier: "Standard", size: "D2s_v3", family: "D", kind: "VirtualMachines");
            var sku2 = ArmComputeModelFactory.ComputeResourceSku(name: "Standard_D4s_v3", tier: "Standard", size: "D4s_v3", family: "D", kind: "VirtualMachines");
            
            var skus = new[] { sku1, sku2 };

            // 4. Create a Page<T> and then AsyncPageable<T>
            var page = Page<ComputeResourceSku>.FromValues(skus, null, Mock.Of<Response>());
            var asyncPageable = AsyncPageable<ComputeResourceSku>.FromPages(new[] { page });

            // 5. Setup the method on the mockable resource
            // The method name found via inspection is GetComputeResourceSkusAsync
            mockableSubscription.Setup(x => x.GetComputeResourceSkusAsync(
                It.IsAny<string>(), // filter
                It.IsAny<string>(), // includeExtendedLocations
                It.IsAny<CancellationToken>()))
                .Returns(asyncPageable);

            // 6. Hook up the GetCachedClient method on the subscription mock
            subscriptionMock.Setup(x => x.GetCachedClient(It.IsAny<Func<ArmClient, MockableComputeSkusSubscriptionResource>>()))
                .Returns(mockableSubscription.Object);

            // 7. Use the mock
            SubscriptionResource subscription = subscriptionMock.Object;

            Console.WriteLine("Calling GetComputeResourceSkusAsync...");
            // The extension method should match the mockable method name usually
            var result = subscription.GetComputeResourceSkusAsync();

            await foreach (var sku in result)
            {
                Console.WriteLine($"SKU: {sku.Name}, Tier: {sku.Tier}, Size: {sku.Size}");
            }
        }
    }
}
