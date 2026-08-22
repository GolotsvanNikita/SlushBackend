using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Slush.Infrastructure.External
{
    public record CheapSharkDeal(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("storeID")] string StoreID,
        [property: JsonPropertyName("salePrice")] string SalePrice,
        [property: JsonPropertyName("savings")] string Savings,
        [property: JsonPropertyName("thumb")] string Thumb,
        [property: JsonPropertyName("dealID")] string DealID
    );
}
