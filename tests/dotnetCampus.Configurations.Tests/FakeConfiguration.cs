namespace dotnetCampus.Configurations.Tests
{
    public class FakeConfiguration : Configuration
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
