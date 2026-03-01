using Framework.Code.Data;

namespace Framework.Code.Infrastructure.Services.PersistentProgress
{
    public interface IPersistentProgressService
    {
        PlayerProgress Progress { get; set; }
		
        PlayerData Data { get; set; }
    }
}