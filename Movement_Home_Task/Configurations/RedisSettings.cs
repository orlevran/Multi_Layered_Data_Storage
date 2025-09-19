namespace Movement_Home_Task.Configurations
{
    public class RedisSettings
    {
        public string Host { get; set; } = default!;
        public int Port { get; set; }
        public string User { get; set; } = default!;
        public string Password { get; set; } = default!;
        public bool Ssl { get; set; }
        public string? InstanceName { get; set; } = "myapp:";
        public int ConnectTimeout { get; set; } = 8000;
        public int SyncTimeout { get; set; } = 8000;
        public bool ResolveDns { get; set; } = true;
        public bool AbortOnConnectFail { get; set; } = false;
    }
}
