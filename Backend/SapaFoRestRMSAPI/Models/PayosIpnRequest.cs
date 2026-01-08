namespace SapaFoRestRMSAPI.Models
{
    public class PayosIpnRequest
    {
        public string code { get; set; } = null!;
        public string desc { get; set; } = null!;
        public bool success { get; set; }
        public PayosIpnData data { get; set; } = null!;
        public string signature { get; set; } = null!;
    }

    public class PayosIpnData
    {
        public long orderCode { get; set; }
        public decimal amount { get; set; }
        public string description { get; set; } = null!;
        public string reference { get; set; } = null!;
        public string accountNumber { get; set; }
        public string transactionDateTime { get; set; }
        public string currency { get; set; }
        public string paymentLinkId { get; set; }
        public string code { get; set; }
        public string desc { get; set; }
    }
}
