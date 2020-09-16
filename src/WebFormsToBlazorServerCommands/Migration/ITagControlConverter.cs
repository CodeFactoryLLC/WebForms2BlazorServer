using System.Threading.Tasks;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Contract that any main Adapter must implement in order to be called by the parent (calling) migration code
    /// </summary>
    public interface ITagControlConverter
    {

        /// <summary>
        /// This method is used to send in a TagControl(eg. 'asp:ListView' etc) and its inclusive node content.
        /// </summary>
        /// <param name="tagControlName"></param>
        /// <param name="tagNodeContent"></param>
        /// <returns>The converted/migrated string content of the TagControl.  It is entirely possible that the return NodeText will have overridded the TagControlName into something else entirely.  ie.  a Blazor control</returns>
        Task<string> MigrateTagControl(string tagControlName, string tagNodeContent);

    }
}
