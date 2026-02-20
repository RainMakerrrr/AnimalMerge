using Code.Services.Physics;
using Code.Services.Random;
using Zenject;

namespace Code.Infrastructure.Installers
{
    /// <summary>
    /// Installer for core services (Physics, Random, etc.).
    /// These services provide testable abstractions over Unity APIs.
    /// </summary>
    public class ServicesInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindPhysicsService();
            BindRandomProvider();
        }

        private void BindPhysicsService()
        {
            Container.Bind<IPhysicsService>().To<UnityPhysicsService>().AsSingle();
        }

        private void BindRandomProvider()
        {
            Container.Bind<IRandomProvider>().To<UnityRandomProvider>().AsSingle();
        }
    }
}
