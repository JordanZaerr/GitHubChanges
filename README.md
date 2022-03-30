# GitHubChanges

### Getting start
You can get the code in one of two ways.
* Clone/Download the repository and compile it yourself
* Download the binaries from the Releases section and extract the zip
  * The executable file is a self contained .net 6 WPF application with runtime included.

### Initial Setup
* You'll need to set the `GitHubPAT` setting in the `UserSettings.json` files.

* You can generate a PAT following [these instructions](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token).

  * The permissions you'll need for the PAT [are here](https://github.com/JordanZaerr/GitHubChanges/blob/aeb6c87d369f815741b65c54e1739a9ae98c5bba/docs/Scopes%20for%20PAT.png).

### Optional Setup
The tool will try to order the tags by their SemVer version.  This requires that we strip off any tag prefixes that have been applied.  If we can't parse the tag to a SemVer version then we display the tag verbatim and order is not guaranteed.  You can customize the tag prefixes in with the `TagPrefixes` setting in the `UserSettings.json` file.
