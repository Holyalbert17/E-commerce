namespace Datamex.ECommerce.Application.Options;

public class BancardOptions
{
    public const string SectionName = "Bancard";

    /// <summary>Off | Stub | Live</summary>
    public string Mode { get; set; } = "Stub";

    public string MultiBuyUrl { get; set; } =
        "https://vpos.infonet.com.py:8888/vpos/api/0.3/multi_buy";

    public string RollbackUrl { get; set; } =
        "https://vpos.infonet.com.py:8888/vpos/api/0.3/multi_buy/rollback";

    public string ConfirmationsUrl { get; set; } =
        "https://vpos.infonet.com.py:8888/vpos/api/0.3/multi_buy/confirmations";

    public string CheckoutScriptUrl { get; set; } =
        "https://vpos.infonet.com.py:8888/checkout/javascript/dist/bancard-checkout-2.0.0.js";

    public bool IsStub => string.Equals(Mode, "Stub", StringComparison.OrdinalIgnoreCase);
    public bool IsLive => string.Equals(Mode, "Live", StringComparison.OrdinalIgnoreCase);
    public bool IsOff => string.Equals(Mode, "Off", StringComparison.OrdinalIgnoreCase);
}
