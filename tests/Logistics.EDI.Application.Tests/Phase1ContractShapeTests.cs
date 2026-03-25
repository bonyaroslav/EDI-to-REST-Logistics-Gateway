using Logistics.EDI.Application.Contracts;

namespace Logistics.EDI.Application.Tests;

public sealed class Phase1ContractShapeTests
{
    [Fact]
    public void LoadTenderResponse_ExposesLockedPhase1Properties()
    {
        LoadTenderResponse response = new(
            TransactionId: "0001",
            LoadNumber: "9999999",
            CarrierAlphaCode: "XXXX",
            SetPurpose: "Original",
            EstimatedDeliveryDate: "2025-01-16T00:00:00Z",
            ShipperName: "DIGIS LOGISTICS",
            Stops: Array.Empty<StopDto>(),
            Status: "Success");

        Assert.Equal("0001", response.TransactionId);
        Assert.Equal("9999999", response.LoadNumber);
        Assert.Equal("XXXX", response.CarrierAlphaCode);
        Assert.Equal("Original", response.SetPurpose);
        Assert.Equal("2025-01-16T00:00:00Z", response.EstimatedDeliveryDate);
        Assert.Equal("DIGIS LOGISTICS", response.ShipperName);
        Assert.Empty(response.Stops);
        Assert.Equal("Success", response.Status);
    }
}
