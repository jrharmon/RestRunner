# Management Scripts

These scripts help manage building/deploying REST Runner.  To use them, you must create a Powershell script called
InitializeEnvironmentVariables.ps1 that sets the required sessions environment variables.  The required variables are:

* publishDir - The directory path to publish a ClickOnce installer to, as well as a portable version

## Scripts

* publish.ps1 - Run this with no parameters to have it publish to a ClickOnce location, as well as to a folder that contains a portable version.
  * This expects ApplicationRevision to already be set to the version that will be published.  After publishing, it will remind you to update it so that the next publish in the future will be correct.

Example:

    New-Item -Name publishDir -Path env: -Value "\\myserver\deployables\RestRunner\" | out-null
