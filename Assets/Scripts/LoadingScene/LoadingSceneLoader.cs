using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace UI
{
    public class LoadingSceneLoader : IInitializable,IDisposable
    {
        [Inject] private SaveController _saveController;
        private CancellationTokenSource _source;
        
        
        public void Initialize()
        {
            _source = new CancellationTokenSource();
            LoadWaiting();
        }

        public void Dispose()
        {
            _source.Cancel();
        }

        private async void LoadWaiting()
        {
            await UniTask.WaitUntil(() => _saveController.LoadingDone, cancellationToken: _source.Token);
            await SceneManager.LoadSceneAsync("GameScene");
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("GameScene"));
        }
    }
}
