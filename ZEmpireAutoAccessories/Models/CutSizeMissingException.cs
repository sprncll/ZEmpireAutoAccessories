namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// A film line names a panel that has no cut size on record, so there is
    /// no length to take off the roll.
    ///
    /// It is an InvalidOperationException so the controllers that already
    /// catch one - and turn it into a message on the document - keep working
    /// untouched. The type exists so those screens can also offer the way out:
    /// this is the one stock problem a user fixes on another screen rather
    /// than by changing the document in front of them.
    /// </summary>
    public class CutSizeMissingException : InvalidOperationException
    {
        public CutSizeMissingException(string message) : base(message) { }
    }
}
