using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services.Interfaces
{
    /// <summary>
    /// The shop's own settings - its name and address, the VAT rate, the
    /// points at which stock counts as low. See SettingsStore for why they
    /// live in a file rather than a table.
    /// </summary>
    public interface ISettingsStore
    {
        /// <summary>What is set now. Never null; the defaults are the figures the code used to hold.</summary>
        ShopSettings Current { get; }

        /// <summary>Writes them, and points the rest of the app at them.</summary>
        Task Save(ShopSettings settings);
    }
}
