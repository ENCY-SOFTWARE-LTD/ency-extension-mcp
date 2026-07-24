using System.Text.RegularExpressions;

namespace EncyExtensionMcp;

/** Renames the "EncyExtension" template placeholder to the real extension name inside a fresh
 * clone — same job as the template's rename.ps1, but with no PowerShell dependency. */
public static class TemplateRenamer
{
    public const string Placeholder = "EncyExtension";

    public static bool IsValidName(string name) =>
        Regex.IsMatch(name, "^[A-Za-z][A-Za-z0-9.]*$") && !name.Contains("..");

    /** Replaces the placeholder in file contents and file names under repoDir/src.
     * Returns the touched file count. */
    public static int Rename(string repoDir, string newName)
    {
        if (!IsValidName(newName))
            throw new ArgumentException($"'{newName}' is not a valid extension name (letters, digits, dots; starts with a letter).");
        var src = Path.Combine(repoDir, "src");
        if (!Directory.Exists(src))
            throw new DirectoryNotFoundException($"no src/ folder under {repoDir} — is this a template clone?");

        int touched = 0;
        foreach (var file in Directory.GetFiles(src))
        {
            var text = File.ReadAllText(file);
            if (text.Contains(Placeholder))
            {
                File.WriteAllText(file, text.Replace(Placeholder, newName));
                touched++;
            }
            var name = Path.GetFileName(file);
            if (name.Contains(Placeholder))
                File.Move(file, Path.Combine(src, name.Replace(Placeholder, newName)));
        }
        return touched;
    }
}
