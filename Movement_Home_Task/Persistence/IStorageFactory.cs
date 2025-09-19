namespace Movement_Home_Task.Persistence
{
    public interface IStorageFactory
    {
        IDataStorage CreateStorage(StorageType type);
    }
}
