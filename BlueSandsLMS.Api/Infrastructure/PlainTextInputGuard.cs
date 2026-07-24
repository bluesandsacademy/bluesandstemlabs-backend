namespace BlueSandsLMS.Api.Infrastructure
{
    public interface IPlainTextInputGuard
    {
        bool IsSafe(string text);
    }

    public sealed class PlainTextInputGuard : IPlainTextInputGuard
    {
        public bool IsSafe(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (text.IndexOf('<') >= 0 || text.IndexOf('>') >= 0)
                return false;

            foreach (var c in text)
            {
                if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t')
                    return false;
            }

            return true;
        }
    }
}
