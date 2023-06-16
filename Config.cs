
using System.Collections.Generic;

public class Config
{
    public List<string> IgnoredNoGitDirs { get; set; } = new();
    public List<string> IgnoredNoGitFiles { get; set; } = new();
}