using Zenject;

namespace InGame
{
    public class UtilsInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<UIHelper>().AsSingle();
        }
    }
}