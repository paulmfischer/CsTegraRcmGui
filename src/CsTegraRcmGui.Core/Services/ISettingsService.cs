using CsTegraRcmGui.Core.Models;

namespace CsTegraRcmGui.Core.Services;

public interface ISettingsService
{
    AppSettings Current { get; }

    void Save();
}
