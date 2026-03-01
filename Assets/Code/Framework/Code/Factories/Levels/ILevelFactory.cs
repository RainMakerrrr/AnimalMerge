namespace Framework.Code.Factories.Levels
{
    public interface ILevelFactory
    {
        void Load();
        Level Create();
        Level CurrentLevel { get; }
    }
}