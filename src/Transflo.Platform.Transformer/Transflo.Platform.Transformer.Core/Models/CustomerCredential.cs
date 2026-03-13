using Amazon.DynamoDBv2.DataModel;

namespace Transflo.Platform.Transformer.Core.Models
{
    public class CustomerCredential
    {
        [DynamoDBHashKey]
        public string CustomerId { get; set; }

        [DynamoDBProperty]
        public string CustomerSecret { get; set; }
    }
}
