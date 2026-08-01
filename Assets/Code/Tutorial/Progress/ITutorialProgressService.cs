namespace Code.Tutorial.Progress
{
    public interface ITutorialProgressService
    {
        bool IsCompleted(string stepId);

        void MarkCompleted(string stepId);

        void Reset(string stepId);
    }
}
