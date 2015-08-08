using Guardian.Resources;

namespace Guardian
{
    // Provides access to string resources.
    public class Strings
    {
        private static AppResources _localizedResources = new AppResources();

        public AppResources Resources { get { return _localizedResources; } }
    }
}