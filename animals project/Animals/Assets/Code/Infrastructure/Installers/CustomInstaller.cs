using Code.Infrastructure.Services.Input;
using Zenject;

namespace Code.Infrastructure.Installers
{
    public class CustomInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindInputService();
        }

        private void BindInputService() => Container.Bind<IInputService>().To<InputService>().AsSingle();
    }
}