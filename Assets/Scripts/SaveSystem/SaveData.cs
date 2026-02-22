namespace SaveSystem
{
    [System.Serializable]
    public class SaveData
    {
        private static SaveData _instance;
        public static SaveData Instance
        {
            get { return _instance ??= new SaveData(); }
        }

        public static void SetLoadedData(SaveData saveData)
        {
            _instance = saveData;
        }
        public int Level = 1;
    }
}
