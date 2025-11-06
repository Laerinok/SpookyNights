// In source/ClientConfig.cs

namespace SpookyNights
{
    public class ClientConfig
    {
        public string Version { get; set; } = "1.1.0";
        public bool EnableJackOLanternParticles { get; set; } = true;

        public ClientConfig() { }
    }
}