using GameScene.GamePlay;
using GameScene.Managers;
using GridSystem;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Injection
{
    public class GameSceneInstaller : MonoInstaller<GameSceneInstaller>
    {
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private GridVisualSettings settings;
        
        
        public override void InstallBindings()
        {
            //Signals
            SignalBusInstaller.Install(Container);
            Container.DeclareSignal<CreateSprites>();
            Container.DeclareSignal<OnLevelUnload>();
            Container.DeclareSignal<OnLevelLoad>();
            Container.DeclareSignal<OnGameplay>();
            Container.DeclareSignal<OnWin>();
            Container.DeclareSignal<OnLost>();
            Container.DeclareSignal<Restart>();
            Container.DeclareSignal<NextLevel>();
            Container.DeclareSignal<OnSwipe>();
            Container.DeclareSignal<OnNodePainted>();
            Container.DeclareSignal<OnLevelMaterialsLoaded>();
            Container.DeclareSignal<OnCameraExitComplete>();
            
            //Bindings
            Container.BindInstance(settings).AsSingle();
            Container.Bind<GridController>().AsSingle();
            Container.Bind<PlayerInput>().FromInstance(playerInput).AsSingle();
            Container.Bind<GameManager>().AsSingle();
            Container.BindInterfacesTo<CreateGroundSprite>().AsSingle();
            Container.BindInterfacesTo<PlayerDirectionManager>().AsSingle();
            Container.BindInterfacesTo<WinManager>().AsSingle();
            Container.BindInterfacesTo<HapticManager>().AsSingle();
        }
    }
}
