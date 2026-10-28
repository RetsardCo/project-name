namespace ProjectName.Shared.Session
{
    /// <summary>
    /// Supported permission scopes for an active remote session.
    /// In accordance with Milestone 2 and security policies:
    /// - view_only: Screen viewing only; all incoming remote input injection is strictly rejected.
    /// - view_and_control: Screen viewing with interactive mouse/keyboard/gesture control.
    /// </summary>
    public static class SessionScopes
    {
        public const string ViewOnly = "view_only";
        public const string ViewAndControl = "view_and_control";

        public static bool IsValid(string? scope) =>
            scope == ViewOnly || scope == ViewAndControl;
    }
}
