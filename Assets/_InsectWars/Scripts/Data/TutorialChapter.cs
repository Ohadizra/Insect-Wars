using System;

namespace InsectWars.Data
{
    public class TutorialChapter
    {
        public string Title { get; }
        public string IntroText { get; }
        public string ObjectiveText { get; }
        public Action SetupWorld { get; }
        public Func<bool> IsComplete { get; }

        public TutorialChapter(string title, string introText, string objectiveText,
            Action setupWorld, Func<bool> isComplete)
        {
            Title = title;
            IntroText = introText;
            ObjectiveText = objectiveText;
            SetupWorld = setupWorld;
            IsComplete = isComplete;
        }
    }
}
