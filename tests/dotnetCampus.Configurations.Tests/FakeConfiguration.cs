namespace dotnetCampus.Configurations.Tests
{
    internal sealed class FakeConfiguration : Configuration
    {
        public FakeConfiguration() : base("")
        {
        }

        public string Key
        {
            get => GetString();
            set => SetValue(value);
        }
    }
}
