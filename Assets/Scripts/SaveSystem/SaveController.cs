using SaveSystem.Serialization;

namespace SaveSystem
{
    public class SaveController : MonoSingleton<SaveController>
    {
        private const string SaveName = "save";
        public bool LoadingDone { get; private set; }
    

        private void Awake()
        {
            LoadSave();
            DontDestroyOnLoad(gameObject);
        }

        private async void LoadSave()
        {
            LoadingDone = false;
            var save = await SerializationManager.Load(SaveName);
           
            if (save != null)
            {
                SaveData.SetLoadedData((SaveData) save);
            }
            LoadingDone = true;
        }

        public void Save()
        {
            SerializationManager.Save(SaveName, SaveData.Instance);
        }
    }
}