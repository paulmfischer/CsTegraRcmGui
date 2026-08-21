using CegraRcmGui.Core.Models;

namespace CegraRcmGui.Core.Services;

public interface ISettingsService
{
    AppSettings Current { get; }

    void Save();
}
