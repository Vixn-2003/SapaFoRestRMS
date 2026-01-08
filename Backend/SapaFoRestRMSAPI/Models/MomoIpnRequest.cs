namespace SapaFoRestRMSAPI.Models
{
    public class MomoIpnRequest
    {
        public string partnerCode { get; set; } = null!;
        public string orderId { get; set; } = null!;
        public string requestId { get; set; } = null!;
        public string amount { get; set; } = null!;
        public string orderInfo { get; set; } = null!;
        public string orderType { get; set; } = null!;
        public string transId { get; set; } = null!;
        public int resultCode { get; set; }
        public string message { get; set; } = null!;
        public string payType { get; set; } = null!;
        public long responseTime { get; set; }
        public string extraData { get; set; } = null!;
        public string signature { get; set; } = null!;
    }
}
