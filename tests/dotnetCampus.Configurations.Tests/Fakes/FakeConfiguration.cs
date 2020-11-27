namespace dotnetCampus.Configurations.Tests.Fakes
{
    internal class FakeConfiguration : Configuration
    {
        public FakeConfiguration() : base("")
        {
        }

        public string Key
        {
            get => GetString();
            set => SetValue(value);
        }

        public string A
        {
            get => GetString();
            set => SetValue(value);
        }

        public string B
        {
            get => GetString();
            set => SetValue(value);
        }
    }
}
